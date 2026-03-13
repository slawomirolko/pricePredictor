namespace PricePredictor.Application.Models;

/// <summary>
/// Represents an article link extracted from news sources.
/// </summary>
public sealed class ArticleLink
{
    private ArticleLink()
    {
    }

    private ArticleLink(
        Guid id,
        string url,
        DateTime publishedAtUtc,
        string source,
        DateTime? extractedAtUtc,
        bool isTradeUseful)
    {
        Id = id;
        Url = url;
        PublishedAtUtc = DateTime.SpecifyKind(publishedAtUtc, DateTimeKind.Utc);
        Source = source;
        ExtractedAtUtc = extractedAtUtc;
        IsTradeUseful = isTradeUseful;
    }

    public Guid Id { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public DateTime PublishedAtUtc { get; private set; }
    public string Source { get; private set; } = string.Empty;
    public DateTime? ExtractedAtUtc { get; private set; }
    public bool IsTradeUseful { get; private set; }

    public static ArticleLink Create(
        string url,
        DateTime publishedAtUtc,
        string source,
        DateTime? extractedAtUtc = null,
        bool isTradeUseful = false)
    {
        return CreateFrom(
            Guid.CreateVersion7(),
            url,
            publishedAtUtc,
            source,
            extractedAtUtc,
            isTradeUseful);
    }

    public static ArticleLink CreateFrom(
        Guid id,
        string url,
        DateTime publishedAtUtc,
        string source,
        DateTime? extractedAtUtc,
        bool isTradeUseful)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Article link id cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("Article link URL cannot be empty.", nameof(url));
        }

        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Article source cannot be empty.", nameof(source));
        }

        return new ArticleLink(
            id,
            url,
            publishedAtUtc,
            source,
            extractedAtUtc,
            isTradeUseful);
    }
}
