namespace PricePredicator.App;
public sealed record GoldNewsSettings
{
    public const string SectionName = "GoldNews";

    // Backward-compatible single URL.
    public string RssUrl { get; init; } = string.Empty;

    // Preferred: multiple URLs, tried in order.
    public string[] RssUrls { get; init; } = Array.Empty<string>();

    public int EmbeddingDimensions { get; init; } = 3072;
    public string OllamaUrl { get; init; } = string.Empty;
    public string OllamaModel { get; init; } = "phi3";
}
