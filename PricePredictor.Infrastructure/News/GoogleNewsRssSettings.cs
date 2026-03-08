namespace PricePredictor.Infrastructure.News;

public sealed record GoogleNewsRssSettings
{
    public const string SectionName = "GoogleNewsRss";

    public string BaseUrl { get; init; } = "https://news.google.com/";
    public string RssPath { get; init; } = "rss/search?q=gold+price+XAUUSD&hl=en-US&gl=US&ceid=US:en";
}

