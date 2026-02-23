using PricePredicator.App.Weather;

namespace PricePredicator.App.Gateway;

public class GatewayService : IGatewayService
{
    private readonly IWeatherService _weatherService;

    public GatewayService(IWeatherService weatherService)
    {
        _weatherService = weatherService;
    }

    public async Task<string> HandleAsync(string payload, CancellationToken cancellationToken)
    {
        if (payload == "dupa")
            return "123";

        // Example of using existing services
        var weather = await _weatherService.GetCitiesWeatherAsync();

        return string.Join(Environment.NewLine,
            weather.Select(x => $"{x.City} Max temp: {x.MaxTemp} Min temp: {x.MinTemp}")
        );
    }
}
