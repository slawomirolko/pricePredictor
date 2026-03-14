using NSubstitute;
using PricePredictor.Application;
using PricePredictor.Application.Finance;
using PricePredictor.Application.Notifications;
using PricePredictor.Application.Weather;
using Shouldly;
using Xunit;

namespace PricePredictor.Tests.Finance;

public sealed class TradingIndicatorNotificationServiceTests
{
    private readonly INtfyClient _ntfyClient = Substitute.For<INtfyClient>();
    private readonly IWeatherService _weatherService = Substitute.For<IWeatherService>();

    [Fact]
    public async Task SendArticleSummaryNotificationAsync_whenCalled_sendsFormattedSummaryMessage()
    {
        var service = new TradingIndicatorNotificationService(_ntfyClient, _weatherService, "topic-1");
        var readAt = new DateTime(2026, 3, 13, 20, 45, 0, DateTimeKind.Utc);

        await service.SendArticleSummaryNotificationAsync(
            source: "reuters",
            url: "https://www.reuters.com/world/example-article",
            readAt: readAt,
            summary: "Gold rises after central bank signals and geopolitical risk support safe-haven demand.",
            cancellationToken: CancellationToken.None);

        await _ntfyClient.Received(1).SendAsync(
            "topic-1",
            Arg.Is<string>(message =>
                message.Contains("TRADING-USEFUL ARTICLE") &&
                message.Contains("Source: reuters") &&
                message.Contains("https://www.reuters.com/world/example-article") &&
                message.Contains("Gold rises after central bank signals")),
            CancellationToken.None);
    }

    [Fact]
    public async Task SendArticleSummaryNotificationAsync_whenClientThrows_doesNotPropagate()
    {
        var service = new TradingIndicatorNotificationService(_ntfyClient, _weatherService, "topic-1");

        _ntfyClient
            .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("ntfy down"));

        var exception = await Record.ExceptionAsync(() => service.SendArticleSummaryNotificationAsync(
            source: "reuters",
            url: "https://example.com/article",
            readAt: DateTime.UtcNow,
            summary: "Useful trading summary",
            cancellationToken: CancellationToken.None));

        exception.ShouldBeNull();
    }
}
