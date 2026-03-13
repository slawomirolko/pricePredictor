using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PricePredictor.Application.Data;

namespace PricePredictor.Persistence.Repositories;

public class GoldNewsEmbeddingRepository : IGoldNewsEmbeddingRepository
{
    private readonly IDbContextFactory<PricePredictorDbContext> _dbContextFactory;

    public GoldNewsEmbeddingRepository(IDbContextFactory<PricePredictorDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<string?> GetContentAsync(string url, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var connection = dbContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT content FROM gold_news_embeddings WHERE url = @url";
        
        AddParameter(command, "@url", url);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result as string;
    }

    public async Task EnsureStorageAsync(int dimensions, CancellationToken cancellationToken)
    {
        // Migrations handle table and extension creation, so this is now a no-op.
        // Kept for backward compatibility and future use (e.g., custom dimension handling).
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string url, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var connection = dbContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM gold_news_embeddings WHERE url = @url";
        
        AddParameter(command, "@url", url);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result) > 0;
    }

    public async Task UpsertAsync(
        string url,
        string content,
        IReadOnlyList<float> embedding,
        int dimensions,
        CancellationToken cancellationToken)
    {
        if (embedding.Count != dimensions)
        {
            throw new InvalidOperationException(
                $"Embedding dimension mismatch. Expected {dimensions}, got {embedding.Count}.");
        }

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var connection = dbContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO gold_news_embeddings (id, url, content, embedding, created_at_utc)
VALUES (@id, @url, @content, CAST(@embedding AS vector), @createdAtUtc)
ON CONFLICT (url)
DO UPDATE SET
    content = EXCLUDED.content,
    embedding = EXCLUDED.embedding,
    created_at_utc = EXCLUDED.created_at_utc;
";

        AddParameter(command, "@id", Guid.CreateVersion7());
        AddParameter(command, "@url", url);
        AddParameter(command, "@content", content);
        AddParameter(command, "@embedding", ToVectorLiteral(embedding));
        AddParameter(command, "@createdAtUtc", DateTime.UtcNow);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static string ToVectorLiteral(IEnumerable<float> embedding)
    {
        var sb = new StringBuilder("[");
        var first = true;

        foreach (var item in embedding)
        {
            if (!first)
            {
                sb.Append(',');
            }

            sb.Append(item.ToString("G9", CultureInfo.InvariantCulture));
            first = false;
        }

        sb.Append(']');
        return sb.ToString();
    }
}

