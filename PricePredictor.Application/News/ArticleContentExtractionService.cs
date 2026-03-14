using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace PricePredictor.Application.News;

public sealed class ArticleContentExtractionService : IArticleContentExtractionService
{
    private const int MinExtractedLength = 100;
    private const int MaxExtractedLength = 3000;

    private readonly IOllamaArticleExtractionClient _extractionClient;
    private readonly ILogger<ArticleContentExtractionService> _logger;

    public ArticleContentExtractionService(
        IOllamaArticleExtractionClient extractionClient,
        ILogger<ArticleContentExtractionService> logger)
    {
        _extractionClient = extractionClient;
        _logger = logger;
    }

    public async Task<string?> ExtractAsync(string html, string? fallbackText, string? articleTitle, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return NormalizeFallback(fallbackText);
        }

        var paragraphText = ExtractParagraphs(html);

        // Only call Ollama with clean paragraph text — never with raw HTML
        var payload = !string.IsNullOrWhiteSpace(paragraphText)
            ? paragraphText
            : NormalizeFallback(fallbackText);

        if (string.IsNullOrWhiteSpace(payload))
        {
            _logger.LogWarning("No paragraph divs found and no fallback body text available. Skipping LLM call.");
            return null;
        }

        _logger.LogInformation("Sending {Length} chars to Ollama (source: {Source})",
            payload.Length,
            !string.IsNullOrWhiteSpace(paragraphText) ? "paragraph divs" : "Selenium body text");

        var llmResult = await _extractionClient.ExtractMainContentAsync(
            PromptHelper.BuildArticleExtractionSystemPrompt(payload),
            payload,
            articleTitle,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(llmResult) && llmResult.Length >= MinExtractedLength)
        {
            return llmResult.Trim();
        }

        _logger.LogWarning("LLM result was empty or too short, falling back to Selenium body text.");
        return NormalizeFallback(fallbackText);
    }

    private static string? NormalizeFallback(string? fallbackText)
    {
        if (string.IsNullOrWhiteSpace(fallbackText) || fallbackText.Length <= MinExtractedLength)
        {
            return null;
        }

        return fallbackText.Length > MaxExtractedLength
            ? fallbackText[..MaxExtractedLength]
            : fallbackText;
    }

    private static string? ExtractParagraphs(string html)
    {
        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Extract divs with data-testid="paragraph-*" pattern
            var paragraphDivs = doc.DocumentNode.SelectNodes("//div[starts-with(@data-testid, 'paragraph-')]");

            if (paragraphDivs == null || paragraphDivs.Count == 0)
            {
                return null;
            }

            var texts = paragraphDivs
                .Select(div => System.Net.WebUtility.HtmlDecode(div.InnerText?.Trim() ?? string.Empty))
                .Where(text => text.Length > 15)
                .Select(text => Regex.Replace(text, @"\s+", " ").Trim())
                .ToList();


            if (texts.Count == 0)
            {
                return null;
            }

            var result = string.Join(" ", texts);
            result = Regex.Replace(result, @"\s+", " ").Trim();
            if (result.Length <= MinExtractedLength)
            {
                return null;
            }

            return result.Length > MaxExtractedLength
                ? result[..MaxExtractedLength]
                : result;
        }
        catch
        {
            return null;
        }
    }
}
