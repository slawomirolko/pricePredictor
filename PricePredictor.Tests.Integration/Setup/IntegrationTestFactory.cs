using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PricePredictor.Infrastructure;

namespace PricePredictor.Tests.Integration.Setup;

public class IntegrationTestFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly bool _useCloud;

    public IntegrationTestFactory(string connectionString, bool useCloud = false)
    {
        _connectionString = connectionString;
        _useCloud = useCloud;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder
            .UseEnvironment("Test")
            .ConfigureTestAppConfiguration(_connectionString)
            .ConfigureTestServices(_connectionString)
            .ConfigureTestLogging();

        builder.ConfigureServices(services =>
        {
            services.PostConfigure<GoldNewsSettings>(options =>
            {
                options.UseCloud = _useCloud;
            });
        });
    }
}