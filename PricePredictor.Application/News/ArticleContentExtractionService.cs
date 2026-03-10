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

        var reducedText = ExtractParagraphs(html);
        var payload = string.IsNullOrWhiteSpace(reducedText) ? html : reducedText;

        var llmResult = await _extractionClient.ExtractMainContentAsync(
            ArticleExtractionPrompts.ArticleExtractionPrompt,
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

            var noisySelectors = new[]
            {
                "//script", "//style", "//noscript", "//div[@class*='cookie']",
                "//nav", "//footer", "//header", "//div[@class*='related']",
                "//div[@class*='advertisement']", "//div[@class*='social']"
            };

            foreach (var selector in noisySelectors)
            {
                var elements = doc.DocumentNode.SelectNodes(selector);
                if (elements == null)
                {
                    continue;
                }

                foreach (var element in elements)
                {
                    element.ParentNode?.RemoveChild(element);
                }
            }

            var article = doc.DocumentNode.SelectSingleNode("//article") ??
                          doc.DocumentNode.SelectSingleNode("//div[@data-testid='article']") ??
                          doc.DocumentNode.SelectSingleNode("//div[@id*='article-body']") ??
                          doc.DocumentNode.SelectSingleNode("//div[@class*='article-body']") ??
                          doc.DocumentNode.SelectSingleNode("//div[@class*='story-body']") ??
                          doc.DocumentNode.SelectSingleNode("//body");

            if (article == null)
            {
                return null;
            }

            var paragraphs = article.SelectNodes(".//p") ??
                             article.SelectNodes(".//div[@class*='paragraph'] | .//span[@class*='paragraph']");

            if (paragraphs == null || paragraphs.Count == 0)
            {
                return null;
            }

            var texts = paragraphs
                .Select(paragraph => System.Net.WebUtility.HtmlDecode(paragraph.InnerText?.Trim() ?? string.Empty))
                .Where(text => text.Length > 15)
                .Select(text => Regex.Replace(text, @"\s+", " ").Trim())
                .ToList();

            if (texts.Count < 3)
            {
                var divs = article.SelectNodes(".//div");
                if (divs != null)
                {
                    foreach (var div in divs)
                    {
                        var text = System.Net.WebUtility.HtmlDecode(div.InnerText?.Trim() ?? string.Empty);
                        if (text.Length > 200 && !text.Contains("<", StringComparison.Ordinal) && !texts.Contains(text))
                        {
                            texts.Add(Regex.Replace(text, @"\s+", " ").Trim());
                        }
                    }
                }
            }

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
