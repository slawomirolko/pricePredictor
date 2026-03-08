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
    public async Task DownloadGoldNewsArticle_whenCalled_returnsOnlineArticleWithRealContent()
    {
        var url = GoldNewsTestConstants.ReutersUrl;

        await DeleteStoredArticleAsync(url, CancellationToken.None);

        var reply = await GatewayClient.DownloadGoldNewsArticleAsync(new DownloadArticleRequest
        {
            Url = url
        });

        reply.Success.ShouldBeTrue(reply.Message);
        reply.WasAlreadyStored.ShouldBeFalse();

        var content = await GetStoredArticleContentAsync(url, CancellationToken.None);
        content.ShouldNotBeNullOrWhiteSpace();
        content!.ShouldContain(GoldNewsTestConstants.ExpectedSentence);
    }
}
