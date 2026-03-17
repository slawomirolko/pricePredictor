using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PricePredictor.ArticlesFinderHostedService.Tests.Integration.Setup;

public sealed class ArticlesFinderIntegrationTestFactory : IDisposable
{
    private readonly IHost _host;

    public ArticlesFinderIntegrationTestFactory(string connectionString)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Production
        });

        var testSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.Test.json");
        var cloudApiKey = Environment.GetEnvironmentVariable("OLLAMA_CLOUD_API_KEY")
                          ?? Environment.GetEnvironmentVariable("OLLAMA_API_KEY");

        builder.Configuration.AddJsonFile(testSettingsPath, optional: false);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PersistenceDefaultConnection:ConnectionString"] = connectionString,
            ["GoldNews:UseCloud"] = "true",
            ["GoldNews:CloudOllamaUrl"] = "https://ollama.com",
            ["GoldNews:CloudOllamaApiKey"] = cloudApiKey
        });

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
        builder.Logging.AddFilter("Microsoft", LogLevel.Information);

        builder.Services.AddArticlesFinderHostedService(builder.Configuration, includeHostedService: false);

        _host = builder.Build();
    }

    public IServiceProvider Services => _host.Services;

    public void Dispose()
    {
        _host.Dispose();
    }
}
