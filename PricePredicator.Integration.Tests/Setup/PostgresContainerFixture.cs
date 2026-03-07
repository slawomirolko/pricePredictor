using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PricePredicator.Infrastructure;
using PricePredicator.Infrastructure.Data;
using Testcontainers.PostgreSql;

namespace PricePredicator.Integration.Tests.Setup;


public class PostgresContainerFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public string ConnectionString => _container?.GetConnectionString() ?? throw new InvalidOperationException("Container not initialized");

    public PostgresContainerFixture()
    {
    }

    public async Task InitializeAsync()
    {
        // Build and start container only when tests actually run
        _container = new PostgreSqlBuilder()
            .WithImage("pgvector/pgvector:pg17")
            .WithDatabase("pricepredictor")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();
        
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand("CREATE EXTENSION IF NOT EXISTS vector;", connection);
        await cmd.ExecuteNonQueryAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<PricePredictorDbContext>(options =>
            options.UseNpgsql(ConnectionString));

        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.ApplyPendingMigrations();
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }
}