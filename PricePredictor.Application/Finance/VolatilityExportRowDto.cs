namespace PricePredictor.Application.Finance;

public sealed record VolatilityExportRowDto(
    DateTime Timestamp,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long? Volume,
    double LogReturn,
    double Vol5,
    double Vol15,
    double Vol60,
    double ShortPanic,
    double LongPanic);
