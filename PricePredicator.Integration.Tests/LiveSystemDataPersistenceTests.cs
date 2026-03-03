using Npgsql;

namespace PricePredicator.Integration.Tests;

/// <summary>
/// Simplified integration tests that connect to an already-running Docker Compose stack.
/// Run 'docker compose up' before executing these tests.
/// </summary>
public class LiveSystemDataPersistenceTests
{
    private const string ConnectionString = "Server=localhost;Port=5432;Database=pricepredictor;User Id=postgres;Password=postgres;";

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
    public async Task LiveSystem_ShouldStoreDataEveryMinute()
    {
        // Arrange - Get baseline counts
        var baselineCounts = await GetTableRowCountsAsync();

        // Log baseline for debugging
        foreach (var (table, count) in baselineCounts)
        {
            Assert.True(count >= 0, $"{table} should have non-negative count");
        }

        // Act - Wait 70 seconds (1 minute + buffer for processing)
        await Task.Delay(TimeSpan.FromSeconds(70));

        var newCounts = await GetTableRowCountsAsync();

        // Assert - At least one table should have increased
        var hasGrowth = newCounts.Any(kv => kv.Value > baselineCounts[kv.Key]);
        
        Assert.True(hasGrowth, 
            $"At least one table should have new data after 1 minute. " +
            $"Gold: {baselineCounts["Volatility_Gold"]} -> {newCounts["Volatility_Gold"]}, " +
            $"Silver: {baselineCounts["Volatility_Silver"]} -> {newCounts["Volatility_Silver"]}, " +
            $"NaturalGas: {baselineCounts["Volatility_NaturalGas"]} -> {newCounts["Volatility_NaturalGas"]}, " +
            $"Oil: {baselineCounts["Volatility_Oil"]} -> {newCounts["Volatility_Oil"]}");
    }

    [Fact]
    public async Task LiveSystem_ShouldHaveValidDataStructure()
    {
        // Act - Get a recent row from each table
        var tables = new[] { "Volatility_Gold", "Volatility_Silver", "Volatility_NaturalGas", "Volatility_Oil" };
        
        foreach (var table in tables)
        {
            var rowData = await GetLatestRowAsync(table);
            
            // Assert
            Assert.NotNull(rowData);
            Assert.True(rowData.ContainsKey("Id"), $"{table} should have Id column");
            Assert.True(rowData.ContainsKey("Timestamp"), $"{table} should have Timestamp column");
            Assert.True(rowData.ContainsKey("Close"), $"{table} should have Close column");
            Assert.True(rowData.ContainsKey("LogarithmicReturn"), $"{table} should have LogarithmicReturn column");
            Assert.True(rowData.ContainsKey("RollingVol5"), $"{table} should have RollingVol5 column");
            Assert.True(rowData.ContainsKey("RollingVol15"), $"{table} should have RollingVol15 column");
            Assert.True(rowData.ContainsKey("RollingVol60"), $"{table} should have RollingVol60 column");
            Assert.True(rowData.ContainsKey("ShortPanicScore"), $"{table} should have ShortPanicScore column");
            Assert.True(rowData.ContainsKey("LongPanicScore"), $"{table} should have LongPanicScore column");
            Assert.True(rowData.ContainsKey("CreatedAtUtc"), $"{table} should have CreatedAtUtc column");

            // Verify values are reasonable
            Assert.True((decimal)rowData["Close"] > 0, $"{table} Close price should be positive");
            Assert.True((DateTime)rowData["CreatedAtUtc"] <= DateTime.UtcNow, $"{table} CreatedAtUtc should not be in the future");
        }
    }

    [Fact]
    public async Task LiveSystem_ShouldAccumulateDataOverTime()
    {
        // Arrange
        var measurements = new List<Dictionary<string, int>>();

        // Act - Take 3 measurements, 1 minute apart
        for (int i = 0; i < 3; i++)
        {
            var counts = await GetTableRowCountsAsync();
            measurements.Add(counts);

            if (i < 2)
            {
                await Task.Delay(TimeSpan.FromSeconds(60));
            }
        }

        // Assert - Counts should not decrease over time
        for (int i = 1; i < measurements.Count; i++)
        {
            foreach (var table in new[] { "Volatility_Gold", "Volatility_Silver", "Volatility_NaturalGas", "Volatility_Oil" })
            {
                Assert.True(measurements[i][table] >= measurements[i - 1][table],
                    $"Measurement {i}: {table} count should not decrease. " +
                    $"Previous: {measurements[i - 1][table]}, Current: {measurements[i][table]}");
            }
        }

        // Verify some growth occurred across all tables
        var anyGrowth = false;
        foreach (var table in new[] { "Volatility_Gold", "Volatility_Silver", "Volatility_NaturalGas", "Volatility_Oil" })
        {
            if (measurements[^1][table] > measurements[0][table])
            {
                anyGrowth = true;
                break;
            }
        }

        Assert.True(anyGrowth, "At least one table should show data growth over the test period");
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

    private static async Task<Dictionary<string, int>> GetTableRowCountsAsync()
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

    private static async Task<List<string>> GetPublicTablesAsync()
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

    private static async Task<Dictionary<string, object>?> GetLatestRowAsync(string tableName)
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

