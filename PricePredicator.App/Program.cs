using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OllamaSharp;
using PricePredicator.App;
using PricePredicator.App.Finance;
using PricePredicator.App.Gateway;
using PricePredicator.App.News;
using PricePredicator.App.Weather;
using PricePredicator.Infrastructure;
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

builder.Services.AddNtfyClient(builder.Configuration);
builder.Services.AddGoldNewsClient();
builder.Services.AddOpenMeteoClient();
builder.Services.AddStooqGoldPriceClient();
builder.Services.AddGoogleNewsRssClient(builder.Configuration);
builder.Services.AddYahooFinanceClient();
builder.Services.AddGrpc();

builder.Services.AddDbContextFactory<PricePredictorDbContext>(options =>
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

builder.Services.Configure<NtfySettings>(builder.Configuration.GetSection(NtfySettings.SectionName));
builder.Services.Configure<GoldNewsSettings>(builder.Configuration.GetSection(GoldNewsSettings.SectionName));
builder.Services.Configure<YahooFinanceSettings>(builder.Configuration.GetSection(YahooFinanceSettings.SectionName));
builder.Services.Configure<GoogleNewsRssSettings>(builder.Configuration.GetSection(GoogleNewsRssSettings.SectionName));

builder.Services.AddScoped<IVolatilityRepository, VolatilityRepository>();
builder.Services.AddScoped<IGoldNewsEmbeddingRepository, GoldNewsEmbeddingRepository>();

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

app.Services.ApplyPendingMigrations();

app.MapGrpcService<GatewayRpcEndpoint>();

app.Run();
