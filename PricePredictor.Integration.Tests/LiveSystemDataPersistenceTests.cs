using Npgsql;
using PricePredictor.Integration.Tests.Setup;

namespace PricePredictor.Integration.Tests;

[Collection("integration")]
public class LiveSystemDataPersistenceTests
{
    private readonly PostgresContainerFixture _fixture;
    private string ConnectionString => _fixture.ConnectionString;

    public LiveSystemDataPersistenceTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }


    [Fact]
    public async Task LiveSystem_ShouldHaveAllVolatilityTables()
    {
        // Arrange & Act
        var tables = await GetPublicTablesAsync();

        // Assert
        Assert.Contains("Volatility_Gold", tables);
        Assert.Contains("Volatility_Silver", tables);
        Assert.Contains("Volatility_NaturalGas", tables);
        Assert.Contains("Volatility_Oil", tables);
        Assert.Contains("__EFMigrationsHistory", tables);
    }
    
    
    [Fact]
    public async Task LiveSystem_ShouldHaveRecentData()
    {
        // Arrange & Act
        var tables = new[] { "Volatility_Gold", "Volatility_Silver", "Volatility_NaturalGas", "Volatility_Oil" };
        var now = DateTime.UtcNow;

        foreach (var table in tables)
        {
            var latestRow = await GetLatestRowAsync(table);
            
            if (latestRow != null && latestRow.ContainsKey("CreatedAtUtc"))
            {
                var createdAt = (DateTime)latestRow["CreatedAtUtc"];
                var age = now - createdAt;

                // Assert - Data should be relatively recent (within last 5 minutes)
                Assert.True(age.TotalMinutes < 5, 
                    $"{table} latest data is {age.TotalMinutes:F1} minutes old, should be fresher");
            }
        }
    }

    [Fact]
    public async Task LiveSystem_ShouldHavePgvectorStorageReady()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var extensionCmd = new NpgsqlCommand(
            "SELECT 1 FROM pg_extension WHERE extname = 'vector'",
            connection);
        var extensionExists = await extensionCmd.ExecuteScalarAsync();
        Assert.NotNull(extensionExists);

        await using var tableCmd = new NpgsqlCommand(
            "SELECT 1 FROM information_schema.tables WHERE table_schema='public' AND table_name='gold_news_embeddings'",
            connection);
        var tableExists = await tableCmd.ExecuteScalarAsync();
        Assert.NotNull(tableExists);
    }

    private async Task<Dictionary<string, int>> GetTableRowCountsAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        var counts = new Dictionary<string, int>();
        var tables = new[] { "Volatility_Gold", "Volatility_Silver", "Volatility_NaturalGas", "Volatility_Oil" };

        foreach (var table in tables)
        {
            await using var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM \"{table}\"", connection);
            var count = (long)(await cmd.ExecuteScalarAsync() ?? 0L);
            counts[table] = (int)count;
        }

        return counts;
    }

    private async Task<List<string>> GetPublicTablesAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            "SELECT tablename FROM pg_tables WHERE schemaname='public' ORDER BY tablename",
            connection);

        var tables = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    private async Task<Dictionary<string, object>?> GetLatestRowAsync(string tableName)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            $@"SELECT ""Id"", ""Timestamp"", ""Close"", ""LogarithmicReturn"", ""RollingVol5"", ""RollingVol15"", 
                      ""RollingVol60"", ""ShortPanicScore"", ""LongPanicScore"", ""CreatedAtUtc""
               FROM ""{tableName}"" 
               ORDER BY ""CreatedAtUtc"" DESC 
               LIMIT 1",
            connection);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        var result = new Dictionary<string, object>();
        for (int i = 0; i < reader.FieldCount; i++)
        {
            result[reader.GetName(i)] = reader.GetValue(i);
        }

        return result;
    }
}

