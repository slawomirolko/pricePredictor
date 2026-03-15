 namespace PricePredictor.Application.Finance;

/// <summary>
/// Indicators calculator for volatility analysis
/// </summary>
public static class IndicatorsCalculator
{
    /// <summary>
    /// Calculate logarithmic return: ln(P_t / P_t-1)
    /// </summary>
    public static double CalculateLogarithmicReturn(decimal currentPrice, decimal previousPrice)
    {
        if (previousPrice == 0)
            return 0;

        return Math.Log((double)currentPrice / (double)previousPrice);
    }

    /// <summary>
    /// Calculate rolling volatility (standard deviation of logarithmic returns)
    /// </summary>
    public static double CalculateRollingVolatility(IEnumerable<double> returns)
    {
        var returnsList = returns.ToList();
        if (returnsList.Count < 2)
            return 0;

        var mean = returnsList.Average();
        var variance = returnsList.Sum(r => Math.Pow(r - mean, 2)) / returnsList.Count;
        return Math.Sqrt(variance);
    }

    /// <summary>
    /// Calculate standard deviation
    /// </summary>
    public static double CalculateStdDev(IEnumerable<double> values)
    {
        var list = values.ToList();
        if (list.Count == 0)
            return 0;

        var mean = list.Average();
        var variance = list.Sum(v => Math.Pow(v - mean, 2)) / list.Count;
        return Math.Sqrt(variance);
    }

    /// <summary>
    /// Normalized Volatility Panic Score (more appropriate for trading)
    /// panic_score = w1*|return| + w2*(rollingVolShort / rollingVolLong)
    /// 
    /// This metric combines:
    /// - Immediate price movement (|return|) 
    /// - Volatility ratio (short-term vs long-term)
    /// 
    /// When short-term volatility exceeds long-term, the ratio > 1, indicating panic
    /// </summary>
    public static double CalculateNormalizedVolatilityPanicScore(
        double logarithmicReturn,
        double rollingVolatilityShort,
        double rollingVolatilityLong,
        double weightReturn = 0.3,
        double weightVol = 0.7)
    {
        if (rollingVolatilityLong == 0)
            rollingVolatilityLong = 1e-9; // Prevent division by zero

        return weightReturn * Math.Abs(logarithmicReturn) + weightVol * (rollingVolatilityShort / rollingVolatilityLong);
    }

    /// <summary>
    /// Calculate simple or logarithmic return.
    /// </summary>
    public static double Return(double close, double prevClose, bool logReturn = true)
    {
        if (prevClose == 0)
        {
            return 0;
        }

        return logReturn
            ? Math.Log(close / prevClose)
            : (close - prevClose) / prevClose;
    }

    /// <summary>
    /// Rolling volatility (standard deviation of returns).
    /// </summary>
    public static double RollingVolatility(double[] returns)
    {
        return CalculateRollingVolatility(returns);
    }

    /// <summary>
    /// Average True Range (ATR).
    /// </summary>
    public static double ATR(double high, double low, double prevClose)
    {
        return Math.Max(high - low, Math.Max(Math.Abs(high - prevClose), Math.Abs(low - prevClose)));
    }

    /// <summary>
    /// RSI deviation from neutral (50).
    /// </summary>
    public static double RSIDeviation(double[] closes)
    {
        if (closes.Length < 2)
        {
            return 0;
        }

        double gain = 0;
        double loss = 0;
        for (int i = 1; i < closes.Length; i++)
        {
            double diff = closes[i] - closes[i - 1];
            if (diff > 0)
            {
                gain += diff;
            }
            else
            {
                loss -= diff;
            }
        }

        double avgGain = gain / closes.Length;
        double avgLoss = loss / closes.Length;
        double rs = avgLoss == 0 ? avgGain : avgGain / avgLoss;
        double rsi = 100 - (100 / (1 + rs));
        return Math.Abs(rsi - 50);
    }

    /// <summary>
    /// Bollinger Bands deviation in standard deviations.
    /// </summary>
    public static double BollingerDeviation(double close, double[] closes, int n = 20, double k = 2)
    {
        if (closes.Length < n)
        {
            return 0;
        }

        double[] window = closes.Skip(closes.Length - n).Take(n).ToArray();
        double ma = window.Average();
        double std = Math.Sqrt(window.Sum(x => Math.Pow(x - ma, 2)) / n);
        double upper = ma + k * std;
        double lower = ma - k * std;

        if (close > upper)
        {
            return (close - upper) / std;
        }

        if (close < lower)
        {
            return (lower - close) / std;
        }

        return 0;
    }

    /// <summary>
    /// Raw volume spike ratio.
    /// </summary>
    public static double VolumeSpike(double volume, double avgVolume)
    {
        if (avgVolume == 0)
        {
            return 0;
        }

        return volume / avgVolume;
    }

    /// <summary>
    /// Volume Rate of Change (VROC).
    /// </summary>
    public static double VROC(double volume, double pastVolume)
    {
        if (pastVolume == 0)
        {
            return 0;
        }

        return (volume - pastVolume) / pastVolume;
    }

    /// <summary>
    /// Composite panic score combining price and volume indicators.
    /// </summary>
    public static double CompositePanicScore(
        double return1Min,
        double rollingVol,
        double atr,
        double rsiDev,
        double bbDev,
        double volSpike,
        double vroc,
        double weightReturn = 0.3,
        double weightVol = 0.3,
        double weightAtr = 0.1,
        double weightRsi = 0.05,
        double weightBb = 0.05,
        double weightVolSpike = 0.1,
        double weightVroc = 0.1)
    {
        return weightReturn * Math.Abs(return1Min) +
               weightVol * rollingVol +
               weightAtr * atr +
               weightRsi * rsiDev +
               weightBb * bbDev +
               weightVolSpike * volSpike +
               weightVroc * vroc;
    }
}

