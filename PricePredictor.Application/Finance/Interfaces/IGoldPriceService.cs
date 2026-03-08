namespace PricePredictor.Application.Finance;

public interface IGoldPriceService
{
    Task<IReadOnlyList<GoldPricePoint>> GetGoldPricesAsync(int days, CancellationToken cancellationToken);
}

public class GoldPricePoint
{
    public DateTime Date { get; set; }
    public decimal Price { get; set; }
}

