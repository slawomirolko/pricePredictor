namespace PricePredictor.Application.News;

public sealed record ImportantArticleDto(
    string Url,
    string Source,
    DateTime ReadAtUtc,
    string? Summary);
