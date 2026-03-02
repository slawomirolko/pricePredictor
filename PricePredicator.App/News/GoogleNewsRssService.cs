using System.Net;
using System.Xml.Linq;

namespace PricePredicator.App.News;

public class GoogleNewsRssService : INewsService
{
    private readonly HttpClient _httpClient;

    public GoogleNewsRssService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<NewsItem>> GetGoldNewsAsync(int count, CancellationToken cancellationToken)
    {
        var requested = Math.Clamp(count, 1, 100);
        var response = await _httpClient.GetAsync("rss/search?q=gold+price+XAUUSD&hl=en-US&gl=US&ceid=US:en", cancellationToken);
        response.EnsureSuccessStatusCode();

        var xml = await response.Content.ReadAsStringAsync(cancellationToken);
        return Parse(xml)
            .Take(requested)
            .ToArray();
    }

    private static IEnumerable<NewsItem> Parse(string xml)
    {
        var doc = XDocument.Parse(xml);
        var items = doc.Descendants("item");

        foreach (var item in items)
        {
            var title = WebUtility.HtmlDecode(item.Element("title")?.Value ?? string.Empty).Trim();
            var link = item.Element("link")?.Value?.Trim() ?? string.Empty;
            var pubDateRaw = item.Element("pubDate")?.Value;
            var source = WebUtility.HtmlDecode(item.Element("source")?.Value ?? "Unknown").Trim();

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(link))
            {
                continue;
            }

            DateTimeOffset? published = null;
            if (DateTimeOffset.TryParse(pubDateRaw, out var parsed))
            {
                published = parsed.ToUniversalTime();
            }

            yield return new NewsItem(
                Title: title,
                Link: link,
                PublishedAtUtc: published,
                Source: source
            );
        }
    }
}
