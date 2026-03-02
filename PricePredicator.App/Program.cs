using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using OllamaSharp;
using PricePredicator.App;
using PricePredicator.App.Gateway;
using PricePredicator.App.Gold;
using PricePredicator.App.News;
using PricePredicator.App.GoldNews;
using PricePredicator.App.Weather;

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

builder.Services.AddSingleton<IOllamaApiClient>(sp => 
{
    var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<GoldNewsSettings>>().Value;
    return new OllamaApiClient(new Uri(settings.OllamaUrl));
});

builder.Services.AddSingleton<IGatewayService, GatewayService>();
builder.Services.AddSingleton<IWeatherService, WeatherService>();

builder.Services.Configure<NtfySettings>(builder.Configuration.GetSection("Ntfy"));
builder.Services.Configure<GoldNewsSettings>(builder.Configuration.GetSection("GoldNews"));

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


builder.Services.AddHostedService<NtfyBackgroundService>();
builder.Services.AddHostedService<GoldNewsBackgroundService>();

var app = builder.Build();

app.MapGrpcService<GatewayRpcEndpoint>();

app.Run();
