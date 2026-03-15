namespace PricePredictor.Application.Models;

public class VolatilityOil : VolatilityBase
{
    public static VolatilityOil Create(
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
    {
        return Create<VolatilityOil>(
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
            longPanicScore,
            id);
    }
}
