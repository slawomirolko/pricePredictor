namespace PricePredictor.Application.Finance;

public interface IGoogleNewsRssClient
{
    Task<string> GetGoldNewsRssAsync(CancellationToken cancellationToken);
}

