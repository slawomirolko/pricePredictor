using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PricePredictor.Infrastructure.GoldNews;

public interface IOllamaCloudHttpClient
{
    Task<string> ChatAsync(string model, string systemPrompt, string userPrompt, CancellationToken cancellationToken);
}

public sealed class OllamaCloudHttpClient : IOllamaCloudHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly GoldNewsSettings _settings;
    private readonly ILogger<OllamaCloudHttpClient> _logger;

    public OllamaCloudHttpClient(
        HttpClient httpClient,
        IOptions<GoldNewsSettings> settings,
        ILogger<OllamaCloudHttpClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> ChatAsync(string model, string systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        var request = new OllamaChatRequest
        {
            Model = model,
            Messages = new[]
            {
                new OllamaChatMessage { Role = "system", Content = systemPrompt },
                new OllamaChatMessage { Role = "user", Content = userPrompt }
            },
            Stream = false
        };
        
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/chat");
        httpRequest.Content = JsonContent.Create(request);

        if (!string.IsNullOrWhiteSpace(_settings.CloudOllamaApiKey))
        {
            httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.CloudOllamaApiKey);
        }

        _logger.LogInformation("Calling Ollama Cloud Chat API: {Url}, Model: {Model}", _httpClient.BaseAddress, model);
        

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<System.Text.Json.Nodes.JsonObject>(cancellationToken: cancellationToken);
        _logger.LogDebug("Ollama Cloud Chat Response: {Json}", json?.ToJsonString());
        
        if (json != null && json.TryGetPropertyValue("message", out var messageNode) && messageNode is System.Text.Json.Nodes.JsonObject messageObj)
        {
            if (messageObj.TryGetPropertyValue("content", out var contentNode))
            {
                var content = contentNode?.ToString();
                if (!string.IsNullOrWhiteSpace(content))
                {
                    return content;
                }
            }
        }

        throw new InvalidOperationException(
            $"Ollama Cloud returned no message content. Model={model}, PromptLength={userPrompt.Length}, Response={json?.ToJsonString()}");
    }

    private sealed class OllamaChatRequest
    {
        [JsonPropertyName("model")]
        public required string Model { get; init; }

        [JsonPropertyName("messages")]
        public required OllamaChatMessage[] Messages { get; init; }

        [JsonPropertyName("stream")]
        public bool Stream { get; init; }
    }

    private sealed class OllamaChatMessage
    {
        [JsonPropertyName("role")]
        public required string Role { get; init; }

        [JsonPropertyName("content")]
        public required string Content { get; init; }
    }

    private sealed class OllamaChatResponse
    {
        [JsonPropertyName("message")]
        public OllamaChatMessage? Message { get; init; }
    }
}
