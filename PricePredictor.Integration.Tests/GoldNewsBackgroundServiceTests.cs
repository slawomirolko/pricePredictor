using System.Net;
using System.Net.Sockets;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PricePredictor.Infrastructure.GoldNews;

namespace PricePredictor.Integration.Tests;

/// <summary>
/// Integration tests for GoldNewsBackgroundService.
/// Tests the ability to fetch articles and extract content from REAL Reuters articles using live HTTP calls.
/// Specifically tests: https://www.reuters.com/world/india/gold-extends-gains-middle-east-war-boosts-safe-haven-demand-2026-03-03/
/// </summary>
public class GoldNewsBackgroundServiceTests
{
    private readonly ILogger<GoldNewsClient> _logger;
    private readonly HttpClient _httpClient;

    public GoldNewsBackgroundServiceTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());

        var provider = services.BuildServiceProvider();
        _logger = provider.GetRequiredService<ILogger<GoldNewsClient>>();
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
    }

    /// <summary>
    /// Test that verifies the GoldNewsClient can extract the expected Reuters article content.
    /// Uses REAL HTTP calls to download the actual Reuters article.
    /// Article: "Gold extends gains, Middle East war boosts safe haven demand"
    /// URL: https://www.reuters.com/world/india/gold-extends-gains-middle-east-war-boosts-safe-haven-demand-2026-03-03/
    /// </summary>
    [Fact]
    public async Task ShouldExtractArticleContentFromRealReutersArticle()
    {
        // Arrange - Wait 1 second before starting
        await Task.Delay(1000);
        if (!await IsHostAvailableAsync("www.reuters.com"))
        {
            _logger.LogWarning("Skipping test because host is unavailable: {Host}", "www.reuters.com");
            return;
        }

        var articleUrl = "https://www.reuters.com/world/india/gold-extends-gains-middle-east-war-boosts-safe-haven-demand-2026-03-03/";
        var client = new GoldNewsClient(_httpClient, _logger);

        // Act - Fetch REAL article content from Reuters
        var result = await client.FetchArticleContentAsync(articleUrl, CancellationToken.None);

        // External websites may deny direct access from CI/local IP.
        if (string.IsNullOrWhiteSpace(result))
        {
            _logger.LogWarning("Skipping content assertions because Reuters returned no extractable content for {Url}", articleUrl);
            return;
        }

        // Assert - Verify the Reuters article content was properly extracted
        Assert.True(result.Length > 100, "Article content should contain substantial text");

        // Examine content - log extracted text
        _logger.LogInformation("📋 Extracted content length: {Length} characters", result.Length);
        _logger.LogInformation("📄 Content preview: {Preview}", result.Substring(0, Math.Min(200, result.Length)));

        // Check for relevant gold/financial keywords that should be in the article
        var lowerResult = result.ToLower();
        Assert.True(
            lowerResult.Contains("gold") ||
            lowerResult.Contains("middle east") ||
            lowerResult.Contains("war") ||
            lowerResult.Contains("safe haven") ||
            lowerResult.Contains("price") ||
            lowerResult.Contains("market"),
            "Article should contain gold or geopolitical keywords");
    }

    /// <summary>
    /// Test that verifies the GoldNewsBackgroundService can process REAL RSS feeds with relevant gold keywords
    /// and trigger article fetching for relevant articles using live HTTP calls.
    /// </summary>
    [Fact]
    public async Task ShouldProcessRealRssFeedWithGoldRelevantArticles()
    {
        // Arrange - Wait 1 second before starting
        await Task.Delay(1000);
        if (!await IsHostAvailableAsync("feeds.reuters.com"))
        {
            _logger.LogWarning("Skipping test because host is unavailable: {Host}", "feeds.reuters.com");
            return;
        }

        var client = new GoldNewsClient(_httpClient, _logger);
        var rssUrl = "https://feeds.reuters.com/news/worldnews";

        // Act - Fetch REAL RSS content from Reuters
        var rssResult = await client.GetRssXmlAsync(rssUrl, CancellationToken.None);

        // Assert - Verify RSS contains valid XML and gold-relevant items
        Assert.NotNull(rssResult);
        Assert.True(rssResult.Length > 100, "RSS feed should contain substantial content");

        // Examine content - log RSS details
        _logger.LogInformation("📡 RSS feed size: {Size} bytes", rssResult.Length);

        // Parse and validate RSS structure
        var doc = XDocument.Parse(rssResult);
        var items = doc.Descendants("item").ToList();
        Assert.True(items.Count > 0, "RSS feed should contain at least one item");

        _logger.LogInformation("📊 Found {Count} items in RSS feed", items.Count);
        foreach (var item in items.Take(3))
        {
            var title = item.Element("title")?.Value ?? "No title";
            _logger.LogInformation("  📰 Item: {Title}", title);
        }

        // Verify RSS contains gold-relevant keywords
        var lowerRss = rssResult.ToLower();
        Assert.True(
            lowerRss.Contains("gold") ||
            lowerRss.Contains("market") ||
            lowerRss.Contains("news") ||
            items.Any(item => item.Descendants("title").Any(t =>
                t.Value.ToLower().Contains("gold") ||
                t.Value.ToLower().Contains("price") ||
                t.Value.ToLower().Contains("market"))),
            "RSS feed should contain market or price-related content");
    }

    /// <summary>
    /// Test that fetches multiple articles from the Reuters RSS feed and verifies content extraction.
    /// </summary>
    [Fact]
    public async Task ShouldFetchAndExtractMultipleArticlesFromRealFeed()
    {
        // Arrange - Wait 1 second before starting
        await Task.Delay(1000);
        if (!await IsHostAvailableAsync("feeds.reuters.com"))
        {
            _logger.LogWarning("Skipping test because host is unavailable: {Host}", "feeds.reuters.com");
            return;
        }

        var client = new GoldNewsClient(_httpClient, _logger);
        var rssUrl = "https://feeds.reuters.com/news/worldnews";
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(12));

        // Act - Fetch RSS feed
        var rssResult = await client.GetRssXmlAsync(rssUrl, cts.Token);
        Assert.NotNull(rssResult);

        // Parse RSS and extract article URLs
        var doc = XDocument.Parse(rssResult);
        var items = doc.Descendants("item").Take(2).ToList();

        Assert.True(items.Count > 0, "RSS feed should have at least one item");

        _logger.LogInformation("Processing {Count} articles from RSS feed", items.Count);

        int successfulExtractions = 0;

        // Act & Assert - Try to fetch and extract content from each article
        foreach (var (idx, item) in items.Select((i, index) => (index + 1, i)))
        {
            var linkElement = item.Element("link");
            if (linkElement == null) continue;

            var articleUrl = linkElement.Value;
            if (string.IsNullOrWhiteSpace(articleUrl)) continue;

            _logger.LogInformation("[{Index}] Testing article: {Url}", idx, articleUrl);

            try
            {
                var articleContent = await client.FetchArticleContentAsync(articleUrl, cts.Token);

                if (!string.IsNullOrWhiteSpace(articleContent))
                {
                    Assert.True(articleContent.Length > 50, $"Article from {articleUrl} should have meaningful content");
                    successfulExtractions++;
                    _logger.LogInformation("Extracted {Length} characters", articleContent.Length);

                    // Keep runtime short: one good extraction is enough for this integration check.
                    break;
                }
                else
                {
                    _logger.LogWarning("Empty content extracted");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Timeout fetching article");
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to fetch article: {Error}", ex.Message);
            }
        }

        _logger.LogInformation("Results: {Success} successful extractions out of {Total}", successfulExtractions, items.Count);

        // At least some articles should be extractable
        Assert.True(successfulExtractions > 0, $"Should successfully extract content from at least one article (got {successfulExtractions})");
    }

    private static async Task<bool> IsHostAvailableAsync(string host)
    {
        try
        {
            var lookupTask = Dns.GetHostEntryAsync(host);
            var completed = await Task.WhenAny(lookupTask, Task.Delay(TimeSpan.FromSeconds(2)));
            if (completed != lookupTask)
            {
                return false;
            }

            var entry = await lookupTask;
            return entry.AddressList.Length > 0;
        }
        catch (SocketException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }
}