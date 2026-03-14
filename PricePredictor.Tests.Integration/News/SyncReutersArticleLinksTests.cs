using Microsoft.Extensions.DependencyInjection;
using PricePredictor.Application.News;
using PricePredictor.Tests.Integration.Setup;
using Shouldly;

namespace PricePredictor.Tests.Integration.News;

public sealed class SyncReutersArticleLinksTests : IntegrationTest
{
    public SyncReutersArticleLinksTests(PostgresContainerFixture postgres)
        : base(postgres)
    {
    }

    [Fact]
    public async Task SyncReutersArticleLinks_whenExecuted_storesArticleLinks()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(5));

        await DeleteStoredArticleLinksBySourceAsync("reuters", cancellation.Token);

        using var scope = Factory.Services.CreateScope();
        var articleService = scope.ServiceProvider.GetRequiredService<IArticleService>();

        var extractedLinks = await articleService.SyncArticleLinksAsync(cancellation.Token);
        extractedLinks.Count.ShouldBeGreaterThan(0);

        var storedLinks = await GetStoredArticleLinksBySourceAsync("reuters", cancellation.Token);
        storedLinks.Count.ShouldBe(extractedLinks.Count);

        var extractedUrlSet = extractedLinks.Select(x => x.Url).ToHashSet();
        foreach (var storedLink in storedLinks)
        {
            extractedUrlSet.Contains(storedLink.Url).ShouldBeTrue();
        }
    }
}
