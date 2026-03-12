namespace PricePredictor.Infrastructure;
public sealed record GoldNewsSettings
{
    public const string SectionName = "GoldNews";

    // Backward-compatible single URL.
    public string RssUrl { get; init; } = string.Empty;

    // Preferred: multiple URLs, tried in order.
    public string[] RssUrls { get; init; } = Array.Empty<string>();

    public int EmbeddingDimensions { get; init; } = 3072;

    public string LocalOllamaUrl { get; init; } = "http://localhost:11434";
    public string LocalOllamaModel { get; init; } = "phi4";

    public string CloudOllamaUrl { get; init; } = "https://ollama.com";
    public string CloudOllamaModel { get; init; } = "gpt-oss:120b";
    public string? CloudOllamaApiKey { get; init; }
    public bool UseCloud { get; set; } = true;
    public bool Headless { get; init; } = true;
}

