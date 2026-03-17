using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PricePredictor.Application;
using PricePredictor.Application.Data;
using PricePredictor.Application.Models;
using PricePredictor.Application.News;
using GoldNewsSettings = PricePredictor.Infrastructure.GoldNewsSettings;

namespace PricePredictor.ArticlesReaderApp;

public sealed class ArticlesReaderApp
{
    private readonly ILogger<ArticlesReaderApp> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly GoldNewsSettings _settings;
    private readonly TradingIndicatorNotificationService _notificationService;
    private readonly INewsArticleChannel _newsArticleChannel;

    public ArticlesReaderApp(
        ILogger<ArticlesReaderApp> logger,
        IServiceScopeFactory scopeFactory,
        IOptions<GoldNewsSettings> settings,
        TradingIndicatorNotificationService notificationService,
        INewsArticleChannel newsArticleChannel)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _notificationService = notificationService;
        _newsArticleChannel = newsArticleChannel;
    }

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Articles reader app started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessUnprocessedLinksAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during article reading cycle");
            }

            var interval = CalculateWaitingTime();
            _logger.LogInformation("Articles reader sleeping for {Interval}", interval);

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("Articles reader app stopping.");
    }

    private TimeSpan CalculateWaitingTime()
    {
        var minDelaySeconds = _settings.ArticlesReaderMinDelaySeconds <= 0 ? 20 : _settings.ArticlesReaderMinDelaySeconds;
        var maxDelaySeconds = _settings.ArticlesReaderMaxDelaySeconds <= 0 ? 30 : _settings.ArticlesReaderMaxDelaySeconds;
        if (minDelaySeconds > maxDelaySeconds)
        {
            (minDelaySeconds, maxDelaySeconds) = (maxDelaySeconds, minDelaySeconds);
        }

        var delaySeconds = Random.Shared.Next(minDelaySeconds, maxDelaySeconds + 1);
        var interval = TimeSpan.FromSeconds(delaySeconds);
        return interval;
    }

    private async Task ProcessUnprocessedLinksAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var extractionClient = scope.ServiceProvider.GetRequiredService<IOllamaArticleExtractionClient>();
        var extractionService = scope.ServiceProvider.GetRequiredService<IArticleContentExtractionService>();
        var seleniumFactory = scope.ServiceProvider.GetRequiredService<ISeleniumFlowBuilderFactory>();

        var unprocessedLinks = await unitOfWork.ArticleLinks.GetUnprocessedLinksAsync(stoppingToken);
        var unknownTradingUsefulnessLinkIds = await unitOfWork.Articles
            .GetArticleLinkIdsWithUnknownTradingUsefulnessAsync(stoppingToken);
        var usefulTradingMissingSummaryLinkIds = await unitOfWork.Articles
            .GetArticleLinkIdsWithUsefulTradingMissingSummaryAsync(stoppingToken);

        var allCandidateLinkIds = unprocessedLinks
            .Select(link => link.Id)
            .Concat(unknownTradingUsefulnessLinkIds)
            .Concat(usefulTradingMissingSummaryLinkIds)
            .Distinct()
            .ToArray();

        if (allCandidateLinkIds.Length == 0)
        {
            _logger.LogInformation("No article links requiring processing found.");
            return;
        }

        var linksToProcess = await unitOfWork.ArticleLinks.GetLinksByIdsAsync(allCandidateLinkIds, stoppingToken);

        if (linksToProcess.Count == 0)
        {
            _logger.LogInformation("No article links found for the candidate IDs.");
            return;
        }

        var linkToProcess = linksToProcess.OrderByDescending(link => link.ReadAt).FirstOrDefault();
        if (linkToProcess is null)
        {
            _logger.LogInformation("No article link selected for processing.");
            return;
        }

        _logger.LogInformation(
            "Processing 1 article link (Unprocessed={UnprocessedCount}, UnknownUsefulness={UnknownUsefulnessCount}, UsefulMissingSummary={UsefulMissingSummaryCount}). Selected={Url}",
            unprocessedLinks.Count,
            unknownTradingUsefulnessLinkIds.Count,
            usefulTradingMissingSummaryLinkIds.Count,
            linkToProcess.Url);

        using var selenium = seleniumFactory.Create();

        await ProcessLinkAsync(
            linkToProcess,
            selenium,
            unitOfWork,
            extractionClient,
            extractionService,
            _settings.EmbeddingDimensions,
            _newsArticleChannel,
            stoppingToken);
    }

    private async Task ProcessLinkAsync(
        ArticleLink link,
        ISeleniumFlowBuilder selenium,
        IUnitOfWork unitOfWork,
        IOllamaArticleExtractionClient extractionClient,
        IArticleContentExtractionService extractionService,
        int embeddingDimensions,
        INewsArticleChannel newsArticleChannel,
        CancellationToken stoppingToken)
    {
        _logger.LogInformation("Processing article link: {Url}", link.Url);

        try
        {
            var navigateResult = await selenium.NavigateToReadyPageAsync(link.Url, stoppingToken);
            if (navigateResult.IsError)
            {
                _logger.LogWarning(
                    "Failed to navigate to article link {Url}. CurrentUrl={CurrentUrl}. Error: {Error}",
                    link.Url,
                    selenium.CurrentUrl,
                    navigateResult.FirstError.Description);
                return;
            }

            var content = await FetchArticleContentAsync(selenium, extractionService, stoppingToken);

            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("No content extracted for {Url}. Leaving link unprocessed for retry.", link.Url);
                return;
            }

            var isTradingUseful = await extractionClient.AssessTradingUsefulnessAsync(content, stoppingToken);
            _logger.LogInformation("Trading usefulness for {Url}: {IsUseful}", link.Url, isTradingUseful);

            string? summary = null;
            if (isTradingUseful)
            {
                summary = await extractionClient.SummarizeAsync(content, stoppingToken);
                _logger.LogInformation("Summary generated for {Url} ({Length} chars)", link.Url, summary.Length);
            }

            var articleResult = Article.Create(link.Id, isTradingUseful, scannedAtUtc: DateTime.UtcNow, summary: summary);
            if (articleResult.IsError)
            {
                _logger.LogWarning(
                    "Skipping link {Url}: failed to create Article model. Errors: {Errors}",
                    link.Url,
                    string.Join("; ", articleResult.Errors.Select(error => error.Description)));
                return;
            }

            var article = articleResult.Value;
            var saved = await unitOfWork.Articles.SaveArticleAsync(article, stoppingToken);

            if (isTradingUseful && summary is not null)
            {
                var embedding = await extractionClient.EmbedAsync(summary, stoppingToken);

                await StoreEmbeddingAsync(
                    article.Id,
                    link.ReadAt,
                    summary,
                    embedding,
                    embeddingDimensions,
                    unitOfWork,
                    stoppingToken);

                await _notificationService.SendArticleSummaryNotificationAsync(
                    source: link.Source,
                    url: link.Url,
                    readAt: link.ReadAt,
                    summary: summary,
                    cancellationToken: stoppingToken);

                newsArticleChannel.Publish(new NewsItem(
                    Title: summary[..Math.Min(120, summary.Length)],
                    Link: link.Url,
                    PublishedAtUtc: new DateTimeOffset(link.ReadAt, TimeSpan.Zero),
                    Source: link.Source));
            }

            if (saved && article.IsTradingUseful is not null)
            {
                link.MarkProcessed();
                await unitOfWork.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("Finished processing {Url}", link.Url);
            }
            else
            {
                _logger.LogWarning(
                    "Link {Url} not marked as processed: saved={Saved}, IsTradingUseful={IsTradingUseful}",
                    link.Url, saved, article.IsTradingUseful);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process article link {Url}", link.Url);
        }
    }

    private static async Task<string?> FetchArticleContentAsync(
        ISeleniumFlowBuilder selenium,
        IArticleContentExtractionService extractionService,
        CancellationToken stoppingToken)
    {
        var htmlResult = selenium.GetHtml();
        if (htmlResult.IsError)
        {
            return null;
        }

        var bodyResult = selenium.GetBodyText();
        var fallback = bodyResult.IsError ? null : bodyResult.Value;

        var extracted = await extractionService.ExtractAsync(htmlResult.Value, fallback, selenium.Title, stoppingToken);
        if (!string.IsNullOrWhiteSpace(extracted))
        {
            return extracted;
        }

        // Retry once after a short wait because Reuters pages can hydrate paragraphs asynchronously.
        var waitBeforeRetryResult = await selenium.WaitAsync(TimeSpan.FromSeconds(4), stoppingToken);
        if (waitBeforeRetryResult.IsError)
        {
            return null;
        }

        var htmlRetryResult = selenium.GetHtml();
        if (htmlRetryResult.IsError)
        {
            return null;
        }

        var bodyRetryResult = selenium.GetBodyText();
        var fallbackRetry = bodyRetryResult.IsError ? fallback : bodyRetryResult.Value;

        return await extractionService.ExtractAsync(htmlRetryResult.Value, fallbackRetry, selenium.Title, stoppingToken);
    }

    private async Task StoreEmbeddingAsync(
        Guid articleId,
        DateTime readAt,
        string summary,
        IReadOnlyList<float> embedding,
        int dimensions,
        IUnitOfWork unitOfWork,
        CancellationToken stoppingToken)
    {
        try
        {
            await unitOfWork.Embeddings.UpsertArticleAsync(
                articleId: articleId,
                readAtUtc: readAt,
                summary: summary,
                embedding: embedding,
                dimensions: dimensions,
                cancellationToken: stoppingToken);

            _logger.LogInformation("Stored embedding for Article {ArticleId} (ReadAt={ReadAt})", articleId, readAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store embedding for Article {ArticleId}", articleId);
        }
    }
}

