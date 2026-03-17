using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PricePredictor.Application.Data;
using PricePredictor.Application.News;

namespace PricePredictor.ArticlesFinderHostedService.BackgroundServices;

public sealed class ArticlesFinderHostedService : BackgroundService
{
    private static readonly TimeSpan SyncInterval = TimeSpan.FromSeconds(60);
    private readonly ILogger<ArticlesFinderHostedService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public ArticlesFinderHostedService(
        ILogger<ArticlesFinderHostedService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Articles finder background service started.");

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

                var processedCount = 0;
                foreach (var link in unprocessedLinks)
                {
                    if (!scannedLinkIds.Contains(link.Id))
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during article sync");
            }

            await Task.Delay(SyncInterval, stoppingToken);
        }

        _logger.LogInformation("Articles finder background service stopping.");
    }
}
