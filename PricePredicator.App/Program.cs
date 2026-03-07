using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OllamaSharp;
using Polly;
using Polly.Extensions.Http;
using PricePredicator.App;
using PricePredicator.App.Finance;
using PricePredicator.App.Gateway;
using PricePredicator.App.Gold;
using PricePredicator.App.News;
using PricePredicator.App.GoldNews;
using PricePredicator.App.Weather;
using PricePredicator.Infrastructure;
using PricePredicator.Infrastructure.Data;

// Build host
ThreadPool.SetMinThreads(200, 200);

var builder = WebApplication.CreateBuilder(args);

var sharedHttpRetryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .OrResult(response => response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: retryAttempt =>
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) +
            TimeSpan.FromMilliseconds(Random.Shared.Next(100, 400))
    );


builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(50051, o =>
    {
        o.Protocols = HttpProtocols.Http2;
    });
});

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

builder.Services.Configure<NtfySettings>(builder.Configuration.GetSection("Ntfy"));
builder.Services.Configure<GoldNewsSettings>(builder.Configuration.GetSection("GoldNews"));
builder.Services.Configure<YahooFinanceSettings>(builder.Configuration.GetSection("YahooFinance"));

builder.Services.AddHttpClient<NtfyClient>((sp, client) =>
{
    var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<NtfySettings>>().Value;
    client.BaseAddress = new Uri(settings.BaseUrl);
})
.AddPolicyHandler(sharedHttpRetryPolicy);

builder.Services.AddHttpClient<IGoldNewsClient, GoldNewsClient>(client =>
{
    // Simulate a real Chrome browser to get past WAF/CDN and cookie consent
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    client.DefaultRequestHeaders.Accept.ParseAdd(
        "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8");
    client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
    client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate, br");
    client.DefaultRequestHeaders.Add("Referer", "https://www.google.com/");
    client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
    client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
    client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
    client.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
    client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
    client.DefaultRequestHeaders.Add("DNT", "1");
    client.DefaultRequestHeaders.Add("Connection", "keep-alive");
    // Pre-accept cookies in headers
    client.DefaultRequestHeaders.Add("Cookie", "consent=accepted; gdpr=accepted; cookies=accepted; cookieConsent=yes");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AllowAutoRedirect = true,
    AutomaticDecompression = System.Net.DecompressionMethods.All,
    UseCookies = true,
    CookieContainer = new System.Net.CookieContainer()
})
.AddPolicyHandler(sharedHttpRetryPolicy);

builder.Services.AddHttpClient<IOpenMeteoClient, OpenMeteoClient>(client =>
{
    client.BaseAddress = new Uri("https://api.open-meteo.com/");
})
.AddPolicyHandler(sharedHttpRetryPolicy);
builder.Services.AddHttpClient<IGoldPriceService, StooqGoldPriceService>(client =>
{
    client.BaseAddress = new Uri("https://stooq.com/");
})
.AddPolicyHandler(sharedHttpRetryPolicy);
builder.Services.AddHttpClient<INewsService, GoogleNewsRssService>(client =>
{
    client.BaseAddress = new Uri("https://news.google.com/");
})
.AddPolicyHandler(sharedHttpRetryPolicy);

// Add Yahoo Finance typed client
builder.Services.AddHttpClient<YahooFinanceClient>(client =>
{
    client.BaseAddress = new Uri("https://query1.finance.yahoo.com/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0 Safari/537.36");
})
.AddPolicyHandler(sharedHttpRetryPolicy);

// Add repositories and hosted services
builder.Services.AddScoped<IVolatilityRepository, VolatilityRepository>();
builder.Services.AddScoped<IGoldNewsEmbeddingRepository, GoldNewsEmbeddingRepository>();

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

app.Services.ApplyPendingMigrations();

app.MapGrpcService<GatewayRpcEndpoint>();

app.Run();
