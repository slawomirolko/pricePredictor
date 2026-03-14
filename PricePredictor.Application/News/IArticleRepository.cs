using PricePredictor.Application.Models;

namespace PricePredictor.Application.News;

/// <summary>
/// Repository interface for persisting article links.
/// </summary>
public interface IArticleRepository
{
    /// <summary>
    /// Saves an article link to the database.
    /// </summary>
    Task SaveArticleLinkAsync(ArticleLink link, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all saved article links.
    /// </summary>
    Task<IReadOnlyList<ArticleLink>> GetAllLinksAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Marks links as processed when a related scanned article already exists.
    /// </summary>
    Task<int> MarkProcessedFromScannedArticlesAsync(CancellationToken cancellationToken);
}
