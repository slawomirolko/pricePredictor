using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using PricePredicator.App;
using PricePredicator.App.Gateway;
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


builder.Services.Configure<NtfySettings>(builder.Configuration.GetSection(NtfySettings.SectionName));
builder.Services.AddHttpClient<NtfyClient>((sp, client) =>
{
    var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<NtfySettings>>().Value;
    client.BaseAddress = new Uri(settings.BaseUrl);
});
builder.Services.AddHttpClient<IOpenMeteoClient, OpenMeteoClient>(client =>
{
    client.BaseAddress = new Uri("https://api.open-meteo.com/");
});


builder.Services.AddHostedService<NtfyBackgroundService>();

builder.Services.AddSingleton<IGatewayService, GatewayService>();
builder.Services.AddSingleton<IWeatherService, WeatherService>();

builder.Services.AddHostedService<NtfyBackgroundService>();

var app = builder.Build();

app.MapGrpcService<GatewayRpcEndpoint>();

app.Run();