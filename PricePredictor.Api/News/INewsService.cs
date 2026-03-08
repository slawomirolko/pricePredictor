namespace PricePredictor.Api.News;

public interface INewsService
{
    Task<IReadOnlyList<NewsItem>> GetGoldNewsAsync(int count, CancellationToken cancellationToken);
}

