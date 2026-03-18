namespace PricePredictor.Application.Finance;

public sealed record VolatilityLatestCommodityDto(
    VolatilityExportRowDto Gold,
    VolatilityExportRowDto Silver,
    VolatilityExportRowDto NaturalGas,
    VolatilityExportRowDto Oil);

public sealed record VolatilityLatestExportDto(
    DateTime RetrievedAtUtc,
    VolatilityLatestCommodityDto Commodity);
