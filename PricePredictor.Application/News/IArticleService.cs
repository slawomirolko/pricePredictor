using PricePredictor.Application.Models;

namespace PricePredictor.Application.News;

/// <summary>
/// Service that extracts article links from news sources and stores them in the database.
/// </summary>
public interface IArticleService
{
    /// <summary>
    /// Scrapes news sources, extracts article links with dates, and saves them to the database.
    /// </summary>
    Task<IReadOnlyList<ArticleLink>> SyncArticleLinksAsync(CancellationToken cancellationToken);
}
