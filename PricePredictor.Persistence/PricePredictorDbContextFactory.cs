using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PricePredictor.Persistence;

public class PricePredictorDbContextFactory : IDesignTimeDbContextFactory<PricePredictorDbContext>
{
    public PricePredictorDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PricePredictorDbContext>();
        var connectionString = ResolveConnectionString();

        optionsBuilder.UseNpgsql(connectionString);

        return new PricePredictorDbContext(optionsBuilder.Options);
    }

    private static string ResolveConnectionString()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable(
            $"{PersistenceDefaultConnectionSettings.SectionName}__ConnectionString");

        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        var fromAppSettings = TryReadConnectionStringFromAppSettings();

        if (!string.IsNullOrWhiteSpace(fromAppSettings))
        {
            return fromAppSettings;
        }

        throw new InvalidOperationException(
            $"Missing required configuration '{PersistenceDefaultConnectionSettings.SectionName}:ConnectionString' for design-time DbContext creation.");
    }

    private static string? TryReadConnectionStringFromAppSettings()
    {
        foreach (var path in GetCandidateAppSettingsPaths())
        {
            if (!File.Exists(path))
            {
                continue;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(path));
            if (!document.RootElement.TryGetProperty(PersistenceDefaultConnectionSettings.SectionName, out var section))
            {
                continue;
            }

            if (!section.TryGetProperty(nameof(PersistenceDefaultConnectionSettings.ConnectionString), out var value))
            {
                continue;
            }

            var connectionString = value.GetString();
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                return connectionString;
            }
        }

        return null;
    }

    private static IEnumerable<string> GetCandidateAppSettingsPaths()
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        yield return Path.Combine(currentDirectory, "appsettings.Development.json");
        yield return Path.Combine(currentDirectory, "appsettings.json");
        yield return Path.Combine(currentDirectory, "PricePredictor.Api", "appsettings.Development.json");
        yield return Path.Combine(currentDirectory, "PricePredictor.Api", "appsettings.json");
    }
}
