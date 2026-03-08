using Microsoft.EntityFrameworkCore;
using PricePredictor.Application.Models;

namespace PricePredictor.Persistence;

public class PricePredictorDbContext : DbContext
{
    public PricePredictorDbContext(DbContextOptions<PricePredictorDbContext> options) : base(options)
    {
    }

    public DbSet<VolatilityGold> VolatilityGold { get; set; } = null!;
    public DbSet<VolatilitySilver> VolatilitySilver { get; set; } = null!;
    public DbSet<VolatilityNaturalGas> VolatilityNaturalGas { get; set; } = null!;
    public DbSet<VolatilityOil> VolatilityOil { get; set; } = null!;
    public DbSet<Commodity> Commodities { get; set; } = null!;
    public DbSet<VolatilityDaily> Volatilities { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure VolatilityGold
        modelBuilder.Entity<VolatilityGold>(entity =>
        {
            entity.ToTable("Volatility_Gold");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.Open).HasPrecision(18, 8);
            entity.Property(e => e.High).HasPrecision(18, 8);
            entity.Property(e => e.Low).HasPrecision(18, 8);
            entity.Property(e => e.Close).HasPrecision(18, 8);
            entity.HasIndex(e => e.Timestamp).IsUnique(true);
            entity.HasOne(e => e.Commodity)
                .WithMany()
                .HasForeignKey(e => e.CommodityId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.CommodityId);
        });

        // Configure VolatilitySilver
        modelBuilder.Entity<VolatilitySilver>(entity =>
        {
            entity.ToTable("Volatility_Silver");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.Open).HasPrecision(18, 8);
            entity.Property(e => e.High).HasPrecision(18, 8);
            entity.Property(e => e.Low).HasPrecision(18, 8);
            entity.Property(e => e.Close).HasPrecision(18, 8);
            entity.HasIndex(e => e.Timestamp).IsUnique(true);
            entity.HasOne(e => e.Commodity)
                .WithMany()
                .HasForeignKey(e => e.CommodityId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.CommodityId);
        });

        // Configure VolatilityNaturalGas
        modelBuilder.Entity<VolatilityNaturalGas>(entity =>
        {
            entity.ToTable("Volatility_NaturalGas");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.Open).HasPrecision(18, 8);
            entity.Property(e => e.High).HasPrecision(18, 8);
            entity.Property(e => e.Low).HasPrecision(18, 8);
            entity.Property(e => e.Close).HasPrecision(18, 8);
            entity.HasIndex(e => e.Timestamp).IsUnique(true);
            entity.HasOne(e => e.Commodity)
                .WithMany()
                .HasForeignKey(e => e.CommodityId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.CommodityId);
        });

        // Configure VolatilityOil
        modelBuilder.Entity<VolatilityOil>(entity =>
        {
            entity.ToTable("Volatility_Oil");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.Open).HasPrecision(18, 8);
            entity.Property(e => e.High).HasPrecision(18, 8);
            entity.Property(e => e.Low).HasPrecision(18, 8);
            entity.Property(e => e.Close).HasPrecision(18, 8);
            entity.HasIndex(e => e.Timestamp).IsUnique(true);
            entity.HasOne(e => e.Commodity)
                .WithMany()
                .HasForeignKey(e => e.CommodityId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.CommodityId);
        });

        modelBuilder.Entity<Commodity>(entity =>
        {
            entity.ToTable("Commodities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Configure VolatilityDaily
        modelBuilder.Entity<VolatilityDaily>(entity =>
        {
            entity.ToTable("Volatilities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Day).HasColumnType("date");
            entity.Property(e => e.Open).HasPrecision(18, 8);
            entity.Property(e => e.Close).HasPrecision(18, 8);
            entity.Property(e => e.High).HasPrecision(18, 8);
            entity.Property(e => e.Low).HasPrecision(18, 8);
            entity.Property(e => e.Avg).HasPrecision(18, 8);
            entity.Property(e => e.RangePct).HasPrecision(18, 8);
            entity.HasOne(e => e.Commodity)
                .WithMany(nameof(Commodity.DailyVolatilities))
                .HasForeignKey(e => e.CommodityId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.CommodityId, e.Day }).IsUnique();
        });
    }
}

