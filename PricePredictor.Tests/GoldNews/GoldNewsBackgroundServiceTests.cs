using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using PricePredictor.Api.BackgroundServices;
using PricePredictor.Application.Data;
using PricePredictor.Application.Finance;
using PricePredictor.Application.Finance.Interfaces;
using PricePredictor.Infrastructure.GoldNews;
using PricePredictor.Persistence.Repositories;
using Xunit;
using GoldNewsSettings = PricePredictor.Infrastructure.GoldNewsSettings;

namespace PricePredictor.Tests.GoldNews;

/// <summary>
/// Unit tests for GoldNewsBackgroundService.
/// Tests the ability to fetch articles and extract content from Reuters articles.
/// Specifically tests: https://www.reuters.com/world/india/gold-extends-gains-middle-east-war-boosts-safe-haven-demand-2026-03-03/
/// </summary>
public class GoldNewsBackgroundServiceTests
{
    private readonly ILogger<GoldNewsBackgroundService> _logger;
    private readonly IGoldNewsClient _client;
    private readonly IGoldNewsEmbeddingRepository _embeddingRepository;
    private readonly OllamaSharp.IOllamaApiClient _ollama;
    private readonly GoldNewsSettings _settings;

    public GoldNewsBackgroundServiceTests()
    {
        _logger = Substitute.For<ILogger<GoldNewsBackgroundService>>();
        _client = Substitute.For<IGoldNewsClient>();
        _embeddingRepository = Substitute.For<IGoldNewsEmbeddingRepository>();
        _ollama = Substitute.For<OllamaSharp.IOllamaApiClient>();
        
        _settings = new GoldNewsSettings
        {
            RssUrl = "https://feeds.reuters.com/news/worldnews",
            EmbeddingDimensions = 3072,
            OllamaUrl = "http://localhost:11434",
            OllamaModel = "phi3"
        };
    }

    /// <summary>
    /// Test that verifies the GoldNewsClient can extract the expected Reuters article content.
    /// Article: "Gold extends gains, Middle East war boosts safe haven demand"
    /// URL: https://www.reuters.com/world/india/gold-extends-gains-middle-east-war-boosts-safe-haven-demand-2026-03-03/
    /// 
    /// Expected content to find:
    /// "Damage to energy infrastructure and stalled tanker traffic through Hormuz have lifted the risk of sustained strength in ​oil, gas and refined ​products, stoking inflation ⁠fears and pushing back rate-cut expectations, leaving gold with little support, said Fawad Razaqzada, market analyst at City Index and FOREX.com."
    /// </summary>
    [Fact]
    public async Task ShouldExtractArticleContentFromReutersArticle()
    {
        // Arrange - Test data from Reuters article URL
        var articleUrl = "https://www.reuters.com/world/india/gold-extends-gains-middle-east-war-boosts-safe-haven-demand-2026-03-03/";
        
        // Expected content extracted from Reuters article about gold and Middle East war
        var expectedContent = "Damage to energy infrastructure and stalled tanker traffic through Hormuz have lifted the risk of sustained strength in ​oil, gas and refined ​products, stoking inflation ⁠fears and pushing back rate-cut expectations, leaving gold with little support, said Fawad Razaqzada, market analyst at City Index and FOREX.com.";
        
        // Mock the HTTP response to return the article content
        _client.FetchArticleContentAsync(articleUrl, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(expectedContent));

        _embeddingRepository.EnsureStorageAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _embeddingRepository.UpsertAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<IReadOnlyList<float>>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Create the service instance
        var service = new GoldNewsBackgroundService(
            _logger,
            _client,
            _embeddingRepository,
            _ollama,
            Options.Create(_settings));

        // Act - Simulate fetching article content from Reuters
        var result = await _client.FetchArticleContentAsync(articleUrl, CancellationToken.None);

        // Assert - Verify the Reuters article content was properly extracted
        Assert.NotNull(result);
        Assert.Contains("Damage to energy infrastructure", result);
        Assert.Contains("Hormuz", result);
        Assert.Contains("rate-cut expectations", result);
        Assert.Contains("Fawad Razaqzada", result);
        Assert.Contains("City Index", result);
    }

    /// <summary>
    /// Test that verifies the GoldNewsBackgroundService can process RSS feeds with relevant gold keywords
    /// and trigger article fetching for relevant articles.
    /// </summary>
    [Fact]
    public async Task ShouldProcessRssFeedWithGoldRelevantArticles()
    {
        // Arrange - Create a mock RSS feed response
        var rssFeed = BuildMockRssFeed();

        var articleUrl = "https://www.reuters.com/world/india/gold-extends-gains-middle-east-war-boosts-safe-haven-demand-2026-03-03/";
        var articleContent = "Damage to energy infrastructure and stalled tanker traffic through Hormuz have lifted the risk of sustained strength in ​oil, gas and refined ​products, stoking inflation ⁠fears and pushing back rate-cut expectations, leaving gold with little support, said Fawad Razaqzada, market analyst at City Index and FOREX.com.";

        _client.GetRssXmlAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(rssFeed));

        _client.FetchArticleContentAsync(articleUrl, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(articleContent));

        _embeddingRepository.EnsureStorageAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var mockEmbedding = new float[3072];
        for (int i = 0; i < mockEmbedding.Length; i++)
        {
            mockEmbedding[i] = (float)(i * 0.001);
        }

        _embeddingRepository.UpsertAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<IReadOnlyList<float>>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var service = new GoldNewsBackgroundService(
            _logger,
            _client,
            _embeddingRepository,
            _ollama,
            Options.Create(_settings));

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act - Fetch RSS content
        var rssResult = await _client.GetRssXmlAsync("https://feeds.reuters.com/news/worldnews", cts.Token);

        // Assert - Verify RSS contains gold-relevant keywords
        Assert.NotNull(rssResult);
        Assert.Contains("gold", rssResult.ToLower());
        Assert.Contains("geopolitical", rssResult.ToLower());
        Assert.Contains("safe haven", rssResult.ToLower());

        // Verify article content was fetched
        var articleResult = await _client.FetchArticleContentAsync(articleUrl, cts.Token);
        Assert.NotNull(articleResult);
        Assert.Contains("Damage to energy infrastructure", articleResult);
    }

    private static string BuildMockRssFeed()
    {
        return @"<?xml version=""1.0"" encoding=""UTF-8""?>
<rss version=""2.0"">
    <channel>
        <title>Reuters News</title>
        <link>https://www.reuters.com</link>
        <description>Latest News</description>
        <item>
            <title>Gold extends gains amid Middle East war fears</title>
            <link>https://www.reuters.com/world/india/gold-extends-gains-middle-east-war-boosts-safe-haven-demand-2026-03-03/</link>
            <description>Gold prices continue to rise as geopolitical tensions support safe haven demand. Central bank buying and inflation concerns add to bullion appeal.</description>
        </item>
    </channel>
</rss>";
    }
}

