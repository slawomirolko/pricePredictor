using Microsoft.Extensions.DependencyInjection;
using PricePredictor.Application.News;
using PricePredictor.Application.Weather;

namespace PricePredictor.Application;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IGatewayService, GatewayService>();
        services.AddScoped<IWeatherService, WeatherService>();
        services.AddScoped<INewsService, NewsService>();

        services.AddSingleton<INewsArticleChannel, NewsArticleChannel>();
        services.AddSingleton<IArticleContentExtractionService, ArticleContentExtractionService>();
        services.AddSingleton<ISeleniumFlowBuilderFactory, SeleniumFlowBuilderFactory>();
        services.AddScoped<IArticleService, ArticleService>();
        return services;
    }
}

