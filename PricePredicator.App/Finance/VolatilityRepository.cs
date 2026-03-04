using Microsoft.EntityFrameworkCore;
using PricePredicator.Infrastructure.Data;
using PricePredicator.Infrastructure.Models;

namespace PricePredicator.App.Finance;

public class VolatilityRepository : IVolatilityRepository
{
    private readonly IDbContextFactory<PricePredictorDbContext> _dbContextFactory;

    public VolatilityRepository(IDbContextFactory<PricePredictorDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task AddVolatilityGoldAsync(VolatilityGold entity, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        if (await dbContext.VolatilityGold.AnyAsync(x => x.Timestamp == entity.Timestamp, cancellationToken))
            return;

        await dbContext.VolatilityGold.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddVolatilitySilverAsync(VolatilitySilver entity, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        if (await dbContext.VolatilitySilver.AnyAsync(x => x.Timestamp == entity.Timestamp, cancellationToken))
            return;

        await dbContext.VolatilitySilver.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddVolatilityNaturalGasAsync(VolatilityNaturalGas entity, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        if (await dbContext.VolatilityNaturalGas.AnyAsync(x => x.Timestamp == entity.Timestamp, cancellationToken))
            return;

        await dbContext.VolatilityNaturalGas.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddVolatilityOilAsync(VolatilityOil entity, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        if (await dbContext.VolatilityOil.AnyAsync(x => x.Timestamp == entity.Timestamp, cancellationToken))
            return;

        await dbContext.VolatilityOil.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<VolatilityGold>> GetGoldLastAsync(int minutes, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddMinutes(-minutes);
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.VolatilityGold
            .Where(x => x.Timestamp >= since)
            .OrderBy(x => x.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<VolatilitySilver>> GetSilverLastAsync(int minutes, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddMinutes(-minutes);
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.VolatilitySilver
            .Where(x => x.Timestamp >= since)
            .OrderBy(x => x.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<VolatilityNaturalGas>> GetNaturalGasLastAsync(int minutes, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddMinutes(-minutes);
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.VolatilityNaturalGas
            .Where(x => x.Timestamp >= since)
            .OrderBy(x => x.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<VolatilityOil>> GetOilLastAsync(int minutes, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddMinutes(-minutes);
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.VolatilityOil
            .Where(x => x.Timestamp >= since)
            .OrderBy(x => x.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task UpsertDailyAsync(string commodityName, VolatilityDaily entity, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var commodity = await dbContext.Commodities
            .FirstOrDefaultAsync(x => x.Name == commodityName, cancellationToken);

        if (commodity == null)
        {
            commodity = new Commodity { Name = commodityName };
            await dbContext.Commodities.AddAsync(commodity, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var existing = await dbContext.Volatilities
            .FirstOrDefaultAsync(x => x.CommodityId == commodity.Id && x.Day == entity.Day, cancellationToken);

        if (existing == null)
        {
            if (entity.Id == Guid.Empty)
                entity.Id = Guid.CreateVersion7();

            entity.CommodityId = commodity.Id;
            await dbContext.Volatilities.AddAsync(entity, cancellationToken);
        }
        else
        {
            existing.Open = entity.Open;
            existing.Close = entity.Close;
            existing.High = entity.High;
            existing.Low = entity.Low;
            existing.Avg = entity.Avg;
            existing.VolumeSum = entity.VolumeSum;
            existing.RangePct = entity.RangePct;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
