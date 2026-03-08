using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PricePredictor.Api.Finance;
using PricePredicator.Infrastructure.Finance;
using PricePredicator.Infrastructure.Models;

namespace PricePredictor.Api;

public class YahooFinanceBackgroundService : BackgroundService
{
    private readonly ILogger<YahooFinanceBackgroundService> _logger;
    private readonly YahooFinanceClient _yahooClient;
    private readonly IVolatilityRepository _repository;
    private readonly YahooFinanceSettings _settings;
    private readonly TradingIndicatorNotificationService _notificationService;
    private DateTime _lastBackupTime = DateTime.UtcNow;
    private DateTime _lastNotificationTime = DateTime.UtcNow;
    private readonly Dictionary<string, List<double>> _returnsBuffer = new();
    private readonly Dictionary<string, List<(DateTime, double)>> _volumeBuffer = new(); // For volume calculations
    private readonly Dictionary<string, List<decimal>> _priceBuffer = new(); // For technical indicators

    public YahooFinanceBackgroundService(
        ILogger<YahooFinanceBackgroundService> logger,
        YahooFinanceClient yahooClient,
        IVolatilityRepository repository,
        TradingIndicatorNotificationService notificationService,
        IOptions<YahooFinanceSettings> settings)
    {
        _logger = logger;
        _yahooClient = yahooClient;
        _repository = repository;
        _notificationService = notificationService;
        _settings = settings.Value;

        // Initialize buffers for each symbol
        foreach (var symbol in _settings.Symbols)
        {
            _returnsBuffer[symbol] = new List<double>();
            _volumeBuffer[symbol] = new List<(DateTime, double)>();
            _priceBuffer[symbol] = new List<decimal>();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Yahoo Finance Background Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Fetching intraday data at {Time}", DateTime.UtcNow);

                foreach (var symbol in _settings.Symbols)
                {
                    await ProcessSymbolAsync(symbol, stoppingToken);
                }

                // Check if it's time for notifications (configurable interval)
                if (DateTime.UtcNow - _lastNotificationTime >= TimeSpan.FromMinutes(_settings.NotificationIntervalMinutes))
                {
                    await SendTradingNotificationsAsync(stoppingToken);
                    _lastNotificationTime = DateTime.UtcNow;
                }

                // Check if it's time for backup logging
                if (DateTime.UtcNow - _lastBackupTime >= TimeSpan.FromMinutes(_settings.VolatilityBackupMinutes))
                {
                    await LogBackupDataAsync(stoppingToken);
                    _lastBackupTime = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Yahoo Finance data");
            }

            // Wait 1 minute before next fetch
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }

        _logger.LogInformation("Yahoo Finance Background Service stopping.");
    }

    private async Task ProcessSymbolAsync(string symbol, CancellationToken cancellationToken)
    {
        try
        {
            var candles = await _yahooClient.GetIntradayDataAsync(symbol, _settings.Interval, _settings.Range, cancellationToken);

            if (candles.Count == 0)
            {
                _logger.LogWarning("No candles received for {Symbol}", symbol);
                return;
            }

            // Process the last candle (most recent)
            var lastCandle = candles[^1];
            var previousCandle = candles.Count > 1 ? candles[^2] : null;

            // Calculate logarithmic return
            var logReturn = previousCandle != null
                ? IndicatorsCalculator.CalculateLogarithmicReturn(lastCandle.Close, previousCandle.Close)
                : 0.0;

            _returnsBuffer[symbol].Add(logReturn);
            _priceBuffer[symbol].Add(lastCandle.Close);
            _volumeBuffer[symbol].Add((lastCandle.Timestamp, (double)lastCandle.Volume));

            // Keep buffers at reasonable size (last 200 candles for 200+ minute history)
            if (_returnsBuffer[symbol].Count > 200)
                _returnsBuffer[symbol].RemoveAt(0);
            if (_priceBuffer[symbol].Count > 200)
                _priceBuffer[symbol].RemoveAt(0);
            if (_volumeBuffer[symbol].Count > 200)
                _volumeBuffer[symbol].RemoveAt(0);

            // Calculate rolling volatilities
            var vol5 = GetRollingVolatility(symbol, 5);
            var vol15 = GetRollingVolatility(symbol, 15);
            var vol60 = GetRollingVolatility(symbol, 60);

            // Calculate panic scores
            var shortPanicScore = IndicatorsCalculator.CalculateNormalizedVolatilityPanicScore(
                logReturn, vol5, vol60, 0.3, 0.7);
            var longPanicScore = IndicatorsCalculator.CalculateNormalizedVolatilityPanicScore(
                logReturn, vol60, vol60, 0.3, 0.7);

            // Calculate technical indicators for comprehensive trading decision support
            var atr = IndicatorsCalculator.ATR((double)lastCandle.High, (double)lastCandle.Low, 
                previousCandle != null ? (double)previousCandle.Close : (double)lastCandle.Close);
            
            var priceArray = _priceBuffer[symbol].Select(p => (double)p).ToArray();
            var rsiDeviation = priceArray.Length >= 2 ? IndicatorsCalculator.RSIDeviation(priceArray) : 0.0;
            var bollingerDeviation = priceArray.Length >= 20 ? IndicatorsCalculator.BollingerDeviation((double)lastCandle.Close, priceArray) : 0.0;
            
            // Calculate volume metrics
            var avgVolume = _volumeBuffer[symbol].Count > 0 ? _volumeBuffer[symbol].Average(v => v.Item2) : 1.0;
            var volumeSpike = IndicatorsCalculator.VolumeSpike((double)lastCandle.Volume, avgVolume);
            var pastVolume = _volumeBuffer[symbol].Count > 1 
                ? _volumeBuffer[symbol][_volumeBuffer[symbol].Count - 2].Item2 
                : (double)lastCandle.Volume;
            var vroc = IndicatorsCalculator.VROC((double)lastCandle.Volume, pastVolume);
            
            // Calculate composite panic score
            var compositePanicScore = IndicatorsCalculator.CompositePanicScore(
                logReturn, vol5, atr, rsiDeviation, bollingerDeviation, volumeSpike, vroc);

            // Log the last point
            _logger.LogInformation(
                "Symbol: {Symbol}, Timestamp: {Timestamp}, Close: {Close}, LogReturn: {Return:F6}, Vol5: {Vol5:F6}, Vol15: {Vol15:F6}, Vol60: {Vol60:F6}, ShortPanic: {ShortPanic:F6}, LongPanic: {LongPanic:F6}, CompositePanic: {CompositePanic:F6}",
                symbol, lastCandle.Timestamp, lastCandle.Close, logReturn, vol5, vol15, vol60, shortPanicScore, longPanicScore, compositePanicScore);

            // Save to database
            await SaveToDatabaseAsync(symbol, lastCandle, logReturn, vol5, vol15, vol60, shortPanicScore, longPanicScore, cancellationToken);

            var dailySummary = await UpdateDailySummaryAsync(symbol, candles, cancellationToken);

            // Store metrics for notification
            StoreMetricsForNotification(symbol, lastCandle, logReturn, vol5, vol15, vol60, 
                shortPanicScore, longPanicScore, compositePanicScore, atr, rsiDeviation, 
                bollingerDeviation, volumeSpike, vroc, dailySummary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing symbol {Symbol}", symbol);
        }
    }

    private double GetRollingVolatility(string symbol, int minutes)
    {
        var buffer = _returnsBuffer[symbol];

        // Rolling volatility is based on 1-minute candles, so:
        // 5 minutes = last 5 returns, 15 minutes = last 15 returns, 60 minutes = last 60 returns
        var window = Math.Min(minutes, buffer.Count);
        if (window < 2)
            return 0;

        var recentReturns = buffer.TakeLast(window);
        return IndicatorsCalculator.CalculateRollingVolatility(recentReturns);
    }

    private async Task SaveToDatabaseAsync(
        string symbol,
        CandlePoint candle,
        double logReturn,
        double vol5,
        double vol15,
        double vol60,
        double shortPanicScore,
        double longPanicScore,
        CancellationToken cancellationToken)
    {
        try
        {
            switch (symbol)
            {
                case "GLD":
                case "XAUUSD=X":
                case "GC=F":
                    var goldEntity = new VolatilityGold
                    {
                        CommodityId = 1, // Gold
                        Timestamp = candle.Timestamp,
                        Open = candle.Open,
                        High = candle.High,
                        Low = candle.Low,
                        Close = candle.Close,
                        Volume = candle.Volume,
                        LogarithmicReturn = logReturn,
                        RollingVol5 = vol5,
                        RollingVol15 = vol15,
                        RollingVol60 = vol60,
                        ShortPanicScore = shortPanicScore,
                        LongPanicScore = longPanicScore
                    };
                    await _repository.AddVolatilityGoldAsync(goldEntity, cancellationToken);
                    break;

                case "SLV":
                case "XAGUSD=X":
                case "SI=F":
                    var silverEntity = new VolatilitySilver
                    {
                        CommodityId = 2, // Silver
                        Timestamp = candle.Timestamp,
                        Open = candle.Open,
                        High = candle.High,
                        Low = candle.Low,
                        Close = candle.Close,
                        Volume = candle.Volume,
                        LogarithmicReturn = logReturn,
                        RollingVol5 = vol5,
                        RollingVol15 = vol15,
                        RollingVol60 = vol60,
                        ShortPanicScore = shortPanicScore,
                        LongPanicScore = longPanicScore
                    };
                    await _repository.AddVolatilitySilverAsync(silverEntity, cancellationToken);
                    break;

                case "NG=F":
                    var ngEntity = new VolatilityNaturalGas
                    {
                        CommodityId = 3, // NaturalGas
                        Timestamp = candle.Timestamp,
                        Open = candle.Open,
                        High = candle.High,
                        Low = candle.Low,
                        Close = candle.Close,
                        Volume = candle.Volume,
                        LogarithmicReturn = logReturn,
                        RollingVol5 = vol5,
                        RollingVol15 = vol15,
                        RollingVol60 = vol60,
                        ShortPanicScore = shortPanicScore,
                        LongPanicScore = longPanicScore
                    };
                    await _repository.AddVolatilityNaturalGasAsync(ngEntity, cancellationToken);
                    break;

                case "CL=F":
                    var clEntity = new VolatilityOil
                    {
                        CommodityId = 4, // Oil
                        Timestamp = candle.Timestamp,
                        Open = candle.Open,
                        High = candle.High,
                        Low = candle.Low,
                        Close = candle.Close,
                        Volume = candle.Volume,
                        LogarithmicReturn = logReturn,
                        RollingVol5 = vol5,
                        RollingVol15 = vol15,
                        RollingVol60 = vol60,
                        ShortPanicScore = shortPanicScore,
                        LongPanicScore = longPanicScore
                    };
                    await _repository.AddVolatilityOilAsync(clEntity, cancellationToken);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving data for {Symbol}", symbol);
        }
    }

    private readonly Dictionary<string, TradingMetrics> _latestMetrics = new();

    private sealed record DailySummary(decimal High, decimal Low);

    private void StoreMetricsForNotification(
        string symbol,
        CandlePoint candle,
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
        DailySummary? dailySummary)
    {
        _latestMetrics[symbol] = new TradingMetrics
        {
            Symbol = symbol,
            Timestamp = candle.Timestamp,
            Close = candle.Close,
            LogReturn = logReturn,
            Vol5 = vol5,
            Vol15 = vol15,
            Vol60 = vol60,
            ShortPanicScore = shortPanicScore,
            LongPanicScore = longPanicScore,
            CompositePanicScore = compositePanicScore,
            ATR = atr,
            RSIDeviation = rsiDeviation,
            BollingerDeviation = bollingerDeviation,
            VolumeSpike = volumeSpike,
            VROC = vroc,
            DailyHigh = dailySummary?.High,
            DailyLow = dailySummary?.Low
        };
    }

    private async Task SendTradingNotificationsAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Sending trading notifications for {Count} symbols", _latestMetrics.Count);

            if (_latestMetrics.Count == 0)
                return;

            await _notificationService.SendSummaryNotificationAsync(_latestMetrics, cancellationToken);

            foreach (var kvp in _latestMetrics.Where(x => x.Value.CompositePanicScore > 1.5))
            {
                var symbol = kvp.Key;
                var metrics = kvp.Value;
                _logger.LogWarning("HIGH PANIC ALERT for {Symbol}: {CompositePanic}", symbol, metrics.CompositePanicScore);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending trading notifications");
        }
    }

    private async Task LogBackupDataAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("=== VOLATILITY BACKUP LOG (Last 10 minutes) - {Time} ===", DateTime.UtcNow);

            foreach (var symbol in _settings.Symbols)
            {
                var commodityName = SymbolMapper.GetFullName(symbol);
                _logger.LogInformation("--- {Symbol} ({CommodityName}) ---", symbol, commodityName);

                var data = symbol switch
                {
                    "GLD" => (object)await _repository.GetGoldLastAsync(10, cancellationToken),
                    "XAUUSD=X" => (object)await _repository.GetGoldLastAsync(10, cancellationToken),
                    "GC=F" => (object)await _repository.GetGoldLastAsync(10, cancellationToken),
                    "SLV" => (object)await _repository.GetSilverLastAsync(10, cancellationToken),
                    "XAGUSD=X" => (object)await _repository.GetSilverLastAsync(10, cancellationToken),
                    "SI=F" => (object)await _repository.GetSilverLastAsync(10, cancellationToken),
                    "NG=F" => (object)await _repository.GetNaturalGasLastAsync(10, cancellationToken),
                    "CL=F" => (object)await _repository.GetOilLastAsync(10, cancellationToken),
                    _ => new List<object>()
                };

                if (data is List<VolatilityGold> goldData)
                {
                    foreach (var item in goldData)
                    {
                        _logger.LogInformation(
                            "  TS: {Timestamp}, O: {Open}, H: {High}, L: {Low}, C: {Close}, V: {Volume}, Ret: {Return:F6}, V5: {Vol5:F6}, V15: {Vol15:F6}, V60: {Vol60:F6}, SP: {SP:F6}, LP: {LP:F6}",
                            item.Timestamp, item.Open, item.High, item.Low, item.Close, item.Volume, item.LogarithmicReturn, item.RollingVol5, item.RollingVol15, item.RollingVol60, item.ShortPanicScore, item.LongPanicScore);
                    }
                }
                else if (data is List<VolatilitySilver> silverData)
                {
                    foreach (var item in silverData)
                    {
                        _logger.LogInformation(
                            "  TS: {Timestamp}, O: {Open}, H: {High}, L: {Low}, C: {Close}, V: {Volume}, Ret: {Return:F6}, V5: {Vol5:F6}, V15: {Vol15:F6}, V60: {Vol60:F6}, SP: {SP:F6}, LP: {LP:F6}",
                            item.Timestamp, item.Open, item.High, item.Low, item.Close, item.Volume, item.LogarithmicReturn, item.RollingVol5, item.RollingVol15, item.RollingVol60, item.ShortPanicScore, item.LongPanicScore);
                    }
                }
                else if (data is List<VolatilityNaturalGas> ngData)
                {
                    foreach (var item in ngData)
                    {
                        _logger.LogInformation(
                            "  TS: {Timestamp}, O: {Open}, H: {High}, L: {Low}, C: {Close}, V: {Volume}, Ret: {Return:F6}, V5: {Vol5:F6}, V15: {Vol15:F6}, V60: {Vol60:F6}, SP: {SP:F6}, LP: {LP:F6}",
                            item.Timestamp, item.Open, item.High, item.Low, item.Close, item.Volume, item.LogarithmicReturn, item.RollingVol5, item.RollingVol15, item.RollingVol60, item.ShortPanicScore, item.LongPanicScore);
                    }
                }
                else if (data is List<VolatilityOil> clData)
                {
                    foreach (var item in clData)
                    {
                        _logger.LogInformation(
                            "  TS: {Timestamp}, O: {Open}, H: {High}, L: {Low}, C: {Close}, V: {Volume}, Ret: {Return:F6}, V5: {Vol5:F6}, V15: {Vol15:F6}, V60: {Vol60:F6}, SP: {SP:F6}, LP: {LP:F6}",
                            item.Timestamp, item.Open, item.High, item.Low, item.Close, item.Volume, item.LogarithmicReturn, item.RollingVol5, item.RollingVol15, item.RollingVol60, item.ShortPanicScore, item.LongPanicScore);
                    }
                }
            }

            _logger.LogInformation("=== END BACKUP LOG ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging backup data");
        }
    }

    private async Task<DailySummary?> UpdateDailySummaryAsync(string symbol, List<CandlePoint> candles, CancellationToken cancellationToken)
    {
        var lastCandle = candles[^1];
        var day = DateOnly.FromDateTime(lastCandle.Timestamp);
        var dayCandles = candles
            .Where(c => DateOnly.FromDateTime(c.Timestamp) == day)
            .OrderBy(c => c.Timestamp)
            .ToList();

        if (dayCandles.Count == 0)
            return null;

        var open = dayCandles[0].Open;
        var close = dayCandles[^1].Close;
        var high = dayCandles.Max(c => c.High);
        var low = dayCandles.Min(c => c.Low);
        var avg = dayCandles.Average(c => c.Close);
        var volumeSum = dayCandles.Sum(c => c.Volume ?? 0L);

        var rangePct = 0m;
        if (open != 0m)
        {
            var baseRange = (high - low) / open * 100m;
            rangePct = close >= open ? baseRange : -baseRange;
        }

        var daily = new VolatilityDaily
        {
            Id = Guid.CreateVersion7(),
            Day = day,
            Open = open,
            Close = close,
            High = high,
            Low = low,
            Avg = avg,
            VolumeSum = volumeSum,
            RangePct = rangePct
        };

        var commodity = symbol switch
        {
            "GLD" or "XAUUSD=X" or "GC=F" => VolatilityCommodity.Gold,
            "SLV" or "XAGUSD=X" or "SI=F" => VolatilityCommodity.Silver,
            "NG=F" => VolatilityCommodity.NaturalGas,
            "CL=F" => VolatilityCommodity.Oil,
            _ => throw new InvalidOperationException($"Unknown symbol: {symbol}")
        };

        await _repository.UpsertDailyAsync(commodity, daily, cancellationToken);

        return new DailySummary(high, low);
    }
}

