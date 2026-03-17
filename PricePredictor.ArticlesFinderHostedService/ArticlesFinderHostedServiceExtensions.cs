using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PricePredictor.Application;
using PricePredictor.Infrastructure;
using PricePredictor.Persistence;
using GoldNewsSettings = PricePredictor.Infrastructure.GoldNewsSettings;
using GoldNewsSettingsApp = PricePredictor.Application.Data.GoldNewsSettings;

namespace PricePredictor.ArticlesFinderHostedService;

public static class ArticlesFinderHostedServiceExtensions
{
    public static IServiceCollection AddArticlesFinderHostedService(
        this IServiceCollection services,
        IConfiguration configuration,
        bool includeHostedService = true)
    {
        services.AddApplication();
        services.AddGoldNewsClient();
        services.AddPersistence(configuration);

        services.Configure<GoldNewsSettings>(configuration.GetSection(GoldNewsSettings.SectionName));
        services.Configure<GoldNewsSettingsApp>(configuration.GetSection(GoldNewsSettingsApp.SectionName));
        services.Configure<PersistenceDefaultConnectionSettings>(
            configuration.GetSection(PersistenceDefaultConnectionSettings.SectionName));

        if (includeHostedService)
        {
            services.AddHostedService<BackgroundServices.ArticlesFinderHostedService>();
        }

        return services;
    }
}
