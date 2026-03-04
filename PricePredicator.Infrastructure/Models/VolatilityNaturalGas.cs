namespace PricePredicator.Infrastructure.Models;

public class VolatilityNaturalGas
{
    public int Id { get; set; }
    public int CommodityId { get; set; }
    public Commodity Commodity { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long? Volume { get; set; }
    public double LogarithmicReturn { get; set; }
    public double RollingVol5 { get; set; }
    public double RollingVol15 { get; set; }
    public double RollingVol60 { get; set; }
    public double ShortPanicScore { get; set; }
    public double LongPanicScore { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
