using PricePredictor.Application.Models;

namespace PricePredictor.Application.News;

/// <summary>
/// Repository for writing and reading scanned Article rows.
/// </summary>
public interface IArticleScanRepository
{
    /// <summary>
    /// Upserts a fully processed article by ArticleLinkId.
    /// </summary>
    Task<bool> SaveArticleAsync(Article article, CancellationToken cancellationToken);

    Task<IReadOnlySet<Guid>> GetScannedArticleLinkIdsAsync(
        IReadOnlyCollection<Guid> articleLinkIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> GetTradingUsefulArticleLinkIdsAsync(int maxCount, CancellationToken cancellationToken);

    Task<IReadOnlyList<Article>> GetByArticleLinkIdsAsync(
        IReadOnlyCollection<Guid> articleLinkIds,
        CancellationToken cancellationToken);

    Task<IReadOnlySet<Guid>> GetArticleLinkIdsWithUnknownTradingUsefulnessAsync(CancellationToken cancellationToken);

    Task<IReadOnlySet<Guid>> GetArticleLinkIdsWithUsefulTradingMissingSummaryAsync(CancellationToken cancellationToken);
}
