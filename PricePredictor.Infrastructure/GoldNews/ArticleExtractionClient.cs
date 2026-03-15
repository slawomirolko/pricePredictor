using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OllamaSharp.Models;
using PricePredictor.Application;
using PricePredictor.Application.News;

namespace PricePredictor.Infrastructure.GoldNews;

public sealed class ArticleExtractionClient : IOllamaArticleExtractionClient
{
    private readonly IOllamaApiClient _localOllama;
    private readonly IOllamaCloudHttpClient _cloudOllama;
    private readonly GoldNewsSettings _settings;
    private readonly ILogger<ArticleExtractionClient> _logger;

    public ArticleExtractionClient(
        IOllamaApiClient localOllama,
        IOllamaCloudHttpClient cloudOllama,
        IOptions<GoldNewsSettings> settings,
        ILogger<ArticleExtractionClient> logger)
    {
        _localOllama = localOllama;
        _cloudOllama = cloudOllama;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string?> ExtractMainContentAsync(string systemPrompt, string htmlContent, string? articleTitle, CancellationToken cancellationToken)
    {
        var model = _settings.UseCloud ? _settings.CloudOllamaModel : _settings.LocalOllamaModel;
        var safeHtmlContent = htmlContent.Length > 10000000 ? htmlContent[..10000000] : htmlContent;
        var userPrompt = PromptHelper.ContentOfHtmlPrefix + safeHtmlContent;

        if (_settings.UseCloud)
        {
            return await _cloudOllama.ChatAsync(model, systemPrompt, userPrompt, cancellationToken);
        }

        _localOllama.SelectedModel = model;

        var request = new GenerateRequest
        {
            Model = model,
            System = systemPrompt,
            Prompt = userPrompt,
            Stream = false
        };

        var result = new StringBuilder();
        var streamedChunks = 0;
        var emptyChunks = 0;

        await foreach (var response in _localOllama.GenerateAsync(request, cancellationToken))
        {
            streamedChunks++;
            var chunk = ExtractChunkText(response);
            if (string.IsNullOrWhiteSpace(chunk))
            {
                emptyChunks++;
                continue;
            }

            result.Append(chunk);
        }

        if (result.Length == 0)
        {
            throw new InvalidOperationException(
                $"Ollama returned no usable content. Model={model}, Chunks={streamedChunks}, EmptyChunks={emptyChunks}, PromptLength={request.Prompt.Length}.");
        }

        return result.ToString().Trim();
    }

    public async Task<bool> AssessTradingUsefulnessAsync(string articleContent, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(articleContent))
        {
            throw new ArgumentException("Article content cannot be empty.", nameof(articleContent));
        }

        var model = _settings.CloudOllamaModel;
        var response = await _cloudOllama.ChatAsync(model, PromptHelper.TradingAssessmentSystemPrompt, articleContent, cancellationToken);

        if (string.IsNullOrWhiteSpace(response))
        {
            throw new InvalidOperationException($"Ollama Cloud returned an empty trading assessment response. Model={model}, ContentLength={articleContent.Length}.");
        }

        return ParseTradeUsefulness(response, model);
    }

    public async Task<string> SummarizeAsync(string articleContent, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(articleContent))
        {
            throw new ArgumentException("Article content cannot be empty.", nameof(articleContent));
        }

        var model = _settings.CloudOllamaModel;
        var response = await _cloudOllama.ChatAsync(model, PromptHelper.SummarizeSystemPrompt, articleContent, cancellationToken);

        if (string.IsNullOrWhiteSpace(response))
        {
            throw new InvalidOperationException($"Ollama Cloud returned an empty summary response. Model={model}, ContentLength={articleContent.Length}.");
        }

        var summary = response.Trim();
        return summary.Length > 500 ? summary[..500] : summary;
    }

    public async Task<IReadOnlyList<float>> EmbedAsync(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text to embed cannot be empty.", nameof(text));
        }

        var model = _settings.LocalOllamaModel;
        _localOllama.SelectedModel = model;

        var preparedText = await PrepareTextForEmbeddingAsync(model, text, cancellationToken);
        var normalizedText = PromptHelper.NormalizeForEmbedding(preparedText);

        var response = await _localOllama.EmbedAsync(normalizedText, cancellationToken);
        if (response?.Embeddings == null || response.Embeddings.Count == 0 || response.Embeddings[0].Length == 0)
        {
            throw new InvalidOperationException(
                $"Local Ollama returned empty embeddings. Model={model}, OriginalLength={text.Length}, NormalizedLength={normalizedText.Length}.");
        }

        return response.Embeddings[0];
    }

    private async Task<string> PrepareTextForEmbeddingAsync(string model, string text, CancellationToken cancellationToken)
    {
        var request = new GenerateRequest
        {
            Model = model,
            System = PromptHelper.EmbeddingPreparationPrompt,
            Prompt = text,
            Stream = false
        };

        var result = new StringBuilder();
        var streamedChunks = 0;

        await foreach (var response in _localOllama.GenerateAsync(request, cancellationToken))
        {
            streamedChunks++;
            var chunk = ExtractChunkText(response);
            if (!string.IsNullOrWhiteSpace(chunk))
            {
                result.Append(chunk);
            }
        }

        if (result.Length == 0)
        {
            throw new InvalidOperationException(
                $"Local Ollama returned empty prepared text for embedding. Model={model}, Chunks={streamedChunks}, InputLength={text.Length}.");
        }

        return result.ToString();
    }

    private static string? ExtractChunkText(object? response)
    {
        if (response == null)
        {
            return null;
        }

        var responseType = response.GetType();

        var directResponse = responseType.GetProperty("Response")?.GetValue(response) as string;
        if (!string.IsNullOrWhiteSpace(directResponse))
        {
            return directResponse;
        }

        var message = responseType.GetProperty("Message")?.GetValue(response);
        if (message != null)
        {
            var content = message.GetType().GetProperty("Content")?.GetValue(message) as string;
            if (!string.IsNullOrWhiteSpace(content))
            {
                return content;
            }
        }

        var directContent = responseType.GetProperty("Content")?.GetValue(response) as string;
        return string.IsNullOrWhiteSpace(directContent) ? null : directContent;
    }

    private static bool ParseTradeUsefulness(string response, string model)
    {
        try
        {
            using var json = JsonDocument.Parse(response);
            if (!json.RootElement.TryGetProperty("isTradeUseful", out var value))
            {
                throw new InvalidOperationException($"Ollama Cloud response is missing isTradeUseful field. Model={model}, Response={response}");
            }

            return value.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => throw new InvalidOperationException($"Ollama Cloud returned invalid isTradeUseful type. Model={model}, Response={response}")
            };
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Ollama Cloud returned non-JSON trading assessment. Model={model}, Response={response}", ex);
        }
    }
}
