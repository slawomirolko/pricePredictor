namespace PricePredictor.Application.Finance;

public interface IGoldNewsClient
{
    Task<string> GetRssXmlAsync(string rssUrl, CancellationToken cancellationToken);
    Task<string?> FetchArticleContentAsync(string articleUrl, CancellationToken cancellationToken);
}

