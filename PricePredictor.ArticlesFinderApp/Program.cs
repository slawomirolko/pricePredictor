using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PricePredictor.ArticlesFinderApp;
using PricePredictor.Persistence;

ThreadPool.SetMinThreads(200, 200);

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<ArticlesFinderSettings>(
    builder.Configuration.GetSection(ArticlesFinderSettings.SectionName));
builder.Services.AddArticlesFinderApp(builder.Configuration);

using var host = builder.Build();
using var cancellationTokenSource = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationTokenSource.Cancel();
};

host.Services.ApplyPendingMigrations();

await host.StartAsync(cancellationTokenSource.Token);

var app = host.Services.GetRequiredService<ArticlesFinderApp>();
await app.RunAsync(cancellationTokenSource.Token);

await host.StopAsync(CancellationToken.None);
