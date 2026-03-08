﻿using Microsoft.EntityFrameworkCore;
using PricePredictor.Infrastructure.Data;
using PricePredictor.Domain.Models;

namespace PricePredictor.Application.Finance;

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

    public async Task<List<VolatilityPointDto>> GetVolatilityForPeriodAsync(VolatilityCommodity commodity, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return commodity.Id switch
        {
            1 => await GetGoldPointsAsync(dbContext, startUtc, endUtc, cancellationToken),
            2 => await GetSilverPointsAsync(dbContext, startUtc, endUtc, cancellationToken),
            3 => await GetNaturalGasPointsAsync(dbContext, startUtc, endUtc, cancellationToken),
            4 => await GetOilPointsAsync(dbContext, startUtc, endUtc, cancellationToken),
            _ => new List<VolatilityPointDto>()
        };
    }

    private static async Task<List<VolatilityPointDto>> GetGoldPointsAsync(PricePredictorDbContext dbContext, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken)
    {
        var rows = await dbContext.VolatilityGold
            .Where(x => x.Timestamp >= startUtc && x.Timestamp <= endUtc)
            .OrderBy(x => x.Timestamp)
            .ToListAsync(cancellationToken);
        return rows.Select(MapPoint).ToList();
    }

    private static async Task<List<VolatilityPointDto>> GetSilverPointsAsync(PricePredictorDbContext dbContext, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken)
    {
        var rows = await dbContext.VolatilitySilver
            .Where(x => x.Timestamp >= startUtc && x.Timestamp <= endUtc)
            .OrderBy(x => x.Timestamp)
            .ToListAsync(cancellationToken);
        return rows.Select(MapPoint).ToList();
    }

    private static async Task<List<VolatilityPointDto>> GetNaturalGasPointsAsync(PricePredictorDbContext dbContext, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken)
    {
        var rows = await dbContext.VolatilityNaturalGas
            .Where(x => x.Timestamp >= startUtc && x.Timestamp <= endUtc)
            .OrderBy(x => x.Timestamp)
            .ToListAsync(cancellationToken);
        return rows.Select(MapPoint).ToList();
    }

    private static async Task<List<VolatilityPointDto>> GetOilPointsAsync(PricePredictorDbContext dbContext, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken)
    {
        var rows = await dbContext.VolatilityOil
            .Where(x => x.Timestamp >= startUtc && x.Timestamp <= endUtc)
            .OrderBy(x => x.Timestamp)
            .ToListAsync(cancellationToken);
        return rows.Select(MapPoint).ToList();
    }

    private static VolatilityPointDto MapPoint(VolatilityBase row) => new(
        row.Timestamp,
        row.Open,
        row.High,
        row.Low,
        row.Close,
        row.Volume ?? 0,
        row.LogarithmicReturn,
        row.RollingVol5,
        row.RollingVol15,
        row.RollingVol60,
        row.ShortPanicScore,
        row.LongPanicScore);


    public async Task UpsertDailyAsync(VolatilityCommodity commodity, VolatilityDaily entity, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var commodityEntity = await dbContext.Commodities
            .FirstOrDefaultAsync(x => x.Name == commodity.Name, cancellationToken);

        if (commodityEntity == null)
        {
            commodityEntity = new Commodity { Name = commodity.Name };
            await dbContext.Commodities.AddAsync(commodityEntity, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var existing = await dbContext.Volatilities
            .FirstOrDefaultAsync(x => x.CommodityId == commodityEntity.Id && x.Day == entity.Day, cancellationToken);

        if (existing == null)
        {
            if (entity.Id == Guid.Empty)
                entity.Id = Guid.CreateVersion7();

            entity.CommodityId = commodityEntity.Id;
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

