using ErrorOr;

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
        DateTime scannedAtUtc,
        string? summary)
    {
        Id = id;
        ArticleLinkId = articleLinkId;
        IsTradingUseful = isTradingUseful;
        ScannedAtUtc = DateTime.SpecifyKind(scannedAtUtc, DateTimeKind.Utc);
        Summary = summary;
    }

    public Guid Id { get; private set; }
    public Guid ArticleLinkId { get; private set; }
    public bool? IsTradingUseful { get; private set; }
    public DateTime ScannedAtUtc { get; private set; }
    public string? Summary { get; private set; }

    public static ErrorOr<Article> Create(
        Guid articleLinkId,
        bool? isTradingUseful,
        DateTime scannedAtUtc,
        string? summary = null,
        Guid? id = null)
    {
        var resolvedId = id ?? Guid.CreateVersion7();

        if (resolvedId == Guid.Empty)
        {
            return Error.Unexpected(
                code: "Article.Id.Empty",
                description: "Article id cannot be empty.");
        }

        if (articleLinkId == Guid.Empty)
        {
            return Error.Unexpected(
                code: "Article.ArticleLinkId.Empty",
                description: "Article link id cannot be empty.");
        }

        return new Article(resolvedId, articleLinkId, isTradingUseful, scannedAtUtc, summary);
    }

    public void UpdateScan(bool? isTradingUseful, DateTime scannedAtUtc, string? summary)
    {
        IsTradingUseful = isTradingUseful;
        ScannedAtUtc = DateTime.SpecifyKind(scannedAtUtc, DateTimeKind.Utc);
        Summary = summary;
    }
}
