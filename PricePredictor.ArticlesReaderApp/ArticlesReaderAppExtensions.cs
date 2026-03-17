using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PricePredictor.Application;
using PricePredictor.Application.Notifications;
using PricePredictor.Application.Weather;
using PricePredictor.Infrastructure;
using PricePredictor.Persistence;
using GoldNewsSettings = PricePredictor.Infrastructure.GoldNewsSettings;
using GoldNewsSettingsApp = PricePredictor.Application.Data.GoldNewsSettings;

namespace PricePredictor.ArticlesReaderApp;

public static class ArticlesReaderAppExtensions
{
    public static IServiceCollection AddArticlesReaderApp(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddApplication();
        services.AddNtfyClient(configuration);
        services.AddOllamaArticleExtractionClient();
        services.AddOpenMeteoClient();
        services.AddPersistence(configuration);

        services.Configure<NtfySettings>(configuration.GetSection(NtfySettings.SectionName));
        services.Configure<GoldNewsSettings>(configuration.GetSection(GoldNewsSettings.SectionName));
        services.Configure<GoldNewsSettingsApp>(configuration.GetSection(GoldNewsSettingsApp.SectionName));
        services.Configure<PersistenceDefaultConnectionSettings>(
            configuration.GetSection(PersistenceDefaultConnectionSettings.SectionName));

        services.AddSingleton(sp =>
        {
            var ntfyClient = sp.GetRequiredService<INtfyClient>();
            var weatherService = sp.GetRequiredService<IWeatherService>();
            var ntfySettings = sp.GetRequiredService<IOptions<NtfySettings>>().Value;
            return new TradingIndicatorNotificationService(ntfyClient, weatherService, ntfySettings.Topic);
        });

        services.AddSingleton<ArticlesReaderApp>();

        return services;
    }
}

