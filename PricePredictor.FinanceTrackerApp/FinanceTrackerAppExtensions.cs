using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PricePredictor.Application;
using PricePredictor.Application.Finance;
using PricePredictor.Application.Notifications;
using PricePredictor.Application.Weather;
using PricePredictor.Infrastructure;
using PricePredictor.Persistence;

namespace PricePredictor.FinanceTrackerApp;

public static class FinanceTrackerAppExtensions
{
    public static IServiceCollection AddFinanceTrackerApp(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddApplication();
        services.AddNtfyClient(configuration);
        services.AddOpenMeteoClient();
        services.AddYahooFinanceClient();
        services.AddPersistence(configuration);

        services.Configure<NtfySettings>(configuration.GetSection(NtfySettings.SectionName));
        services.Configure<YahooFinanceSettings>(configuration.GetSection(YahooFinanceSettings.SectionName));
        services.Configure<PersistenceDefaultConnectionSettings>(
            configuration.GetSection(PersistenceDefaultConnectionSettings.SectionName));

        services.AddSingleton(sp =>
        {
            var ntfyClient = sp.GetRequiredService<INtfyClient>();
            var weatherService = sp.GetRequiredService<IWeatherService>();
            var ntfySettings = sp.GetRequiredService<IOptions<NtfySettings>>().Value;
            return new TradingIndicatorNotificationService(ntfyClient, weatherService, ntfySettings.Topic);
        });
        services.AddSingleton<YahooFinanceApp>();

        return services;
    }
}
