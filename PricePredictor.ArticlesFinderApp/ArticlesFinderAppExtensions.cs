using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PricePredictor.Application;
using PricePredictor.Infrastructure;
using PricePredictor.Persistence;
using GoldNewsSettings = PricePredictor.Infrastructure.GoldNewsSettings;
using GoldNewsSettingsApp = PricePredictor.Application.Data.GoldNewsSettings;

namespace PricePredictor.ArticlesFinderApp;

public static class ArticlesFinderAppExtensions
{
    public static IServiceCollection AddArticlesFinderApp(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddApplication();
        services.AddGoldNewsClient();
        services.AddPersistence(configuration);

        services.Configure<GoldNewsSettings>(configuration.GetSection(GoldNewsSettings.SectionName));
        services.Configure<GoldNewsSettingsApp>(configuration.GetSection(GoldNewsSettingsApp.SectionName));
        services.Configure<PersistenceDefaultConnectionSettings>(
            configuration.GetSection(PersistenceDefaultConnectionSettings.SectionName));

        services.AddSingleton<ArticlesFinderApp>();

        return services;
    }
}
