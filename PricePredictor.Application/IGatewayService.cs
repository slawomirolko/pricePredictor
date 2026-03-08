namespace PricePredictor.Api.Gateway;

public interface IGatewayService
{
    Task<string> HandleAsync(string payload, CancellationToken cancellationToken);
}

