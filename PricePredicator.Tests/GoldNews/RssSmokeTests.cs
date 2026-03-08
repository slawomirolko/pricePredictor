using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace PricePredicator.Tests.GoldNews;

public class RssSmokeTests
{
    private readonly ITestOutputHelper _output;

    public RssSmokeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // This is an opt-in smoke test (skipped by default) because it depends on an external network endpoint.
    [Fact]
    public async Task Configured_Rss_Feed_ShouldBeReachable_AndXmlLike()
    {
        var appSettingsPath = FindAppSettingsPath();
        _output.WriteLine($"Using appsettings: {appSettingsPath}");

        var config = new ConfigurationBuilder()
            .AddJsonFile(appSettingsPath, optional: false)
            .Build();

        var urls = config.GetSection("GoldNews:RssUrls").Get<string[]>()
                   ?? (config.GetSection("GoldNews").GetValue<string>("RssUrl") is { Length: > 0 } single
                       ? new[] { single }
                       : Array.Empty<string>());

        Assert.NotEmpty(urls);

        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.All
        };

        using var http = new HttpClient(handler);
        http.Timeout = TimeSpan.FromSeconds(20);

        http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0 Safari/537.36");
        http.DefaultRequestHeaders.Accept.ParseAdd("application/rss+xml,application/atom+xml,application/xml;q=0.9,text/xml;q=0.8,*/*;q=0.1");
        http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
        http.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };

        var failures = new List<string>();

        foreach (var urlString in urls.Where(u => !string.IsNullOrWhiteSpace(u)).Select(u => u.Trim()))
        {
            if (!Uri.TryCreate(urlString, UriKind.Absolute, out var url) || string.IsNullOrWhiteSpace(url.Host))
            {
                failures.Add($"{urlString} -> invalid URL");
                continue;
            }

            _output.WriteLine($"GET {url}");

            // DNS preflight so we can tell apart DNS failures from HTTP failures.
            try
            {
                var addresses = await Dns.GetHostAddressesAsync(url.Host);
                _output.WriteLine($"DNS: {url.Host} -> {string.Join(", ", addresses.Select(a => a.ToString()))}");
            }
            catch (SocketException se)
            {
                _output.WriteLine($"DNS FAILED: {url.Host} -> {se.SocketErrorCode} ({se.Message})");
                _output.WriteLine("---");
                failures.Add($"{url} -> DNS failure: {se.SocketErrorCode}");
                continue;
            }

            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Referrer = new Uri("https://www.google.com/");

                using var resp = await http.SendAsync(req);
                var body = await resp.Content.ReadAsStringAsync();

                _output.WriteLine($"Final URL: {resp.RequestMessage?.RequestUri}");
                _output.WriteLine($"Status: {(int)resp.StatusCode} {resp.ReasonPhrase}");
                _output.WriteLine($"Content-Type: {resp.Content.Headers.ContentType}");
                _output.WriteLine($"Body length: {body.Length}");
                _output.WriteLine($"Body snippet: {TrimForLog(body)}");
                _output.WriteLine("---");

                if (string.IsNullOrWhiteSpace(body))
                {
                    failures.Add($"{url} -> {(int)resp.StatusCode} {resp.ReasonPhrase} but empty body");
                    continue;
                }

                if (resp.StatusCode != HttpStatusCode.OK)
                {
                    failures.Add($"{url} -> {(int)resp.StatusCode} {resp.ReasonPhrase}");
                    continue;
                }

                if (body.Contains("<rss", StringComparison.OrdinalIgnoreCase) ||
                    body.Contains("<feed", StringComparison.OrdinalIgnoreCase))
                {
                    _output.WriteLine($"Success: looks like RSS/Atom for {url}");
                    return;
                }

                failures.Add($"{url} -> 200 but not RSS/Atom (first chars: {TrimForLog(body)})");
            }
            catch (HttpRequestException ex)
            {
                _output.WriteLine($"HTTP EX for {url}: {ex.GetType().Name}: {ex.Message}");
                _output.WriteLine("---");
                failures.Add($"{url} -> EX: {ex.GetType().Name}: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                _output.WriteLine($"TIMEOUT for {url}: {ex.Message}");
                _output.WriteLine("---");
                failures.Add($"{url} -> TIMEOUT");
            }
        }

        Assert.Fail("All configured RSS URLs failed. Details: " + string.Join(" | ", failures));
    }

    private static string FindAppSettingsPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var sln = Path.Combine(dir.FullName, "PricePredictor.sln");
            if (File.Exists(sln))
            {
                var candidate = Path.Combine(dir.FullName, "PricePredictor.Api", "appsettings.json");
                if (!File.Exists(candidate))
                {
                    throw new FileNotFoundException($"Found solution at '{sln}', but appsettings not found at '{candidate}'.");
                }

                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException("Could not locate 'PricePredictor.sln' by walking up from test output directory. Can't resolve PricePredictor.Api/appsettings.json.");
    }

    private static string TrimForLog(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
        s = s.Replace("\r", " ").Replace("\n", " ");
        return s.Length <= 400 ? s : s[..400] + "...";
    }
}
