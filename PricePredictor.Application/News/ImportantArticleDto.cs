namespace PricePredictor.Application.News;

public sealed record ImportantArticleDto(
    Guid ArticleId,
    string Url,
    string Source,
    DateTime ReadAtUtc,
    string? Summary);
