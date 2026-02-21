using System.Text.Json.Serialization;

namespace PricePredicator.App.Weather;

public class WeatherForecastResponse
{
    [JsonPropertyName("daily")]
    public DailyForecast? Daily { get; set; }
}

public class DailyForecast
{
    [JsonPropertyName("time")]
    public string[]? Time { get; set; }

    [JsonPropertyName("temperature_2m_max")]
    public double[]? TempMax { get; set; }

    [JsonPropertyName("temperature_2m_min")]
    public double[]? TempMin { get; set; }

    [JsonPropertyName("weathercode")]
    public int[]? WeatherCode { get; set; }
}