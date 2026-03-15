namespace PricePredictor.Application.Weather;

public class WeatherForecastResponse
{
    public DailyForecast? Daily { get; set; }
}

public class DailyForecast
{
    public string[]? Time { get; set; }
    public double[]? TempMax { get; set; }
    public double[]? TempMin { get; set; }
    public int[]? WeatherCode { get; set; }
}


