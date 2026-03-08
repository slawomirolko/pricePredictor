using Grpc.Core;
using Google.Protobuf.WellKnownTypes;
using PricePredictor.Application;
using PricePredictor.Application.Finance;

namespace PricePredictor.Api.Gateway;

public class GatewayRpcEndpoint : Gateway.GatewayBase
{
    private readonly IGatewayService _gatewayService;
    private readonly IVolatilityRepository _volatilityRepository;
    private readonly INewsService _goldNewsService;
    private readonly ILogger<GatewayRpcEndpoint> _logger;

    public GatewayRpcEndpoint(
        IGatewayService gatewayService,
        IVolatilityRepository volatilityRepository,
        INewsService goldNewsService,
        ILogger<GatewayRpcEndpoint> logger)
    {
        _gatewayService = gatewayService;
        _volatilityRepository = volatilityRepository;
        _goldNewsService = goldNewsService;
        _logger = logger;
    }

    public override async Task<GatewayReply> Send(
        GatewayRequest request,
        ServerCallContext context)
    {
        var result = await _gatewayService
            .HandleAsync(request.Payload, context.CancellationToken);

        return new GatewayReply
        {
            Result = result
        };
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

        try
        {
            var result = await _goldNewsService.DownloadAndStoreAsync(request.Url, context.CancellationToken);

            return new DownloadArticleReply
            {
                Success = result.Success,
                Message = result.Message,
                WasAlreadyStored = result.WasAlreadyStored,
                ContentLength = result.ContentLength
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing article: {Url}", request.Url);
            throw new RpcException(new Status(StatusCode.Internal, $"Error processing article: {ex.Message}"));
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
}
