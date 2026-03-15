using Npgsql;
using PricePredictor.Tests.Integration.Setup;
using Shouldly;

namespace PricePredictor.Tests.Integration.Persistence;

[Collection("integration")]
public sealed class GoldNewsEmbeddingMigrationTests
{
    private readonly PostgresContainerFixture _postgres;

    public GoldNewsEmbeddingMigrationTests(PostgresContainerFixture postgres)
    {
        _postgres = postgres;
    }

    [Fact]
    public async Task ApplyPendingMigrations_whenCompleted_createsUniqueArticleIdIndex()
    {
        await using var connection = new NpgsqlConnection(_postgres.ConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(@"
SELECT COALESCE((
    SELECT i.indisunique
    FROM pg_class AS c
    INNER JOIN pg_index AS i ON i.indexrelid = c.oid
    INNER JOIN pg_class AS t ON t.oid = i.indrelid
    WHERE c.relname = 'ix_gold_news_embeddings_article_id'
      AND t.relname = 'gold_news_embeddings'
    LIMIT 1
), FALSE);", connection);

        var result = await command.ExecuteScalarAsync();

        Convert.ToBoolean(result).ShouldBeTrue();
    }
}

