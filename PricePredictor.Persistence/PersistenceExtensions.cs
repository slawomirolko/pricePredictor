using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PricePredictor.Application.Data;
using PricePredictor.Application.Finance.Interfaces;
using PricePredictor.Application.News;
using PricePredictor.Persistence.Repositories;

namespace PricePredictor.Persistence;

/// <summary>
/// Extension methods for registering Persistence layer services.
/// </summary>
public static class PersistenceExtensions
{
    /// <summary>
    /// Adds Persistence layer services to the dependency injection container.
    /// </summary>
    /// <remarks>
    /// This includes:
    /// - DbContext factory for database operations
    /// - Repository implementations for data access
    /// 
    /// Must be called after AddApplication() and before calling repository services.
    /// </remarks>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext factory
        services.AddDbContextFactory<PricePredictorDbContext>(options =>
        {
            var connectionString = configuration["ConnectionStrings:DefaultConnection"] ??
                                   "Server=localhost;Port=5432;Database=pricepredictor;User Id=postgres;Password=postgres;";
            options.UseNpgsql(connectionString);
        });
        
        services.AddScoped<IVolatilityRepository, VolatilityRepository>();
        services.AddScoped<IGoldNewsEmbeddingRepository, GoldNewsEmbeddingRepository>();
        services.AddScoped<IArticleRepository, ArticleRepository>();
        services.AddScoped<IArticleReaderRepository, ArticleReaderRepository>();

        return services;
    }

    /// <summary>
    /// Applies pending EF Core migrations to the database with retry logic.
    /// </summary>
    /// <remarks>
    /// This method:
    /// - Checks for pending migrations
    /// - Applies them if any exist
    /// - Retries up to 10 times with delays to handle database startup race conditions
    /// 
    /// Should be called after app.Build() in Program.cs.
    /// </remarks>
    public static void ApplyPendingMigrations(this IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Migrations");
        const int maxAttempts = 10;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var scope = services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<PricePredictorDbContext>();
                var pendingMigrations = dbContext.Database.GetPendingMigrations().ToList();

                if (pendingMigrations.Count > 0)
                {
                    logger.LogInformation(
                        "Applying {Count} pending migration(s): {Migrations}",
                        pendingMigrations.Count,
                        string.Join(", ", pendingMigrations));

                    dbContext.Database.Migrate();
                    logger.LogInformation("Migrations applied successfully.");
                }
                else
                {
                    logger.LogInformation("Database is up to date. No pending migrations.");
                }

                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Migration attempt {Attempt}/{MaxAttempts} failed. Retrying in {Delay}s...",
                    attempt, maxAttempts, attempt);

                if (attempt == maxAttempts)
                {
                    logger.LogError(ex, "Failed to apply migrations after {MaxAttempts} attempts.", maxAttempts);
                    throw;
                }

                Thread.Sleep(TimeSpan.FromSeconds(attempt));
            }
        }
    }
}
