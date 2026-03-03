using Microsoft.EntityFrameworkCore;
using PricePredicator.Infrastructure.Data;
using PricePredicator.Infrastructure.Models;

namespace PricePredicator.App.Finance;

public class VolatilityRepository : IVolatilityRepository
{
    private readonly PricePredictorDbContext _dbContext;

    public VolatilityRepository(PricePredictorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddVolatilityGoldAsync(VolatilityGold entity, CancellationToken cancellationToken = default)
    {
        if (await _dbContext.VolatilityGold.AnyAsync(x => x.Timestamp == entity.Timestamp, cancellationToken))
            return;

        await _dbContext.VolatilityGold.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddVolatilitySilverAsync(VolatilitySilver entity, CancellationToken cancellationToken = default)
    {
        if (await _dbContext.VolatilitySilver.AnyAsync(x => x.Timestamp == entity.Timestamp, cancellationToken))
            return;

        await _dbContext.VolatilitySilver.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddVolatilityNaturalGasAsync(VolatilityNaturalGas entity, CancellationToken cancellationToken = default)
    {
        if (await _dbContext.VolatilityNaturalGas.AnyAsync(x => x.Timestamp == entity.Timestamp, cancellationToken))
            return;

        await _dbContext.VolatilityNaturalGas.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddVolatilityOilAsync(VolatilityOil entity, CancellationToken cancellationToken = default)
    {
        if (await _dbContext.VolatilityOil.AnyAsync(x => x.Timestamp == entity.Timestamp, cancellationToken))
            return;

        await _dbContext.VolatilityOil.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<VolatilityGold>> GetGoldLastAsync(int minutes, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddMinutes(-minutes);
        return await _dbContext.VolatilityGold
            .Where(x => x.Timestamp >= since)
            .OrderBy(x => x.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<VolatilitySilver>> GetSilverLastAsync(int minutes, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddMinutes(-minutes);
        return await _dbContext.VolatilitySilver
            .Where(x => x.Timestamp >= since)
            .OrderBy(x => x.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<VolatilityNaturalGas>> GetNaturalGasLastAsync(int minutes, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddMinutes(-minutes);
        return await _dbContext.VolatilityNaturalGas
            .Where(x => x.Timestamp >= since)
            .OrderBy(x => x.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<VolatilityOil>> GetOilLastAsync(int minutes, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddMinutes(-minutes);
        return await _dbContext.VolatilityOil
            .Where(x => x.Timestamp >= since)
            .OrderBy(x => x.Timestamp)
            .ToListAsync(cancellationToken);
    }
}

