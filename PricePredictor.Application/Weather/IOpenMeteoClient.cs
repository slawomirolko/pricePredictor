namespace PricePredictor.Application.Weather;

public interface IOpenMeteoClient
{
    Task<WeatherForecastResponse?> GetForecastAsync(City city);
}

public enum City
{
    Tokyo = 1,
    Beijing = 2,
    Moscow = 3,
    Warsaw = 4,
    Berlin = 5,
    Paris = 6,
    London = 7,
    NewYork = 8,
    Ottawa = 9,
    LosAngeles = 10
}

public class WeatherForecastResponse
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DailyData Daily { get; set; } = new();
}

public class DailyData
{
    public List<double>? TempMax { get; set; }
    public List<double>? TempMin { get; set; }
    public List<int>? WeatherCode { get; set; }
}

public class HourlyData
{
    public List<DateTime> Time { get; set; } = new();
    public List<double> Temperature2m { get; set; } = new();
    public List<double> RelativeHumidity2m { get; set; } = new();
    public List<double> PrecipitationProbability { get; set; } = new();
}



