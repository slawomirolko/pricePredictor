namespace PricePredictor.Application.Finance.Interfaces;

public interface ICommodityMarketDataClient
{
    Task<CommodityLatestMarketDataDto> GetLatestAsync(
        string symbol,
        string interval = "1m",
        string range = "1d",
        CancellationToken cancellationToken = default);
}
