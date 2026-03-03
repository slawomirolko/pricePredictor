using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OllamaSharp;
using PricePredicator.App;
using PricePredicator.App.Finance;
using PricePredicator.App.Gateway;
using PricePredicator.App.Gold;
using PricePredicator.App.News;
using PricePredicator.App.GoldNews;
using PricePredicator.App.Weather;
using PricePredicator.Infrastructure.Data;

// Build host
ThreadPool.SetMinThreads(200, 200);

var builder = WebApplication.CreateBuilder(args);


builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(50051, o =>
    {
        o.Protocols = HttpProtocols.Http2;
    });
});

builder.Services.AddGrpc();

// Add DbContext
builder.Services.AddDbContext<PricePredictorDbContext>(options =>
{
    var connectionString = builder.Configuration["ConnectionStrings:DefaultConnection"] ?? 
        "Server=localhost;Port=5432;Database=pricepredictor;User Id=postgres;Password=postgres;";
    options.UseNpgsql(connectionString);
});

builder.Services.AddSingleton<IOllamaApiClient>(sp => 
{
    var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<GoldNewsSettings>>().Value;
    return new OllamaApiClient(new Uri(settings.OllamaUrl));
});

builder.Services.AddSingleton<IGatewayService, GatewayService>();
builder.Services.AddSingleton<IWeatherService, WeatherService>();

builder.Services.Configure<NtfySettings>(builder.Configuration.GetSection("Ntfy"));
builder.Services.Configure<GoldNewsSettings>(builder.Configuration.GetSection("GoldNews"));
builder.Services.Configure<YahooFinanceSettings>(builder.Configuration.GetSection("YahooFinance"));

builder.Services.AddHttpClient<NtfyClient>((sp, client) =>
{
    var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<NtfySettings>>().Value;
    client.BaseAddress = new Uri(settings.BaseUrl);
});

builder.Services.AddHttpClient<IGoldNewsClient, GoldNewsClient>(client =>
{
    // Some RSS endpoints are fronted by WAF/CDN that returns 404/403 for "unknown" clients.
    // These headers make requests look more like a regular browser.
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0 Safari/537.36");
    client.DefaultRequestHeaders.Accept.ParseAdd(
        "application/rss+xml, application/atom+xml, application/xml;q=0.9, text/xml;q=0.8, */*;q=0.1");
    client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
    client.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true };
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AllowAutoRedirect = true,
    AutomaticDecompression = System.Net.DecompressionMethods.All
});

builder.Services.AddHttpClient<IOpenMeteoClient, OpenMeteoClient>(client =>
{
    client.BaseAddress = new Uri("https://api.open-meteo.com/");
});
builder.Services.AddHttpClient<IGoldPriceService, StooqGoldPriceService>(client =>
{
    client.BaseAddress = new Uri("https://stooq.com/");
});
builder.Services.AddHttpClient<INewsService, GoogleNewsRssService>(client =>
{
    client.BaseAddress = new Uri("https://news.google.com/");
});

// Add Yahoo Finance typed client
builder.Services.AddHttpClient<YahooFinanceClient>(client =>
{
    client.BaseAddress = new Uri("https://query1.finance.yahoo.com/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0 Safari/537.36");
});

// Add repositories and hosted services
builder.Services.AddScoped<IVolatilityRepository, VolatilityRepository>();

// Register TradingIndicatorNotificationService for real-time panic score notifications
builder.Services.AddSingleton(sp =>
{
    var ntfyClient = sp.GetRequiredService<NtfyClient>();
    var weatherService = sp.GetRequiredService<IWeatherService>();
    var ntfySettings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<NtfySettings>>().Value;
    return new TradingIndicatorNotificationService(ntfyClient, weatherService, ntfySettings.Topic);
});

builder.Services.AddHostedService<GoldNewsBackgroundService>();
builder.Services.AddHostedService<YahooFinanceBackgroundService>();

var app = builder.Build();

// Run migrations on startup with retries to handle DB warm-up/race conditions.
var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
var migrationAttempts = 10;

for (var attempt = 1; attempt <= migrationAttempts; attempt++)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PricePredictorDbContext>();
        var pendingMigrations = dbContext.Database.GetPendingMigrations().ToList();

        if (pendingMigrations.Count > 0)
        {
            startupLogger.LogInformation(
                "Applying {Count} pending migration(s): {Migrations}",
                pendingMigrations.Count,
                string.Join(", ", pendingMigrations));

            dbContext.Database.Migrate();
            startupLogger.LogInformation("Database migrations applied successfully.");
        }
        else
        {
            startupLogger.LogInformation("No pending migrations detected.");
        }

        break;
    }
    catch (Exception ex) when (attempt < migrationAttempts)
    {
        startupLogger.LogWarning(
            ex,
            "Migration check/apply attempt {Attempt}/{Total} failed. Retrying in 3 seconds...",
            attempt,
            migrationAttempts);
        await Task.Delay(TimeSpan.FromSeconds(3));
    }
    catch (Exception ex)
    {
        startupLogger.LogCritical(
            ex,
            "Failed to complete migration check/apply after {Total} attempts. App startup aborted.",
            migrationAttempts);
        throw;
    }
}

app.MapGrpcService<GatewayRpcEndpoint>();

app.Run();
