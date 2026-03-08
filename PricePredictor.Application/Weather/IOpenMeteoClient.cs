namespace PricePredictor.Application.Weather;

public interface IOpenMeteoClient
{
    Task<WeatherForecastResponse?> GetForecastAsync(City city);
}




