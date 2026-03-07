namespace PricePredicator.Infrastructure.Gold;

public record GoldPricePoint(
    DateOnly Date,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long? Volume
);
