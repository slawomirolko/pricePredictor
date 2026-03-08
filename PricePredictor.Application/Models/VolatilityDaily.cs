namespace PricePredictor.Application.Models;

public class VolatilityDaily
{
    public Guid Id { get; set; }
    public int CommodityId { get; set; }
    public Commodity Commodity { get; set; } = null!;
    public DateOnly Day { get; set; }
    public decimal Open { get; set; }
    public decimal Close { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Avg { get; set; }
    public long VolumeSum { get; set; }
    public decimal RangePct { get; set; }
}

