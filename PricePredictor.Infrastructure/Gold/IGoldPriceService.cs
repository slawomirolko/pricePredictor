namespace PricePredictor.Infrastructure.Gold;

public interface IGoldPriceService
{
    Task<IReadOnlyList<GoldPricePoint>> GetGoldPricesAsync(int days, CancellationToken cancellationToken);
}
