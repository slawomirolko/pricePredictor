namespace PricePredicator.App.GoldNews;

public interface IGoldNewsClient
{
    Task<string> GetRssXmlAsync(string rssUrl, CancellationToken cancellationToken);
    Task<string?> FetchArticleContentAsync(string articleUrl, CancellationToken cancellationToken);
}
