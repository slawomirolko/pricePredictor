using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PricePredictor.Application.Finance;
using PricePredictor.Application.Finance.Interfaces;
using PricePredictor.Persistence.Data;
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

        return services;
    }
}

