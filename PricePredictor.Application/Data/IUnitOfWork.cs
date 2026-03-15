using PricePredictor.Application.News;

namespace PricePredictor.Application.Data;

public interface IUnitOfWork
{
    IArticleReaderRepository ArticleLinks { get; }
    IArticleScanRepository Articles { get; }
    IGoldNewsEmbeddingRepository Embeddings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

