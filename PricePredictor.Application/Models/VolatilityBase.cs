namespace PricePredictor.Application.Models;

public abstract class VolatilityBase
{
    public int Id { get; private set; }
    public int CommodityId { get; private set; }
    public Commodity Commodity { get; private set; } = null!;
    public DateTime Timestamp { get; private set; }
    public decimal Open { get; private set; }
    public decimal High { get; private set; }
    public decimal Low { get; private set; }
    public decimal Close { get; private set; }
    public long? Volume { get; private set; }
    public double LogarithmicReturn { get; private set; }
    public double RollingVol5 { get; private set; }
    public double RollingVol15 { get; private set; }
    public double RollingVol60 { get; private set; }
    public double ShortPanicScore { get; private set; }
    public double LongPanicScore { get; private set; }
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    protected void UpdateSnapshot(
        int commodityId,
        DateTime timestamp,
        decimal open,
        decimal high,
        decimal low,
        decimal close,
        long? volume,
        double logarithmicReturn,
        double rollingVol5,
        double rollingVol15,
        double rollingVol60,
        double shortPanicScore,
        double longPanicScore)
    {
        CommodityId = commodityId;
        Timestamp = timestamp;
        Open = open;
        High = high;
        Low = low;
        Close = close;
        Volume = volume;
        LogarithmicReturn = logarithmicReturn;
        RollingVol5 = rollingVol5;
        RollingVol15 = rollingVol15;
        RollingVol60 = rollingVol60;
        ShortPanicScore = shortPanicScore;
        LongPanicScore = longPanicScore;
    }

    protected void AssignIdentity(int? id)
    {
        var resolvedId = id ?? 0;
        if (resolvedId < 0)
        {
            throw new ArgumentException("Volatility id cannot be negative.", nameof(id));
        }

        Id = resolvedId;
    }

    protected static TModel Create<TModel>(
        int commodityId,
        DateTime timestamp,
        decimal open,
        decimal high,
        decimal low,
        decimal close,
        long? volume,
        double logarithmicReturn,
        double rollingVol5,
        double rollingVol15,
        double rollingVol60,
        double shortPanicScore,
        double longPanicScore,
        int? id = null)
        where TModel : VolatilityBase, new()
    {
        var model = new TModel();
        model.AssignIdentity(id);
        model.UpdateSnapshot(
            commodityId,
            timestamp,
            open,
            high,
            low,
            close,
            volume,
            logarithmicReturn,
            rollingVol5,
            rollingVol15,
            rollingVol60,
            shortPanicScore,
            longPanicScore);

        return model;
    }
}
