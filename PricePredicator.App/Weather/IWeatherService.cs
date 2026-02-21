namespace PricePredicator.App.Weather;

public interface IWeatherService
{
    /// <summary>
    /// Gets the current forecast (next day) for all predefined cities.
    /// </summary>
    /// <returns>List of CityWeather objects</returns>
    Task<List<CityWeather>> GetCitiesWeatherAsync();
}