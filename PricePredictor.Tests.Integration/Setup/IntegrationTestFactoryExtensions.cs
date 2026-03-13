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
            return builder.ConfigureAppConfiguration((context, config) =>
            {
                var testSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.Test.json");
                var cloudApiKey = Environment.GetEnvironmentVariable("OLLAMA_CLOUD_API_KEY")
                                  ?? Environment.GetEnvironmentVariable("OLLAMA_API_KEY");

                config.AddJsonFile(testSettingsPath, optional: false);
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = connectionString,
                    ["GoldNews:UseCloud"] = "true",
                    ["GoldNews:CloudOllamaUrl"] = "https://ollama.com",
                    ["GoldNews:CloudOllamaApiKey"] = cloudApiKey
                });

                if (context.HostingEnvironment.IsDevelopment())
                {
                    config.AddUserSecrets<Program>(optional: true);
                }
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

                // Register persistence repositories
                services.AddPersistence(context.Configuration);

                // Register infrastructure clients and settings
                services.AddGoldNewsClient();
                services.AddOpenMeteoClient();
                services.AddStooqGoldPriceClient();
                services.AddGoogleNewsRssClient(context.Configuration);
                services.AddOllamaArticleExtractionClient();

                services.Configure<GoldNewsSettings>(context.Configuration.GetSection(GoldNewsSettings.SectionName));

                // Register Application services
                services.AddApplication();
                services.AddScoped<INewsService, NewsService>();
                services.AddScoped<PricePredictor.Application.News.INewsService, Application.News.GoogleNewsRssService>();
                services.AddScoped<Application.Weather.IWeatherService, Application.Weather.WeatherService>();
                services.AddScoped<PricePredictor.Application.IGatewayService, GatewayService>();

                // Ensure GoldNewsSettings from Application namespace is also configured
                services.Configure<PricePredictor.Application.Data.GoldNewsSettings>(context.Configuration.GetSection(GoldNewsSettings.SectionName));
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
