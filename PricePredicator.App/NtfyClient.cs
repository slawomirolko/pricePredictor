namespace PricePredicator.App;

using System.Text;

public class NtfyClient
{
    private readonly HttpClient _httpClient;

    public NtfyClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task SendAsync(string topic, string message)
    {
        var content = new StringContent(message, Encoding.UTF8, "text/plain");
        var response = await _httpClient.PostAsync(topic, content);
        response.EnsureSuccessStatusCode();
    }
}