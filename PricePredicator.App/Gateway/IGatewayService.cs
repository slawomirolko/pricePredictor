namespace PricePredicator.App.Gateway;

public interface IGatewayService
{
    Task<string> HandleAsync(string payload, CancellationToken cancellationToken);
}
