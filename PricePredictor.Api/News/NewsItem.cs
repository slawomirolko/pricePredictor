namespace PricePredictor.Api.News;

public record NewsItem(
    string Title,
    string Link,
    DateTimeOffset? PublishedAtUtc,
    string Source
);

