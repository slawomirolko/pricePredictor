using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PricePredictor.Infrastructure.Data;

namespace PricePredictor.Infrastructure;

public static class InfrastructureMigrationExtensions
{
    public static void ApplyPendingMigrations(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Startup");
        var dbContext = scope.ServiceProvider.GetRequiredService<PricePredictorDbContext>();
        var pendingMigrations = dbContext.Database.GetPendingMigrations().ToList();

        if (pendingMigrations.Count == 0)
        {
            logger.LogInformation("No pending migrations detected.");
            return;
        }

        logger.LogInformation(
            "Applying {Count} pending migration(s): {Migrations}",
            pendingMigrations.Count,
            string.Join(", ", pendingMigrations));

        dbContext.Database.Migrate();
        logger.LogInformation("Database migrations applied successfully.");
    }
}

