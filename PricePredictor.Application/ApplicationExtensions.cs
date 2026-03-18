using Microsoft.Extensions.DependencyInjection;
using PricePredictor.Application.Finance;
using PricePredictor.Application.Finance.Interfaces;
using PricePredictor.Application.News;
using PricePredictor.Application.Weather;

namespace PricePredictor.Application;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IWeatherService, WeatherService>();
        services.AddScoped<INewsService, NewsService>();
        services.AddScoped<IVolatilityExportService, VolatilityExportService>();

        services.AddSingleton<INewsArticleChannel, NewsArticleChannel>();
        services.AddSingleton<IArticleContentExtractionService, ArticleContentExtractionService>();
        services.AddSingleton<ISeleniumFlowBuilderFactory, SeleniumFlowBuilderFactory>();
        services.AddScoped<IArticleService, ArticleService>();
        services.AddScoped<IImportantArticleService, ImportantArticleService>();
        return services;
    }
}
