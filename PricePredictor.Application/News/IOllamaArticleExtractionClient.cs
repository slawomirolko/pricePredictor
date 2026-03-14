namespace PricePredictor.Application.News;

public interface IOllamaArticleExtractionClient
{
    Task<string?> ExtractMainContentAsync(string systemPrompt, string htmlContent, string? articleTitle, CancellationToken cancellationToken);

    /// <summary>
    /// Asks Cloud Ollama whether the article content is useful for short-term commodities trading.
    /// </summary>
    Task<bool> AssessTradingUsefulnessAsync(string articleContent, CancellationToken cancellationToken);

    /// <summary>
    /// Asks Cloud Ollama for a ≤500-character trading-relevant summary of the article content.
    /// </summary>
    Task<string> SummarizeAsync(string articleContent, CancellationToken cancellationToken);

    /// <summary>
    /// Generates a vector embedding for the given text using Local Ollama.
    /// </summary>
    Task<IReadOnlyList<float>> EmbedAsync(string text, CancellationToken cancellationToken);
}
