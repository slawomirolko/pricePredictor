namespace PricePredictor.Application.Finance.Interfaces;

public interface IGoogleNewsRssClient
{
    Task<string> GetGoldNewsRssAsync(CancellationToken cancellationToken);
}

