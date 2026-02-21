using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PricePredicator.App.Weather;

namespace PricePredicator.App;

public class NtfyBackgroundService : BackgroundService
{
    private readonly ILogger<NtfyBackgroundService> _logger;
    private readonly NtfyClient _ntfyClient;
    private readonly IWeatherService _weatherService;
    private readonly NtfySettings _settings;

    public NtfyBackgroundService(
        ILogger<NtfyBackgroundService> logger,
        NtfyClient ntfyClient,
        IWeatherService weatherService,
        IOptions<NtfySettings> settings)
    {
        _logger = logger;
        _ntfyClient = ntfyClient;
        _weatherService = weatherService;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Ntfy Background Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cityWeatherList = await _weatherService.GetCitiesWeatherAsync();


                var sb = new StringBuilder();

                foreach (var cityWeather in cityWeatherList)
                {
                    sb.AppendLine($"{cityWeather.City}: Max {cityWeather.MaxTemp}°C, Min {cityWeather.MinTemp}°C, Code {cityWeather.WeatherCode}");
                }

                var resultString = sb.ToString();
                
                await _ntfyClient.SendAsync(_settings.Topic, resultString);
                _logger.LogInformation("Notification sent successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification");
            }
            
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }

        _logger.LogInformation("Ntfy Background Service stopping.");
    }
}