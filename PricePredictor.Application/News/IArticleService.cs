using PricePredictor.Application.Models;

namespace PricePredictor.Application.News;

public interface IArticleService
{
    Task<ArticleSyncResult> SyncArticleLinksAsync(CancellationToken cancellationToken);
}

public sealed record ArticleSyncResult(
    bool Succeeded,
    bool IsSourceBlocked,
    string Message,
    IReadOnlyList<ArticleLink> ArticleLinks)
{
    public static ArticleSyncResult Success(IReadOnlyList<ArticleLink> articleLinks) => new(
        Succeeded: true,
        IsSourceBlocked: false,
        Message: $"Saved {articleLinks.Count} article links.",
        ArticleLinks: articleLinks);

    public static ArticleSyncResult SourceBlocked(string message) => new(
        Succeeded: false,
        IsSourceBlocked: true,
        Message: message,
        ArticleLinks: []);
}
