namespace PricePredictor.Infrastructure.Data;

public interface IGoldNewsEmbeddingRepository
{
    Task EnsureStorageAsync(int dimensions, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string url, CancellationToken cancellationToken);
    Task UpsertAsync(string url, string content, IReadOnlyList<float> embedding, int dimensions, CancellationToken cancellationToken);
}

