namespace PricePredictor.Application.Finance;

public interface IGoldPriceService
{
    Task<IReadOnlyList<GoldPricePoint>> GetGoldPricesAsync(int days, CancellationToken cancellationToken);
}

public record GoldPricePoint(
    DateOnly Date,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long? Volume
);
