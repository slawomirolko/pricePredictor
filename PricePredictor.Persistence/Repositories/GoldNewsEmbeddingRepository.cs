using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PricePredictor.Application.Data;

namespace PricePredictor.Persistence.Repositories;

public class GoldNewsEmbeddingsRepository : IGoldNewsEmbeddingRepository
{
    private readonly PricePredictorDbContext _context;

    public GoldNewsEmbeddingsRepository(PricePredictorDbContext context)
    {
        _context = context;
    }

    public async Task<string?> GetContentAsync(string url, CancellationToken cancellationToken)
    {
        var connection = await OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT content FROM gold_news_embeddings WHERE url = @url";

        AttachCurrentTransaction(command);
        AddParameter(command, "@url", url);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result as string;
    }

    public async Task EnsureStorageAsync(int dimensions, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string url, CancellationToken cancellationToken)
    {
        var connection = await OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM gold_news_embeddings WHERE url = @url";

        AttachCurrentTransaction(command);
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

        var connection = await OpenConnectionAsync(cancellationToken);

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

        AttachCurrentTransaction(command);
        AddParameter(command, "@id", Guid.CreateVersion7());
        AddParameter(command, "@url", url);
        AddParameter(command, "@content", content);
        AddParameter(command, "@embedding", ToVectorLiteral(embedding));
        AddParameter(command, "@createdAtUtc", DateTime.UtcNow);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpsertArticleAsync(
        Guid articleId,
        DateTime readAtUtc,
        string summary,
        IReadOnlyList<float> embedding,
        int dimensions,
        CancellationToken cancellationToken)
    {
        if (embedding.Count != dimensions)
        {
            throw new InvalidOperationException(
                $"Embedding dimension mismatch. Expected {dimensions}, got {embedding.Count}.");
        }

        var connection = await OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO gold_news_embeddings (id, url, content, summary, article_id, read_at_utc, embedding, created_at_utc)
VALUES (@id, @url, @content, @summary, @articleId, @readAtUtc, CAST(@embedding AS vector), @createdAtUtc)
ON CONFLICT (article_id)
DO UPDATE SET
    summary = EXCLUDED.summary,
    content = EXCLUDED.content,
    read_at_utc = EXCLUDED.read_at_utc,
    embedding = EXCLUDED.embedding,
    created_at_utc = EXCLUDED.created_at_utc;
";

        AttachCurrentTransaction(command);
        AddParameter(command, "@id", Guid.CreateVersion7());
        AddParameter(command, "@url", articleId.ToString());
        AddParameter(command, "@content", summary);
        AddParameter(command, "@summary", summary);
        AddParameter(command, "@articleId", articleId);
        AddParameter(command, "@readAtUtc", DateTime.SpecifyKind(readAtUtc, DateTimeKind.Utc));
        AddParameter(command, "@embedding", ToVectorLiteral(embedding));
        AddParameter(command, "@createdAtUtc", DateTime.UtcNow);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = _context.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        return connection;
    }

    private void AttachCurrentTransaction(DbCommand command)
    {
        IDbContextTransaction? transaction = _context.Database.CurrentTransaction;
        if (transaction is not null)
        {
            command.Transaction = transaction.GetDbTransaction();
        }
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
