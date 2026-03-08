using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PricePredictor.Persistence.Data;

public class PricePredictorDbContextFactory : IDesignTimeDbContextFactory<PricePredictorDbContext>
{
    public PricePredictorDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PricePredictorDbContext>();
        
        // Use a default connection string for design time
        var connectionString = "Server=localhost;Port=5432;Database=pricepredictor;User Id=postgres;Password=postgres;";
        optionsBuilder.UseNpgsql(connectionString);

        return new PricePredictorDbContext(optionsBuilder.Options);
    }
}

