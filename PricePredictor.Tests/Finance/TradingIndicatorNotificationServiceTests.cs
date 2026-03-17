using NSubstitute;
using PricePredictor.Application;
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
            .Returns(_ => throw new InvalidOperationException("ntfy down"));

        var exception = await Record.ExceptionAsync(() => service.SendArticleSummaryNotificationAsync(
            source: "reuters",
            url: "https://example.com/article",
            readAt: DateTime.UtcNow,
            summary: "Useful trading summary",
            cancellationToken: CancellationToken.None));

        exception.ShouldBeNull();
    }

    [Fact]
    public async Task SendSummaryNotificationAsync_whenCalled_forwardsCancellationToken()
    {
        var service = new TradingIndicatorNotificationService(_ntfyClient, _weatherService, "topic-1");
        var cancellationToken = new CancellationTokenSource().Token;

        var metrics = new Dictionary<string, TradingMetrics>
        {
            ["GC=F"] = new TradingMetrics
            {
                Symbol = "GC=F",
                Timestamp = DateTime.UtcNow,
                Close = 3000m,
                LogReturn = 0.001,
                Vol5 = 0.01,
                Vol15 = 0.02,
                Vol60 = 0.03,
                CompositePanicScore = 0.4
            }
        };

        await service.SendSummaryNotificationAsync(metrics, cancellationToken);

        await _ntfyClient.Received(1).SendAsync(
            "topic-1",
            Arg.Is<string>(message => message.Contains("TRADING DASHBOARD SUMMARY")),
            cancellationToken);
    }

    [Fact]
    public async Task SendTradingIndicatorsNotificationAsync_whenCalled_forwardsCancellationToken()
    {
        var service = new TradingIndicatorNotificationService(_ntfyClient, _weatherService, "topic-1");
        var timestamp = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);
        var cancellationToken = new CancellationTokenSource().Token;

        await service.SendTradingIndicatorsNotificationAsync(
            symbol: "GC=F",
            timestamp: timestamp,
            close: 3025.5m,
            logReturn: 0.002,
            vol5: 0.015,
            vol15: 0.020,
            vol60: 0.028,
            shortPanicScore: 0.6,
            longPanicScore: 0.5,
            compositePanicScore: 0.7,
            atr: 12.4,
            rsiDeviation: 0.3,
            bollingerDeviation: 0.2,
            volumeSpike: 1.1,
            vroc: 0.4,
            cancellationToken: cancellationToken);

        await _ntfyClient.Received(1).SendAsync(
            "topic-1",
            Arg.Is<string>(message =>
                message.Contains("GC=F") &&
                message.Contains("TECHNICAL INDICATORS")),
            cancellationToken);
    }
}
