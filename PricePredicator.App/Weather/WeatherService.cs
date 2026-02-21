namespace PricePredicator.App.Weather;

public class WeatherService : IWeatherService
{
    private readonly IOpenMeteoClient _client;

    public WeatherService(IOpenMeteoClient client)
    {
        _client = client;
    }

    public async Task<List<CityWeather>> GetCitiesWeatherAsync()
    {
        var cities = Enum.GetValues<City>();
        var result = new List<CityWeather>();

        foreach (var city in cities)
        {
            var forecast = await _client.GetForecastAsync(city);

            if (forecast?.Daily != null)
            {
                result.Add(new CityWeather
                {
                    City = city,
                    MaxTemp = forecast.Daily.TempMax?[0] ?? double.NaN,
                    MinTemp = forecast.Daily.TempMin?[0] ?? double.NaN,
                    WeatherCode = forecast.Daily.WeatherCode?[0] ?? -1
                });
            }
        }

        return result;
    }
}

public record CityWeather
{
    public City City { get; init; }
    public double MaxTemp { get; init; }
    public double MinTemp { get; init; }
    public int WeatherCode { get; init; }
}