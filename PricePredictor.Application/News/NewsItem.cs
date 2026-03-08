namespace PricePredictor.Application.News;

public record NewsItem(
    string Title,
    string Link,
    DateTimeOffset? PublishedAtUtc,
    string Source
);

