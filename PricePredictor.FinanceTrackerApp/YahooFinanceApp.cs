using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PricePredictor.Application;
using PricePredictor.Application.Finance;
using PricePredictor.Application.Finance.Interfaces;
using PricePredictor.Application.Models;
using PricePredictor.Infrastructure.Finance;

namespace PricePredictor.FinanceTrackerApp;

public sealed class YahooFinanceApp
{
    private readonly ILogger<YahooFinanceApp> _logger;
    private readonly YahooFinanceClient _yahooClient;
    private readonly IVolatilityRepository _repository;
    private readonly YahooFinanceSettings _settings;
    private readonly TradingIndicatorNotificationService _notificationService;
    private readonly Dictionary<string, TradingMetrics> _latestMetrics = new();
    private readonly Dictionary<string, List<double>> _returnsBuffer = new();
    private readonly Dictionary<string, List<(DateTime, double)>> _volumeBuffer = new();
    private readonly Dictionary<string, List<decimal>> _priceBuffer = new();
    private DateTime _lastBackupTime = DateTime.UtcNow;
    private DateTime _lastNotificationTime = DateTime.MinValue;

    public YahooFinanceApp(
        ILogger<YahooFinanceApp> logger,
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

        foreach (var symbol in _settings.Symbols)
        {
            _returnsBuffer[symbol] = [];
            _volumeBuffer[symbol] = [];
            _priceBuffer[symbol] = [];
        }
    }

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Yahoo Finance application started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Fetching intraday data at {Time}", DateTime.UtcNow);

                foreach (var symbol in _settings.Symbols)
                {
                    await ProcessSymbolAsync(symbol, stoppingToken);
                }

                if (DateTime.UtcNow - _lastNotificationTime >= TimeSpan.FromMinutes(_settings.NotificationIntervalMinutes))
                {
                    await SendTradingNotificationsAsync(stoppingToken);
                    _lastNotificationTime = DateTime.UtcNow;
                }

                if (DateTime.UtcNow - _lastBackupTime >= TimeSpan.FromMinutes(_settings.VolatilityBackupMinutes))
                {
                    await LogBackupDataAsync(stoppingToken);
                    _lastBackupTime = DateTime.UtcNow;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Yahoo Finance data");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }

        _logger.LogInformation("Yahoo Finance application stopping.");
    }

    private async Task ProcessSymbolAsync(string symbol, CancellationToken cancellationToken)
    {
        try
        {
            var candles = await FetchCandlesAsync(symbol, cancellationToken);

            if (candles.Count == 0)
            {
                _logger.LogWarning(
                    "No candles received for {Symbol}. Primary request used Interval={Interval}, Range={Range}.",
                    symbol,
                    _settings.Interval,
                    _settings.Range);
                return;
            }

            var lastCandle = candles[^1];
            var previousCandle = candles.Count > 1 ? candles[^2] : null;

            var logReturn = previousCandle != null
                ? IndicatorsCalculator.CalculateLogarithmicReturn(lastCandle.Close, previousCandle.Close)
                : 0.0;

            var currentVolume = (double)(lastCandle.Volume ?? 0L);

            _returnsBuffer[symbol].Add(logReturn);
            _priceBuffer[symbol].Add(lastCandle.Close);
            _volumeBuffer[symbol].Add((lastCandle.Timestamp, currentVolume));

            if (_returnsBuffer[symbol].Count > 200)
            {
                _returnsBuffer[symbol].RemoveAt(0);
            }

            if (_priceBuffer[symbol].Count > 200)
            {
                _priceBuffer[symbol].RemoveAt(0);
            }

            if (_volumeBuffer[symbol].Count > 200)
            {
                _volumeBuffer[symbol].RemoveAt(0);
            }

            var vol5 = GetRollingVolatility(symbol, 5);
            var vol15 = GetRollingVolatility(symbol, 15);
            var vol60 = GetRollingVolatility(symbol, 60);

            var shortPanicScore = IndicatorsCalculator.CalculateNormalizedVolatilityPanicScore(
                logReturn, vol5, vol60, 0.3, 0.7);
            var longPanicScore = IndicatorsCalculator.CalculateNormalizedVolatilityPanicScore(
                logReturn, vol60, vol60, 0.3, 0.7);

            var atr = IndicatorsCalculator.ATR(
                (double)lastCandle.High,
                (double)lastCandle.Low,
                previousCandle != null ? (double)previousCandle.Close : (double)lastCandle.Close);

            var priceArray = _priceBuffer[symbol].Select(p => (double)p).ToArray();
            var rsiDeviation = priceArray.Length >= 2 ? IndicatorsCalculator.RSIDeviation(priceArray) : 0.0;
            var bollingerDeviation = priceArray.Length >= 20
                ? IndicatorsCalculator.BollingerDeviation((double)lastCandle.Close, priceArray)
                : 0.0;

            var avgVolume = _volumeBuffer[symbol].Count > 0 ? _volumeBuffer[symbol].Average(v => v.Item2) : 1.0;
            var volumeSpike = IndicatorsCalculator.VolumeSpike(currentVolume, avgVolume);
            var pastVolume = _volumeBuffer[symbol].Count > 1
                ? _volumeBuffer[symbol][^2].Item2
                : currentVolume;
            var vroc = IndicatorsCalculator.VROC(currentVolume, pastVolume);

            var compositePanicScore = IndicatorsCalculator.CompositePanicScore(
                logReturn, vol5, atr, rsiDeviation, bollingerDeviation, volumeSpike, vroc);

            _logger.LogInformation(
                "Symbol: {Symbol}, Timestamp: {Timestamp}, Close: {Close}, LogReturn: {Return:F6}, Vol5: {Vol5:F6}, Vol15: {Vol15:F6}, Vol60: {Vol60:F6}, ShortPanic: {ShortPanic:F6}, LongPanic: {LongPanic:F6}, CompositePanic: {CompositePanic:F6}",
                symbol, lastCandle.Timestamp, lastCandle.Close, logReturn, vol5, vol15, vol60, shortPanicScore, longPanicScore, compositePanicScore);

            await SaveToDatabaseAsync(symbol, lastCandle, logReturn, vol5, vol15, vol60, shortPanicScore, longPanicScore, cancellationToken);

            DailySummary? dailySummary = null;
            try
            {
                dailySummary = await UpdateDailySummaryAsync(symbol, candles, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update daily summary for {Symbol}; notification will use intraday-only metrics.", symbol);
            }

            StoreMetricsForNotification(
                symbol,
                lastCandle,
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
                vroc,
                dailySummary);

            _logger.LogInformation(
                "Stored latest metrics for {Symbol} at {Timestamp}. Tracked symbols: {Count}",
                symbol,
                lastCandle.Timestamp,
                _latestMetrics.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing symbol {Symbol}", symbol);
        }
    }

    private async Task<List<CandlePoint>> FetchCandlesAsync(string symbol, CancellationToken cancellationToken)
    {
        var primaryCandles = await _yahooClient.GetIntradayDataAsync(symbol, _settings.Interval, _settings.Range, cancellationToken);
        if (primaryCandles.Count > 0)
        {
            return primaryCandles;
        }

        var fallbackInterval = _settings.Interval.Equals("1m", StringComparison.OrdinalIgnoreCase)
            ? "5m"
            : _settings.Interval;
        var fallbackRange = _settings.Range.Equals("1d", StringComparison.OrdinalIgnoreCase)
            ? "5d"
            : _settings.Range;

        if (fallbackInterval.Equals(_settings.Interval, StringComparison.OrdinalIgnoreCase)
            && fallbackRange.Equals(_settings.Range, StringComparison.OrdinalIgnoreCase))
        {
            return primaryCandles;
        }

        var fallbackCandles = await _yahooClient.GetIntradayDataAsync(symbol, fallbackInterval, fallbackRange, cancellationToken);
        if (fallbackCandles.Count > 0)
        {
            _logger.LogWarning(
                "Primary candle request returned 0 rows for {Symbol}. Using fallback Interval={FallbackInterval}, Range={FallbackRange} with {Count} rows.",
                symbol,
                fallbackInterval,
                fallbackRange,
                fallbackCandles.Count);
        }

        return fallbackCandles;
    }

    private double GetRollingVolatility(string symbol, int minutes)
    {
        var buffer = _returnsBuffer[symbol];
        var window = Math.Min(minutes, buffer.Count);
        if (window < 2)
        {
            return 0;
        }

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
                    var goldEntity = VolatilityGold.Create(
                        commodityId: 1,
                        timestamp: candle.Timestamp,
                        open: candle.Open,
                        high: candle.High,
                        low: candle.Low,
                        close: candle.Close,
                        volume: candle.Volume,
                        logarithmicReturn: logReturn,
                        rollingVol5: vol5,
                        rollingVol15: vol15,
                        rollingVol60: vol60,
                        shortPanicScore: shortPanicScore,
                        longPanicScore: longPanicScore);
                    await _repository.AddVolatilityGoldAsync(goldEntity, cancellationToken);
                    break;

                case "SLV":
                case "XAGUSD=X":
                case "SI=F":
                    var silverEntity = VolatilitySilver.Create(
                        commodityId: 2,
                        timestamp: candle.Timestamp,
                        open: candle.Open,
                        high: candle.High,
                        low: candle.Low,
                        close: candle.Close,
                        volume: candle.Volume,
                        logarithmicReturn: logReturn,
                        rollingVol5: vol5,
                        rollingVol15: vol15,
                        rollingVol60: vol60,
                        shortPanicScore: shortPanicScore,
                        longPanicScore: longPanicScore);
                    await _repository.AddVolatilitySilverAsync(silverEntity, cancellationToken);
                    break;

                case "NG=F":
                    var ngEntity = VolatilityNaturalGas.Create(
                        commodityId: 3,
                        timestamp: candle.Timestamp,
                        open: candle.Open,
                        high: candle.High,
                        low: candle.Low,
                        close: candle.Close,
                        volume: candle.Volume,
                        logarithmicReturn: logReturn,
                        rollingVol5: vol5,
                        rollingVol15: vol15,
                        rollingVol60: vol60,
                        shortPanicScore: shortPanicScore,
                        longPanicScore: longPanicScore);
                    await _repository.AddVolatilityNaturalGasAsync(ngEntity, cancellationToken);
                    break;

                case "CL=F":
                    var clEntity = VolatilityOil.Create(
                        commodityId: 4,
                        timestamp: candle.Timestamp,
                        open: candle.Open,
                        high: candle.High,
                        low: candle.Low,
                        close: candle.Close,
                        volume: candle.Volume,
                        logarithmicReturn: logReturn,
                        rollingVol5: vol5,
                        rollingVol15: vol15,
                        rollingVol60: vol60,
                        shortPanicScore: shortPanicScore,
                        longPanicScore: longPanicScore);
                    await _repository.AddVolatilityOilAsync(clEntity, cancellationToken);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving data for {Symbol}", symbol);
        }
    }

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
            {
                return;
            }

            await _notificationService.SendSummaryNotificationAsync(_latestMetrics, cancellationToken);

            foreach (var kvp in _latestMetrics.Where(x => x.Value.CompositePanicScore > 1.5))
            {
                var symbol = kvp.Key;
                var metrics = kvp.Value;
                _logger.LogWarning("HIGH PANIC ALERT for {Symbol}: {CompositePanic}", symbol, metrics.CompositePanicScore);

                await _notificationService.SendTradingIndicatorsNotificationAsync(
                    symbol: symbol,
                    timestamp: metrics.Timestamp,
                    close: metrics.Close,
                    logReturn: metrics.LogReturn,
                    vol5: metrics.Vol5,
                    vol15: metrics.Vol15,
                    vol60: metrics.Vol60,
                    shortPanicScore: metrics.ShortPanicScore,
                    longPanicScore: metrics.LongPanicScore,
                    compositePanicScore: metrics.CompositePanicScore,
                    atr: metrics.ATR,
                    rsiDeviation: metrics.RSIDeviation,
                    bollingerDeviation: metrics.BollingerDeviation,
                    volumeSpike: metrics.VolumeSpike,
                    vroc: metrics.VROC,
                    cancellationToken: cancellationToken);
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

                switch (data)
                {
                    case List<VolatilityGold> goldData:
                        foreach (var item in goldData)
                        {
                            _logger.LogInformation(
                                "  TS: {Timestamp}, O: {Open}, H: {High}, L: {Low}, C: {Close}, V: {Volume}, Ret: {Return:F6}, V5: {Vol5:F6}, V15: {Vol15:F6}, V60: {Vol60:F6}, SP: {SP:F6}, LP: {LP:F6}",
                                item.Timestamp, item.Open, item.High, item.Low, item.Close, item.Volume, item.LogarithmicReturn, item.RollingVol5, item.RollingVol15, item.RollingVol60, item.ShortPanicScore, item.LongPanicScore);
                        }
                        break;

                    case List<VolatilitySilver> silverData:
                        foreach (var item in silverData)
                        {
                            _logger.LogInformation(
                                "  TS: {Timestamp}, O: {Open}, H: {High}, L: {Low}, C: {Close}, V: {Volume}, Ret: {Return:F6}, V5: {Vol5:F6}, V15: {Vol15:F6}, V60: {Vol60:F6}, SP: {SP:F6}, LP: {LP:F6}",
                                item.Timestamp, item.Open, item.High, item.Low, item.Close, item.Volume, item.LogarithmicReturn, item.RollingVol5, item.RollingVol15, item.RollingVol60, item.ShortPanicScore, item.LongPanicScore);
                        }
                        break;

                    case List<VolatilityNaturalGas> ngData:
                        foreach (var item in ngData)
                        {
                            _logger.LogInformation(
                                "  TS: {Timestamp}, O: {Open}, H: {High}, L: {Low}, C: {Close}, V: {Volume}, Ret: {Return:F6}, V5: {Vol5:F6}, V15: {Vol15:F6}, V60: {Vol60:F6}, SP: {SP:F6}, LP: {LP:F6}",
                                item.Timestamp, item.Open, item.High, item.Low, item.Close, item.Volume, item.LogarithmicReturn, item.RollingVol5, item.RollingVol15, item.RollingVol60, item.ShortPanicScore, item.LongPanicScore);
                        }
                        break;

                    case List<VolatilityOil> clData:
                        foreach (var item in clData)
                        {
                            _logger.LogInformation(
                                "  TS: {Timestamp}, O: {Open}, H: {High}, L: {Low}, C: {Close}, V: {Volume}, Ret: {Return:F6}, V5: {Vol5:F6}, V15: {Vol15:F6}, V60: {Vol60:F6}, SP: {SP:F6}, LP: {LP:F6}",
                                item.Timestamp, item.Open, item.High, item.Low, item.Close, item.Volume, item.LogarithmicReturn, item.RollingVol5, item.RollingVol15, item.RollingVol60, item.ShortPanicScore, item.LongPanicScore);
                        }
                        break;
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
        {
            return null;
        }

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

        var daily = VolatilityDaily.Create(
            day: day,
            open: open,
            close: close,
            high: high,
            low: low,
            avg: avg,
            volumeSum: volumeSum,
            rangePct: rangePct);

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

    private sealed record DailySummary(decimal High, decimal Low);
}
