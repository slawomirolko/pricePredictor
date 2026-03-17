using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using PricePredictor.Application.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace PricePredictor.Application.News;

internal sealed class ArticleService : IArticleService
{
    private const string Source = "reuters";
    private readonly ILogger<ArticleService> _logger;
    private readonly IArticleReaderRepository _repository;
    private readonly ISeleniumFlowBuilderFactory _seleniumFlowBuilderFactory;

    public ArticleService(
        ILogger<ArticleService> logger,
        IArticleReaderRepository repository,
        ISeleniumFlowBuilderFactory seleniumFlowBuilderFactory)
    {
        _logger = logger;
        _repository = repository;
        _seleniumFlowBuilderFactory = seleniumFlowBuilderFactory;
    }

    public async Task<ArticleSyncResult> SyncArticleLinksAsync(CancellationToken cancellationToken)
    {
        const string newsUrl = "https://www.reuters.com";
        _logger.LogInformation("Starting article extraction from {Url}", newsUrl);

        try
        {
            var discoveredLinks = await CollectReutersLinksAsync(newsUrl, cancellationToken);
            _logger.LogInformation("Found {Count} article links", discoveredLinks.Count);
            var savedLinks = await PersistArticleLinksAsync(discoveredLinks, cancellationToken);
            return ArticleSyncResult.Success(savedLinks);
        }
        catch (ReutersSourceBlockedException ex)
        {
            _logger.LogWarning(ex, "Article extraction blocked by Reuters anti-bot protection");
            return ArticleSyncResult.SourceBlocked(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Article extraction failed");
            throw;
        }
    }

    private async Task<IReadOnlyList<DiscoveredArticleLink>> CollectReutersLinksAsync(
        string newsUrl,
        CancellationToken cancellationToken)
    {
        using var seleniumFlow = _seleniumFlowBuilderFactory.Create();

        var navigationResult = await NavigateAsync(seleniumFlow, newsUrl, cancellationToken);
        if (!navigationResult.IsSuccess)
        {
            throw new ReutersSourceBlockedException($"Reuters homepage navigation failed: {navigationResult.Error}");
        }

        var linksResult = CollectArticleLinks(seleniumFlow);
        if (!linksResult.IsSuccess)
        {
            throw new ReutersSourceBlockedException($"Reuters homepage parsing failed: {linksResult.Error}");
        }

        if (linksResult.Value.Count > 0)
        {
            return linksResult.Value;
        }

        throw new ReutersSourceBlockedException("Reuters homepage returned zero article links.");
    }

    private async Task<FlowResult<bool>> NavigateAsync(
        ISeleniumFlowBuilder seleniumFlow,
        string newsUrl,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Navigating to homepage");
        var openResult = await seleniumFlow.OpenAsync(newsUrl, cancellationToken);
        if (openResult.IsError)
        {
            return FlowResult<bool>.Fail(openResult.FirstError.Description);
        }

        var waitAfterOpenResult = await seleniumFlow.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
        if (waitAfterOpenResult.IsError)
        {
            return FlowResult<bool>.Fail(waitAfterOpenResult.FirstError.Description);
        }

        _logger.LogInformation("Page title: {Title}", seleniumFlow.Title);

        var blockedResult = seleniumFlow.IsBlockedPage();
        if (blockedResult.IsError)
        {
            return FlowResult<bool>.Fail(blockedResult.FirstError.Description);
        }

        if (blockedResult.Value)
        {
            _logger.LogWarning("Bot detection detected");
            var waitBlockedResult = await seleniumFlow.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
            if (waitBlockedResult.IsError)
            {
                return FlowResult<bool>.Fail(waitBlockedResult.FirstError.Description);
            }

            blockedResult = seleniumFlow.IsBlockedPage();
            if (blockedResult.IsError)
            {
                return FlowResult<bool>.Fail(blockedResult.FirstError.Description);
            }

            if (blockedResult.Value)
            {
                var refreshResult = await seleniumFlow.RefreshAsync(cancellationToken);
                if (refreshResult.IsError)
                {
                    return FlowResult<bool>.Fail(refreshResult.FirstError.Description);
                }

                _logger.LogInformation("Page refreshed");

                var waitAfterRefreshResult = await seleniumFlow.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
                if (waitAfterRefreshResult.IsError)
                {
                    return FlowResult<bool>.Fail(waitAfterRefreshResult.FirstError.Description);
                }
            }
        }

        var waitBeforeConsentResult = await seleniumFlow.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        if (waitBeforeConsentResult.IsError)
        {
            return FlowResult<bool>.Fail(waitBeforeConsentResult.FirstError.Description);
        }

        var consentResult = await seleniumFlow.AcceptConsentAsync(cancellationToken);
        if (consentResult.IsError)
        {
            return FlowResult<bool>.Fail(consentResult.FirstError.Description);
        }

        var waitAfterConsentResult = await seleniumFlow.WaitAsync(TimeSpan.FromSeconds(3), cancellationToken);
        if (waitAfterConsentResult.IsError)
        {
            return FlowResult<bool>.Fail(waitAfterConsentResult.FirstError.Description);
        }

        return FlowResult<bool>.Success(true);
    }

    private FlowResult<List<DiscoveredArticleLink>> CollectArticleLinks(ISeleniumFlowBuilder seleniumFlow)
    {
        var pageUri = new Uri(seleniumFlow.CurrentUrl);
        var htmlResult = seleniumFlow.GetHtml();
        if (htmlResult.IsError)
        {
            return FlowResult<List<DiscoveredArticleLink>>.Fail(htmlResult.FirstError.Description);
        }

        _logger.LogInformation("Page source size: {Bytes} bytes", htmlResult.Value.Length);

        var allLinksResult = seleniumFlow.GetElements(By.TagName("a"));
        if (allLinksResult.IsError)
        {
            return FlowResult<List<DiscoveredArticleLink>>.Fail(allLinksResult.FirstError.Description);
        }

        _logger.LogInformation("Total <a> tags found: {Count}", allLinksResult.Value.Count);

        var articleLinks = allLinksResult.Value
            .Select(linkElement =>
            {
                try
                {
                    return linkElement.GetAttribute("href");
                }
                catch
                {
                    return null;
                }
            })
            .Select(href => NormalizeToAbsoluteUrl(pageUri, href))
            .Where(IsArticleLink)
            .OfType<string>()
            .SelectMany(ParseDiscoveredArticleLinks)
            .Distinct(DiscoveredArticleLink.Comparer)
            .OrderBy(x => x.Url, StringComparer.Ordinal)
            .ToList();

        if (articleLinks.Count == 0)
        {
            _logger.LogInformation("Running html fallback scan");
            articleLinks = ExtractLinksFromHtml(pageUri, htmlResult.Value)
                .Where(IsArticleLink)
                .OfType<string>()
                .SelectMany(ParseDiscoveredArticleLinks)
                .Distinct(DiscoveredArticleLink.Comparer)
                .OrderBy(x => x.Url, StringComparer.Ordinal)
                .ToList();
        }

        return FlowResult<List<DiscoveredArticleLink>>.Success(articleLinks);
    }

    private async Task<IReadOnlyList<ArticleLink>> PersistArticleLinksAsync(
        IReadOnlyList<DiscoveredArticleLink> articleLinks,
        CancellationToken cancellationToken)
    {
        var savedLinks = new List<ArticleLink>();
        foreach (var discoveredLink in articleLinks)
        {
            var readTimeUtc = DateTime.UtcNow;
            var readAt = ComposeReadAt(discoveredLink.PublishedAtUtc, readTimeUtc);

            var articleLink = ArticleLink.Create(
                url: discoveredLink.Url,
                readAt: readAt,
                source: Source);

            await _repository.SaveArticleLinkAsync(articleLink, cancellationToken);
            savedLinks.Add(articleLink);
            _logger.LogInformation("Saved: {Url} (ReadAt={ReadAt:O})", discoveredLink.Url, readAt);
        }

        _logger.LogInformation("Saved {Count} article links to database", savedLinks.Count);
        return savedLinks;
    }

    private static DateTime ComposeReadAt(DateTime publishedDateUtc, DateTime readTimeUtc)
    {
        var publishedDate = DateTime.SpecifyKind(publishedDateUtc.Date, DateTimeKind.Utc);
        var readTime = DateTime.SpecifyKind(readTimeUtc, DateTimeKind.Utc);

        return new DateTime(
            publishedDate.Year,
            publishedDate.Month,
            publishedDate.Day,
            readTime.Hour,
            readTime.Minute,
            readTime.Second,
            readTime.Millisecond,
            DateTimeKind.Utc);
    }

    private static FlowResult<DateTime> ParsePublishedDate(string link)
    {
        if (string.IsNullOrWhiteSpace(link))
        {
            return FlowResult<DateTime>.Fail("Link is empty.");
        }

        var dateMatch = Regex.Match(link, @"(\d{4})-(\d{2})-(\d{2})");
        if (!dateMatch.Success)
        {
            return FlowResult<DateTime>.Fail("No date segment in link.");
        }

        var dateValue = $"{dateMatch.Groups[1].Value}-{dateMatch.Groups[2].Value}-{dateMatch.Groups[3].Value}";
        if (!DateTime.TryParseExact(
                dateValue,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var publishedDate))
        {
            return FlowResult<DateTime>.Fail("Invalid date format in link.");
        }

        return FlowResult<DateTime>.Success(DateTime.SpecifyKind(publishedDate.Date, DateTimeKind.Utc));
    }

    private static IEnumerable<DiscoveredArticleLink> ParseDiscoveredArticleLinks(string url)
    {
        var dateResult = ParsePublishedDate(url);
        if (!dateResult.IsSuccess)
        {
            return [];
        }

        return [new DiscoveredArticleLink(url, dateResult.Value)];
    }

    private static string? NormalizeToAbsoluteUrl(Uri pageUri, string? href)
    {
        if (string.IsNullOrWhiteSpace(href))
        {
            return null;
        }

        if (Uri.TryCreate(href, UriKind.Absolute, out var absolute))
        {
            return absolute.AbsoluteUri;
        }

        if (!href.StartsWith('/'))
        {
            return null;
        }

        return new Uri(pageUri, href).AbsoluteUri;
    }

    private static IReadOnlyList<string> ExtractLinksFromHtml(Uri pageUri, string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return Array.Empty<string>();
        }

        var results = new HashSet<string>(StringComparer.Ordinal);

        foreach (Match match in Regex.Matches(html, "href=\"(?<u>[^\"]+)\"", RegexOptions.IgnoreCase))
        {
            var normalized = NormalizeToAbsoluteUrl(pageUri, match.Groups["u"].Value);
            if (normalized != null)
            {
                results.Add(normalized);
            }
        }

        foreach (Match match in Regex.Matches(html, @"https://www\.reuters\.com/[A-Za-z0-9/_\-\.?=&%]+", RegexOptions.IgnoreCase))
        {
            results.Add(match.Value);
        }

        return results.ToArray();
    }

    private static bool IsArticleLink(string? href)
    {
        if (string.IsNullOrWhiteSpace(href))
        {
            return false;
        }

        if (!Uri.TryCreate(href, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!uri.Host.Equals("www.reuters.com", StringComparison.OrdinalIgnoreCase)
            && !uri.Host.Equals("reuters.com", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!Regex.IsMatch(uri.AbsolutePath, @"\d{4}-\d{2}-\d{2}", RegexOptions.IgnoreCase))
        {
            return false;
        }

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= 2 && !string.IsNullOrWhiteSpace(segments[^1]);
    }

    private readonly record struct FlowResult<T>(bool IsSuccess, T Value, string? Error)
    {
        public static FlowResult<T> Success(T value) => new(true, value, null);
        public static FlowResult<T> Fail(string error) => new(false, default!, error);
    }

    private readonly record struct DiscoveredArticleLink(string Url, DateTime PublishedAtUtc)
    {
        public static IEqualityComparer<DiscoveredArticleLink> Comparer { get; } = new UrlEqualityComparer();

        private sealed class UrlEqualityComparer : IEqualityComparer<DiscoveredArticleLink>
        {
            public bool Equals(DiscoveredArticleLink x, DiscoveredArticleLink y) =>
                StringComparer.Ordinal.Equals(x.Url, y.Url);

            public int GetHashCode(DiscoveredArticleLink obj) =>
                StringComparer.Ordinal.GetHashCode(obj.Url);
        }
    }

    private sealed class ReutersSourceBlockedException(string message) : Exception(message);
}
