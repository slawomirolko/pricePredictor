using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Npgsql;
using Testcontainers.PostgreSql;

namespace PricePredictor.Tests.Integration;

/// <summary>
/// Integration tests that verify the application stores volatility data every 1 minute.
/// Uses TestContainers to spin up real PostgreSQL and application containers.
/// </summary>
public class YahooFinanceDataPersistenceTests : IAsyncLifetime
{
    private PostgreSqlContainer? _postgresContainer;
    private IContainer? _appContainer;
    private string? _connectionString;

    public async Task InitializeAsync()
    {
        // Start PostgreSQL container - using dynamic port assignment
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("pricepredictor")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

        await _postgresContainer.StartAsync();

        // Use the connection string provided by the container (with dynamic port)
        _connectionString = _postgresContainer.GetConnectionString();

        // Build the application container with overridden settings
        // This sets the interval to 1 minute for faster testing
        _appContainer = new ContainerBuilder()
            .WithImage("pricepredictor.app:latest")
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Production")
            .WithEnvironment("ConnectionStrings__DefaultConnection", _connectionString)
            .WithEnvironment("YahooFinance__Symbols__0", "GC=F")
            .WithEnvironment("YahooFinance__Symbols__1", "SI=F")
            .WithEnvironment("YahooFinance__Symbols__2", "NG=F")
            .WithEnvironment("YahooFinance__Symbols__3", "CL=F")
            .WithEnvironment("YahooFinance__Interval", "1m")
            .WithEnvironment("YahooFinance__Range", "1d")
            .WithEnvironment("GoldNews__EmbeddingDimensions", "3072")
            .WithEnvironment("GoldNews__OllamaUrl", "http://host.docker.internal:11434")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Yahoo Finance Background Service started"))
            .WithCleanUp(true)
            .Build();

        await _appContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_appContainer != null)
        {
            await _appContainer.DisposeAsync();
        }

        if (_postgresContainer != null)
        {
            await _postgresContainer.DisposeAsync();
        }
    }

    [Fact]
    public async Task ShouldStoreVolatilityDataInDatabase_AfterOneMinute()
    {
        // Arrange
        Assert.NotNull(_connectionString);

        // Wait for initial data population (app starts and runs immediately)
        await Task.Delay(TimeSpan.FromSeconds(10));

        // Get baseline counts
        var baselineCounts = await GetTableRowCountsAsync();

        // Assert baseline - should have some data from initial run
        Assert.True(baselineCounts["Gold"] > 0, "Gold should have initial data");
        Assert.True(baselineCounts["Silver"] > 0, "Silver should have initial data");
        Assert.True(baselineCounts["NaturalGas"] > 0, "NaturalGas should have initial data");
        Assert.True(baselineCounts["Oil"] > 0, "Oil should have initial data");

        // Act - Wait for the next data ingestion cycle (1 minute + buffer)
        await Task.Delay(TimeSpan.FromSeconds(70));

        // Get new counts after waiting
        var newCounts = await GetTableRowCountsAsync();

        // Assert - Verify data increased in all tables
        Assert.True(newCounts["Gold"] > baselineCounts["Gold"],
            $"Gold count should increase. Before: {baselineCounts["Gold"]}, After: {newCounts["Gold"]}");

        Assert.True(newCounts["Silver"] > baselineCounts["Silver"],
            $"Silver count should increase. Before: {baselineCounts["Silver"]}, After: {newCounts["Silver"]}");

        Assert.True(newCounts["NaturalGas"] > baselineCounts["NaturalGas"],
            $"NaturalGas count should increase. Before: {baselineCounts["NaturalGas"]}, After: {newCounts["NaturalGas"]}");

        Assert.True(newCounts["Oil"] > baselineCounts["Oil"],
            $"Oil count should increase. Before: {baselineCounts["Oil"]}, After: {newCounts["Oil"]}");
    }

    [Fact]
    public async Task ShouldHaveCorrectSchemaAndTables()
    {
        // Arrange
        Assert.NotNull(_connectionString);
        await Task.Delay(TimeSpan.FromSeconds(10)); // Wait for migrations

        // Act
        var tables = await GetPublicTablesAsync();

        // Assert
        Assert.Contains("Gold", tables);
        Assert.Contains("Silver", tables);
        Assert.Contains("NaturalGas", tables);
        Assert.Contains("Oil", tables);
        Assert.Contains("__EFMigrationsHistory", tables);
    }

    [Fact]
    public async Task ShouldStoreDataWithCorrectStructure()
    {
        // Arrange
        Assert.NotNull(_connectionString);
        await Task.Delay(TimeSpan.FromSeconds(10));

        // Act - Get a sample row from Gold
        const string sql = @"
            SELECT *
              FROM ""Gold""
             LIMIT 1";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, connection);

        await using var reader = await cmd.ExecuteReaderAsync();

        // Assert - Verify row exists and has expected structure
        Assert.True(await reader.ReadAsync(), "Should have at least one row in Gold");

        Assert.IsType<int>(reader["Id"]);
        Assert.IsType<DateTime>(reader["Timestamp"]);
        Assert.True(reader["Close"] is decimal);
        Assert.True(reader["LogarithmicReturn"] is double);
        Assert.True(reader["RollingVol5"] is double);
        Assert.True(reader["RollingVol15"] is double);
        Assert.True(reader["RollingVol60"] is double);
        Assert.True(reader["ShortPanicScore"] is double);
        Assert.True(reader["LongPanicScore"] is double);
        Assert.IsType<DateTime>(reader["CreatedAtUtc"]);
    }

    [Fact]
    public async Task ShouldPersistDataAfterMultipleCycles()
    {
        // Arrange
        Assert.NotNull(_connectionString);
        await Task.Delay(TimeSpan.FromSeconds(10));

        var measurements = new List<Dictionary<string, int>>();

        // Act - Take measurements over 3 minutes
        for (int i = 0; i < 3; i++)
        {
            var counts = await GetTableRowCountsAsync();
            measurements.Add(counts);

            if (i < 2) // Don't wait after the last measurement
            {
                await Task.Delay(TimeSpan.FromSeconds(60));
            }
        }

        // Assert - Verify progressive data accumulation
        for (int i = 1; i < measurements.Count; i++)
        {
            Assert.True(measurements[i]["Gold"] >= measurements[i - 1]["Gold"],
                $"Cycle {i}: Gold count should not decrease");
            Assert.True(measurements[i]["Silver"] >= measurements[i - 1]["Silver"],
                $"Cycle {i}: Silver count should not decrease");
            Assert.True(measurements[i]["NaturalGas"] >= measurements[i - 1]["NaturalGas"],
                $"Cycle {i}: NaturalGas count should not decrease");
            Assert.True(measurements[i]["Oil"] >= measurements[i - 1]["Oil"],
                $"Cycle {i}: Oil count should not decrease");
        }

        // Verify at least some growth occurred
        var totalGrowth = measurements[^1]["Gold"] - measurements[0]["Gold"];
        Assert.True(totalGrowth > 0, $"Should have some data growth over 3 minutes. Growth: {totalGrowth}");
    }

    private async Task<Dictionary<string, int>> GetTableRowCountsAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var counts = new Dictionary<string, int>();
        var tables = new[] { "Gold", "Silver", "NaturalGas", "Oil" };

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
        await using var connection = new NpgsqlConnection(_connectionString);
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
}

