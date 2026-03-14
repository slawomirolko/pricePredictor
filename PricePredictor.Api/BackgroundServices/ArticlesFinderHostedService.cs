using PricePredictor.Application.News;

namespace PricePredictor.Api.BackgroundServices;

public class ArticlesFinderHostedService : BackgroundService
{
    private readonly ILogger<ArticlesFinderHostedService> _logger;
    private readonly IArticleService _articleService;
    private readonly IArticleRepository _articleRepository;

    public ArticlesFinderHostedService(
        ILogger<ArticlesFinderHostedService> logger,
        IArticleService articleService,
        IArticleRepository articleRepository)
    {
        _logger = logger;
        _articleService = articleService;
        _articleRepository = articleRepository;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Articles finder background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Starting article sync at {Time}", DateTime.UtcNow);

                var processedCount = await _articleRepository.MarkProcessedFromScannedArticlesAsync(stoppingToken);
                if (processedCount > 0)
                {
                    _logger.LogInformation("Marked {Count} ArticleLinks as processed from scanned Articles.", processedCount);
                }

                var articles = await _articleService.SyncArticleLinksAsync(stoppingToken);

                _logger.LogInformation("Article sync completed. Saved {Count} article links.", articles.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during article sync");
            }

            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }

        _logger.LogInformation("Articles finder background service stopping.");
    }
}