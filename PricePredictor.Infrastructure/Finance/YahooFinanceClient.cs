using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace PricePredictor.Infrastructure.Finance;

/// <summary>
/// Typed HTTP Client for Yahoo Finance API
/// Uses 1-minute intervals for intraday data
/// </summary>
public class YahooFinanceClient
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

            if (response?.Chart?.Result?.Length == 0 || response?.Chart?.Error != null)
            {
                if (response?.Chart?.Error != null)
                {
                    _logger.LogError("Yahoo Finance error for {Symbol}: {Code} - {Description}",
                        symbol, response.Chart.Error.Code, response.Chart.Error.Description);
                }
                else
                {
                    _logger.LogWarning("No data returned for symbol {Symbol}", symbol);
                }
                return new List<CandlePoint>();
            }

            var result = response?.Chart?.Result?[0];
            var candles = ParseCandles(result, symbol);
            _logger.LogInformation("Fetched {Count} candles for {Symbol}", candles.Count, symbol);

            return candles;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching data for {Symbol}", symbol);
            return new List<CandlePoint>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Yahoo Finance data for {Symbol}", symbol);
            return new List<CandlePoint>();
        }
    }

    private static List<CandlePoint> ParseCandles(ChartResult? result, string symbol)
    {
        var candles = new List<CandlePoint>();

        if (result?.Timestamp == null || result.Indicators?.Quote?.Length == 0)
        {
            return candles;
        }

        var timestamps = result.Timestamp;
        var quote = result.Indicators.Quote[0];
        var opens = quote.Open ?? Array.Empty<decimal?>();
        var highs = quote.High ?? Array.Empty<decimal?>();
        var lows = quote.Low ?? Array.Empty<decimal?>();
        var closes = quote.Close ?? Array.Empty<decimal?>();
        var volumes = quote.Volume ?? Array.Empty<long?>();

        for (int i = 0; i < timestamps.Length; i++)
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
                Volume = volumes[i]
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





