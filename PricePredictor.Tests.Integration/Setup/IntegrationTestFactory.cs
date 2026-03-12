using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PricePredictor.Infrastructure;

namespace PricePredictor.Tests.Integration.Setup;

public class IntegrationTestFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public IntegrationTestFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder
            .UseEnvironment("Development")
            .ConfigureTestAppConfiguration(_connectionString)
            .ConfigureTestServices(_connectionString)
            .ConfigureTestLogging();
    }
}