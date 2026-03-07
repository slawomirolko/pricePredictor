using System.Globalization;

namespace PricePredicator.Infrastructure.Gold;

public class StooqGoldPriceService : IGoldPriceService
{
    private readonly HttpClient _httpClient;

    public StooqGoldPriceService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<GoldPricePoint>> GetGoldPricesAsync(
        int days,
        CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync("q/d/l/?s=xauusd&i=d", cancellationToken);
        response.EnsureSuccessStatusCode();

        var csv = await response.Content.ReadAsStringAsync(cancellationToken);
        var points = ParseCsv(csv);

        if (points.Count == 0)
        {
            return Array.Empty<GoldPricePoint>();
        }

        var requestedDays = Math.Clamp(days, 1, 365);
        return points
            .TakeLast(requestedDays)
            .ToArray();
    }

    private static List<GoldPricePoint> ParseCsv(string csv)
    {
        var points = new List<GoldPricePoint>();
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var line in lines.Skip(1))
        {
            var parts = line.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length < 6)
            {
                continue;
            }

            if (!DateOnly.TryParseExact(parts[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                continue;
            }

            if (!decimal.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var open) ||
                !decimal.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var high) ||
                !decimal.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var low) ||
                !decimal.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var close))
            {
                continue;
            }

            long? volume = null;
            if (parts.Length > 5 && long.TryParse(parts[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedVolume))
            {
                volume = parsedVolume;
            }

            points.Add(new GoldPricePoint(date, open, high, low, close, volume));
        }

        return points;
    }
}
