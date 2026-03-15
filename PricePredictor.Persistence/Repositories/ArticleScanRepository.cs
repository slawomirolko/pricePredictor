using Microsoft.EntityFrameworkCore;
using AppModels = PricePredictor.Application.Models;
using AppNews = PricePredictor.Application.News;

namespace PricePredictor.Persistence.Repositories;

internal sealed class ArticlesRepository : AppNews.IArticleScanRepository
{
    private readonly PricePredictorDbContext _context;

    public ArticlesRepository(PricePredictorDbContext context)
    {
        _context = context;
    }

    // TODO to remove in next refactoring and replace with ddd...
    public async Task<bool> SaveArticleAsync(AppModels.Article article, CancellationToken cancellationToken)
    {
        var existingArticle = await _context.Articles
            .FirstOrDefaultAsync(e => e.ArticleLinkId == article.ArticleLinkId, cancellationToken);

        if (existingArticle is null)
        {
            var newArticleResult = AppModels.Article.Create(
                articleLinkId: article.ArticleLinkId,
                isTradingUseful: article.IsTradingUseful,
                scannedAtUtc: article.ScannedAtUtc,
                summary: article.Summary,
                id: article.Id == Guid.Empty ? null : article.Id);

            if (newArticleResult.IsError)
            {
                return false;
            }

            await _context.Articles.AddAsync(newArticleResult.Value, cancellationToken);
            return true;
        }

        existingArticle.UpdateScan(article.IsTradingUseful, article.ScannedAtUtc, article.Summary);
        return true;
    }

    public async Task<IReadOnlySet<Guid>> GetScannedArticleLinkIdsAsync(
        IReadOnlyCollection<Guid> articleLinkIds,
        CancellationToken cancellationToken)
    {
        if (articleLinkIds.Count == 0)
        {
            return new HashSet<Guid>();
        }

        var ids = await _context.Articles
            .AsNoTracking()
            .Where(x => articleLinkIds.Contains(x.ArticleLinkId) && x.ScannedAtUtc != default)
            .Select(x => x.ArticleLinkId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }
}
