namespace PricePredictor.Application.News;

public interface IArticleContentExtractionService
{
    Task<string?> ExtractAsync(string html, string? fallbackText, string? articleTitle, CancellationToken cancellationToken);
}
