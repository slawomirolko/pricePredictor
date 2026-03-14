namespace PricePredictor.Application.Models;

/// <summary>
/// Represents a fully processed article with trading assessment and embedding data.
/// </summary>
public sealed class Article
{
    private Article()
    {
    }

    private Article(
        Guid id,
        Guid articleLinkId,
        bool? isTradingUseful,
        DateTime scannedAtUtc)
    {
        Id = id;
        ArticleLinkId = articleLinkId;
        IsTradingUseful = isTradingUseful;
        ScannedAtUtc = DateTime.SpecifyKind(scannedAtUtc, DateTimeKind.Utc);
    }

    public Guid Id { get; private set; }
    public Guid ArticleLinkId { get; private set; }
    public bool? IsTradingUseful { get; private set; }
    public DateTime ScannedAtUtc { get; private set; }

    public static Article Create(
        Guid articleLinkId,
        bool? isTradingUseful,
        DateTime scannedAtUtc)
    {
        return CreateFrom(
            Guid.CreateVersion7(),
            articleLinkId,
            isTradingUseful,
            scannedAtUtc);
    }

    public static Article CreateFrom(
        Guid id,
        Guid articleLinkId,
        bool? isTradingUseful,
        DateTime scannedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Article id cannot be empty.", nameof(id));
        }

        if (articleLinkId == Guid.Empty)
        {
            throw new ArgumentException("Article link id cannot be empty.", nameof(articleLinkId));
        }

        return new Article(id, articleLinkId, isTradingUseful, scannedAtUtc);
    }
}
