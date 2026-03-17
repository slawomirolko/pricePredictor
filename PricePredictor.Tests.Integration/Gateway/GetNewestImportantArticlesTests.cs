using Microsoft.Extensions.DependencyInjection;
using PricePredictor.Api.Gateway;
using PricePredictor.Application.Models;
using PricePredictor.Persistence;
using PricePredictor.Tests.Integration.Setup;
using Shouldly;

namespace PricePredictor.Tests.Integration.Gateway;

public sealed class GetNewestImportantArticlesTests : IntegrationTest
{
    public GetNewestImportantArticlesTests(PostgresContainerFixture postgres)
        : base(postgres)
    {
    }

    [Fact]
    public async Task GetNewestImportantArticles_whenCalled_returnsLastThreeUsefulArticlesOrderedByDate()
    {
        var testId = Guid.NewGuid().ToString("N");
        var source = $"test-important-articles-{testId}";
        var articles = new[]
        {
            new SeedArticle(
                $"https://example.com/{testId}/article-1",
                source,
                new DateTime(2099, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                true,
                "summary-1"),
            new SeedArticle(
                $"https://example.com/{testId}/article-2",
                source,
                new DateTime(2099, 1, 1, 11, 0, 0, DateTimeKind.Utc),
                true,
                "summary-2"),
            new SeedArticle(
                $"https://example.com/{testId}/article-3",
                source,
                new DateTime(2099, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                false,
                "summary-3"),
            new SeedArticle(
                $"https://example.com/{testId}/article-4",
                source,
                new DateTime(2099, 1, 1, 13, 0, 0, DateTimeKind.Utc),
                true,
                "summary-4"),
            new SeedArticle(
                $"https://example.com/{testId}/article-5",
                source,
                new DateTime(2099, 1, 1, 14, 0, 0, DateTimeKind.Utc),
                true,
                "summary-5")
        };

        await SeedArticlesAsync(articles, CancellationToken.None);

        var reply = await GatewayClient.GetNewestImportantArticlesAsync(new NewestImportantArticlesRequest());

        reply.Articles.Count.ShouldBe(3);
        reply.Articles.Select(x => x.Url).ToArray().ShouldBe([
            $"https://example.com/{testId}/article-5",
            $"https://example.com/{testId}/article-4",
            $"https://example.com/{testId}/article-2"
        ]);
        reply.Articles.Select(x => x.Source).Distinct().Single().ShouldBe(source);
        reply.Articles.Select(x => x.ReadAtUtc.ToDateTime()).ToArray().ShouldBe([
            new DateTime(2099, 1, 1, 14, 0, 0, DateTimeKind.Utc),
            new DateTime(2099, 1, 1, 13, 0, 0, DateTimeKind.Utc),
            new DateTime(2099, 1, 1, 11, 0, 0, DateTimeKind.Utc)
        ]);
        reply.Articles.Select(x => x.Summary).ToArray().ShouldBe([
            "summary-5",
            "summary-4",
            "summary-2"
        ]);
    }

    private async Task SeedArticlesAsync(IEnumerable<SeedArticle> articles, CancellationToken cancellationToken)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PricePredictorDbContext>();

        foreach (var seedArticle in articles)
        {
            var link = ArticleLink.Create(
                seedArticle.Url,
                seedArticle.ReadAtUtc,
                seedArticle.Source);

            dbContext.ArticleLinks.Add(link);

            var articleResult = Article.Create(
                articleLinkId: link.Id,
                isTradingUseful: seedArticle.IsTradingUseful,
                scannedAtUtc: seedArticle.ReadAtUtc.AddMinutes(1),
                summary: seedArticle.Summary);

            articleResult.IsError.ShouldBeFalse();
            dbContext.Articles.Add(articleResult.Value);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private sealed record SeedArticle(
        string Url,
        string Source,
        DateTime ReadAtUtc,
        bool IsTradingUseful,
        string Summary);
}
