using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Shouldly;
using System.Text.RegularExpressions;
using Xunit.Abstractions;

namespace PricePredictor.ArticlesFinderApp.Tests.Integration.News;

public sealed class ReutersSeleniumLiveTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly IWebDriver _driver;

    public ReutersSeleniumLiveTests(ITestOutputHelper output)
    {
        _output = output;

        var options = new ChromeOptions();
        // options.AddArgument("--headless=new");
        options.AddArgument("--start-minimized");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--window-size=1920,1080");
        options.AddArgument("--lang=en-US");
        options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
        options.AddArgument("--disable-blink-features=AutomationControlled");
        options.AddExcludedArgument("enable-automation");

        _driver = new ChromeDriver(options);
        _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
    }

    [Fact(Skip = "Manual live Selenium test. Requires a stable interactive Chrome session and external Reuters availability.")]
    public async Task LoadReutersHomePage_ShouldReturnArticleLinks()
    {
        const string reutersUrl = "https://www.reuters.com";

        _output.WriteLine($"Navigating to {reutersUrl}...");
        _driver.Navigate().GoToUrl(reutersUrl);

        ApplyStealthScripts();

        _output.WriteLine("Waiting 10s for page to render...");
        await Task.Delay(10_000);

        _output.WriteLine($"Page title: {_driver.Title}");
        _output.WriteLine($"Current URL: {_driver.Url}");

        if (IsBlockedPage())
        {
            _output.WriteLine("Bot detection detected. Waiting 5s then refreshing...");
            await Task.Delay(5_000);

            if (IsBlockedPage())
            {
                _driver.Navigate().Refresh();
                _output.WriteLine("Refreshed. Waiting 10s more...");
                await Task.Delay(10_000);
                _output.WriteLine($"Title after refresh: {_driver.Title}");
            }
        }

        await Task.Delay(5_000);
        TryAcceptConsentBanner();
        await Task.Delay(3_000);

        _output.WriteLine($"Final title: {_driver.Title}");
        _output.WriteLine($"Final URL: {_driver.Url}");

        var pageUri = new Uri(_driver.Url);
        pageUri.Host.EndsWith("reuters.com", StringComparison.OrdinalIgnoreCase).ShouldBeTrue(
            $"Expected to be on reuters.com but was on {_driver.Url}");

        var html = (((IJavaScriptExecutor)_driver).ExecuteScript("return document.documentElement.outerHTML;") as string)
                   ?? _driver.PageSource;

        _output.WriteLine($"Page source: {html.Length} bytes");

        var allLinks = _driver.FindElements(By.TagName("a"));
        _output.WriteLine($"Total <a> tags found in DOM: {allLinks.Count}");

        var articleLinks = allLinks
            .Select(a =>
            {
                try
                {
                    return a.GetAttribute("href");
                }
                catch
                {
                    return null;
                }
            })
            .Select(href => NormalizeToAbsoluteReutersUrl(pageUri, href))
            .Where(IsReutersArticleLink)
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        if (articleLinks.Count == 0)
        {
            _output.WriteLine("DOM scan found 0 links, falling back to HTML regex scan...");
            articleLinks = ExtractReutersLinksFromHtml(pageUri, html)
                .Where(IsReutersArticleLink)
                .OfType<string>()
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList();
        }

        _output.WriteLine($"Found {articleLinks.Count} article links on {reutersUrl}:");
        for (var i = 0; i < articleLinks.Count; i++)
        {
            _output.WriteLine($"[{i + 1,3}] {articleLinks[i]}");
        }

        if (articleLinks.Count == 0)
        {
            _output.WriteLine("Zero article links found. HTML snippet (first 3000 chars):");
            _output.WriteLine(html.Length > 3000 ? html[..3000] : html);
        }

        articleLinks.Count.ShouldBeGreaterThan(0);
    }

    private void ApplyStealthScripts()
    {
        try
        {
            var js = (IJavaScriptExecutor)_driver;
            js.ExecuteScript("Object.defineProperty(navigator, 'webdriver', {get: () => undefined})");

            js.ExecuteScript(@"
                const getParameter = WebGLRenderingContext.prototype.getParameter;
                WebGLRenderingContext.prototype.getParameter = function(parameter) {
                    if (parameter === 37445) return 'Intel Open Source Technology Center';
                    if (parameter === 37446) return 'Mesa DRI Intel(R) HD Graphics 520 (Skylake GT2)';
                    return getParameter(parameter);
                };

                Object.defineProperty(navigator, 'languages', {get: () => ['en-US', 'en']});
                Object.defineProperty(navigator, 'plugins', {get: () => [1, 2, 3, 4, 5]});

                const originalQuery = window.navigator.permissions.query;
                window.navigator.permissions.query = (parameters) => (
                    parameters.name === 'notifications' ?
                        Promise.resolve({ state: Notification.permission }) :
                        originalQuery(parameters)
                );

                window.chrome = {
                    runtime: {},
                    loadTimes: function() {},
                    csi: function() {},
                    app: {}
                };
            ");
        }
        catch
        {
        }
    }

    private bool IsBlockedPage()
    {
        try
        {
            return _driver.PageSource.Contains("captcha", StringComparison.OrdinalIgnoreCase)
                   || _driver.PageSource.Contains("Verify you are human", StringComparison.OrdinalIgnoreCase)
                   || _driver.PageSource.Contains("Please verify you are a human", StringComparison.OrdinalIgnoreCase)
                   || _driver.PageSource.Contains("Automated (bot) activity", StringComparison.OrdinalIgnoreCase)
                   || _driver.PageSource.Contains("Rapid taps or clicks", StringComparison.OrdinalIgnoreCase)
                   || _driver.PageSource.Contains("JavaScript disabled", StringComparison.OrdinalIgnoreCase)
                   || _driver.Title.Contains("Access Denied", StringComparison.OrdinalIgnoreCase)
                   || _driver.Title.Contains("Just a moment", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private void TryAcceptConsentBanner()
    {
        try
        {
            var buttons = _driver.FindElements(By.TagName("button"));
            var acceptButton = buttons.FirstOrDefault(b =>
            {
                try
                {
                    var text = b.Text;
                    return text.Contains("Accept", StringComparison.OrdinalIgnoreCase)
                           || text.Contains("Agree", StringComparison.OrdinalIgnoreCase)
                           || text.Contains("Allow All", StringComparison.OrdinalIgnoreCase)
                           || text.Contains("Zaakceptuj", StringComparison.OrdinalIgnoreCase)
                           || text.Contains("Akceptuje", StringComparison.OrdinalIgnoreCase)
                           || text.Contains("Approve", StringComparison.OrdinalIgnoreCase)
                           || text.Contains("Zgadzam sie", StringComparison.OrdinalIgnoreCase)
                           || text.Contains("Got it", StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    return false;
                }
            });

            if (acceptButton?.Displayed == true)
            {
                // Human-like delay before clicking
                var randomDelayMs = new Random().Next(500, 2000);
                Task.Delay(randomDelayMs).Wait();

                _output.WriteLine($"Clicking consent button: '{acceptButton.Text}'");
                acceptButton.Click();
                Task.Delay(5_000).Wait();
            }
        }
        catch
        {
        }
    }

    private static string? NormalizeToAbsoluteReutersUrl(Uri pageUri, string? href)
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

    private static IReadOnlyList<string> ExtractReutersLinksFromHtml(Uri pageUri, string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return Array.Empty<string>();
        }

        var results = new HashSet<string>(StringComparer.Ordinal);

        foreach (Match match in Regex.Matches(html, "href=\"(?<u>[^\"]+)\"", RegexOptions.IgnoreCase))
        {
            var normalized = NormalizeToAbsoluteReutersUrl(pageUri, match.Groups["u"].Value);
            if (normalized is not null)
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

    private static bool IsReutersArticleLink(string? href)
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

    public void Dispose()
    {
        try
        {
            _driver.Quit();
        }
        catch
        {
        }

        _driver.Dispose();
    }
}
