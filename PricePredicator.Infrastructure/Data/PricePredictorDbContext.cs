using Microsoft.EntityFrameworkCore;
using PricePredicator.Infrastructure.Models;

namespace PricePredicator.Infrastructure.Data;

public class PricePredictorDbContext : DbContext
{
    public PricePredictorDbContext(DbContextOptions<PricePredictorDbContext> options) : base(options)
    {
    }

    public DbSet<VolatilityGold> VolatilityGold { get; set; } = null!;
    public DbSet<VolatilitySilver> VolatilitySilver { get; set; } = null!;
    public DbSet<VolatilityNaturalGas> VolatilityNaturalGas { get; set; } = null!;
    public DbSet<VolatilityOil> VolatilityOil { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure VolatilityGold
        modelBuilder.Entity<VolatilityGold>(entity =>
        {
            entity.ToTable("Gold");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.Open).HasPrecision(18, 8);
            entity.Property(e => e.High).HasPrecision(18, 8);
            entity.Property(e => e.Low).HasPrecision(18, 8);
            entity.Property(e => e.Close).HasPrecision(18, 8);
            entity.HasIndex(e => e.Timestamp).IsUnique(true);
        });

        // Configure VolatilitySilver
        modelBuilder.Entity<VolatilitySilver>(entity =>
        {
            entity.ToTable("Silver");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.Open).HasPrecision(18, 8);
            entity.Property(e => e.High).HasPrecision(18, 8);
            entity.Property(e => e.Low).HasPrecision(18, 8);
            entity.Property(e => e.Close).HasPrecision(18, 8);
            entity.HasIndex(e => e.Timestamp).IsUnique(true);
        });

        // Configure VolatilityNaturalGas
        modelBuilder.Entity<VolatilityNaturalGas>(entity =>
        {
            entity.ToTable("NaturalGas");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.Open).HasPrecision(18, 8);
            entity.Property(e => e.High).HasPrecision(18, 8);
            entity.Property(e => e.Low).HasPrecision(18, 8);
            entity.Property(e => e.Close).HasPrecision(18, 8);
            entity.HasIndex(e => e.Timestamp).IsUnique(true);
        });

        // Configure VolatilityOil
        modelBuilder.Entity<VolatilityOil>(entity =>
        {
            entity.ToTable("Oil");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.Open).HasPrecision(18, 8);
            entity.Property(e => e.High).HasPrecision(18, 8);
            entity.Property(e => e.Low).HasPrecision(18, 8);
            entity.Property(e => e.Close).HasPrecision(18, 8);
            entity.HasIndex(e => e.Timestamp).IsUnique(true);
        });
    }
}
