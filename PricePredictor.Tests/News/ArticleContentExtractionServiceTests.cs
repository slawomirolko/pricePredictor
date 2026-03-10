using Microsoft.Extensions.Logging;
using NSubstitute;
using PricePredictor.Application.News;
using Shouldly;
using Xunit;

namespace PricePredictor.Tests.News;

public sealed class ArticleContentExtractionServiceTests
{
    private readonly IOllamaArticleExtractionClient _ollamaClient = Substitute.For<IOllamaArticleExtractionClient>();
    private readonly ILogger<ArticleContentExtractionService> _logger = Substitute.For<ILogger<ArticleContentExtractionService>>();

    [Fact]
    public async Task ShouldUseSystemPromptAndReturnOllamaResult()
    {
        var html = "<html><body><article><p>Gold prices rose as central bank demand remained strong.</p><p>Investors were cautious ahead of inflation data.</p></article></body></html>";
        var extracted = "Gold prices rose as central bank demand remained strong. Investors were cautious ahead of inflation data. Analysts expect volatility in commodity markets.";

        _ollamaClient
            .ExtractMainContentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(extracted);

        var service = new ArticleContentExtractionService(_ollamaClient, _logger);

        var result = await service.ExtractAsync(html, null, "Manual Reuters Title", CancellationToken.None);

        result.ShouldBe(extracted);

        await _ollamaClient.Received(1).ExtractMainContentAsync(
            ArticleExtractionPrompts.ArticleExtractionPrompt,
            Arg.Any<string>(),
            "Manual Reuters Title",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldFallbackToBodyTextWhenOllamaReturnsShortText()
    {
        var html = "<html><body><article><p>Short</p></article></body></html>";
        var bodyText = new string('A', 180);

        _ollamaClient
            .ExtractMainContentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("too short");

        var service = new ArticleContentExtractionService(_ollamaClient, _logger);

        var result = await service.ExtractAsync(html, bodyText, null, CancellationToken.None);

        result.ShouldBe(bodyText);
    }
}
