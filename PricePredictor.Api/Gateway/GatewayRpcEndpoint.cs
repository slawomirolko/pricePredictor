using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using PricePredictor.Application.Finance;
using PricePredictor.Application.Finance.Interfaces;
using PricePredictor.Application.News;
using System.Text.Json;
using IGoldNewsService = PricePredictor.Application.INewsService;

namespace PricePredictor.Api.Gateway;

public class GatewayRpcEndpoint : Gateway.GatewayBase
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IVolatilityRepository _volatilityRepository;
    private readonly IVolatilityExportService _volatilityExportService;
    private readonly IGoldNewsService _goldNewsService;
    private readonly IImportantArticleService _importantArticleService;
    private readonly INewsArticleChannel _newsArticleChannel;
    private readonly ILogger<GatewayRpcEndpoint> _logger;

    public GatewayRpcEndpoint(
        IVolatilityRepository volatilityRepository,
        IVolatilityExportService volatilityExportService,
        IGoldNewsService goldNewsService,
        IImportantArticleService importantArticleService,
        INewsArticleChannel newsArticleChannel,
        ILogger<GatewayRpcEndpoint> logger)
    {
        _volatilityRepository = volatilityRepository;
        _volatilityExportService = volatilityExportService;
        _goldNewsService = goldNewsService;
        _importantArticleService = importantArticleService;
        _newsArticleChannel = newsArticleChannel;
        _logger = logger;
    }

    public override async Task<VolatilityQueryReply> GetVolatility(
        VolatilityQueryRequest request,
        ServerCallContext context)
    {
        if (request.Commodity == Commodity.Unspecified)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Commodity is required."));
        }

        if (request.Date == null)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Date is required."));
        }

        if (request.Minutes <= 0 || request.Minutes > 1440)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Minutes must be between 1 and 1440."));
        }

        var requestedDate = request.Date.ToDateTime();
        var dateUtc = requestedDate.Kind == DateTimeKind.Utc
            ? requestedDate
            : DateTime.SpecifyKind(requestedDate, DateTimeKind.Utc);

        var startUtc = dateUtc.Date;
        var endUtc = startUtc.AddMinutes(request.Minutes);

        var reply = new VolatilityQueryReply { Commodity = request.Commodity };
        var commodity = MapCommodity(request.Commodity);
        var points = await _volatilityRepository.GetVolatilityForPeriodAsync(commodity, startUtc, endUtc, context.CancellationToken);
        reply.Points.AddRange(points.Select(MapPoint));

        return reply;
    }

    public override async Task<JsonPayloadReply> ExportVolatilityPeriodJson(
        VolatilityPeriodRequest request,
        ServerCallContext context)
    {
        if (request.StartUtc == null)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "StartUtc is required."));
        }

        if (request.EndUtc == null)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "EndUtc is required."));
        }

        var startUtc = NormalizeUtc(request.StartUtc);
        var endUtc = NormalizeUtc(request.EndUtc);

        if (startUtc >= endUtc)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "StartUtc must be earlier than EndUtc."));
        }

        var export = await _volatilityExportService.GetPeriodAsync(startUtc, endUtc, context.CancellationToken);

        return new JsonPayloadReply
        {
            Json = JsonSerializer.Serialize(export, SerializerOptions),
            ContentType = "application/json"
        };
    }

    public override async Task<JsonPayloadReply> ExportLatestVolatilityJson(
        Empty request,
        ServerCallContext context)
    {
        var export = await _volatilityExportService.GetNewestAsync(context.CancellationToken);

        return new JsonPayloadReply
        {
            Json = JsonSerializer.Serialize(export, SerializerOptions),
            ContentType = "application/json"
        };
    }

    public override async Task<DownloadArticleReply> DownloadGoldNewsArticle(
        DownloadArticleRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "URL is required."));
        }

        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out _))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid URL format."));
        }

        _logger.LogInformation("Download article request: {Url}", request.Url);

        var title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim();
        var result = await _goldNewsService.DownloadAndStoreAsync(request.Url, title, context.CancellationToken);

        return new DownloadArticleReply
        {
            Success = result.Success,
            Message = result.Message,
            WasAlreadyStored = result.WasAlreadyStored,
            ContentLength = result.ContentLength
        };
    }

    public override async Task<NewestImportantArticlesReply> GetNewestImportantArticles(
        NewestImportantArticlesRequest request,
        ServerCallContext context)
    {
        var articles = await _importantArticleService.GetNewestAsync(context.CancellationToken);
        var reply = new NewestImportantArticlesReply();
        reply.Articles.AddRange(articles.Select(MapImportantArticle));
        return reply;
    }

    public override async Task SubscribeLatestNews(
        LatestNewsSubscriptionRequest request,
        IServerStreamWriter<LatestNewsNotification> responseStream,
        ServerCallContext context)
    {
        if (!string.Equals(request.Subscriber?.Trim(), "army", StringComparison.OrdinalIgnoreCase))
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Only army subscriber is allowed."));
        }

        _logger.LogInformation("Latest-news stream opened for subscriber {Subscriber}.", request.Subscriber);

        try
        {
            await foreach (var item in _newsArticleChannel.ReadAllAsync(context.CancellationToken))
            {
                var notification = new LatestNewsNotification
                {
                    Title = item.Title,
                    Link = item.Link,
                    Source = item.Source,
                    SentAtUtc = Timestamp.FromDateTime(DateTime.UtcNow)
                };

                if (item.PublishedAtUtc.HasValue)
                {
                    notification.PublishedAtUtc = Timestamp.FromDateTime(item.PublishedAtUtc.Value.UtcDateTime);
                }

                await responseStream.WriteAsync(notification);
            }
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Latest-news stream closed for subscriber {Subscriber}.", request.Subscriber);
        }
    }

    private static VolatilityCommodity MapCommodity(Commodity commodity) => commodity switch
    {
        Commodity.Gold => VolatilityCommodity.Gold,
        Commodity.Silver => VolatilityCommodity.Silver,
        Commodity.NaturalGas => VolatilityCommodity.NaturalGas,
        Commodity.Oil => VolatilityCommodity.Oil,
        _ => throw new RpcException(new Status(StatusCode.InvalidArgument, "Unsupported commodity."))
    };

    private static VolatilityPoint MapPoint(VolatilityPointDto row) => new()
    {
        Timestamp = Timestamp.FromDateTime(row.Timestamp.ToUniversalTime()),
        Open = (double)row.Open,
        High = (double)row.High,
        Low = (double)row.Low,
        Close = (double)row.Close,
        Volume = row.Volume,
        LogReturn = row.LogReturn,
        Vol5 = row.Vol5,
        Vol15 = row.Vol15,
        Vol60 = row.Vol60,
        ShortPanic = row.ShortPanic,
        LongPanic = row.LongPanic
    };

    private static ImportantArticle MapImportantArticle(ImportantArticleDto article) => new()
    {
        ArticleId = article.ArticleId.ToString(),
        Url = article.Url,
        Source = article.Source,
        ReadAtUtc = Timestamp.FromDateTime(article.ReadAtUtc),
        Summary = article.Summary ?? string.Empty
    };

    private static DateTime NormalizeUtc(Timestamp timestamp)
    {
        var value = timestamp.ToDateTime();
        return value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}
