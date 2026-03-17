namespace PricePredictor.Application.News;

public interface IImportantArticleService
{
    Task<IReadOnlyList<ImportantArticleDto>> GetNewestAsync(CancellationToken cancellationToken);
}
