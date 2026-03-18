using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PricePredictor.Application.Data;
using PricePredictor.Application.News;

namespace PricePredictor.ArticlesFinderApp;

public sealed class ArticlesFinderApp
{
    private readonly ILogger<ArticlesFinderApp> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly int _syncIntervalSecondsMin;
    private readonly int _syncIntervalSecondsMax;

    public ArticlesFinderApp(
        ILogger<ArticlesFinderApp> logger,
        IServiceScopeFactory scopeFactory,
        IOptions<ArticlesFinderSettings> settings)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        var appSettings = settings.Value;
        if (appSettings.SyncIntervalSecondsMin <= 0)
        {
            throw new InvalidOperationException(
                $"{ArticlesFinderSettings.SectionName}:{nameof(ArticlesFinderSettings.SyncIntervalSecondsMin)} must be greater than 0.");
        }

        if (appSettings.SyncIntervalSecondsMax < appSettings.SyncIntervalSecondsMin)
        {
            throw new InvalidOperationException(
                $"{ArticlesFinderSettings.SectionName}:{nameof(ArticlesFinderSettings.SyncIntervalSecondsMax)} must be greater than or equal to {nameof(ArticlesFinderSettings.SyncIntervalSecondsMin)}.");
        }

        _syncIntervalSecondsMin = appSettings.SyncIntervalSecondsMin;
        _syncIntervalSecondsMax = appSettings.SyncIntervalSecondsMax;
    }

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Articles finder application started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Starting article sync at {Time}", DateTime.UtcNow);

                using var scope = _scopeFactory.CreateScope();
                var articleService = scope.ServiceProvider.GetRequiredService<IArticleService>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var unprocessedLinks = await unitOfWork.ArticleLinks.GetUnprocessedLinksAsync(stoppingToken);
                var unprocessedIds = unprocessedLinks.Select(x => x.Id).ToArray();
                var scannedLinkIds = await unitOfWork.Articles
                    .GetScannedArticleLinkIdsAsync(unprocessedIds, stoppingToken);
                var scannedLinkIdsSet = scannedLinkIds.ToHashSet();

                var processedCount = 0;
                foreach (var link in unprocessedLinks)
                {
                    if (!scannedLinkIdsSet.Contains(link.Id))
                    {
                        continue;
                    }

                    link.MarkProcessed();
                    processedCount++;
                }

                if (processedCount > 0)
                {
                    await unitOfWork.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Marked {Count} ArticleLinks as processed from scanned Articles.", processedCount);
                }

                var syncResult = await articleService.SyncArticleLinksAsync(stoppingToken);
                if (syncResult.IsSourceBlocked)
                {
                    _logger.LogWarning("Article sync skipped because Reuters blocked access: {Message}", syncResult.Message);
                }
                else
                {
                    _logger.LogInformation("Article sync completed. Saved {Count} article links.", syncResult.ArticleLinks.Count);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during article sync");
            }

            try
            {
                var delaySeconds = Random.Shared.Next(_syncIntervalSecondsMin, _syncIntervalSecondsMax + 1);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("Articles finder application stopping.");
    }
}
