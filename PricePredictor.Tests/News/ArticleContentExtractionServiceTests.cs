using Microsoft.Extensions.Logging;
using NSubstitute;
using PricePredictor.Application.News;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace PricePredictor.Tests.News;

public sealed class ArticleContentExtractionServiceTests
{
    private readonly IOllamaArticleExtractionClient _ollamaClient = Substitute.For<IOllamaArticleExtractionClient>();
    private readonly ILogger<ArticleContentExtractionService> _logger = Substitute.For<ILogger<ArticleContentExtractionService>>();
    private readonly ITestOutputHelper _testOutputHelper;

    public ArticleContentExtractionServiceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    private static readonly string[] ExpectedParagraphSnippets = new[]
    {
        "Oct 13 (Reuters) - Gold broke through $4,100 per ounce",
        "Spot gold was up 2.2% to $4,106.48 per ounce",
        "U.S. gold futures for December settled 3.3% higher at $4,133",
        "Gold has climbed 56% this year",
        "Gold could easily continue its upward momentum",
        "Steady central bank purchases",
        "Donald Trump",
        "traders are pricing in a 97% probability",
        "Bank of America and Societe Generale now expect gold to reach",
        "This rally has legs in our view",
        "Spot silver rose 3.1% to $51.82",
        "Technical indicators show both are overbought",
        "Platinum rose 3.9% to $1,648.25"
    };

    [Fact]
    public async Task ShouldUseSystemPromptAndReturnOllamaResult()
    {
        var html =
            "<html><body><article>" +
            "<div data-testid='paragraph-1'>Gold prices rose as central bank demand remained strong. This is additional text to ensure the combined length is over 100 characters.</div>" +
            "<div data-testid='paragraph-2'>Investors were cautious ahead of inflation data. Analysts expect more volatility.</div>" +
            "</article></body></html>";
        var extracted =
            "Gold prices rose as central bank demand remained strong. Investors were cautious ahead of inflation data. Analysts expect volatility in commodity markets. This extracted text is also long enough to pass the 100 character minimum requirement.";

        _ollamaClient
            .ExtractMainContentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(extracted);

        var service = new ArticleContentExtractionService(_ollamaClient, _logger);

        var result = await service.ExtractAsync(html, null, "Manual Reuters Title", CancellationToken.None);

        result.ShouldBe(extracted);

        await _ollamaClient.Received(1).ExtractMainContentAsync(
            Arg.Is<string>(s => s.Contains("Your task is to transform")),
            Arg.Any<string>(),
            "Manual Reuters Title",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldFallbackToBodyTextWhenOllamaReturnsShortText()
    {
        var html = "<html><body><article><div data-testid='paragraph-1'>Short</div></article></body></html>";
        var bodyText = new string('A', 180);

        _ollamaClient
            .ExtractMainContentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("too short");

        var service = new ArticleContentExtractionService(_ollamaClient, _logger);

        var result = await service.ExtractAsync(html, bodyText, null, CancellationToken.None);

        result.ShouldBe(bodyText);
    }

    [Fact]
    public async Task ExtractParagraphs_ShouldExtractAll13ParagraphsFromReutersSnapshot()
    {
        var snapshotPath = Path.Combine(AppContext.BaseDirectory, "Snapshot.txt");
        var html = await File.ReadAllTextAsync(snapshotPath);

        string? capturedPayload = null;
        _ollamaClient
            .ExtractMainContentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedPayload = callInfo.ArgAt<string>(1);
                return "LLM result placeholder for snapshot test with sufficient length to pass minimum";
            });

        var service = new ArticleContentExtractionService(_ollamaClient, _logger);
        await service.ExtractAsync(html, null, null, CancellationToken.None);

        capturedPayload.ShouldNotBeNull("ExtractParagraphs should have found paragraph divs and called Ollama");

        // Log the extracted content for the user
        _testOutputHelper.WriteLine($"[DEBUG_LOG] Extracted Content from Snapshot:\n{capturedPayload}");

        foreach (var snippet in ExpectedParagraphSnippets)
        {
            capturedPayload.ShouldContain(
                snippet, Case.Sensitive,
                $"Extracted payload should contain paragraph text: '{snippet}'");
        }
    }

    [Fact]
    public async Task ExtractParagraphs_ShouldExtractDivsWithDataTestidParagraph()
    {
        var html = "<html><body>" +
                   "<div data-testid='paragraph-1'>First paragraph with substantial content about markets and trading that needs to be long enough to pass the minimum length requirement for extraction.</div>" +
                   "<div data-testid='paragraph-2'>Second paragraph with additional information about gold prices and economic indicators that also helps in reaching the required length for processing.</div>" +
                   "<div data-testid='paragraph-3'>Third paragraph with valuable data for comprehensive market analysis and ensuring the total content is sufficient for the service to accept it.</div>" +
                   "<div>This div without data-testid should be ignored even if it contains some content.</div>" +
                   "</body></html>";

        _ollamaClient
            .ExtractMainContentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("Extracted by Ollama which is long enough now to pass the minimum length check of one hundred characters.");

        var service = new ArticleContentExtractionService(_ollamaClient, _logger);
        var result = await service.ExtractAsync(html, null, null, CancellationToken.None);

        result.ShouldBe("Extracted by Ollama which is long enough now to pass the minimum length check of one hundred characters.");

        await _ollamaClient.Received(1).ExtractMainContentAsync(
            Arg.Any<string>(),
            Arg.Is<string>(payload =>
                payload.Contains("First paragraph") &&
                payload.Contains("Second paragraph") &&
                payload.Contains("Third paragraph") &&
                !payload.Contains("This div without data-testid")),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExtractParagraphs_ShouldOnlyExtractFromSpecificDivs()
    {
        var html = "<html><body>" +
                   "<script>var x = 'should be removed';</script>" +
                   "<div data-testid='paragraph-1'>Article content here with substantial information and details about gold market trends and it needs to be long enough.</div>" +
                   "<style>body { color: red; }</style>" +
                   "<div class='cookie-banner'>Cookie notice should be ignored but we also need more text here to reach the limit of one hundred characters in total.</div>" +
                   "<div data-testid='paragraph-2'>More article content here with substance and valuable information for readers that helps passing the length check easily.</div>" +
                   "<nav>Navigation should be ignored and shouldn't contribute to the extracted content at all during the extraction process.</nav>" +
                   "<p>This paragraph without testid should also be ignored by the extraction logic that only looks for specific data-testid attributes.</p>" +
                   "</body></html>";

        _ollamaClient
            .ExtractMainContentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("Extracted by Ollama which is long enough now to pass the minimum length check of one hundred characters.");

        var service = new ArticleContentExtractionService(_ollamaClient, _logger);
        var result = await service.ExtractAsync(html, null, null, CancellationToken.None);

        result.ShouldBe("Extracted by Ollama which is long enough now to pass the minimum length check of one hundred characters.");

        await _ollamaClient.Received(1).ExtractMainContentAsync(
            Arg.Any<string>(),
            Arg.Is<string>(payload =>
                !payload.Contains("var x") &&
                !payload.Contains("Cookie notice") &&
                !payload.Contains("Navigation") &&
                !payload.Contains("This paragraph without testid") &&
                payload.Contains("Article content") &&
                payload.Contains("More article content")),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExtractParagraphs_ShouldReturnNullWhenNoParagraphDivsFound()
    {
        var html = "<html><body>" +
                   "<div>Regular div without data-testid</div>" +
                   "<p>Paragraph without testid</p>" +
                   "</body></html>";

        _ollamaClient
            .ExtractMainContentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var service = new ArticleContentExtractionService(_ollamaClient, _logger);
        var fallback = new string('X', 150);

        var result = await service.ExtractAsync(html, fallback, null, CancellationToken.None);

        result.ShouldBe(fallback);
    }

    [Fact]
    public async Task ExtractParagraphs_ShouldNormalizeWhitespace()
    {
        var html = "<html><body>" +
                   "<div data-testid='paragraph-1'>Text   with    multiple\n\nspaces\t\tand\rnewlines that continues with more information to ensure it exceeds the minimum length for extraction service processing.</div>" +
                   "<div data-testid='paragraph-2'>Another paragraph with additional substantial content for testing purposes and reaching the threshold of one hundred characters.</div>" +
                   "</body></html>";

        _ollamaClient
            .ExtractMainContentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("Normalized by Ollama which is long enough now to pass the minimum length check of one hundred characters.");

        var service = new ArticleContentExtractionService(_ollamaClient, _logger);
        var result = await service.ExtractAsync(html, null, null, CancellationToken.None);

        result.ShouldBe("Normalized by Ollama which is long enough now to pass the minimum length check of one hundred characters.");

        await _ollamaClient.Received(1).ExtractMainContentAsync(
            Arg.Any<string>(),
            Arg.Is<string>(payload =>
                payload.Contains("Text with multiple spaces and newlines") &&
                !payload.Contains("  ")),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExtractParagraphs_ShouldReturnNullWhenExtractedLengthBelowMinimum()
    {
        var html = "<html><body><div data-testid='paragraph-1'>Short</div></body></html>";
        var fallback = new string('Z', 150);

        _ollamaClient
            .ExtractMainContentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("very short");

        var service = new ArticleContentExtractionService(_ollamaClient, _logger);
        var result = await service.ExtractAsync(html, fallback, null, CancellationToken.None);

        result.ShouldBe(fallback);
    }
}
