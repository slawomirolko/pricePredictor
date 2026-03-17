using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PricePredictor.Application.Models;
using PricePredictor.Persistence;

namespace PricePredictor.ArticlesFinderHostedService.Tests.Integration.Setup;

[Collection("integration")]
public abstract class IntegrationTest : IDisposable
{
    private readonly ArticlesFinderIntegrationTestFactory _factory;

    protected IntegrationTest(PostgresContainerFixture postgres)
    {
        _factory = new ArticlesFinderIntegrationTestFactory(postgres.ConnectionString);
    }

    protected ArticlesFinderIntegrationTestFactory Factory => _factory;

    public void Dispose()
    {
        _factory.Dispose();
    }

    protected async Task DeleteStoredArticleLinksBySourceAsync(string source, CancellationToken cancellationToken)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PricePredictorDbContext>();

        await dbContext.ArticleLinks
            .Where(x => x.Source == source)
            .ExecuteDeleteAsync(cancellationToken);
    }

    protected async Task<IReadOnlyList<ArticleLink>> GetStoredArticleLinksBySourceAsync(string source, CancellationToken cancellationToken)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PricePredictorDbContext>();

        return await dbContext.ArticleLinks
            .AsNoTracking()
            .Where(x => x.Source == source)
            .OrderByDescending(x => x.ReadAt)
            .ToListAsync(cancellationToken);
    }
}
