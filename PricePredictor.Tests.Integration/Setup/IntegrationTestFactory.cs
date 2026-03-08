using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

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
            .UseEnvironment("Test")
            .ConfigureTestAppConfiguration(_connectionString)
            .ConfigureTestServices(_connectionString)
            .ConfigureTestLogging();
    }
}