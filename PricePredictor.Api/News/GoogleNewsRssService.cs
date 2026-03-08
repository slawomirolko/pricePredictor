using System.Net;
using System.Xml.Linq;
using PricePredictor.Infrastructure.News;

namespace PricePredictor.Api.News;

public class GoogleNewsRssService : INewsService
{
    private readonly IGoogleNewsRssClient _rssClient;

    public GoogleNewsRssService(IGoogleNewsRssClient rssClient)
    {
        _rssClient = rssClient;
    }

    public async Task<IReadOnlyList<NewsItem>> GetGoldNewsAsync(int count, CancellationToken cancellationToken)
    {
        var requested = Math.Clamp(count, 1, 100);
        var xml = await _rssClient.GetGoldNewsRssAsync(cancellationToken);

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

