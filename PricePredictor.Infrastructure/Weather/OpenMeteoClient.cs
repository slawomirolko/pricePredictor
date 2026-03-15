using System.Net.Http.Json;
using PricePredictor.Application.Weather;

namespace PricePredictor.Infrastructure.Weather;

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
        
        var dto = await _http.GetFromJsonAsync<WeatherForecastResponseDto>(url);
        return dto?.MapToModel();
    }
}