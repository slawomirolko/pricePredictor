using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OllamaSharp;
using PricePredicator.App.GoldNews;

namespace PricePredicator.App;

public class GoldNewsBackgroundService : BackgroundService
{
    private readonly ILogger<GoldNewsBackgroundService> _logger;
    private readonly IGoldNewsClient _client;
    private readonly IOllamaApiClient _ollama;
    private readonly GoldNewsSettings _settings;
    private readonly HashSet<string> _seen = new();

    public GoldNewsBackgroundService(
        ILogger<GoldNewsBackgroundService> logger,
        IGoldNewsClient client,
        IOllamaApiClient ollama,
        IOptions<GoldNewsSettings> settings)
    {
        _logger = logger;
        _client = client;
        _ollama = ollama;
        _settings = settings.Value;
        _ollama.SelectedModel = _settings.OllamaModel;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Gold News Background Service started.");

        try
        {
            await CreateCollectionIfNotExists(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure Qdrant collection exists at {Url}.", _settings.QdrantUrl);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Checking news at {Time}", DateTime.UtcNow);

                var xml = await TryGetRssXmlAsync(stoppingToken);
                if (string.IsNullOrWhiteSpace(xml))
                {
                    _logger.LogWarning("No RSS content fetched (all configured URLs failed).");
                    continue;
                }

                var doc = XDocument.Parse(xml);

                foreach (var (title, link, desc) in ExtractItems(doc))
                {
                    if (string.IsNullOrEmpty(link) || _seen.Contains(link))
                        continue;

                    _seen.Add(link);

                    var content = $"{title}. {desc}";

                    var responseEmbed = await _ollama.EmbedAsync(content, stoppingToken);

                    await StoreInQdrant(link, content, responseEmbed.Embeddings[0], stoppingToken);
                    _logger.LogInformation("Stored: {Title}", title);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing RSS news");
            }

            await Task.Delay(TimeSpan.FromMinutes(3), stoppingToken);
        }

        _logger.LogInformation("Gold News Background Service stopping.");
    }

    private async Task CreateCollectionIfNotExists(CancellationToken stoppingToken)
    {
        await _client.EnsureQdrantCollectionAsync(_settings.QdrantUrl, stoppingToken);
        _logger.LogInformation("Collection 'gold_news' ensure requested.");
    }

    private async Task StoreInQdrant(string id, string text, IEnumerable<float> vector, CancellationToken stoppingToken)
    {
        var body = new
        {
            points = new[]
            {
                new
                {
                    id = Guid.NewGuid().ToString(),
                    vector,
                    payload = new
                    {
                        url = id,
                        content = text,
                        timestamp = DateTime.UtcNow
                    }
                }
            }
        };

        try
        {
            await _client.UpsertPointsAsync(_settings.QdrantUrl, "gold_news", body, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store point in Qdrant");
        }
    }

    private IReadOnlyList<string> GetRssUrls()
    {
        if (_settings.RssUrls is { Length: > 0 })
        {
            return _settings.RssUrls
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Select(u => u.Trim())
                .ToArray();
        }

        return string.IsNullOrWhiteSpace(_settings.RssUrl)
            ? Array.Empty<string>()
            : new[] { _settings.RssUrl.Trim() };
    }

    private async Task<string?> TryGetRssXmlAsync(CancellationToken stoppingToken)
    {
        var urls = GetRssUrls();
        foreach (var url in urls)
        {
            try
            {
                _logger.LogInformation("Fetching RSS from {RssUrl}", url);
                return await _client.GetRssXmlAsync(url, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch RSS from {RssUrl}", url);
            }
        }

        return null;
    }

    private static IEnumerable<(string Title, string Link, string Description)> ExtractItems(XDocument doc)
    {
        // RSS 2.0
        var rssItems = doc.Descendants("item")
            .Select(item => (
                Title: item.Element("title")?.Value ?? string.Empty,
                Link: item.Element("link")?.Value ?? string.Empty,
                Description: item.Element("description")?.Value ?? string.Empty));

        foreach (var i in rssItems)
            yield return i;

        // Atom
        XNamespace atom = "http://www.w3.org/2005/Atom";
        var atomEntries = doc.Descendants(atom + "entry")
            .Select(entry => (
                Title: entry.Element(atom + "title")?.Value ?? string.Empty,
                Link: entry.Elements(atom + "link")
                          .Select(l => l.Attribute("href")?.Value)
                          .FirstOrDefault(h => !string.IsNullOrWhiteSpace(h)) ??
                      entry.Element(atom + "link")?.Attribute("href")?.Value ?? string.Empty,
                Description: entry.Element(atom + "summary")?.Value ?? entry.Element(atom + "content")?.Value ?? string.Empty));

        foreach (var e in atomEntries)
            yield return e;
    }
}
