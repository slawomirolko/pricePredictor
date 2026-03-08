using PricePredictor.Api.Gateway;
using PricePredictor.Tests.Integration.Setup;
using Shouldly;

namespace PricePredictor.Tests.Integration.Gateway;

public sealed class DownloadGoldNewsArticleTests : IntegrationTest
{
    public DownloadGoldNewsArticleTests(PostgresContainerFixture postgres)
        : base(postgres)
    {
    }

    [Fact]
    public async Task DownloadGoldNewsArticle_StoresContent_WithExpectedSentence()
    {
        var url = GoldNewsTestConstants.ReutersUrl;
        var seededContent = GoldNewsTestConstants.ArticleContent;

        await DeleteStoredArticleAsync(url, CancellationToken.None);
        await SeedStoredArticleAsync(url, seededContent, CancellationToken.None);

        var reply = await GatewayClient.DownloadGoldNewsArticleAsync(new DownloadArticleRequest
        {
            Url = url
        });

        reply.Success.ShouldBeTrue();
        reply.WasAlreadyStored.ShouldBeTrue();

        var content = await GetStoredArticleContentAsync(url, CancellationToken.None);
        content.ShouldNotBeNullOrWhiteSpace();
        content!.ShouldContain(GoldNewsTestConstants.ExpectedSentence);
    }
}
