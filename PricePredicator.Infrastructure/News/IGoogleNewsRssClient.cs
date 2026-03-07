namespace PricePredicator.Infrastructure.News;

public interface IGoogleNewsRssClient
{
    Task<string> GetGoldNewsRssAsync(CancellationToken cancellationToken);
}

