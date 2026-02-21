namespace PricePredicator.App.Weather;

public interface IOpenMeteoClient
{
    Task<WeatherForecastResponse?> GetForecastAsync(City city);
}