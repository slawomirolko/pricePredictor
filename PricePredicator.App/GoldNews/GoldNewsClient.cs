using System.Net.Http;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace PricePredicator.App.GoldNews;

public sealed class GoldNewsClient : IGoldNewsClient
{
    private readonly HttpClient _http;
    private readonly ILogger<GoldNewsClient> _logger;

    public GoldNewsClient(HttpClient http, ILogger<GoldNewsClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<string> GetRssXmlAsync(string rssUrl, CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync(rssUrl, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"RSS request failed with status {(int)response.StatusCode} ({response.StatusCode}) for URL '{rssUrl}'. Response: {TrimForLog(content)}");
        }

        return content;
    }

    public async Task<string?> FetchArticleContentAsync(string articleUrl, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("📄 Fetching article: {Url}", articleUrl);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            using var request = new HttpRequestMessage(HttpMethod.Get, articleUrl);
            request.Headers.Add("Accept-Language", "en-US,en;q=0.9");
            request.Headers.Add("Accept-Encoding", "gzip, deflate, br");
            request.Headers.Add("Cache-Control", "no-cache");
            request.Headers.Add("Sec-Fetch-Dest", "document");
            request.Headers.Add("Sec-Fetch-Mode", "navigate");

            using var response = await _http.SendAsync(request, cts.Token);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogInformation("⚠️ Article fetch failed: {Status}", response.StatusCode);
                return null;
            }

            var html = await response.Content.ReadAsStringAsync(cts.Token);
            if (string.IsNullOrWhiteSpace(html))
            {
                _logger.LogInformation("⚠️ Empty response");
                return null;
            }

            _logger.LogInformation("📥 Received {Bytes} bytes", html.Length);

            var content = ExtractParagraphs(html);
            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogInformation("⚠️ Trying text extraction");
                content = ExtractAllText(html);
            }

            if (!string.IsNullOrWhiteSpace(content))
            {
                _logger.LogInformation("✅ Extracted {Length} chars", content.Length);
                return content;
            }

            _logger.LogInformation("❌ No content extracted");
            return null;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("⏱️ Timeout");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogInformation("❌ Exception: {Message}", ex.Message);
            return null;
        }
    }

    private string? ExtractParagraphs(string html)
    {
        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Remove noise elements - be specific and conservative
            var noisySelectors = new[] 
            { 
                "//script", 
                "//style", 
                "//noscript", 
                "//div[@class*='cookie']", 
                "//nav", 
                "//footer",
                "//div[@class*='related']",
                "//div[@class*='advertisement']",
                "//div[@class*='social']",
                "//header"
            };
            foreach (var selector in noisySelectors)
            {
                var elements = doc.DocumentNode.SelectNodes(selector);
                if (elements != null)
                    foreach (var el in elements)
                        el.ParentNode?.RemoveChild(el);
            }

            // Try multiple strategies to find the article content
            HtmlNode? article = null;
            
            // Strategy 1: Look for article element
            article = doc.DocumentNode.SelectSingleNode("//article");
            
            // Strategy 2: Look for main content divs (common in Reuters)
            if (article == null)
                article = doc.DocumentNode.SelectSingleNode("//div[@data-testid='article']") ??
                         doc.DocumentNode.SelectSingleNode("//div[@id*='article-body']") ??
                         doc.DocumentNode.SelectSingleNode("//div[@class*='article-body']") ??
                         doc.DocumentNode.SelectSingleNode("//div[@class*='story-body']");
            
            // Strategy 3: Look for content divs
            if (article == null)
                article = doc.DocumentNode.SelectSingleNode("//div[@class*='article']") ??
                         doc.DocumentNode.SelectSingleNode("//div[@class*='content']");
            
            // Strategy 4: Fall back to body
            if (article == null)
                article = doc.DocumentNode.SelectSingleNode("//body");
            
            if (article == null) return null;

            // Extract paragraphs, handling both direct p tags and nested structures
            var paragraphs = article.SelectNodes(".//p");
            if (paragraphs == null || paragraphs.Count == 0) 
            {
                // Fallback: try to extract any meaningful text sections
                paragraphs = article.SelectNodes(".//div[@class*='paragraph'] | .//span[@class*='paragraph']");
            }
            
            if (paragraphs == null || paragraphs.Count == 0) return null;

            var texts = new List<string>();
            foreach (var p in paragraphs)
            {
                var text = p.InnerText?.Trim();
                if (!string.IsNullOrWhiteSpace(text) && text.Length > 15)
                {
                    // Clean up the text but preserve meaningful content
                    text = System.Net.WebUtility.HtmlDecode(text);
                    text = Regex.Replace(text, @"[\r\n]+", " ");
                    text = Regex.Replace(text, @"\s+", " ").Trim();
                    
                    // Don't filter based on keywords, just length
                    if (text.Length > 15)
                        texts.Add(text);
                }
            }

            if (texts.Count == 0) return null;
            
            var result = string.Join(" ", texts);
            result = Regex.Replace(result, @"\s+", " ").Trim();
            return result.Length > 100 ? (result.Length > 3000 ? result[..3000] : result) : null;
        }
        catch
        {
            return null;
        }
    }

    private string? ExtractAllText(string html)
    {
        try
        {
            // First, try a more targeted approach - extract from article content divs
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            
            // Remove script and style tags
            var cleaned = Regex.Replace(html, @"<script[^>]*>.*?</script>", " ", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            cleaned = Regex.Replace(cleaned, @"<style[^>]*>.*?</style>", " ", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            cleaned = Regex.Replace(cleaned, @"<noscript[^>]*>.*?</noscript>", " ", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            
            // Remove HTML tags
            cleaned = Regex.Replace(cleaned, @"<[^>]+>", " ");
            
            // Decode HTML entities
            cleaned = System.Net.WebUtility.HtmlDecode(cleaned);
            
            // Normalize whitespace
            cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
            
            // Remove common noise patterns more conservatively
            // Only remove text that is clearly UI element labels or interactions, not article content
            cleaned = Regex.Replace(cleaned, @"(click here|sign in|log in|subscribe|read more|accept cookies|share this|follow us|newsletter)\b", " ", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
            
            return cleaned.Length > 100 ? (cleaned.Length > 3000 ? cleaned[..3000] : cleaned) : null;
        }
        catch
        {
            return null;
        }
    }

    private static string TrimForLog(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
        s = s.Replace("\r", " ").Replace("\n", " ");
        return s.Length <= 400 ? s : s[..400] + "...";
    }
}

