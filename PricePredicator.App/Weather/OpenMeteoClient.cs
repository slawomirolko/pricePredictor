using System.Net.Http.Json;

namespace PricePredicator.App.Weather;

internal class OpenMeteoClient : IOpenMeteoClient
{
    private readonly HttpClient _http;

    public OpenMeteoClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<WeatherForecastResponse?> GetForecastAsync(City city)
    {
        var (lat, lon) = CityCoordinates.GetCoordinates(city);
        var url = $"v1/forecast?latitude={lat}&longitude={lon}&daily=temperature_2m_max,temperature_2m_min,weathercode&timezone=auto";
        
        return await _http.GetFromJsonAsync<WeatherForecastResponse>(url);
    }
}