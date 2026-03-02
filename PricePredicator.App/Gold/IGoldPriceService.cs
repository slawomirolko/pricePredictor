namespace PricePredicator.App.Gold;

public interface IGoldPriceService
{
    Task<IReadOnlyList<GoldPricePoint>> GetGoldPricesAsync(int days, CancellationToken cancellationToken);
}
