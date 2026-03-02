namespace PricePredicator.App.News;

public record NewsItem(
    string Title,
    string Link,
    DateTimeOffset? PublishedAtUtc,
    string Source
);
