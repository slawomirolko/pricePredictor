using Microsoft.Extensions.Logging;
using NSubstitute;
using PricePredictor.Application.News;
using Shouldly;
using Xunit;

namespace PricePredictor.Tests.News;

public sealed class ArticleContentExtractionServiceArticleParagraphTests
{
    private readonly IOllamaArticleExtractionClient _ollamaClient = Substitute.For<IOllamaArticleExtractionClient>();
    private readonly ILogger<ArticleContentExtractionService> _logger = Substitute.For<ILogger<ArticleContentExtractionService>>();

    [Fact]
    public async Task ExtractAsync_ShouldUseArticleParagraphsWhenDataTestIdIsMissing()
    {
        var html = "<html><body><article>" +
                   "<p>First paragraph from article markup with enough text to make extraction meaningful for downstream LLM processing and classification.</p>" +
                   "<p>Second paragraph from article markup with extra context for market movement and macro events that keeps payload length above threshold.</p>" +
                   "</article></body></html>";

        _ollamaClient
            .ExtractMainContentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("LLM extracted content that is intentionally above one hundred characters to satisfy the minimum gate in extraction service.");

        var service = new ArticleContentExtractionService(_ollamaClient, _logger);
        var result = await service.ExtractAsync(html, null, "Reuters Title", CancellationToken.None);

        result.ShouldBe("LLM extracted content that is intentionally above one hundred characters to satisfy the minimum gate in extraction service.");

        await _ollamaClient.Received(1).ExtractMainContentAsync(
            Arg.Any<string>(),
            Arg.Is<string>(payload =>
                payload.Contains("First paragraph from article markup", StringComparison.Ordinal) &&
                payload.Contains("Second paragraph from article markup", StringComparison.Ordinal)),
            "Reuters Title",
            Arg.Any<CancellationToken>());
    }
}

