using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PricePredicator.App;

public class NtfyBackgroundService : BackgroundService
{
    private readonly ILogger<NtfyBackgroundService> _logger;
    private readonly NtfyClient _ntfyClient;
    private readonly NtfySettings _settings;

    public NtfyBackgroundService(
        ILogger<NtfyBackgroundService> logger,
        NtfyClient ntfyClient,
        IOptions<NtfySettings> settings)
    {
        _logger = logger;
        _ntfyClient = ntfyClient;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Ntfy Background Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _ntfyClient.SendAsync(_settings.Topic, "Test");
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