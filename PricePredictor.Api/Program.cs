using Microsoft.AspNetCore.Server.Kestrel.Core;
using PricePredictor.Api.Gateway;
using PricePredictor.Application;
using PricePredictor.Application.Finance;
using PricePredictor.Infrastructure;
using PricePredictor.Persistence;
using GoldNewsSettings = PricePredictor.Infrastructure.GoldNewsSettings;
using GoldNewsSettingsApp = PricePredictor.Application.Data.GoldNewsSettings;

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

builder.Services.AddApplication();

// Infrastructure: External adapters and clients
builder.Services.AddOllamaArticleExtractionClient();
builder.Services.AddGoldNewsClient();
builder.Services.AddStooqGoldPriceClient();
builder.Services.AddYahooFinanceClient();

// Persistence: Database and repository setup
builder.Services.AddPersistence(builder.Configuration);

builder.Services.Configure<GoldNewsSettings>(builder.Configuration.GetSection(GoldNewsSettings.SectionName));
builder.Services.Configure<GoldNewsSettingsApp>(builder.Configuration.GetSection(GoldNewsSettingsApp.SectionName));
builder.Services.Configure<YahooFinanceSettings>(builder.Configuration.GetSection(YahooFinanceSettings.SectionName));
var app = builder.Build();

app.Services.ApplyPendingMigrations();

// Configure Swagger (Development & Production for API exploration)
app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();
app.MapControllers();
app.MapGrpcService<GatewayRpcEndpoint>();

app.Run();
