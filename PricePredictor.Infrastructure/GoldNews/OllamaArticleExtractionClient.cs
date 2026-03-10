using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OllamaSharp;
using PricePredictor.Application.News;

namespace PricePredictor.Infrastructure.GoldNews;

public sealed class OllamaArticleExtractionClient : IOllamaArticleExtractionClient
{
    private readonly IOllamaApiClient _ollama;
    private readonly GoldNewsSettings _settings;
    private readonly ILogger<OllamaArticleExtractionClient> _logger;

    public OllamaArticleExtractionClient(
        IOllamaApiClient ollama,
        IOptions<GoldNewsSettings> settings,
        ILogger<OllamaArticleExtractionClient> logger)
    {
        _ollama = ollama;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string?> ExtractMainContentAsync(string systemPrompt, string htmlContent, string? articleTitle, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Extracting article content using Ollama model {Model}", _settings.OllamaModel);

            _ollama.SelectedModel = _settings.OllamaModel;

            var titlePrefix = string.IsNullOrWhiteSpace(articleTitle)
                ? ""
                : $"ARTICLE_TITLE: {articleTitle}\n\n";

            var request = new OllamaGenerateRequestBuilder()
                .WithModel(_settings.OllamaModel)
                .WithSystemPrompt(systemPrompt)
                .WithUserHtmlContent(titlePrefix + htmlContent)
                .WithPromptLimit(15000)
                .Build();

            var result = new StringBuilder();
            var streamedChunks = 0;
            var emptyChunks = 0;

            await foreach (var response in _ollama.GenerateAsync(request, cancellationToken))
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
                _logger.LogWarning(
                    "Ollama returned no usable content. Model={Model}, Chunks={Chunks}, EmptyChunks={EmptyChunks}, PromptLength={PromptLength}",
                    _settings.OllamaModel,
                    streamedChunks,
                    emptyChunks,
                    request.Prompt?.Length ?? 0);
                return null;
            }

            return result.ToString().Trim();
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning(
                "Ollama returned 404 for model {Model}. Verify model availability in Ollama.",
                _settings.OllamaModel);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama extraction failed: {Message}", ex.Message);
            return null;
        }
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
}
