namespace PricePredictor.Application.Models;

public class VolatilityDaily
{
    private VolatilityDaily()
    {
    }

    private VolatilityDaily(
        Guid id,
        DateOnly day,
        decimal open,
        decimal close,
        decimal high,
        decimal low,
        decimal avg,
        long volumeSum,
        decimal rangePct)
    {
        Id = id;
        Day = day;
        Open = open;
        Close = close;
        High = high;
        Low = low;
        Avg = avg;
        VolumeSum = volumeSum;
        RangePct = rangePct;
    }

    public Guid Id { get; private set; }
    public int CommodityId { get; private set; }
    public Commodity Commodity { get; private set; } = null!;
    public DateOnly Day { get; private set; }
    public decimal Open { get; private set; }
    public decimal Close { get; private set; }
    public decimal High { get; private set; }
    public decimal Low { get; private set; }
    public decimal Avg { get; private set; }
    public long VolumeSum { get; private set; }
    public decimal RangePct { get; private set; }

    public static VolatilityDaily Create(
        DateOnly day,
        decimal open,
        decimal close,
        decimal high,
        decimal low,
        decimal avg,
        long volumeSum,
        decimal rangePct,
        Guid? id = null)
    {
        var resolvedId = id ?? Guid.CreateVersion7();
        if (resolvedId == Guid.Empty)
        {
            throw new ArgumentException("VolatilityDaily id cannot be empty.", nameof(id));
        }

        return new VolatilityDaily(
            resolvedId,
            day,
            open,
            close,
            high,
            low,
            avg,
            volumeSum,
            rangePct);
    }

    public void BindCommodity(int commodityId)
    {
        CommodityId = commodityId;
    }

    public void UpdateSnapshot(
        decimal open,
        decimal close,
        decimal high,
        decimal low,
        decimal avg,
        long volumeSum,
        decimal rangePct)
    {
        Open = open;
        Close = close;
        High = high;
        Low = low;
        Avg = avg;
        VolumeSum = volumeSum;
        RangePct = rangePct;
    }

    public void UpdateFrom(VolatilityDaily source)
    {
        UpdateSnapshot(
            source.Open,
            source.Close,
            source.High,
            source.Low,
            source.Avg,
            source.VolumeSum,
            source.RangePct);
    }
}
