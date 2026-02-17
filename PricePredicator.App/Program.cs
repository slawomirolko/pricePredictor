using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PricePredicator.App;

// Build host
var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<NtfySettings>(builder.Configuration.GetSection(NtfySettings.SectionName));

builder.Services.AddHttpClient<NtfyClient>((sp, client) =>
{
    var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<NtfySettings>>().Value;
    client.BaseAddress = new Uri(settings.BaseUrl);
});

builder.Services.AddHostedService<NtfyBackgroundService>();

await builder.Build().RunAsync();