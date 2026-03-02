using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace PricePredicator.App.GoldNews;

internal sealed class GoldNewsClient : IGoldNewsClient
{
    private readonly HttpClient _http;

    public GoldNewsClient(HttpClient http)
    {
        _http = http;
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

    private static string TrimForLog(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
        s = s.Replace("\r", " ").Replace("\n", " ");
        return s.Length <= 400 ? s : s[..400] + "...";
    }

    public async Task EnsureQdrantCollectionAsync(string qdrantBaseUrl, CancellationToken cancellationToken)
    {
        var body = """
                   {
                     "vectors": {
                       "size": 3072,
                       "distance": "Cosine"
                     }
                   }
                   """;

        try
        {
            using var response = await _http.PutAsync(
                $"{qdrantBaseUrl.TrimEnd('/')}/collections/gold_news",
                new StringContent(body, Encoding.UTF8, "application/json"),
                cancellationToken);

            // Don't throw: collection may already exist; caller will log based on status code.
            // Still ensure request completes.
        }
        catch (HttpRequestException) when (!cancellationToken.IsCancellationRequested)
        {
            // Network/DNS issues (e.g. running outside Docker with qdrant hostname) should not crash startup.
            // Caller logs a generic error; we intentionally swallow here.
        }
        catch (SocketException) when (!cancellationToken.IsCancellationRequested)
        {
            // Same as above; keep service alive.
        }
    }

    public async Task UpsertPointsAsync(string qdrantBaseUrl, string collectionName, object body, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(body);

        using var response = await _http.PutAsync(
            $"{qdrantBaseUrl.TrimEnd('/')}/collections/{collectionName}/points",
            new StringContent(json, Encoding.UTF8, "application/json"),
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }
}
