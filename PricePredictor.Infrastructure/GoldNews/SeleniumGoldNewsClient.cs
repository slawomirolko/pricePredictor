using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PricePredictor.Application.Finance.Interfaces;
using PricePredictor.Application.News;

namespace PricePredictor.Infrastructure.GoldNews;

public sealed class SeleniumGoldNewsClient : IGoldNewsClient
{
    private readonly HttpClient _http;
    private readonly IArticleContentExtractionService _articleContentExtractionService;
    private readonly ISeleniumFlowBuilderFactory _seleniumFlowBuilderFactory;
    private readonly GoldNewsSettings _settings;
    private readonly ILogger<SeleniumGoldNewsClient> _logger;

    public SeleniumGoldNewsClient(
        HttpClient http,
        IArticleContentExtractionService articleContentExtractionService,
        ISeleniumFlowBuilderFactory seleniumFlowBuilderFactory,
        IOptions<GoldNewsSettings> settings,
        ILogger<SeleniumGoldNewsClient> logger)
    {
        _http = http;
        _articleContentExtractionService = articleContentExtractionService;
        _seleniumFlowBuilderFactory = seleniumFlowBuilderFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> GetRssXmlAsync(string rssUrl, CancellationToken cancellationToken)
    {
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
            _logger.LogInformation("Fetching article via Selenium flow: {Url}", articleUrl);

            using var seleniumFlow = _seleniumFlowBuilderFactory.Create(_settings.Headless);

            var navigationResult = await OpenArticleAsync(seleniumFlow, articleUrl, cancellationToken);
            if (!navigationResult.IsSuccess)
            {
                _logger.LogWarning("{Error}", navigationResult.Error);
                return null;
            }

            var htmlResult = ReadHtml(seleniumFlow);
            if (!htmlResult.IsSuccess)
            {
                _logger.LogWarning("{Error}", htmlResult.Error);
                return null;
            }

            var contentResult = await ExtractContentAsync(seleniumFlow, htmlResult.Value, articleTitle, cancellationToken);
            if (!contentResult.IsSuccess)
            {
                _logger.LogWarning("{Error}", contentResult.Error);
                return null;
            }

            _logger.LogInformation("Extracted {Length} chars from article", contentResult.Value.Length);
            return contentResult.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Selenium flow exception: {Message}", ex.Message);
            return null;
        }
    }

    private async Task<FlowResult<bool>> OpenArticleAsync(
        ISeleniumFlowBuilder seleniumFlow,
        string articleUrl,
        CancellationToken cancellationToken)
    {
        var openResult = await seleniumFlow.OpenAsync(articleUrl, cancellationToken);
        if (openResult.IsError)
        {
            return FlowResult<bool>.Fail(openResult.FirstError.Description);
        }

        var waitAfterOpenResult = await seleniumFlow.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
        if (waitAfterOpenResult.IsError)
        {
            return FlowResult<bool>.Fail(waitAfterOpenResult.FirstError.Description);
        }

        _logger.LogInformation("Initial page title: {Title}", seleniumFlow.Title);

        var blockedResult = seleniumFlow.IsBlockedPage();
        if (blockedResult.IsError)
        {
            return FlowResult<bool>.Fail(blockedResult.FirstError.Description);
        }

        if (blockedResult.Value)
        {
            _logger.LogWarning("Bot detection or captcha detected on {Url}", articleUrl);
            var waitBlockedResult = await seleniumFlow.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
            if (waitBlockedResult.IsError)
            {
                return FlowResult<bool>.Fail(waitBlockedResult.FirstError.Description);
            }

            blockedResult = seleniumFlow.IsBlockedPage();
            if (blockedResult.IsError)
            {
                return FlowResult<bool>.Fail(blockedResult.FirstError.Description);
            }

            if (blockedResult.Value)
            {
                _logger.LogInformation("Refreshing page");
                var refreshResult = await seleniumFlow.RefreshAsync(cancellationToken);
                if (refreshResult.IsError)
                {
                    return FlowResult<bool>.Fail(refreshResult.FirstError.Description);
                }

                var waitAfterRefreshResult = await seleniumFlow.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
                if (waitAfterRefreshResult.IsError)
                {
                    return FlowResult<bool>.Fail(waitAfterRefreshResult.FirstError.Description);
                }

                _logger.LogInformation("Page title after refresh: {Title}", seleniumFlow.Title);
            }
        }

        var waitBeforeConsentResult = await seleniumFlow.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        if (waitBeforeConsentResult.IsError)
        {
            return FlowResult<bool>.Fail(waitBeforeConsentResult.FirstError.Description);
        }

        var consentResult = await seleniumFlow.AcceptConsentAsync(cancellationToken);
        if (consentResult.IsError)
        {
            return FlowResult<bool>.Fail(consentResult.FirstError.Description);
        }

        return FlowResult<bool>.Success(true);
    }

    private FlowResult<string> ReadHtml(ISeleniumFlowBuilder seleniumFlow)
    {
        var htmlResult = seleniumFlow.GetHtml();
        if (htmlResult.IsError)
        {
            return FlowResult<string>.Fail(htmlResult.FirstError.Description);
        }

        if (string.IsNullOrWhiteSpace(htmlResult.Value))
        {
            return FlowResult<string>.Fail("Selenium flow returned empty page source");
        }

        _logger.LogInformation("Selenium flow received {Bytes} bytes", htmlResult.Value.Length);
        return FlowResult<string>.Success(htmlResult.Value);
    }

    private async Task<FlowResult<string>> ExtractContentAsync(
        ISeleniumFlowBuilder seleniumFlow,
        string html,
        string? articleTitle,
        CancellationToken cancellationToken)
    {
        var bodyTextResult = seleniumFlow.GetBodyText();
        if (bodyTextResult.IsError)
        {
            return FlowResult<string>.Fail(bodyTextResult.FirstError.Description);
        }

        var extractionTitle = string.IsNullOrWhiteSpace(articleTitle) ? seleniumFlow.Title : articleTitle;

        var content = await _articleContentExtractionService.ExtractAsync(
            html,
            bodyTextResult.Value,
            extractionTitle,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(content))
        {
            return FlowResult<string>.Fail("No content extracted");
        }

        return FlowResult<string>.Success(content);
    }

    private readonly record struct FlowResult<T>(bool IsSuccess, T Value, string? Error)
    {
        public static FlowResult<T> Success(T value) => new(true, value, null);
        public static FlowResult<T> Fail(string error) => new(false, default!, error);
    }
}
