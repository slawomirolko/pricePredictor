using Grpc.Core;
using Google.Protobuf.WellKnownTypes;
using PricePredicator.App.Finance;

namespace PricePredicator.App.Gateway;

public class GatewayRpcEndpoint : Gateway.GatewayBase
{
    private readonly IGatewayService _gatewayService;
    private readonly IVolatilityRepository _volatilityRepository;

    public GatewayRpcEndpoint(IGatewayService gatewayService, IVolatilityRepository volatilityRepository)
    {
        _gatewayService = gatewayService;
        _volatilityRepository = volatilityRepository;
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