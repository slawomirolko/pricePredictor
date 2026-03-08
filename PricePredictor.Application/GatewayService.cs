using System.Text.Json;
using PricePredictor.Api.News;
using PricePredictor.Api.Weather;
using PricePredictor.Infrastructure.Gold;

namespace PricePredictor.Api.Gateway;

public class GatewayService : IGatewayService
{
    private readonly IWeatherService _weatherService;
    private readonly IGoldPriceService _goldPriceService;
    private readonly INewsService _newsService;

    public GatewayService(
        IWeatherService weatherService,
        IGoldPriceService goldPriceService,
        INewsService newsService)
    {
        _weatherService = weatherService;
        _goldPriceService = goldPriceService;
        _newsService = newsService;
    }

    public async Task<string> HandleAsync(string payload, CancellationToken cancellationToken)
    {
        if (payload.StartsWith("gold-prices", StringComparison.OrdinalIgnoreCase))
        {
            var days = ExtractDays(payload);
            var prices = await _goldPriceService.GetGoldPricesAsync(days, cancellationToken);

            return JsonSerializer.Serialize(new
            {
                symbol = "XAUUSD",
                granularity = "daily",
                requestedDays = days,
                returnedPoints = prices.Count,
                prices
            });
        }

        if (payload.StartsWith("gold-news", StringComparison.OrdinalIgnoreCase))
        {
            var requestedItems = ExtractCount(payload, defaultValue: 20, maxValue: 100);
            var news = await _newsService.GetGoldNewsAsync(requestedItems, cancellationToken);

            return JsonSerializer.Serialize(new
            {
                topic = "gold",
                requestedItems,
                returnedItems = news.Count,
                generatedAtUtc = DateTimeOffset.UtcNow,
                articles = news
            });
        }

        // Example of using existing services
        var weather = await _weatherService.GetCitiesWeatherAsync();

        return string.Join(Environment.NewLine,
            weather.Select(x => $"{x.City} Max temp: {x.MaxTemp} Min temp: {x.MinTemp}")
        );
    }

    private static int ExtractDays(string payload)
    {
        return ExtractCount(payload, defaultValue: 30, maxValue: 365);
    }

    private static int ExtractCount(string payload, int defaultValue, int maxValue)
    {
        var parts = payload.Split(':', 2, StringSplitOptions.TrimEntries);
        if (parts.Length == 2 && int.TryParse(parts[1], out var parsed))
        {
            return Math.Clamp(parsed, 1, maxValue);
        }

        return defaultValue;
    }
}

