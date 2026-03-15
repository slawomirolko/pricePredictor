namespace PricePredictor.Infrastructure;

public sealed record NtfySettings
{
    public const string SectionName = "Ntfy";
    
    public string BaseUrl { get; init; } = string.Empty;
    public string Topic { get; init; } = string.Empty;
}