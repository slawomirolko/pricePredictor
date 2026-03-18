using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using PricePredictor.Application.Finance;
using PricePredictor.Application.Finance.Interfaces;

namespace PricePredictor.Infrastructure.Finance;

/// <summary>
/// Typed HTTP Client for Yahoo Finance API
/// Uses 1-minute intervals for intraday data
/// </summary>
public class YahooFinanceClient : ICommodityMarketDataClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<YahooFinanceClient> _logger;

    public YahooFinanceClient(HttpClient httpClient, ILogger<YahooFinanceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Fetch 1-minute intraday data for a symbol
    /// </summary>
    public async Task<List<CandlePoint>> GetIntradayDataAsync(
        string symbol,
        string interval = "1m",
        string range = "1d",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"v8/finance/chart/{symbol}?interval={interval}&range={range}";
            _logger.LogDebug("Fetching Yahoo Finance data: {Url}", url);

            var response = await _httpClient.GetFromJsonAsync<YahooFinanceResponse>(url, cancellationToken);
            var chart = response?.Chart;

            if (chart is null)
            {
                throw new InvalidOperationException($"Yahoo Finance returned null chart payload for symbol={symbol}, interval={interval}, range={range}.");
            }

            if (chart.Error != null)
            {
                throw new InvalidOperationException(
                    $"Yahoo Finance error for symbol={symbol}, interval={interval}, range={range}, code={chart.Error.Code}, description={chart.Error.Description}.");
            }

            if (chart.Result is null || chart.Result.Length == 0)
            {
                throw new InvalidOperationException($"Yahoo Finance returned no result payload for symbol={symbol}, interval={interval}, range={range}.");
            }

            var result = chart.Result[0];
            if (result is null)
            {
                throw new InvalidOperationException($"Yahoo Finance returned a null result item for symbol={symbol}, interval={interval}, range={range}.");
            }

            var candles = ParseCandles(result);
            if (candles.Count == 0)
            {
                throw new InvalidOperationException($"Yahoo Finance returned zero parsed candles for symbol={symbol}, interval={interval}, range={range}.");
            }

            _logger.LogInformation("Fetched {Count} candles for {Symbol}", candles.Count, symbol);

            return candles;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching data for {Symbol}", symbol);
            throw new InvalidOperationException($"HTTP error fetching Yahoo Finance data for symbol={symbol}, interval={interval}, range={range}.", ex);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Yahoo Finance data for {Symbol}", symbol);
            throw new InvalidOperationException($"Error fetching Yahoo Finance data for symbol={symbol}, interval={interval}, range={range}.", ex);
        }
    }

    public async Task<CommodityLatestMarketDataDto> GetLatestAsync(
        string symbol,
        string interval = "1m",
        string range = "1d",
        CancellationToken cancellationToken = default)
    {
        var candles = await GetIntradayDataAsync(symbol, interval, range, cancellationToken);
        var latest = candles.OrderByDescending(x => x.Timestamp).First();

        return new CommodityLatestMarketDataDto(
            symbol,
            latest.Timestamp,
            latest.Open,
            latest.High,
            latest.Low,
            latest.Close,
            latest.Volume);
    }

    private static List<CandlePoint> ParseCandles(ChartResult result)
    {
        var candles = new List<CandlePoint>();

        var timestamps = result.Timestamp;
        var quote = result.Indicators?.Quote?.FirstOrDefault();

        if (timestamps is null || quote is null)
        {
            return candles;
        }

        var opens = quote.Open ?? Array.Empty<decimal?>();
        var highs = quote.High ?? Array.Empty<decimal?>();
        var lows = quote.Low ?? Array.Empty<decimal?>();
        var closes = quote.Close ?? Array.Empty<decimal?>();
        var volumes = quote.Volume ?? Array.Empty<long?>();

        // Volume can be missing for some symbols; build candles from OHLC + timestamp only.
        var upperBound = new[] { timestamps.Length, opens.Length, highs.Length, lows.Length, closes.Length }.Min();

        for (int i = 0; i < upperBound; i++)
        {
            // Skip if any OHLC value is null
            if (opens[i] == null || highs[i] == null || lows[i] == null || closes[i] == null)
            {
                continue;
            }

            var timestamp = UnixTimeStampToDateTime(timestamps[i]);

            candles.Add(new CandlePoint
            {
                Timestamp = timestamp,
                Open = opens[i]!.Value,
                High = highs[i]!.Value,
                Low = lows[i]!.Value,
                Close = closes[i]!.Value,
                Volume = i < volumes.Length ? volumes[i] : null
            });
        }

        return candles;
    }

    private static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimeStamp).ToUniversalTime();
        return dateTime;
    }
}

