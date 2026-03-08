namespace PricePredictor.Infrastructure.News;

public interface IGoogleNewsRssClient
{
    Task<string> GetGoldNewsRssAsync(CancellationToken cancellationToken);
}

