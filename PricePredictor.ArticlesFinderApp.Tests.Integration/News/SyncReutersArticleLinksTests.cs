using Microsoft.Extensions.DependencyInjection;
using PricePredictor.Application.News;
using PricePredictor.ArticlesFinderApp.Tests.Integration.Setup;
using Shouldly;
using System.Globalization;
using System.Text.RegularExpressions;

namespace PricePredictor.ArticlesFinderApp.Tests.Integration.News;

public sealed class SyncReutersArticleLinksTests : IntegrationTest
{
    public SyncReutersArticleLinksTests(PostgresContainerFixture postgres)
        : base(postgres)
    {
    }

    [Fact]
    public async Task SyncReutersArticleLinks_whenExecuted_returnsSuccessOrBlockedResult()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(5));

        await DeleteStoredArticleLinksBySourceAsync("reuters", cancellation.Token);

        using var scope = Factory.Services.CreateScope();
        var articleService = scope.ServiceProvider.GetRequiredService<IArticleService>();

        var syncResult = await articleService.SyncArticleLinksAsync(cancellation.Token);

        if (syncResult.IsSourceBlocked)
        {
            syncResult.Succeeded.ShouldBeFalse();
            syncResult.ArticleLinks.Count.ShouldBe(0);
            syncResult.Message.ShouldContain("Reuters");
            return;
        }

        syncResult.Succeeded.ShouldBeTrue();
        syncResult.ArticleLinks.Count.ShouldBeGreaterThan(0);

        var storedLinks = await GetStoredArticleLinksBySourceAsync("reuters", cancellation.Token);
        storedLinks.Count.ShouldBe(syncResult.ArticleLinks.Count);

        var extractedUrlSet = syncResult.ArticleLinks.Select(x => x.Url).ToHashSet();
        foreach (var storedLink in storedLinks)
        {
            extractedUrlSet.Contains(storedLink.Url).ShouldBeTrue();

            var parsedDate = ParseDateFromUrl(storedLink.Url);
            if (parsedDate.HasValue)
            {
                storedLink.ReadAt.Date.ShouldBe(parsedDate.Value.Date);
            }
            else
            {
                storedLink.ReadAt.ShouldBeGreaterThan(DateTime.UnixEpoch);
            }

            storedLink.ReadAt.TimeOfDay.ShouldNotBe(TimeSpan.Zero);
        }
    }

    private static DateTime? ParseDateFromUrl(string url)
    {
        var dateMatch = Regex.Match(url, @"(\d{4})-(\d{2})-(\d{2})");
        if (!dateMatch.Success)
        {
            return null;
        }

        var dateValue = $"{dateMatch.Groups[1].Value}-{dateMatch.Groups[2].Value}-{dateMatch.Groups[3].Value}";
        if (!DateTime.TryParseExact(
                dateValue,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedDate))
        {
            return null;
        }

        return DateTime.SpecifyKind(parsedDate.Date, DateTimeKind.Utc);
    }
}
