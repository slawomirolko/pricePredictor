using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using PricePredictor.Application.Models;
using System.Text.RegularExpressions;

namespace PricePredictor.Application.News;

/// <summary>
/// Selenium-based service that scrapes news sources to extract article links with dates.
/// Uses anti-bot techniques from SeleniumGoldNewsClient.
/// </summary>
internal sealed class ArticleService : IArticleService
{
    private readonly ILogger<ArticleService> _logger;
    private readonly IArticleRepository _repository;
    private readonly IOllamaArticleExtractionClient _ollamaClient;
    private const string Source = "reuters";

    public ArticleService(
        ILogger<ArticleService> logger,
        IArticleRepository repository,
        IOllamaArticleExtractionClient ollamaClient)
    {
        _logger = logger;
        _repository = repository;
        _ollamaClient = ollamaClient;
    }

    public async Task<IReadOnlyList<ArticleLink>> SyncArticleLinksAsync(CancellationToken cancellationToken)
    {
        const string newsUrl = "https://www.reuters.com";
        _logger.LogInformation("🌐 Starting article extraction from {Url}", newsUrl);

        var options = new ChromeOptions();
        options.AddArgument("--disable-gpu");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--window-size=1920,1080");
        options.AddArgument("--lang=en-US");
        options.AddArgument("--disable-blink-features=AutomationControlled");
        options.AddArgument(
            "--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

        options.AddExcludedArgument("enable-automation");
        options.AddAdditionalChromeOption("useAutomationExtension", false);

        using var driver = new ChromeDriver(options);
        driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);

        try
        {
            _logger.LogInformation("Navigating to homepage...");
            driver.Navigate().GoToUrl(newsUrl);

            // Apply stealth scripts
            ApplyStealthScripts(driver);

            // Wait for JS-heavy page
            _logger.LogInformation("⏳ Waiting 10s for page to render...");
            await Task.Delay(10_000, cancellationToken);

            _logger.LogInformation("Page title: {Title}", driver.Title);

            // Handle Cloudflare/bot detection
            if (IsBlockedPage(driver))
            {
                _logger.LogWarning("⚠️ Bot detection detected. Refreshing...");
                await Task.Delay(5_000, cancellationToken);

                if (IsBlockedPage(driver))
                {
                    driver.Navigate().Refresh();
                    _logger.LogInformation("Refreshed. Waiting 10s more...");
                    await Task.Delay(10_000, cancellationToken);
                }
            }

            // Accept cookies
            await Task.Delay(5_000, cancellationToken);
            TryAcceptConsentBanner(driver);
            await Task.Delay(3_000, cancellationToken);

            var pageUri = new Uri(driver.Url);

            // Get full DOM via JS
            var html = (((IJavaScriptExecutor)driver).ExecuteScript("return document.documentElement.outerHTML;") as string)
                       ?? driver.PageSource;

            _logger.LogInformation("📥 Page source: {Bytes} bytes", html.Length);

            // Collect links from live DOM
            var allLinks = driver.FindElements(By.TagName("a"));
            _logger.LogInformation("🔍 Total <a> tags found: {Count}", allLinks.Count);

            var articleLinks = allLinks
                .Select(a =>
                {
                    try { return a.GetAttribute("href"); }
                    catch { return null; }
                })
                .Select(href => NormalizeToAbsoluteUrl(pageUri, href))
                .Where(IsArticleLink)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList();

            // Fallback: regex scan HTML
            if (articleLinks.Count == 0)
            {
                _logger.LogInformation("Falling back to HTML regex scan...");
                articleLinks = ExtractLinksFromHtml(pageUri, html)
                    .Where(IsArticleLink)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToList();
            }

            _logger.LogInformation("✅ Found {Count} article links", articleLinks.Count);

            // Extract date from each link and save to database
            var savedLinks = new List<ArticleLink>();
            foreach (var link in articleLinks)
            {
                if (string.IsNullOrWhiteSpace(link)) continue;

                var dateMatch = Regex.Match(link, @"(\d{4})-(\d{2})-(\d{2})");
                if (dateMatch.Success)
                {
                    if (DateTime.TryParseExact(dateMatch.Groups[1].Value + "-" + dateMatch.Groups[2].Value + "-" + dateMatch.Groups[3].Value,
                        "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.AssumeUniversal, out var publishedDate))
                    {
                        var isTradeUseful = await _ollamaClient.AssessTradingUsefulnessAsync(
                            articleLink: link,
                            source: Source,
                            publishedAtUtc: publishedDate,
                            cancellationToken: cancellationToken);

                        var articleLink = ArticleLink.Create(
                            url: link,
                            publishedAtUtc: publishedDate,
                            source: Source,
                            extractedAtUtc: DateTime.UtcNow,
                            isTradeUseful: isTradeUseful);

                        await _repository.SaveArticleLinkAsync(articleLink, cancellationToken);
                        savedLinks.Add(articleLink);
                        _logger.LogInformation("💾 Saved: {Url} (IsTradeUseful={IsTradeUseful})", link, isTradeUseful);
                    }
                }
            }

            _logger.LogInformation("✅ Saved {Count} article links to database", savedLinks.Count);
            return savedLinks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Article extraction failed");
            throw;
        }
        finally
        {
            try { driver.Quit(); } catch { /* ignore */ }
            driver.Dispose();
        }
    }

    // ─── helpers ───────────────────────────────────────────────────────────

    private static void ApplyStealthScripts(IWebDriver driver)
    {
        try
        {
            ((IJavaScriptExecutor)driver).ExecuteScript(
                "Object.defineProperty(navigator, 'webdriver', {get: () => undefined})");

            ((IJavaScriptExecutor)driver).ExecuteScript(@"
                const getParameter = WebGLRenderingContext.prototype.getParameter;
                WebGLRenderingContext.prototype.getParameter = function(parameter) {
                    if (parameter === 37445) return 'Intel Open Source Technology Center';
                    if (parameter === 37446) return 'Mesa DRI Intel(R) HD Graphics 520 (Skylake GT2)';
                    return getParameter(parameter);
                };
            ");
        }
        catch
        {
            // Best-effort
        }
    }

    private static bool IsBlockedPage(IWebDriver driver)
    {
        try
        {
            return driver.PageSource.Contains("captcha", StringComparison.OrdinalIgnoreCase)
                || driver.PageSource.Contains("Verify you are human", StringComparison.OrdinalIgnoreCase)
                || driver.PageSource.Contains("Please verify you are a human", StringComparison.OrdinalIgnoreCase)
                || driver.Title.Contains("Access Denied", StringComparison.OrdinalIgnoreCase)
                || driver.Title.Contains("Just a moment", StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    private static void TryAcceptConsentBanner(IWebDriver driver)
    {
        try
        {
            var buttons = driver.FindElements(By.TagName("button"));
            var acceptButton = buttons.FirstOrDefault(b =>
            {
                try
                {
                    var text = b.Text;
                    return text.Contains("Accept", StringComparison.OrdinalIgnoreCase)
                        || text.Contains("Agree", StringComparison.OrdinalIgnoreCase)
                        || text.Contains("Allow All", StringComparison.OrdinalIgnoreCase)
                        || text.Contains("Zaakceptuj", StringComparison.OrdinalIgnoreCase)
                        || text.Contains("Akceptuję", StringComparison.OrdinalIgnoreCase)
                        || text.Contains("Approve", StringComparison.OrdinalIgnoreCase)
                        || text.Contains("Zgadzam się", StringComparison.OrdinalIgnoreCase)
                        || text.Contains("Got it", StringComparison.OrdinalIgnoreCase);
                }
                catch { return false; }
            });

            if (acceptButton?.Displayed == true)
            {
                acceptButton.Click();
                Task.Delay(5_000).Wait();
            }
        }
        catch
        {
            // Optional
        }
    }

    private static string? NormalizeToAbsoluteUrl(Uri pageUri, string? href)
    {
        if (string.IsNullOrWhiteSpace(href)) return null;
        if (Uri.TryCreate(href, UriKind.Absolute, out var absolute)) return absolute.AbsoluteUri;
        if (!href.StartsWith('/')) return null;
        return new Uri(pageUri, href).AbsoluteUri;
    }

    private static IReadOnlyList<string> ExtractLinksFromHtml(Uri pageUri, string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return Array.Empty<string>();

        var results = new HashSet<string>(StringComparer.Ordinal);

        foreach (Match m in Regex.Matches(html, "href=\"(?<u>[^\"]+)\"", RegexOptions.IgnoreCase))
        {
            var normalized = NormalizeToAbsoluteUrl(pageUri, m.Groups["u"].Value);
            if (normalized != null) results.Add(normalized);
        }

        foreach (Match m in Regex.Matches(html, @"https://www\.reuters\.com/[A-Za-z0-9/_\-\.?=&%]+", RegexOptions.IgnoreCase))
            results.Add(m.Value);

        return results.ToArray();
    }

    private static bool IsArticleLink(string? href)
    {
        if (string.IsNullOrWhiteSpace(href)) return false;
        if (!Uri.TryCreate(href, UriKind.Absolute, out var uri)) return false;

        if (!uri.Host.Equals("www.reuters.com", StringComparison.OrdinalIgnoreCase)
            && !uri.Host.Equals("reuters.com", StringComparison.OrdinalIgnoreCase))
            return false;

        // Must contain a date in YYYY-MM-DD format
        if (!Regex.IsMatch(uri.AbsolutePath, @"\d{4}-\d{2}-\d{2}", RegexOptions.IgnoreCase))
            return false;

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2) return false;
        if (string.IsNullOrWhiteSpace(segments[^1])) return false;

        return true;
    }
}

