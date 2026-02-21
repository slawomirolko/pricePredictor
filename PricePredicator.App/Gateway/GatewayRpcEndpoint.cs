using Grpc.Core;

namespace PricePredicator.App.Gateway;

public class GatewayRpcEndpoint : Gateway.GatewayBase
{
    private readonly IGatewayService _gatewayService;

    public GatewayRpcEndpoint(IGatewayService gatewayService)
    {
        _gatewayService = gatewayService;
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
}