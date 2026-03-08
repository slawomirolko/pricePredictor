using PricePredictor.Application.Weather;

namespace PricePredictor.Infrastructure.Weather;

internal static class WeatherMapperExtensions
{
    public static WeatherForecastResponse MapToModel(this WeatherForecastResponseDto dto)
    {
        return new WeatherForecastResponse
        {
            Daily = dto.Daily?.MapToModel()
        };
    }

    private static DailyForecast MapToModel(this DailyForecastDto dto)
    {
        return new DailyForecast
        {
            Time = dto.Time,
            TempMax = dto.TempMax,
            TempMin = dto.TempMin,
            WeatherCode = dto.WeatherCode
        };
    }
}



