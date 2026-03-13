using Microsoft.EntityFrameworkCore;
using AppModels = PricePredictor.Application.Models;
using AppNews = PricePredictor.Application.News;

namespace PricePredictor.Persistence.Repositories;

/// <summary>
/// EF Core repository for persisting article links.
/// </summary>
internal sealed class ArticleRepository : AppNews.IArticleRepository
{
    private readonly PricePredictorDbContext _context;

    public ArticleRepository(PricePredictorDbContext context)
    {
        _context = context;
    }

    public async Task SaveArticleLinkAsync(AppModels.ArticleLink link, CancellationToken cancellationToken)
    {
        var entity = AppModels.ArticleLink.CreateFrom(
            id: link.Id == Guid.Empty ? Guid.CreateVersion7() : link.Id,
            url: link.Url,
            publishedAtUtc: link.PublishedAtUtc,
            source: link.Source,
            extractedAtUtc: link.ExtractedAtUtc,
            isTradeUseful: link.IsTradeUseful);

        try
        {
            _context.ArticleLinks.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            // URL already exists (unique constraint) — ignore
        }
    }

    public async Task<IReadOnlyList<AppModels.ArticleLink>> GetAllLinksAsync(CancellationToken cancellationToken)
    {
        var entities = await _context.ArticleLinks
            .OrderByDescending(e => e.PublishedAtUtc)
            .ToListAsync(cancellationToken);

        return entities
            .Select(e => AppModels.ArticleLink.CreateFrom(
                id: e.Id,
                url: e.Url,
                publishedAtUtc: e.PublishedAtUtc,
                source: e.Source,
                extractedAtUtc: e.ExtractedAtUtc,
                isTradeUseful: e.IsTradeUseful))
            .ToList();
    }
}
