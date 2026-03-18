using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PricePredictor.Api.Gateway;
using PricePredictor.Application.Models;
using PricePredictor.Persistence;
using PricePredictor.Tests.Integration.Setup;
using Shouldly;
using System.Globalization;
using PricePredictor.Application.Data;
using GoldNewsSettings = PricePredictor.Infrastructure.GoldNewsSettings;

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
        var article1Id = Guid.CreateVersion7();
        var article2Id = Guid.CreateVersion7();
        var article3Id = Guid.CreateVersion7();
        var article4Id = Guid.CreateVersion7();
        var article5Id = Guid.CreateVersion7();
        var articles = new[]
        {
            new SeedArticle(
                article1Id,
                $"https://example.com/{testId}/article-1",
                source,
                new DateTime(2099, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                true,
                "summary-1"),
            new SeedArticle(
                article2Id,
                $"https://example.com/{testId}/article-2",
                source,
                new DateTime(2099, 1, 1, 11, 0, 0, DateTimeKind.Utc),
                true,
                "summary-2"),
            new SeedArticle(
                article3Id,
                $"https://example.com/{testId}/article-3",
                source,
                new DateTime(2099, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                false,
                "summary-3"),
            new SeedArticle(
                article4Id,
                $"https://example.com/{testId}/article-4",
                source,
                new DateTime(2099, 1, 1, 13, 0, 0, DateTimeKind.Utc),
                true,
                "summary-4"),
            new SeedArticle(
                article5Id,
                $"https://example.com/{testId}/article-5",
                source,
                new DateTime(2099, 1, 1, 14, 0, 0, DateTimeKind.Utc),
                true,
                "summary-5")
        };

        await SeedArticlesAsync(articles, CancellationToken.None);
        using var scope = Factory.Services.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<IOptions<GoldNewsSettings>>().Value;

        var reply = await GatewayClient.GetNewestImportantArticlesAsync(new NewestImportantArticlesRequest());
        var expectedArticle5Embedding = CreateExpectedEmbeddingText(5, settings.EmbeddingDimensions);
        var expectedArticle4Embedding = CreateExpectedEmbeddingText(4, settings.EmbeddingDimensions);
        var expectedArticle2Embedding = CreateExpectedEmbeddingText(2, settings.EmbeddingDimensions);

        reply.Articles.Count.ShouldBe(3);
        reply.Articles.Select(x => x.ArticleId).ToArray().ShouldBe([
            article5Id.ToString(),
            article4Id.ToString(),
            article2Id.ToString()
        ]);
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
            expectedArticle5Embedding,
            expectedArticle4Embedding,
            expectedArticle2Embedding
        ]);
    }

    private async Task SeedArticlesAsync(IEnumerable<SeedArticle> articles, CancellationToken cancellationToken)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PricePredictorDbContext>();
        var embeddingsRepository = scope.ServiceProvider.GetRequiredService<IGoldNewsEmbeddingRepository>();
        var settings = scope.ServiceProvider.GetRequiredService<IOptions<GoldNewsSettings>>().Value;

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
                summary: seedArticle.Summary,
                id: seedArticle.ArticleId);

            articleResult.IsError.ShouldBeFalse();
            dbContext.Articles.Add(articleResult.Value);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var seedArticle in articles)
        {
            await embeddingsRepository.UpsertArticleAsync(
                seedArticle.ArticleId,
                seedArticle.ReadAtUtc,
                seedArticle.Summary,
                CreateEmbedding(seedArticle.EmbeddingMarker, settings.EmbeddingDimensions),
                settings.EmbeddingDimensions,
                cancellationToken);
        }
    }

    private static float[] CreateEmbedding(int marker, int dimensions)
    {
        var embedding = new float[dimensions];
        embedding[0] = marker;
        embedding[1] = marker + 0.25f;
        embedding[2] = marker + 0.5f;
        return embedding;
    }

    private static string CreateExpectedEmbeddingText(int marker, int dimensions)
    {
        return "[" + string.Join(
            ",",
            CreateEmbedding(marker, dimensions).Select(x => x.ToString("G9", CultureInfo.InvariantCulture))) + "]";
    }

    private sealed record SeedArticle(
        Guid ArticleId,
        string Url,
        string Source,
        DateTime ReadAtUtc,
        bool IsTradingUseful,
        string Summary)
    {
        public int EmbeddingMarker => int.Parse(Summary.Split('-')[1], CultureInfo.InvariantCulture);
    }
}
