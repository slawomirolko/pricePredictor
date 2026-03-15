using PricePredictor.Application.Finance;
using PricePredictor.Application.Finance.Interfaces;

namespace PricePredictor.Infrastructure.News;

public sealed class GoogleNewsRssClient : IGoogleNewsRssClient
{
    private readonly HttpClient _httpClient;
    private readonly GoogleNewsRssSettings _settings;

    public GoogleNewsRssClient(HttpClient httpClient, Microsoft.Extensions.Options.IOptions<GoogleNewsRssSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task<string> GetGoldNewsRssAsync(CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(_settings.RssPath, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
