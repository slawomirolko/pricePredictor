using Microsoft.Extensions.Options;
using PricePredictor.Application.Finance.Interfaces;

namespace PricePredictor.Application.Finance;

public sealed class VolatilityExportService : IVolatilityExportService
{
    private readonly IVolatilityRepository _volatilityRepository;
    private readonly ICommodityMarketDataClient _marketDataClient;
    private readonly YahooFinanceSettings _yahooFinanceSettings;

    public VolatilityExportService(
        IVolatilityRepository volatilityRepository,
        ICommodityMarketDataClient marketDataClient,
        IOptions<YahooFinanceSettings> yahooFinanceSettings)
    {
        _volatilityRepository = volatilityRepository;
        _marketDataClient = marketDataClient;
        _yahooFinanceSettings = yahooFinanceSettings.Value;
    }

    public async Task<VolatilityPeriodExportDto> GetPeriodAsync(
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default)
    {
        var goldTask = _volatilityRepository.GetVolatilityForPeriodAsync(VolatilityCommodity.Gold, startUtc, endUtc, cancellationToken);
        var silverTask = _volatilityRepository.GetVolatilityForPeriodAsync(VolatilityCommodity.Silver, startUtc, endUtc, cancellationToken);
        var naturalGasTask = _volatilityRepository.GetVolatilityForPeriodAsync(VolatilityCommodity.NaturalGas, startUtc, endUtc, cancellationToken);
        var oilTask = _volatilityRepository.GetVolatilityForPeriodAsync(VolatilityCommodity.Oil, startUtc, endUtc, cancellationToken);

        await Task.WhenAll(goldTask, silverTask, naturalGasTask, oilTask);

        return new VolatilityPeriodExportDto(
            startUtc,
            endUtc,
            new VolatilityCommodityPeriodDto(
                goldTask.Result.Select(MapRow).ToList(),
                silverTask.Result.Select(MapRow).ToList(),
                naturalGasTask.Result.Select(MapRow).ToList(),
                oilTask.Result.Select(MapRow).ToList()));
    }

    public async Task<VolatilityLatestExportDto> GetNewestAsync(CancellationToken cancellationToken = default)
    {
        var goldTask = GetLatestCommodityAsync(VolatilityCommodity.Gold, cancellationToken);
        var silverTask = GetLatestCommodityAsync(VolatilityCommodity.Silver, cancellationToken);
        var naturalGasTask = GetLatestCommodityAsync(VolatilityCommodity.NaturalGas, cancellationToken);
        var oilTask = GetLatestCommodityAsync(VolatilityCommodity.Oil, cancellationToken);

        await Task.WhenAll(goldTask, silverTask, naturalGasTask, oilTask);

        return new VolatilityLatestExportDto(
            DateTime.UtcNow,
            new VolatilityLatestCommodityDto(
                MapLatest(goldTask.Result),
                MapLatest(silverTask.Result),
                MapLatest(naturalGasTask.Result),
                MapLatest(oilTask.Result)));
    }

    private static VolatilityExportRowDto MapRow(VolatilityPointDto row) => new(
        row.Timestamp,
        row.Open,
        row.High,
        row.Low,
        row.Close,
        row.Volume,
        row.LogReturn,
        row.Vol5,
        row.Vol15,
        row.Vol60,
        row.ShortPanic,
        row.LongPanic);

    private static VolatilityExportRowDto MapLatest(CommodityLatestMarketDataDto row) => new(
        row.Timestamp,
        row.Open,
        row.High,
        row.Low,
        row.Close,
        row.Volume,
        0,
        0,
        0,
        0,
        0,
        0);

    private Task<CommodityLatestMarketDataDto> GetLatestCommodityAsync(
        VolatilityCommodity commodity,
        CancellationToken cancellationToken)
    {
        var symbol = _yahooFinanceSettings.Symbols.FirstOrDefault(x => SymbolMapper.GetTableName(x) == commodity.Name);
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new InvalidOperationException($"YahooFinance symbol mapping is missing for commodity={commodity.Name}.");
        }

        return _marketDataClient.GetLatestAsync(
            symbol,
            _yahooFinanceSettings.Interval,
            _yahooFinanceSettings.Range,
            cancellationToken);
    }
}
