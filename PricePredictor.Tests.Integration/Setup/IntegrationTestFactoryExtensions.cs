using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PricePredictor.Application;
using PricePredictor.Infrastructure;
using PricePredictor.Persistence;

namespace PricePredictor.Tests.Integration.Setup;

internal static class IntegrationTestFactoryExtensions
{
    extension(IWebHostBuilder builder)
    {
        public IWebHostBuilder ConfigureTestAppConfiguration(string connectionString)
        {
            return builder.ConfigureAppConfiguration((_, config) =>
            {
                var testSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.Test.json");
                config.AddJsonFile(testSettingsPath, optional: false);
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = connectionString
                });
            });
        }

        public IWebHostBuilder ConfigureTestServices(string connectionString)
        {
            return builder.ConfigureServices((context, services) =>
            {
                // Remove default registrations that cause lifetime issues
                services.RemoveAll<DbContextOptions<PricePredictorDbContext>>();
                services.RemoveAll<IDbContextFactory<PricePredictorDbContext>>();
                services.RemoveAll<IHostedService>();
                services.RemoveAll<IGatewayService>();

                // Register DbContext factory instead of direct DbContext
                services.AddDbContextFactory<PricePredictorDbContext>(options =>
                    options.UseNpgsql(connectionString));

                // Register repositories
                services.AddScoped<Application.Data.IGoldNewsEmbeddingRepository, 
                    Persistence.Repositories.GoldNewsEmbeddingRepository>();
                services.AddScoped<PricePredictor.Application.Finance.Interfaces.IVolatilityRepository, 
                    PricePredictor.Persistence.Repositories.VolatilityRepository>();

                // Register infrastructure clients and settings
                services.AddGoldNewsClient();
                services.AddOpenMeteoClient();
                services.AddStooqGoldPriceClient();
                services.AddGoogleNewsRssClient(context.Configuration);
                services.AddOllamaArticleExtractionClient();

                services.Configure<GoldNewsSettings>(context.Configuration.GetSection(GoldNewsSettings.SectionName));

                services.AddSingleton<OllamaSharp.IOllamaApiClient>(sp =>
                {
                    var settings = sp.GetRequiredService<IOptions<GoldNewsSettings>>().Value;
                    return new OllamaSharp.OllamaApiClient(new Uri(settings.OllamaUrl));
                });

                // Register Application services
                services.AddApplication();
                services.AddScoped<INewsService, NewsService>();
                services.AddScoped<PricePredictor.Application.News.INewsService, Application.News.GoogleNewsRssService>();
                services.AddScoped<Application.Weather.IWeatherService, Application.Weather.WeatherService>();
                services.AddScoped<PricePredictor.Application.IGatewayService, GatewayService>();
            });
        }

        public IWebHostBuilder ConfigureTestLogging()
        {
            return builder.ConfigureLogging((_, logging) =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddFilter("Microsoft.AspNetCore", LogLevel.Information);
                logging.AddFilter("Grpc", LogLevel.Debug);
            });
        }
    }
}
