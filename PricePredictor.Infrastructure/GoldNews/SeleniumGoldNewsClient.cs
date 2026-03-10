using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using PricePredictor.Application.Finance.Interfaces;
using PricePredictor.Application.News;

namespace PricePredictor.Infrastructure.GoldNews;

public sealed class SeleniumGoldNewsClient : IGoldNewsClient
{
    private readonly HttpClient _http;
    private readonly IArticleContentExtractionService _articleContentExtractionService;
    private readonly ILogger<SeleniumGoldNewsClient> _logger;

    public SeleniumGoldNewsClient(
        HttpClient http,
        IArticleContentExtractionService articleContentExtractionService,
        ILogger<SeleniumGoldNewsClient> logger)
    {
        _http = http;
        _articleContentExtractionService = articleContentExtractionService;
        _logger = logger;
    }

    public async Task<string> GetRssXmlAsync(string rssUrl, CancellationToken cancellationToken)
    {
        // RSS is usually fine with HttpClient
        using var response = await _http.GetAsync(rssUrl, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"RSS request failed with status {(int)response.StatusCode} ({response.StatusCode}) for URL '{rssUrl}'.");
        }

        return content;
    }

    public async Task<string?> FetchArticleContentAsync(string articleUrl, string? articleTitle, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("🌐 Fetching article via Selenium: {Url}", articleUrl);

            var options = new ChromeOptions();
            // options.AddArgument("--headless=new");
            // options.AddArgument("--disable-gpu");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--window-size=1920,1080");
            options.AddArgument("--lang=en-US");
            options.AddArgument("--disable-blink-features=AutomationControlled");
            options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            // Avoid detection
            options.AddExcludedArgument("enable-automation");
            options.AddAdditionalChromeOption("useAutomationExtension", false);

            using var driver = new ChromeDriver(options);

            // Apply stealth scripts
            driver.ExecuteScript("Object.defineProperty(navigator, 'webdriver', {get: () => undefined})");
            driver.ExecuteScript(@"
                // Pass the WebGL Test
                const getParameter = WebGLRenderingContext.prototype.getParameter;
                WebGLRenderingContext.prototype.getParameter = function(parameter) {
                    // UNMASKED_VENDOR_WEBGL
                    if (parameter === 37445) {
                        return 'Intel Open Source Technology Center';
                    }
                    // UNMASKED_RENDERER_WEBGL
                    if (parameter === 37446) {
                        return 'Mesa DRI Intel(R) HD Graphics 520 (Skylake GT2)';
                    }
                    return getParameter(parameter);
                };
            ");

            driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
            await Task.Run(() => driver.Navigate().GoToUrl(articleUrl), cancellationToken);

            await Task.Delay(10000, cancellationToken);
            _logger.LogInformation("📄 Initial Page Title: {Title}", driver.Title);
            _logger.LogDebug("📄 HTML Sample (first 2000 chars): {Sample}", driver.PageSource.Length > 2000 ? driver.PageSource[..2000] : driver.PageSource);

            if (driver.PageSource.Contains("captcha", StringComparison.OrdinalIgnoreCase) ||
                driver.PageSource.Contains("Verify you are human", StringComparison.OrdinalIgnoreCase) ||
                driver.Title.Contains("Access Denied", StringComparison.OrdinalIgnoreCase) ||
                driver.Title.Contains("Just a moment", StringComparison.OrdinalIgnoreCase) ||
                driver.PageSource.Contains("Please verify you are a human", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("⚠️ Bot detection or captcha detected on {Url}. Page title: {Title}", articleUrl, driver.Title);

                await Task.Delay(5000, cancellationToken);

                if (driver.Title.Contains("Just a moment", StringComparison.OrdinalIgnoreCase) ||
                    driver.Title.Contains("Access Denied", StringComparison.OrdinalIgnoreCase) ||
                    driver.PageSource.Contains("Please verify you are a human", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("🔄 Refreshing page...");
                    await Task.Run(() => driver.Navigate().Refresh(), cancellationToken);
                    await Task.Delay(5000, cancellationToken);
                    _logger.LogInformation("📄 Page Title after refresh: {Title}", driver.Title);
                }
            }

            await Task.Delay(5000, cancellationToken);

            try
            {
                var buttons = driver.FindElements(By.TagName("button"));
                var acceptButton = buttons.FirstOrDefault(b => b.Text.Contains("Accept", StringComparison.OrdinalIgnoreCase) ||
                                                               b.Text.Contains("Agree", StringComparison.OrdinalIgnoreCase) ||
                                                               b.Text.Contains("Zaakceptuj", StringComparison.OrdinalIgnoreCase) ||
                                                               b.Text.Contains("Akceptuję", StringComparison.OrdinalIgnoreCase) ||
                                                               b.Text.Contains("Approve", StringComparison.OrdinalIgnoreCase) ||
                                                               b.Text.Contains("Allow All", StringComparison.OrdinalIgnoreCase) ||
                                                               b.Text.Contains("Zgadzam się", StringComparison.OrdinalIgnoreCase));
                if (acceptButton != null && acceptButton.Displayed)
                {
                    _logger.LogInformation("🖱️ Clicking cookie button: {Text}", acceptButton.Text);
                    acceptButton.Click();
                    await Task.Delay(5000, cancellationToken);
                }
            }
            catch
            {
                // ignore
            }

            var html = driver.PageSource;
            if (string.IsNullOrWhiteSpace(html))
            {
                _logger.LogWarning("⚠️ Selenium returned empty page source");
                return null;
            }

            _logger.LogInformation("📥 Selenium received {Bytes} bytes", html.Length);

            string? bodyText = null;
            try
            {
                bodyText = driver.FindElement(By.TagName("body")).Text;
            }
            catch
            {
                // Keep null body text when body is unavailable.
            }

            var extractionTitle = string.IsNullOrWhiteSpace(articleTitle) ? driver.Title : articleTitle;
            var content = await _articleContentExtractionService.ExtractAsync(html, bodyText, extractionTitle, cancellationToken);
            if (!string.IsNullOrWhiteSpace(content))
            {
                _logger.LogInformation("✅ Extracted {Length} chars from article", content.Length);
                return content;
            }

            _logger.LogWarning("❌ No content extracted. HTML Sample: {Sample}", html.Length > 2000 ? html[..2000] : html);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Selenium Exception: {Message}", ex.Message);
            return null;
        }
    }
}
