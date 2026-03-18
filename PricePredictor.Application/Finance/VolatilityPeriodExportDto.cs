namespace PricePredictor.Application.Finance;

public sealed record VolatilityCommodityPeriodDto(
    IReadOnlyList<VolatilityExportRowDto> Gold,
    IReadOnlyList<VolatilityExportRowDto> Silver,
    IReadOnlyList<VolatilityExportRowDto> NaturalGas,
    IReadOnlyList<VolatilityExportRowDto> Oil);

public sealed record VolatilityPeriodExportDto(
    DateTime StartUtc,
    DateTime EndUtc,
    VolatilityCommodityPeriodDto Commodity);
