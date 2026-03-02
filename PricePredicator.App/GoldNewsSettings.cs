namespace PricePredicator.App;

public record GoldNewsSettings
{
    internal const string SectionName = "GoldNews";

    // Backward-compatible single URL.
    public string RssUrl { get; init; } = string.Empty;

    // Preferred: multiple URLs, tried in order.
    public string[] RssUrls { get; init; } = Array.Empty<string>();

    public string QdrantUrl { get; init; } = string.Empty;
    public string OllamaUrl { get; init; } = string.Empty;
    public string OllamaModel { get; init; } = "phi3";
}
