namespace PricePredicator.Infrastructure.Weather;

public interface IOpenMeteoClient
{
    Task<WeatherForecastResponse?> GetForecastAsync(City city);
}