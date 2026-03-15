using Microsoft.Extensions.DependencyInjection;
using PricePredictor.Application.News;

namespace PricePredictor.Application;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IArticleContentExtractionService, ArticleContentExtractionService>();
        services.AddSingleton<ISeleniumFlowBuilderFactory, SeleniumFlowBuilderFactory>();
        services.AddScoped<IArticleService, ArticleService>();
        return services;
    }
}

