using System.Data;
using System.Data.Common;
using System.Net;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PricePredictor.Persistence;
using PricePredictor.Application.Data;
using GoldNewsSettings = PricePredictor.Infrastructure.GoldNewsSettings;
using PricePredictor.Application.Models;

namespace PricePredictor.Tests.Integration.Setup;

[Collection("integration")]
public abstract class IntegrationTest : IAsyncLifetime
{
    private readonly PostgresContainerFixture _postgres;
    private IntegrationTestFactory? _factory;
    private GrpcChannel? _channel;

    protected IntegrationTest(PostgresContainerFixture postgres)
    {
        _postgres = postgres;
    }

    protected IntegrationTestFactory Factory => _factory ?? throw new InvalidOperationException("Factory not initialized.");
    protected PricePredictor.Api.Gateway.Gateway.GatewayClient GatewayClient { get; private set; } = null!;

    public Task InitializeAsync()
    {
        _factory = new IntegrationTestFactory(_postgres.ConnectionString);

        var client = _factory.CreateDefaultClient();
        client.DefaultRequestVersion = HttpVersion.Version20;
        client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;

        _channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
        {
            HttpClient = client
        });

        GatewayClient = new PricePredictor.Api.Gateway.Gateway.GatewayClient(_channel);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _channel?.Dispose();
        _factory?.Dispose();
        return Task.CompletedTask;
    }

    protected async Task DeleteStoredArticleAsync(string url, CancellationToken cancellationToken)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PricePredictorDbContext>();
        
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM gold_news_embeddings WHERE url = @url";
        AddParameter(command, "@url", url);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    protected async Task<string?> GetStoredArticleContentAsync(string url, CancellationToken cancellationToken)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PricePredictorDbContext>();
        
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

    protected async Task SeedStoredArticleAsync(string url, string content, CancellationToken cancellationToken)
    {
        using var scope = Factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IGoldNewsEmbeddingRepository>();
        var settings = scope.ServiceProvider.GetRequiredService<IOptions<GoldNewsSettings>>().Value;
        var embedding = new float[settings.EmbeddingDimensions];

        await repository.UpsertAsync(url, content, embedding, settings.EmbeddingDimensions, cancellationToken);
    }

    protected async Task DeleteStoredArticleLinksBySourceAsync(string source, CancellationToken cancellationToken)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PricePredictorDbContext>();

        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM \"ArticleLinks\" WHERE \"Source\" = @source";
        AddParameter(command, "@source", source);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    protected async Task<IReadOnlyList<ArticleLink>> GetStoredArticleLinksBySourceAsync(string source, CancellationToken cancellationToken)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PricePredictorDbContext>();
        return await dbContext.ArticleLinks
            .AsNoTracking()
            .Where(x => x.Source == source)
            .OrderByDescending(x => x.PublishedAtUtc)
            .ToListAsync(cancellationToken);
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}


