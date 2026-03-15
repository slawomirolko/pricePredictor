namespace PricePredictor.Application.Finance.Interfaces;

public interface IGoldNewsClient
{
    Task<string> GetRssXmlAsync(string rssUrl, CancellationToken cancellationToken);
    Task<string?> FetchArticleContentAsync(string articleUrl, string? articleTitle, CancellationToken cancellationToken);
}
