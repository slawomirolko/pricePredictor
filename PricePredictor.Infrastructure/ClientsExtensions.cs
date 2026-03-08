using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using PricePredictor.Infrastructure.Finance;
using PricePredictor.Infrastructure.Gold;
using PricePredictor.Infrastructure.GoldNews;
using PricePredictor.Infrastructure.News;
using PricePredictor.Infrastructure.Weather;

namespace PricePredictor.Infrastructure;

public static class ClientsExtensions
{
    private static IAsyncPolicy<HttpResponseMessage> CreateSharedHttpRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(response => response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) +
                    TimeSpan.FromMilliseconds(Random.Shared.Next(100, 400))
            );
    }

    public static IServiceCollection AddNtfyClient(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var retryPolicy = CreateSharedHttpRetryPolicy();

        services.AddHttpClient<NtfyClient>((sp, client) =>
            {
                var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<NtfySettings>>().Value;
                client.BaseAddress = new Uri(settings.BaseUrl);
            })
            .AddPolicyHandler(retryPolicy);

        return services;
    }

    public static IServiceCollection AddGoldNewsClient(this IServiceCollection services)
    {
        var retryPolicy = CreateSharedHttpRetryPolicy();

        services.AddHttpClient<IGoldNewsClient, GoldNewsClient>(client =>
            {
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
                client.DefaultRequestHeaders.Add("Cookie", "consent=accepted; gdpr=accepted; cookies=accepted; cookieConsent=yes");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = System.Net.DecompressionMethods.All,
                UseCookies = true,
                CookieContainer = new System.Net.CookieContainer()
            })
            .AddPolicyHandler(retryPolicy);

        return services;
    }

    public static IServiceCollection AddOpenMeteoClient(this IServiceCollection services)
    {
        var retryPolicy = CreateSharedHttpRetryPolicy();

        services.AddHttpClient<IOpenMeteoClient, OpenMeteoClient>(client =>
            {
                client.BaseAddress = new Uri("https://api.open-meteo.com/");
            })
            .AddPolicyHandler(retryPolicy);

        return services;
    }

    public static IServiceCollection AddStooqGoldPriceClient(this IServiceCollection services)
    {
        var retryPolicy = CreateSharedHttpRetryPolicy();

        services.AddHttpClient<IGoldPriceService, StooqGoldPriceService>(client =>
            {
                client.BaseAddress = new Uri("https://stooq.com/");
            })
            .AddPolicyHandler(retryPolicy);

        return services;
    }

    public static IServiceCollection AddGoogleNewsRssClient(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var retryPolicy = CreateSharedHttpRetryPolicy();

        services.AddHttpClient<IGoogleNewsRssClient, GoogleNewsRssClient>((sp, client) =>
            {
                var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<GoogleNewsRssSettings>>().Value;
                client.BaseAddress = new Uri(settings.BaseUrl);
            })
            .AddPolicyHandler(retryPolicy);

        return services;
    }

    public static IServiceCollection AddYahooFinanceClient(this IServiceCollection services)
    {
        var retryPolicy = CreateSharedHttpRetryPolicy();

        services.AddHttpClient<YahooFinanceClient>(client =>
            {
                client.BaseAddress = new Uri("https://query1.finance.yahoo.com/");
                client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0 Safari/537.36");
            })
            .AddPolicyHandler(retryPolicy);

        return services;
    }
}
