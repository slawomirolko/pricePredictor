using System.Text;
using PricePredictor.Application.Notifications;

namespace PricePredictor.Infrastructure;

public class NtfyClient : INtfyClient
{
    private readonly HttpClient _httpClient;

    public NtfyClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task SendAsync(string topic, string message, CancellationToken cancellationToken = default)
    {
        var content = new StringContent(message, Encoding.UTF8, "text/plain");
        var response = await _httpClient.PostAsync(topic, content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}