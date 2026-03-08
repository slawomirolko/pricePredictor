﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OllamaSharp;
using PricePredictor.Api;
using PricePredictor.Application.Finance;
using PricePredictor.Api.Gateway;
using PricePredictor.Api.Weather;
using PricePredictor.Infrastructure;
using PricePredictor.Infrastructure.Data;
using PricePredictor.Infrastructure.News;

// Build host
ThreadPool.SetMinThreads(200, 200);

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    // gRPC endpoint (HTTP/2)
    options.ListenLocalhost(50051, o =>
    {
        o.Protocols = HttpProtocols.Http2;
    });
    
    // REST API endpoint (HTTP/1.1)
    options.ListenLocalhost(5000, o =>
    {
        o.Protocols = HttpProtocols.Http1;
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddGrpc();

builder.Services.AddNtfyClient(builder.Configuration);
builder.Services.AddGoldNewsClient();
builder.Services.AddOpenMeteoClient();
builder.Services.AddStooqGoldPriceClient();
builder.Services.AddGoogleNewsRssClient(builder.Configuration);
builder.Services.AddYahooFinanceClient();

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

// Configure Swagger (Development & Production for API exploration)
app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();
app.MapControllers();
app.MapGrpcService<GatewayRpcEndpoint>();

app.Run();

