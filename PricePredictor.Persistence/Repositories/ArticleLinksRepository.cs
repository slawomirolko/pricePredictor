using Microsoft.EntityFrameworkCore;
using Npgsql;
using AppModels = PricePredictor.Application.Models;
using AppNews = PricePredictor.Application.News;

namespace PricePredictor.Persistence.Repositories;

internal sealed class ArticleLinksRepository : AppNews.IArticleReaderRepository
{
    private readonly PricePredictorDbContext _context;

    public ArticleLinksRepository(PricePredictorDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AppModels.ArticleLink>> GetUnprocessedLinksAsync(CancellationToken cancellationToken)
    {
        return await _context.ArticleLinks
            .Where(e => !e.IsProcessed)
            .OrderBy(e => e.ReadAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AppModels.ArticleLink>> GetLinksByIdsAsync(
        IReadOnlyCollection<Guid> articleLinkIds,
        CancellationToken cancellationToken)
    {
        if (articleLinkIds.Count == 0)
        {
            return [];
        }

        return await _context.ArticleLinks
            .Where(e => articleLinkIds.Contains(e.Id))
            .OrderBy(e => e.ReadAt)
            .ToListAsync(cancellationToken);
    }

    // TODO to remove in next refactoring
    public async Task MarkLinkAsProcessedAsync(Guid articleLinkId, CancellationToken cancellationToken)
    {
        var articleLink = await _context.ArticleLinks
            .FirstOrDefaultAsync(e => e.Id == articleLinkId, cancellationToken);

        if (articleLink is null)
        {
            return;
        }

        articleLink.MarkProcessed();
    }

    public async Task SaveArticleLinkAsync(AppModels.ArticleLink link, CancellationToken cancellationToken)
    {
        var entity = AppModels.ArticleLink.Create(
            url: link.Url,
            readAt: link.ReadAt,
            source: link.Source,
            isProcessed: link.IsProcessed,
            id: link.Id == Guid.Empty ? null : link.Id);

        var exists = await _context.ArticleLinks
            .AsNoTracking()
            .AnyAsync(x => x.Url == entity.Url, cancellationToken);

        if (exists)
        {
            return;
        }

        _context.ArticleLinks.Add(entity);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsArticleLinkUrlUniqueViolation(ex))
        {
            _context.Entry(entity).State = EntityState.Detached;
        }
    }

    private static bool IsArticleLinkUrlUniqueViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException pg &&
               pg.SqlState == PostgresErrorCodes.UniqueViolation &&
               string.Equals(pg.ConstraintName, "IX_ArticleLinks_Url", StringComparison.Ordinal);
    }
}
