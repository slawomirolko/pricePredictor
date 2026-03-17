using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PricePredictor.Persistence;
using Testcontainers.PostgreSql;

namespace PricePredictor.ArticlesFinderHostedService.Tests.Integration.Setup;

public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("Container not initialized.");

    public async Task InitializeAsync()
    {
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
        services.AddDbContext<PricePredictorDbContext>(
            options => options.UseNpgsql(ConnectionString),
            optionsLifetime: ServiceLifetime.Singleton);
        services.AddDbContextFactory<PricePredictorDbContext>(options => options.UseNpgsql(ConnectionString));

        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.ApplyPendingMigrations();
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}
