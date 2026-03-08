using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using PricePredictor.Application.Data;
using PricePredictor.Application.Finance.Interfaces;
using System.Linq;

namespace PricePredictor.Infrastructure.GoldNews;

public sealed class SeleniumGoldNewsClient : IGoldNewsClient
{
    private readonly HttpClient _http;
    private readonly IOllamaApiClient _ollama;
    private readonly GoldNewsSettings _settings;
    private readonly ILogger<SeleniumGoldNewsClient> _logger;

    private const string ArticleExtractionPrompt = """
                                                  You are an information extraction system.

                                                  Task:
                                                  Extract ONLY the main article body text from the provided HTML in Reuters.

                                                  Important:
                                                  - The article text may be split across multiple HTML elements (for example <p>, <div>, <span>, etc.).
                                                  - Combine the text from these elements into one string, separating paragraphs with a single space.
                                                  - Preserve the original reading order.

                                                  Output requirements:
                                                  - Return the result as a single plain text string suitable for embedding in code.
                                                  - Use newline characters between paragraphs.
                                                  - Do NOT include HTML tags.
                                                  - Do NOT include explanations, comments, or formatting such as markdown.

                                                  Rules:
                                                  - Include ONLY the article text.
                                                  - Ignore navigation, ads, promo bars, headers, footers, sidebars, and UI elements.
                                                  - If the HTML does NOT contain article text, return an empty string.

                                                  HTML:
                                                  {HTML_CONTENT}
                                                  """;
    
    public SeleniumGoldNewsClient(
        HttpClient http, 
        IOllamaApiClient ollama,
        IOptions<GoldNewsSettings> settings,
        ILogger<SeleniumGoldNewsClient> logger)
    {
        _http = http;
        _ollama = ollama;
        _settings = settings.Value;
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

    public async Task<string?> FetchArticleContentAsync(string articleUrl, CancellationToken cancellationToken)
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
            
            // Set page load timeout
            driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);

            // Navigate to the article
            await Task.Run(() => driver.Navigate().GoToUrl(articleUrl));

            // Wait for initial load
            await Task.Delay(10000, cancellationToken);
            _logger.LogInformation("📄 Initial Page Title: {Title}", driver.Title);
            _logger.LogDebug("📄 HTML Sample (first 2000 chars): {Sample}", driver.PageSource.Length > 2000 ? driver.PageSource[..2000] : driver.PageSource);

            // Anti-bot check
            if (driver.PageSource.Contains("captcha") || 
                driver.PageSource.Contains("Verify you are human") || 
                driver.Title.Contains("Access Denied") ||
                driver.Title.Contains("Just a moment") ||
                driver.PageSource.Contains("Please verify you are a human"))
            {
                _logger.LogWarning("⚠️ Bot detection or captcha detected on {Url}. Page title: {Title}", articleUrl, driver.Title);
                
                // Wait for potential auto-resolution (e.g. Cloudflare)
                await Task.Delay(5000, cancellationToken);
                
                if (driver.Title.Contains("Just a moment") || driver.Title.Contains("Access Denied") || driver.PageSource.Contains("Please verify you are a human"))
                {
                    _logger.LogInformation("🔄 Refreshing page...");
                    await Task.Run(() => driver.Navigate().Refresh());
                    await Task.Delay(5000, cancellationToken);
                    _logger.LogInformation("📄 Page Title after refresh: {Title}", driver.Title);
                }
            }

            // Wait a bit for content to load and any redirects/cookie overlays to handle themselves
            await Task.Delay(5000, cancellationToken);

            // Try to find and click "Accept" button if it exists (very naive)
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
            catch { /* ignore */ }

            var html = driver.PageSource;
            
            if (string.IsNullOrWhiteSpace(html))
            {
                _logger.LogWarning("⚠️ Selenium returned empty page source");
                return null;
            }

            _logger.LogInformation("📥 Selenium received {Bytes} bytes", html.Length);

            var content = await ExtractContentWithOllamaAsync(html, cancellationToken);
            
            if (!string.IsNullOrWhiteSpace(content))
            {
                _logger.LogInformation("✅ Extracted {Length} chars via Ollama", content.Length);
                return content;
            }

            // Fallback: try to extract ANY text if Ollama extraction failed
            var allText = driver.FindElement(By.TagName("body")).Text;
            if (!string.IsNullOrWhiteSpace(allText) && allText.Length > 100)
            {
                 var result = allText.Length > 3000 ? allText[..3000] : allText;
                 _logger.LogInformation("✅ Extracted {Length} chars via fallback Selenium", result.Length);
                 return result;
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

    private async Task<string?> ExtractContentWithOllamaAsync(string html, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("🤖 Extracting content using Ollama ({Model})...", _settings.OllamaModel);

            // Basic cleanup to reduce tokens
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            var noisySelectors = new[] 
            { 
                "//script", "//style", "//noscript", "//nav", "//footer", "//header"
            };
            foreach (var selector in noisySelectors)
            {
                var elements = doc.DocumentNode.SelectNodes(selector);
                if (elements != null)
                    foreach (var el in elements)
                        el.ParentNode?.RemoveChild(el);
            }

            var cleanHtml = doc.DocumentNode.InnerHtml;
            // Take a reasonable chunk of HTML to avoid context window issues
            var htmlSample = cleanHtml.Length > 15000 ? cleanHtml[..15000] : cleanHtml;

            _ollama.SelectedModel = _settings.OllamaModel;

            var result = "";
            await foreach (var response in _ollama.GenerateAsync(ArticleExtractionPrompt, null, cancellationToken))
            {
                result += response?.Response;
            }

            if (string.IsNullOrWhiteSpace(result) || result.Length < 100)
            {
                _logger.LogWarning("⚠️ Ollama returned too short or empty result.");
                return null;
            }

            return result.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Ollama extraction failed: {Message}", ex.Message);
            return null;
        }
    }

    private string? ExtractParagraphs(string html)
    {
        try
        {
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            var noisySelectors = new[] 
            { 
                "//script", "//style", "//noscript", "//div[@class*='cookie']", 
                "//nav", "//footer", "//header", "//div[@class*='related']",
                "//div[@class*='advertisement']", "//div[@class*='social']"
            };
            foreach (var selector in noisySelectors)
            {
                var elements = doc.DocumentNode.SelectNodes(selector);
                if (elements != null)
                    foreach (var el in elements)
                        el.ParentNode?.RemoveChild(el);
            }

            var article = doc.DocumentNode.SelectSingleNode("//article") ??
                          doc.DocumentNode.SelectSingleNode("//div[@data-testid='article']") ??
                          doc.DocumentNode.SelectSingleNode("//div[@id*='article-body']") ??
                          doc.DocumentNode.SelectSingleNode("//div[@class*='article-body']") ??
                          doc.DocumentNode.SelectSingleNode("//div[@class*='story-body']") ??
                          doc.DocumentNode.SelectSingleNode("//body");

            if (article == null) return null;

            var paragraphs = article.SelectNodes(".//p") ?? 
                             article.SelectNodes(".//div[@class*='paragraph'] | .//span[@class*='paragraph']");

            if (paragraphs == null || paragraphs.Count == 0) return null;

            var texts = paragraphs
                .Select(p => System.Net.WebUtility.HtmlDecode(p.InnerText?.Trim() ?? ""))
                .Where(t => t.Length > 15)
                .Select(t => System.Text.RegularExpressions.Regex.Replace(t, @"\s+", " ").Trim())
                .ToList();

            if (texts.Count < 3) 
            {
                // Fallback: Try to get any div with long text
                var divs = article.SelectNodes(".//div");
                if (divs != null)
                {
                    foreach (var div in divs)
                    {
                        var text = System.Net.WebUtility.HtmlDecode(div.InnerText?.Trim() ?? "");
                        if (text.Length > 200 && !text.Contains("<") && !texts.Contains(text))
                        {
                            texts.Add(System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim());
                        }
                    }
                }
            }

            if (texts.Count == 0) return null;
            
            var result = string.Join(" ", texts);
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\s+", " ").Trim();
            return result.Length > 100 ? (result.Length > 3000 ? result[..3000] : result) : null;
        }
        catch
        {
            return null;
        }
    }
}
