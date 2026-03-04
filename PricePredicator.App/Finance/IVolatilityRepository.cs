using PricePredicator.Infrastructure.Models;

namespace PricePredicator.App.Finance;

/// <summary>
/// Repository for storing volatility data
/// </summary>
public interface IVolatilityRepository
{
    Task AddVolatilityGoldAsync(VolatilityGold entity, CancellationToken cancellationToken = default);
    Task AddVolatilitySilverAsync(VolatilitySilver entity, CancellationToken cancellationToken = default);
    Task AddVolatilityNaturalGasAsync(VolatilityNaturalGas entity, CancellationToken cancellationToken = default);
    Task AddVolatilityOilAsync(VolatilityOil entity, CancellationToken cancellationToken = default);

    Task<List<VolatilityGold>> GetGoldLastAsync(int minutes, CancellationToken cancellationToken = default);
    Task<List<VolatilitySilver>> GetSilverLastAsync(int minutes, CancellationToken cancellationToken = default);
    Task<List<VolatilityNaturalGas>> GetNaturalGasLastAsync(int minutes, CancellationToken cancellationToken = default);
    Task<List<VolatilityOil>> GetOilLastAsync(int minutes, CancellationToken cancellationToken = default);
    Task UpsertDailyAsync(string commodityName, VolatilityDaily entity, CancellationToken cancellationToken = default);
}
