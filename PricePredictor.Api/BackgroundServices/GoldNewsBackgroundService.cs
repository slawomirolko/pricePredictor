using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using OllamaSharp;
using PricePredictor.Application.Data;
using PricePredictor.Application.Finance.Interfaces;
using GoldNewsSettings = PricePredictor.Infrastructure.GoldNewsSettings;

namespace PricePredictor.Api.BackgroundServices;

public class GoldNewsBackgroundService : BackgroundService
{
    private const int MaxSeenLinks = 2000;
    private static readonly string[] RelevanceKeywords =
    [
        "gold",
        "bullion",
        "xau",
        "xauusd",
        "precious metal",
        "safe haven",
        "geopolitic",
        "war",
        "conflict",
        "sanction",
        "tariff",
        "central bank",
        "fed",
        "ecb",
        "pbo",
        "boe",
        "inflation",
        "interest rate",
        "rate hike",
        "rate cut",
        "recession",
        "crisis"
    ];

    private readonly ILogger<GoldNewsBackgroundService> _logger;
    private readonly IGoldNewsClient _client;
    private readonly IGoldNewsEmbeddingRepository _embeddingRepository;
    private readonly IOllamaApiClient _ollama;
    private readonly GoldNewsSettings _settings;
    private readonly HashSet<string> _seen = new();
    private readonly Queue<string> _seenOrder = new();

    public GoldNewsBackgroundService(
        ILogger<GoldNewsBackgroundService> logger,
        IGoldNewsClient client,
        IGoldNewsEmbeddingRepository embeddingRepository,
        [FromKeyedServices("LocalOllama")] IOllamaApiClient ollama,
        IOptions<GoldNewsSettings> settings)
    {
        _logger = logger;
        _client = client;
        _embeddingRepository = embeddingRepository;
        _ollama = ollama;
        _settings = settings.Value;
        _ollama.SelectedModel = _settings.LocalOllamaModel;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Gold News Background Service started.");

        try
        {
            await EnsureVectorStorageAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure pgvector storage for gold news embeddings.");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Checking news at {Time}", DateTime.UtcNow);

                var feeds = await GetRssFeedsAsync(stoppingToken);
                if (feeds.Count == 0)
                {
                    _logger.LogWarning("No RSS content fetched (all configured URLs failed).");
                }
                else
                {
                    foreach (var (url, xml) in feeds)
                    {
                        XDocument doc;
                        try
                        {
                            doc = XDocument.Parse(xml);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse RSS XML from {RssUrl}", url);
                            continue;
                        }

                        foreach (var (title, link, desc) in ExtractItems(doc))
                        {
                            var normalizedLink = link?.Trim() ?? string.Empty;
                            if (string.IsNullOrWhiteSpace(normalizedLink))
                                continue;

                            if (!TryMarkSeen(normalizedLink))
                                continue;

                            var normalizedTitle = NormalizeText(title);
                            var normalizedDesc = NormalizeText(desc);

                            if (!IsRelevant(normalizedTitle, normalizedDesc))
                                continue;

                            // ALWAYS use RSS description as base content (reliable)
                            var content = BuildContent(normalizedTitle, normalizedDesc, null);
                            
                            // OPTIONALLY try to enhance with article content (non-blocking)
                            try
                            {
                                var articleContent = await _client.FetchArticleContentAsync(normalizedLink, normalizedTitle, stoppingToken);
                                if (!string.IsNullOrWhiteSpace(articleContent))
                                {
                                    // Only append if we got meaningful article content
                                    content = content + ". " + articleContent;
                                    _logger.LogDebug("Enhanced {Title} with article content ({Chars} chars)", normalizedTitle, articleContent.Length);
                                }
                                else
                                {
                                    _logger.LogDebug("No article content extracted for {Title}, using RSS only", normalizedTitle);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogDebug(ex, "Article fetch failed for {Title}, continuing with RSS description", normalizedTitle);
                            }

                            if (string.IsNullOrWhiteSpace(content))
                                continue;

                            var responseEmbed = await _ollama.EmbedAsync(content, stoppingToken);

                            await StoreEmbeddingAsync(normalizedLink, content, responseEmbed.Embeddings[0], stoppingToken);
                            _logger.LogInformation("Stored: {Title} ({TotalLength} chars)", normalizedTitle, content.Length);
                        }
                    }
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

    private async Task EnsureVectorStorageAsync(CancellationToken stoppingToken)
    {
        await _embeddingRepository.EnsureStorageAsync(_settings.EmbeddingDimensions, stoppingToken);
        _logger.LogInformation("PostgreSQL pgvector storage ensure requested.");
    }

    private async Task StoreEmbeddingAsync(string id, string text, IEnumerable<float> vector, CancellationToken stoppingToken)
    {
        try
        {
            await _embeddingRepository.UpsertAsync(
                id,
                text,
                vector as IReadOnlyList<float> ?? vector.ToArray(),
                _settings.EmbeddingDimensions,
                stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store gold news embedding in PostgreSQL pgvector.");
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

    private async Task<IReadOnlyList<(string Url, string Xml)>> GetRssFeedsAsync(CancellationToken stoppingToken)
    {
        var urls = GetRssUrls();
        var feeds = new List<(string Url, string Xml)>();

        foreach (var url in urls)
        {
            try
            {
                _logger.LogInformation("Fetching RSS from {RssUrl}", url);
                var xml = await _client.GetRssXmlAsync(url, stoppingToken);
                if (!string.IsNullOrWhiteSpace(xml))
                {
                    feeds.Add((url, xml));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch RSS from {RssUrl}", url);
            }
        }

        return feeds;
    }

    private bool TryMarkSeen(string link)
    {
        if (_seen.Contains(link))
        {
            return false;
        }

        _seen.Add(link);
        _seenOrder.Enqueue(link);

        while (_seenOrder.Count > MaxSeenLinks)
        {
            var toRemove = _seenOrder.Dequeue();
            _seen.Remove(toRemove);
        }

        return true;
    }

    private static string NormalizeText(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var decoded = WebUtility.HtmlDecode(input);
        var withoutTags = Regex.Replace(decoded, "<.*?>", string.Empty);
        return withoutTags.Replace("\r", " ").Replace("\n", " ").Trim();
    }

    private static bool IsRelevant(string title, string description)
    {
        var text = $"{title} {description}".ToLowerInvariant();
        return RelevanceKeywords.Any(k => text.Contains(k));
    }

    private static string BuildContent(string title, string description, string? articleText)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(title))
            parts.Add(title.Trim());

        if (!string.IsNullOrWhiteSpace(description))
            parts.Add(description.Trim());

        if (!string.IsNullOrWhiteSpace(articleText))
            parts.Add(articleText.Trim());

        return parts.Count == 0 ? string.Empty : string.Join(". ", parts);
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

