using Microsoft.Extensions.Options;
using PricePredictor.Application;
using PricePredictor.Application.Data;
using PricePredictor.Application.Models;
using PricePredictor.Application.News;
using GoldNewsSettings = PricePredictor.Infrastructure.GoldNewsSettings;

namespace PricePredictor.Api.BackgroundServices;

public sealed class ArticlesReaderHostedService : BackgroundService
{
    private readonly ILogger<ArticlesReaderHostedService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly GoldNewsSettings _settings;
    private readonly TradingIndicatorNotificationService _notificationService;

    public ArticlesReaderHostedService(
        ILogger<ArticlesReaderHostedService> logger,
        IServiceScopeFactory scopeFactory,
        IOptions<GoldNewsSettings> settings,
        TradingIndicatorNotificationService notificationService)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _notificationService = notificationService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Articles reader background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessUnprocessedLinksAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during article reading cycle");
            }

            var intervalMinutes = _settings.ArticlesReaderInterval <= 0 ? 1 : _settings.ArticlesReaderInterval;
            var interval = TimeSpan.FromMinutes(intervalMinutes);
            _logger.LogInformation("Articles reader sleeping for {Interval}", interval);
            await Task.Delay(interval, stoppingToken);
        }

        _logger.LogInformation("Articles reader background service stopping.");
    }

    private async Task ProcessUnprocessedLinksAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IArticleReaderRepository>();
        var embeddingRepository = scope.ServiceProvider.GetRequiredService<IGoldNewsEmbeddingRepository>();
        var extractionClient = scope.ServiceProvider.GetRequiredService<IOllamaArticleExtractionClient>();
        var extractionService = scope.ServiceProvider.GetRequiredService<IArticleContentExtractionService>();
        var seleniumFactory = scope.ServiceProvider.GetRequiredService<ISeleniumFlowBuilderFactory>();

        var unprocessedLinks = await repository.GetUnprocessedLinksAsync(stoppingToken);

        if (unprocessedLinks.Count == 0)
        {
            _logger.LogInformation("No unprocessed article links found.");
            return;
        }

        _logger.LogInformation("Processing {Count} unprocessed article links.", unprocessedLinks.Count);

        using var selenium = seleniumFactory.Create(headless: _settings.Headless);

        foreach (var link in unprocessedLinks)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await ProcessLinkAsync(
                link,
                selenium,
                repository,
                embeddingRepository,
                extractionClient,
                extractionService,
                _settings.EmbeddingDimensions,
                stoppingToken);
        }
    }

    private async Task ProcessLinkAsync(
        ArticleLink link,
        ISeleniumFlowBuilder selenium,
        IArticleReaderRepository repository,
        IGoldNewsEmbeddingRepository embeddingRepository,
        IOllamaArticleExtractionClient extractionClient,
        IArticleContentExtractionService extractionService,
        int embeddingDimensions,
        CancellationToken stoppingToken)
    {
        _logger.LogInformation("Processing article link: {Url}", link.Url);

        try
        {
            var content = await FetchArticleContentAsync(link, selenium, extractionService, stoppingToken);

            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("No content extracted for {Url}. Marking as processed.", link.Url);
                var emptyArticle = Article.Create(link.Id, isTradingUseful: null, scannedAtUtc: DateTime.UtcNow);
                await repository.SaveArticleAsync(emptyArticle, stoppingToken);
                await repository.MarkLinkAsProcessedAsync(link.Id, stoppingToken);
                return;
            }

            var isTradingUseful = await extractionClient.AssessTradingUsefulnessAsync(content, stoppingToken);
            _logger.LogInformation("Trading usefulness for {Url}: {IsUseful}", link.Url, isTradingUseful);

            var article = Article.Create(link.Id, isTradingUseful, scannedAtUtc: DateTime.UtcNow);
            await repository.SaveArticleAsync(article, stoppingToken);

            if (isTradingUseful)
            {
                var summary = await extractionClient.SummarizeAsync(content, stoppingToken);
                _logger.LogInformation("Summary generated for {Url} ({Length} chars)", link.Url, summary.Length);

                var embedding = await extractionClient.EmbedAsync(summary, stoppingToken);

                await StoreEmbeddingAsync(
                    article.Id,
                    link.ReadAt,
                    summary,
                    embedding,
                    embeddingDimensions,
                    embeddingRepository,
                    stoppingToken);

                await _notificationService.SendArticleSummaryNotificationAsync(
                    source: link.Source,
                    url: link.Url,
                    readAt: link.ReadAt,
                    summary: summary,
                    cancellationToken: stoppingToken);
            }

            await repository.MarkLinkAsProcessedAsync(link.Id, stoppingToken);
            _logger.LogInformation("Finished processing {Url}", link.Url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process article link {Url}", link.Url);
        }
    }

    private static async Task<string?> FetchArticleContentAsync(
        ArticleLink link,
        ISeleniumFlowBuilder selenium,
        IArticleContentExtractionService extractionService,
        CancellationToken stoppingToken)
    {
        var openResult = await selenium.OpenAsync(link.Url, stoppingToken);
        if (openResult.IsError)
        {
            return null;
        }

        await selenium.WaitAsync(TimeSpan.FromSeconds(5), stoppingToken);

        var htmlResult = selenium.GetHtml();
        if (htmlResult.IsError)
        {
            return null;
        }

        var bodyResult = selenium.GetBodyText();
        var fallback = bodyResult.IsError ? null : bodyResult.Value;

        return await extractionService.ExtractAsync(htmlResult.Value, fallback, null, stoppingToken);
    }

    private async Task StoreEmbeddingAsync(
        Guid articleId,
        DateTime readAt,
        string summary,
        IReadOnlyList<float> embedding,
        int dimensions,
        IGoldNewsEmbeddingRepository embeddingRepository,
        CancellationToken stoppingToken)
    {
        try
        {
            await embeddingRepository.UpsertArticleAsync(
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

