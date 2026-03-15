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
        DateTime readAt,
        string source,
        bool isProcessed)
    {
        Id = id;
        Url = url;
        ReadAt = DateTime.SpecifyKind(readAt, DateTimeKind.Utc);
        Source = source;
        IsProcessed = isProcessed;
    }

    public Guid Id { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public DateTime ReadAt { get; private set; }
    public string Source { get; private set; } = string.Empty;
    public bool IsProcessed { get; private set; }

    public static ArticleLink Create(
        string url,
        DateTime readAt,
        string source,
        bool isProcessed = false,
        Guid? id = null)
    {
        var resolvedId = id ?? Guid.CreateVersion7();

        if (resolvedId == Guid.Empty)
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
            resolvedId,
            url,
            readAt,
            source,
            isProcessed);
    }

    public void MarkProcessed()
    {
        IsProcessed = true;
    }
}
