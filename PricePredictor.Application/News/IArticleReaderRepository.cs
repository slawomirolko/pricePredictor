using PricePredictor.Application.Models;

namespace PricePredictor.Application.News;

/// <summary>
/// Repository for reading and updating article links.
/// </summary>
public interface IArticleReaderRepository
{
    /// <summary>
    /// Returns all ArticleLinks where IsProcessed is false.
    /// </summary>
    Task<IReadOnlyList<ArticleLink>> GetUnprocessedLinksAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns ArticleLinks matching the provided IDs.
    /// </summary>
    Task<IReadOnlyList<ArticleLink>> GetLinksByIdsAsync(
        IReadOnlyCollection<Guid> articleLinkIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// Marks an ArticleLink as processed.
    /// </summary>
    Task MarkLinkAsProcessedAsync(Guid articleLinkId, CancellationToken cancellationToken);

    /// <summary>
    /// Saves an ArticleLink.
    /// </summary>
    Task SaveArticleLinkAsync(ArticleLink link, CancellationToken cancellationToken);
}
