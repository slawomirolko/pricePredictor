using PricePredictor.Application.Data;

namespace PricePredictor.Application.News;

internal sealed class ImportantArticleService : IImportantArticleService
{
    private const int NewestArticleCount = 3;
    private readonly IUnitOfWork _unitOfWork;

    public ImportantArticleService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ImportantArticleDto>> GetNewestAsync(CancellationToken cancellationToken)
    {
        var usefulArticleLinkIds = await _unitOfWork.Articles.GetTradingUsefulArticleLinkIdsAsync(
            NewestArticleCount,
            cancellationToken);
        if (usefulArticleLinkIds.Count == 0)
        {
            return [];
        }

        var articleLinks = await _unitOfWork.ArticleLinks.GetLinksByIdsAsync(usefulArticleLinkIds, cancellationToken);
        var articles = await _unitOfWork.Articles.GetByArticleLinkIdsAsync(usefulArticleLinkIds, cancellationToken);
        var articlesByArticleLinkId = articles.ToDictionary(x => x.ArticleLinkId);

        return articleLinks
            .OrderByDescending(x => x.ReadAt)
            .Take(NewestArticleCount)
            .Select(x =>
            {
                var article = articlesByArticleLinkId[x.Id];

                return new ImportantArticleDto(
                    article.Id,
                    x.Url,
                    x.Source,
                    DateTime.SpecifyKind(x.ReadAt, DateTimeKind.Utc),
                    article.Summary);
            })
            .ToList();
    }
}
