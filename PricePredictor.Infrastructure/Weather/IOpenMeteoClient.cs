namespace PricePredictor.Infrastructure.Weather;

public interface IOpenMeteoClient
{
    Task<WeatherForecastResponse?> GetForecastAsync(City city);
}