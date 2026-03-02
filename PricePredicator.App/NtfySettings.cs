namespace PricePredicator.App;

public record NtfySettings
{
    internal const string SectionName = "Ntfy";
    
    public string BaseUrl { get; init; } = string.Empty;
    public string Topic { get; init; } = string.Empty;
}