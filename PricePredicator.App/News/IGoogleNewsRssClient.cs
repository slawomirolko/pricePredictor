namespace PricePredicator.App.News;

public interface IGoogleNewsRssClient
{
    Task<string> GetGoldNewsRssAsync(CancellationToken cancellationToken);
}

