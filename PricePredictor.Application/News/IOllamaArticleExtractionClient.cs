namespace PricePredictor.Application.News;

public interface IOllamaArticleExtractionClient
{
    Task<string?> ExtractMainContentAsync(string systemPrompt, string htmlContent, string? articleTitle, CancellationToken cancellationToken);
}
