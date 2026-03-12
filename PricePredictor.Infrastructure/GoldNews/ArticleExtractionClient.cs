using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OllamaSharp.Models;
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

        _logger.LogInformation("Extracting article content using Ollama model {Model} (UseCloud={UseCloud})", 
            model, _settings.UseCloud);
        
        // Truncate HTML content to avoid context window issues
        var maxHtmlLength = 10000000;
        var safeHtmlContent = htmlContent.Length > maxHtmlLength 
            ? htmlContent[..maxHtmlLength] 
            : htmlContent;

        var userPrompt = "Content of HTML is: " + safeHtmlContent;
        _logger.LogDebug("Prompt to Ollama: {Prompt}", userPrompt);

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

        _logger.LogInformation("🚀 Calling Local Ollama {Url} for model {Model} with {PromptLength} chars", _localOllama.Uri, model, request.Prompt.Length);

        await foreach (var response in _localOllama.GenerateAsync(request, cancellationToken))
        {
            streamedChunks++;

            _logger.LogDebug("Chunk received: {Chunk}", response);

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
                $"Ollama returned no usable content. Model={model}, Chunks={streamedChunks}, EmptyChunks={emptyChunks}, PromptLength={request.Prompt.Length}. " +
                "This indicates either the model is not responding correctly or the prompt was invalid.");
        }

        return result.ToString().Trim();
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
        if (!string.IsNullOrWhiteSpace(directContent))
        {
            return directContent;
        }

        return null;
    }
}
