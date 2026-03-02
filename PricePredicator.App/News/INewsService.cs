namespace PricePredicator.App.News;

public interface INewsService
{
    Task<IReadOnlyList<NewsItem>> GetGoldNewsAsync(int count, CancellationToken cancellationToken);
}
