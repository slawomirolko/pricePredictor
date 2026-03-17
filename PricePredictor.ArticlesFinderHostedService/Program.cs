using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PricePredictor.ArticlesFinderHostedService;
using PricePredictor.Application;
using PricePredictor.Persistence;

ThreadPool.SetMinThreads(200, 200);

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddArticlesFinderHostedService(builder.Configuration);

using var host = builder.Build();

host.Services.ApplyPendingMigrations();

await host.RunAsync();
