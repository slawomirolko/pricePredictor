using PricePredictor.Application.Models;

namespace PricePredictor.Application.News;

/// <summary>
/// Repository for reading unprocessed article links and persisting processed articles.
/// </summary>
public interface IArticleReaderRepository
{
    /// <summary>
    /// Returns all ArticleLinks where IsProcessed is false.
    /// </summary>
    Task<IReadOnlyList<ArticleLink>> GetUnprocessedLinksAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Persists a fully processed article.
    /// </summary>
    Task SaveArticleAsync(Article article, CancellationToken cancellationToken);

    /// <summary>
    /// Marks an ArticleLink as processed.
    /// </summary>
    Task MarkLinkAsProcessedAsync(Guid articleLinkId, CancellationToken cancellationToken);
}

