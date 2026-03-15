namespace PricePredictor.Application.Models;

public class VolatilitySilver : VolatilityBase
{
    public static VolatilitySilver Create(
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
        return Create<VolatilitySilver>(
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
