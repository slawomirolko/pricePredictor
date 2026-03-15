using System.Text.Json.Serialization;

namespace PricePredictor.Infrastructure.Weather;

public class WeatherForecastResponseDto
{
    [JsonPropertyName("daily")]
    public DailyForecastDto? Daily { get; set; }
}

public class DailyForecastDto
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

