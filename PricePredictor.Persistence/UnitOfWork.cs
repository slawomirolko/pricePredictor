using PricePredictor.Application.Data;
using PricePredictor.Application.News;

namespace PricePredictor.Persistence;

internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly PricePredictorDbContext _context;

    public UnitOfWork(
        IArticleReaderRepository articleLinks,
        IArticleScanRepository articles,
        IGoldNewsEmbeddingRepository embeddings,
        PricePredictorDbContext context)
    {
        ArticleLinks = articleLinks;
        Articles = articles;
        Embeddings = embeddings;
        _context = context;
    }

    public IArticleReaderRepository ArticleLinks { get; }
    public IArticleScanRepository Articles { get; }
    public IGoldNewsEmbeddingRepository Embeddings { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}

