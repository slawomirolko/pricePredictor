using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PricePredictor.Application.Data;
using PricePredictor.Application.News;

namespace PricePredictor.ArticlesFinderApp;

public sealed class ArticlesFinderApp
{
    private static readonly TimeSpan SyncInterval = TimeSpan.FromSeconds(60);
    private readonly ILogger<ArticlesFinderApp> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public ArticlesFinderApp(
        ILogger<ArticlesFinderApp> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
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
                await Task.Delay(SyncInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("Articles finder application stopping.");
    }
}
