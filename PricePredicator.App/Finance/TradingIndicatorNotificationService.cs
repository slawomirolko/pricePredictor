using System.Text;
using PricePredicator.App.Weather;

namespace PricePredicator.App.Finance;

/// <summary>
/// Service to format and send trading indicators as notifications
/// Includes panic scores, volatility, ATR, RSI, Bollinger Bands, and volume metrics
/// </summary>
public class TradingIndicatorNotificationService
{
    private readonly NtfyClient _ntfyClient;
    private readonly IWeatherService _weatherService;
    private readonly string _topic;

    public TradingIndicatorNotificationService(NtfyClient ntfyClient, IWeatherService weatherService, string topic)
    {
        _ntfyClient = ntfyClient;
        _weatherService = weatherService;
        _topic = topic;
    }

    /// <summary>
    /// Send a comprehensive trading notification with all indicators and weather context
    /// </summary>
    public async Task SendTradingIndicatorsNotificationAsync(
        string symbol,
        DateTime timestamp,
        decimal close,
        double logReturn,
        double vol5,
        double vol15,
        double vol60,
        double shortPanicScore,
        double longPanicScore,
        double compositePanicScore,
        double atr,
        double rsiDeviation,
        double bollingerDeviation,
        double volumeSpike,
        double vroc,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var commodityName = SymbolMapper.GetFullName(symbol);
            var message = FormatTradingIndicatorsMessage(
                symbol,
                commodityName,
                timestamp,
                close,
                logReturn,
                vol5,
                vol15,
                vol60,
                shortPanicScore,
                longPanicScore,
                compositePanicScore,
                atr,
                rsiDeviation,
                bollingerDeviation,
                volumeSpike,
                vroc);

            await _ntfyClient.SendAsync(_topic, message);
        }
        catch (Exception ex)
        {
            // Log but don't throw - notification failures shouldn't break main service
            System.Diagnostics.Debug.WriteLine($"Failed to send trading notification for {symbol}: {ex.Message}");
        }
    }

    /// <summary>
    /// Send a summary notification for all symbols with weather context
    /// </summary>
    public async Task SendSummaryNotificationAsync(
        Dictionary<string, TradingMetrics> allMetrics,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var message = await FormatSummaryMessageAsync(allMetrics);
            await _ntfyClient.SendAsync(_topic, message);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to send summary notification: {ex.Message}");
        }
    }

    private string FormatTradingIndicatorsMessage(
        string symbol,
        string commodityName,
        DateTime timestamp,
        decimal close,
        double logReturn,
        double vol5,
        double vol15,
        double vol60,
        double shortPanicScore,
        double longPanicScore,
        double compositePanicScore,
        double atr,
        double rsiDeviation,
        double bollingerDeviation,
        double volumeSpike,
        double vroc)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"🔔 {commodityName} ({symbol}) - {timestamp:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        // Price Section
        sb.AppendLine("📊 PRICE DATA");
        sb.AppendLine($"  Close: ${close:F2}");
        sb.AppendLine($"  Return: {logReturn:F6} ({(logReturn > 0 ? "📈" : "📉")})");
        sb.AppendLine();

        // Volatility Section
        sb.AppendLine("📈 VOLATILITY METRICS");
        sb.AppendLine($"  5-Min Vol:  {vol5:F6}");
        sb.AppendLine($"  15-Min Vol: {vol15:F6}");
        sb.AppendLine($"  60-Min Vol: {vol60:F6}");
        sb.AppendLine();

        // Panic Score Section - Color coded
        sb.AppendLine("🚨 PANIC SCORES");
        sb.Append($"  Short-Term: {shortPanicScore:F4}");
        sb.AppendLine(GetPanicScoreEmoji(shortPanicScore));

        sb.Append($"  Long-Term:  {longPanicScore:F4}");
        sb.AppendLine(GetPanicScoreEmoji(longPanicScore));

        sb.Append($"  Composite:  {compositePanicScore:F4}");
        sb.AppendLine(GetPanicScoreEmoji(compositePanicScore));
        sb.AppendLine();

        // Technical Indicators
        sb.AppendLine("🎯 TECHNICAL INDICATORS");
        sb.AppendLine($"  ATR:                {atr:F4}");
        sb.AppendLine($"  RSI Deviation:      {rsiDeviation:F4}");
        sb.AppendLine($"  Bollinger Dev:      {bollingerDeviation:F4}");
        sb.AppendLine();

        // Volume Section
        sb.AppendLine("📦 VOLUME METRICS");
        sb.AppendLine($"  Volume Spike Ratio: {volumeSpike:F4}x");
        sb.AppendLine($"  Volume ROC:         {vroc:F4}");
        sb.AppendLine();

        // Trading Decision Hint
        sb.AppendLine("💡 TRADING CONTEXT");
        sb.Append("  Signal: ");
        if (compositePanicScore > 1.5)
        {
            sb.AppendLine("HIGH PANIC ⚠️ - Consider protective measures");
        }
        else if (compositePanicScore > 1.0)
        {
            sb.AppendLine("ELEVATED ⚠️ - Increased market activity");
        }
        else if (compositePanicScore > 0.5)
        {
            sb.AppendLine("MODERATE ⚖️ - Normal trading conditions");
        }
        else
        {
            sb.AppendLine("LOW 😌 - Calm market");
        }

        return sb.ToString();
    }

    private async Task<string> FormatSummaryMessageAsync(Dictionary<string, TradingMetrics> allMetrics)
    {
        var sb = new StringBuilder();
        sb.AppendLine("═════════════════════════════════════════════");
        sb.AppendLine("📊 TRADING DASHBOARD SUMMARY");
        sb.AppendLine($"⏰ {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine("═════════════════════════════════════════════");
        sb.AppendLine();

        // Fixed order: Gold, Silver, Natural Gas, Oil
        var symbolOrder = new[] { "GC=F", "SI=F", "NG=F", "CL=F" };
        
        foreach (var symbol in symbolOrder)
        {
            if (allMetrics.TryGetValue(symbol, out var metrics))
            {
                var commodityName = SymbolMapper.GetFullName(symbol);
                
                // Determine reliability (data age in minutes)
                var dataAge = DateTime.UtcNow - metrics.Timestamp;
                var isReliable = dataAge.TotalMinutes >= 30;
                var reliabilityIndicator = isReliable ? "✅ RELIABLE" : "⚠️ BUILDING";

                sb.AppendLine($"{commodityName} ({symbol}) [{reliabilityIndicator}]");
                sb.AppendLine($"  Price: ${metrics.Close:F2} | Return: {metrics.LogReturn:F6}");
                sb.AppendLine($"  Composite Score: {metrics.CompositePanicScore:F4} {GetPanicScoreEmoji(metrics.CompositePanicScore)}");
                sb.AppendLine($"  Volatility (5/60m): {metrics.Vol5:F6} / {metrics.Vol60:F6}");
                sb.AppendLine();
            }
        }

        // Add weather context
        try
        {
            var cityWeatherList = await _weatherService.GetCitiesWeatherAsync();
            if (cityWeatherList.Any())
            {
                sb.AppendLine("🌤️ WEATHER CONTEXT");
                foreach (var cityWeather in cityWeatherList)
                {
                    sb.AppendLine($"  {cityWeather.City}: Max {cityWeather.MaxTemp}°C, Min {cityWeather.MinTemp}°C");
                }
            }
        }
        catch
        {
            // Weather data is optional
        }

        sb.AppendLine();
        sb.AppendLine("═════════════════════════════════════════════");

        return sb.ToString();
    }

    private static string GetPanicScoreEmoji(double score)
    {
        return score switch
        {
            >= 1.5 => "🔴 CRITICAL",
            >= 1.0 => "🟠 HIGH",
            >= 0.5 => "🟡 MODERATE",
            _ => "🟢 LOW"
        };
    }
}

/// <summary>
/// Container for trading metrics to be notified
/// </summary>
public record TradingMetrics
{
    public string Symbol { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public decimal Close { get; init; }
    public double LogReturn { get; init; }
    public double Vol5 { get; init; }
    public double Vol15 { get; init; }
    public double Vol60 { get; init; }
    public double ShortPanicScore { get; init; }
    public double LongPanicScore { get; init; }
    public double CompositePanicScore { get; init; }
    public double ATR { get; init; }
    public double RSIDeviation { get; init; }
    public double BollingerDeviation { get; init; }
    public double VolumeSpike { get; init; }
    public double VROC { get; init; }
}

