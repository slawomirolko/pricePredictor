namespace PricePredictor.Application.News;

public interface IOllamaArticleExtractionClient
{
    Task<string?> ExtractMainContentAsync(string systemPrompt, string htmlContent, string? articleTitle, CancellationToken cancellationToken);
    Task<bool> AssessTradingUsefulnessAsync(string articleLink, string source, DateTime publishedAtUtc, CancellationToken cancellationToken);
}
