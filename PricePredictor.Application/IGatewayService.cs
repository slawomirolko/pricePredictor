namespace PricePredictor.Application;

public interface IGatewayService
{
    Task<string> HandleAsync(string payload, CancellationToken cancellationToken);
}

