using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Npgsql;
using Testcontainers.PostgreSql;

namespace PricePredicator.Integration.Tests;

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
        // Start PostgreSQL container
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("pricepredictor")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

        await _postgresContainer.StartAsync();

        _connectionString = _postgresContainer.GetConnectionString();

        // Build the application container with overridden settings
        // This sets the interval to 1 minute for faster testing
        _appContainer = new ContainerBuilder()
            .WithImage("pricepredicator.app:latest")
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Production")
            .WithEnvironment("ConnectionStrings__DefaultConnection", _connectionString)
            .WithEnvironment("YahooFinance__Symbols__0", "GLD")
            .WithEnvironment("YahooFinance__Symbols__1", "SLV")
            .WithEnvironment("YahooFinance__Symbols__2", "NG=F")
            .WithEnvironment("YahooFinance__Symbols__3", "CL=F")
            .WithEnvironment("YahooFinance__Interval", "1m")
            .WithEnvironment("YahooFinance__Range", "1d")
            .WithEnvironment("GoldNews__OllamaUrl", "http://host.docker.internal:11434")
            .WithEnvironment("GoldNews__QdrantUrl", "http://host.docker.internal:6333")
            .WithPortBinding(50051, 50051)
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
        Assert.True(baselineCounts["Volatility_Gold"] > 0, "Volatility_Gold should have initial data");
        Assert.True(baselineCounts["Volatility_Silver"] > 0, "Volatility_Silver should have initial data");
        Assert.True(baselineCounts["Volatility_NaturalGas"] > 0, "Volatility_NaturalGas should have initial data");
        Assert.True(baselineCounts["Volatility_Oil"] > 0, "Volatility_Oil should have initial data");

        // Act - Wait for the next data ingestion cycle (1 minute + buffer)
        await Task.Delay(TimeSpan.FromSeconds(70));

        // Get new counts after waiting
        var newCounts = await GetTableRowCountsAsync();

        // Assert - Verify data increased in all tables
        Assert.True(newCounts["Volatility_Gold"] > baselineCounts["Volatility_Gold"],
            $"Volatility_Gold count should increase. Before: {baselineCounts["Volatility_Gold"]}, After: {newCounts["Volatility_Gold"]}");

        Assert.True(newCounts["Volatility_Silver"] > baselineCounts["Volatility_Silver"],
            $"Volatility_Silver count should increase. Before: {baselineCounts["Volatility_Silver"]}, After: {newCounts["Volatility_Silver"]}");

        Assert.True(newCounts["Volatility_NaturalGas"] > baselineCounts["Volatility_NaturalGas"],
            $"Volatility_NaturalGas count should increase. Before: {baselineCounts["Volatility_NaturalGas"]}, After: {newCounts["Volatility_NaturalGas"]}");

        Assert.True(newCounts["Volatility_Oil"] > baselineCounts["Volatility_Oil"],
            $"Volatility_Oil count should increase. Before: {baselineCounts["Volatility_Oil"]}, After: {newCounts["Volatility_Oil"]}");
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
        Assert.Contains("Volatility_Gold", tables);
        Assert.Contains("Volatility_Silver", tables);
        Assert.Contains("Volatility_NaturalGas", tables);
        Assert.Contains("Volatility_Oil", tables);
        Assert.Contains("__EFMigrationsHistory", tables);
    }

    [Fact]
    public async Task ShouldStoreDataWithCorrectStructure()
    {
        // Arrange
        Assert.NotNull(_connectionString);
        await Task.Delay(TimeSpan.FromSeconds(10));

        // Act - Get a sample row from Volatility_Gold
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            @"SELECT ""Id"", ""Timestamp"", ""Close"", ""LogarithmicReturn"", ""RollingVol5"", ""RollingVol15"", 
                     ""RollingVol60"", ""ShortPanicScore"", ""LongPanicScore"", ""CreatedAtUtc""
              FROM ""Volatility_Gold"" 
              ORDER BY ""CreatedAtUtc"" DESC 
              LIMIT 1", 
            connection);

        await using var reader = await cmd.ExecuteReaderAsync();

        // Assert - Verify row exists and has expected structure
        Assert.True(await reader.ReadAsync(), "Should have at least one row in Volatility_Gold");

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
            Assert.True(measurements[i]["Volatility_Gold"] >= measurements[i - 1]["Volatility_Gold"],
                $"Cycle {i}: Gold count should not decrease");
            Assert.True(measurements[i]["Volatility_Silver"] >= measurements[i - 1]["Volatility_Silver"],
                $"Cycle {i}: Silver count should not decrease");
            Assert.True(measurements[i]["Volatility_NaturalGas"] >= measurements[i - 1]["Volatility_NaturalGas"],
                $"Cycle {i}: NaturalGas count should not decrease");
            Assert.True(measurements[i]["Volatility_Oil"] >= measurements[i - 1]["Volatility_Oil"],
                $"Cycle {i}: Oil count should not decrease");
        }

        // Verify at least some growth occurred
        var totalGrowth = measurements[^1]["Volatility_Gold"] - measurements[0]["Volatility_Gold"];
        Assert.True(totalGrowth > 0, $"Should have some data growth over 3 minutes. Growth: {totalGrowth}");
    }

    private async Task<Dictionary<string, int>> GetTableRowCountsAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
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


