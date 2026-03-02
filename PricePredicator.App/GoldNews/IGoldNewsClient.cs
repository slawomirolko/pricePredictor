namespace PricePredicator.App.GoldNews;

public interface IGoldNewsClient
{
    Task<string> GetRssXmlAsync(string rssUrl, CancellationToken cancellationToken);
    Task EnsureQdrantCollectionAsync(string qdrantBaseUrl, CancellationToken cancellationToken);
    Task UpsertPointsAsync(string qdrantBaseUrl, string collectionName, object body, CancellationToken cancellationToken);
}

