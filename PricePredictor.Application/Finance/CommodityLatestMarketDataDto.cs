namespace PricePredictor.Application.Finance;

public sealed record CommodityLatestMarketDataDto(
    string Symbol,
    DateTime Timestamp,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long? Volume);
