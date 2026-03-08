using System.Net;
using System.Text.Json;
using System.Xml.Linq;

namespace PricePredictor.Integration.Tests;

public class ExternalResourceAvailabilityTests
{
    private static readonly HttpClient Http = CreateHttpClient();

    [Fact]
    public async Task OpenMeteo_ShouldReturnReadableForecast()
    {
        var url = "https://api.open-meteo.com/v1/forecast?latitude=40.7128&longitude=-74.0060&daily=temperature_2m_max,temperature_2m_min,weathercode&timezone=auto";

        using var response = await Http.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("daily", out var daily));
        Assert.True(daily.TryGetProperty("temperature_2m_max", out var maxTemps));
        Assert.True(maxTemps.GetArrayLength() > 0, "temperature_2m_max should have at least one value");
    }

    [Fact]
    public async Task StooqGoldCsv_ShouldReturnReadableData()
    {
        var url = "https://stooq.com/q/d/l/?s=xauusd&i=d";

        using var response = await Http.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var csv = await response.Content.ReadAsStringAsync();
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        Assert.True(lines.Length > 1, "CSV should contain header and at least one data row");
        Assert.Contains("Date", lines[0]);

        var parts = lines[1].Split(',', StringSplitOptions.TrimEntries);
        Assert.True(parts.Length >= 5, "CSV data row should have at least 5 columns");
        Assert.True(DateOnly.TryParse(parts[0], out _), "First column should be a date");
    }

    [Fact]
    public async Task GoogleNewsRss_ShouldReturnReadableData()
    {
        var url = "https://news.google.com/rss/search?q=gold+price+XAUUSD&hl=en-US&gl=US&ceid=US:en";

        using var response = await Http.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var xml = await response.Content.ReadAsStringAsync();
        var doc = XDocument.Parse(xml);
        var items = doc.Descendants("item").ToList();

        Assert.True(items.Count > 0, "RSS should contain at least one item");

        var first = items[0];
        var title = first.Element("title")?.Value ?? string.Empty;
        var link = first.Element("link")?.Value ?? string.Empty;

        Assert.False(string.IsNullOrWhiteSpace(title), "RSS item title should not be empty");
        Assert.False(string.IsNullOrWhiteSpace(link), "RSS item link should not be empty");
    }

    [Fact]
    public async Task YahooFinance_ShouldReturnReadableData()
    {
        var url = "https://query1.finance.yahoo.com/v8/finance/chart/GC=F?interval=1m&range=1d";

        using var response = await Http.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("chart", out var chart));
        Assert.True(chart.TryGetProperty("result", out var result));
        Assert.True(result.ValueKind == JsonValueKind.Array && result.GetArrayLength() > 0, "chart.result should contain data");

        var first = result[0];
        Assert.True(first.TryGetProperty("timestamp", out var timestamps));
        Assert.True(timestamps.ValueKind == JsonValueKind.Array && timestamps.GetArrayLength() > 0, "timestamp should contain values");
    }

    [Theory]
    [MemberData(nameof(RequestedPublisherFeeds))]
    public async Task RequestedPublisherFeed_ShouldReturnReadableData(string publisher, string url)
    {
        using var response = await Http.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var xml = await response.Content.ReadAsStringAsync();
        var doc = XDocument.Parse(xml);
        var items = doc.Descendants("item").ToList();

        Assert.True(items.Count > 0, $"{publisher} feed should contain at least one item");

        var first = items[0];
        var title = first.Element("title")?.Value ?? string.Empty;
        var link = first.Element("link")?.Value ?? string.Empty;
        var source = first.Element("source")?.Value ?? string.Empty;

        Assert.False(string.IsNullOrWhiteSpace(title), $"{publisher} title should not be empty");
        Assert.False(string.IsNullOrWhiteSpace(link), $"{publisher} link should not be empty");
        Assert.False(string.IsNullOrWhiteSpace(source), $"{publisher} source should not be empty");
    }

    public static IEnumerable<object[]> RequestedPublisherFeeds()
    {
        yield return new object[]
        {
            "Reuters",
            "https://news.google.com/rss/search?q=gold+war+geopolitics+source:Reuters&hl=en-US&gl=US&ceid=US:en"
        };

        yield return new object[]
        {
            "Financial Times",
            "https://news.google.com/rss/search?q=gold+war+geopolitics+source:Financial+Times&hl=en-GB&gl=GB&ceid=GB:en"
        };

        yield return new object[]
        {
            "Kitco News",
            "https://news.google.com/rss/search?q=gold+war+geopolitics+source:Kitco+News&hl=en-US&gl=US&ceid=US:en"
        };

        yield return new object[]
        {
            "CNBC",
            "https://news.google.com/rss/search?q=gold+war+geopolitics+source:CNBC&hl=en-US&gl=US&ceid=US:en"
        };

        yield return new object[]
        {
            "Bloomberg",
            "https://news.google.com/rss/search?q=gold+war+geopolitics+source:Bloomberg&hl=en-US&gl=US&ceid=US:en"
        };

        yield return new object[]
        {
            "The Wall Street Journal",
            "https://news.google.com/rss/search?q=gold+war+geopolitics+source:The+Wall+Street+Journal&hl=en-US&gl=US&ceid=US:en"
        };

        yield return new object[]
        {
            "South China Morning Post",
            "https://news.google.com/rss/search?q=gold+war+geopolitics+source:South+China+Morning+Post&hl=en&gl=CN&ceid=CN:en"
        };

        yield return new object[]
        {
            "Caixin",
            "https://news.google.com/rss/search?q=gold+war+geopolitics+source:Caixin+Global&hl=en&gl=CN&ceid=CN:en"
        };

        yield return new object[]
        {
            "Xinhua News Agency",
            "https://news.google.com/rss/search?q=gold+war+geopolitics+source:Xinhua&hl=en&gl=CN&ceid=CN:en"
        };
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(20)
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0 Safari/537.36");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json, application/xml, text/xml, */*");

        return client;
    }
}
