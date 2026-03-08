using Microsoft.AspNetCore.Server.Kestrel.Core;
using OllamaSharp;
using PricePredictor.Api.BackgroundServices;
using PricePredictor.Api.Gateway;
using PricePredictor.Application;
using PricePredictor.Application.Finance;
using PricePredictor.Application.Notifications;
using PricePredictor.Application.Weather;
using PricePredictor.Infrastructure;
using PricePredictor.Infrastructure.News;
using PricePredictor.Persistence;
using GoldNewsSettings = PricePredictor.Infrastructure.GoldNewsSettings;

// Build host
ThreadPool.SetMinThreads(200, 200);

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(50051, o =>
    {
        o.Protocols = HttpProtocols.Http2;
    });
    
    options.ListenLocalhost(5000, o =>
    {
        o.Protocols = HttpProtocols.Http1;
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddGrpc();

// Infrastructure: External adapters and clients
builder.Services.AddNtfyClient(builder.Configuration);
builder.Services.AddGoldNewsClient();
builder.Services.AddOpenMeteoClient();
builder.Services.AddStooqGoldPriceClient();
builder.Services.AddGoogleNewsRssClient(builder.Configuration);
builder.Services.AddYahooFinanceClient();

// Persistence: Database and repository setup
builder.Services.AddPersistence(builder.Configuration);

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


builder.Services.AddSingleton(sp =>
{
    var ntfyClient = sp.GetRequiredService<INtfyClient>();
    var weatherService = sp.GetRequiredService<IWeatherService>();
    var ntfySettings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<NtfySettings>>().Value;
    return new TradingIndicatorNotificationService(ntfyClient, weatherService, ntfySettings.Topic);
});

builder.Services.AddHostedService<GoldNewsBackgroundService>();
builder.Services.AddHostedService<YahooFinanceBackgroundService>();
var app = builder.Build();

app.Services.ApplyPendingMigrations();

// Configure Swagger (Development & Production for API exploration)
app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();
app.MapControllers();
app.MapGrpcService<GatewayRpcEndpoint>();

app.Run();

