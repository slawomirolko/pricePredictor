using Npgsql;
using PricePredicator.Integration.Tests.Setup;
using Testcontainers.PostgreSql;

namespace PricePredicator.Integration.Tests;

/// <summary>
/// Integration tests for Gold News embeddings using pgvector.
/// Uses shared PostgresContainerFixture from the integration collection.
/// </summary>
[Collection("integration")]
public class GoldNewsEmbeddingsTests
{
    private readonly PostgresContainerFixture _fixture;

    public GoldNewsEmbeddingsTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task SetupGoldNewsEmbeddingsTableAsync()
    {
        // Create gold_news_embeddings table if it doesn't exist
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        // Create gold_news_embeddings table
        await using var tableCmd = new NpgsqlCommand(@"
            CREATE TABLE IF NOT EXISTS gold_news_embeddings (
                id uuid PRIMARY KEY,
                url text NOT NULL UNIQUE,
                content text NOT NULL,
                embedding vector(3072) NOT NULL,
                created_at_utc timestamptz NOT NULL
            );", connection);
        await tableCmd.ExecuteNonQueryAsync();

        // Create index for timestamp-based queries
        await using var indexCmd = new NpgsqlCommand(@"
            CREATE INDEX IF NOT EXISTS ix_gold_news_embeddings_created_at_utc 
            ON gold_news_embeddings (created_at_utc DESC);", connection);
        await indexCmd.ExecuteNonQueryAsync();
    }


    [Fact]
    public async Task ShouldHavePgvectorExtensionEnabled()
    {
        // Setup table for this test
        await SetupGoldNewsEmbeddingsTableAsync();

        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            "SELECT 1 FROM pg_extension WHERE extname = 'vector'",
            connection);
        var result = await cmd.ExecuteScalarAsync();

        Assert.NotNull(result);
    }

    [Fact]
    public async Task ShouldHaveGoldNewsEmbeddingsTable()
    {
        // Setup table for this test
        await SetupGoldNewsEmbeddingsTableAsync();

        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            "SELECT 1 FROM information_schema.tables WHERE table_schema='public' AND table_name='gold_news_embeddings'",
            connection);
        var result = await cmd.ExecuteScalarAsync();

        Assert.NotNull(result);
    }

    [Fact]
    public async Task ShouldHaveCorrectEmbeddingsTableSchema()
    {
        // Setup table for this test
        await SetupGoldNewsEmbeddingsTableAsync();

        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            @"SELECT column_name, data_type 
              FROM information_schema.columns 
              WHERE table_schema='public' AND table_name='gold_news_embeddings'
              ORDER BY ordinal_position",
            connection);

        var columns = new Dictionary<string, string>();
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var colName = reader.GetString(0);
            var dataType = reader.GetString(1);
            columns[colName] = dataType;
        }

        // Verify all expected columns exist
        Assert.Contains("id", columns.Keys);
        Assert.Contains("url", columns.Keys);
        Assert.Contains("content", columns.Keys);
        Assert.Contains("embedding", columns.Keys);
        Assert.Contains("created_at_utc", columns.Keys);

        // Verify UUID type for id
        Assert.Equal("uuid", columns["id"]);

        // Verify text type for url and content
        Assert.Equal("text", columns["url"]);
        Assert.Equal("text", columns["content"]);

        // Verify timestamptz for created_at_utc
        Assert.Equal("timestamp with time zone", columns["created_at_utc"]);
    }

    [Fact]
    public async Task ShouldHaveIndexOnCreatedAtUtc()
    {
        // Setup table for this test
        await SetupGoldNewsEmbeddingsTableAsync();

        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            "SELECT 1 FROM pg_indexes WHERE tablename='gold_news_embeddings' AND indexname='ix_gold_news_embeddings_created_at_utc'",
            connection);
        var result = await cmd.ExecuteScalarAsync();

        Assert.NotNull(result);
    }

    [Fact]
    public async Task ShouldBeAbleToInsertEmbeddingVector()
    {
        // Setup table for this test
        await SetupGoldNewsEmbeddingsTableAsync();

        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        // Create a simple vector (3072 dimensions as per config)
        var vectorValues = string.Join(",", Enumerable.Range(0, 3072).Select(i => $"{i * 0.001f:F3}"));
        var vectorLiteral = $"[{vectorValues}]";

        await using var cmd = new NpgsqlCommand(
            @"INSERT INTO gold_news_embeddings (id, url, content, embedding, created_at_utc)
              VALUES (@id, @url, @content, CAST(@embedding AS vector), @createdAtUtc)",
            connection);

        cmd.Parameters.AddWithValue("@id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("@url", $"https://example.com/test-article-{Guid.NewGuid()}");
        cmd.Parameters.AddWithValue("@content", "Test article about gold prices and market conditions");
        cmd.Parameters.AddWithValue("@embedding", vectorLiteral);
        cmd.Parameters.AddWithValue("@createdAtUtc", DateTime.UtcNow);

        var rowsAffected = await cmd.ExecuteNonQueryAsync();
        Assert.Equal(1, rowsAffected);
    }

    [Fact]
    public async Task ShouldEnforceUrlUniqueness()
    {
        Assert.NotNull(_fixture.ConnectionString);

        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        var vectorValues = string.Join(",", Enumerable.Range(0, 3072).Select(i => $"{i * 0.001f:F3}"));
        var vectorLiteral = $"[{vectorValues}]";
        var testUrl = $"https://example.com/unique-article-{Guid.NewGuid()}";

        // Insert first record
        await using var cmd1 = new NpgsqlCommand(
            @"INSERT INTO gold_news_embeddings (id, url, content, embedding, created_at_utc)
              VALUES (@id, @url, @content, CAST(@embedding AS vector), @createdAtUtc)",
            connection);

        cmd1.Parameters.AddWithValue("@id", Guid.NewGuid());
        cmd1.Parameters.AddWithValue("@url", testUrl);
        cmd1.Parameters.AddWithValue("@content", "First version");
        cmd1.Parameters.AddWithValue("@embedding", vectorLiteral);
        cmd1.Parameters.AddWithValue("@createdAtUtc", DateTime.UtcNow);

        await cmd1.ExecuteNonQueryAsync();

        // Try to insert duplicate URL (should upsert, not fail)
        await using var cmd2 = new NpgsqlCommand(
            @"INSERT INTO gold_news_embeddings (id, url, content, embedding, created_at_utc)
              VALUES (@id, @url, @content, CAST(@embedding AS vector), @createdAtUtc)
              ON CONFLICT (url)
              DO UPDATE SET
                  content = EXCLUDED.content,
                  embedding = EXCLUDED.embedding,
                  created_at_utc = EXCLUDED.created_at_utc",
            connection);

        cmd2.Parameters.AddWithValue("@id", Guid.NewGuid());
        cmd2.Parameters.AddWithValue("@url", testUrl);
        cmd2.Parameters.AddWithValue("@content", "Updated version");
        cmd2.Parameters.AddWithValue("@embedding", vectorLiteral);
        cmd2.Parameters.AddWithValue("@createdAtUtc", DateTime.UtcNow);

        var rowsAffected = await cmd2.ExecuteNonQueryAsync();
        Assert.Equal(1, rowsAffected); // Should be 1 for upsert
    }
}





