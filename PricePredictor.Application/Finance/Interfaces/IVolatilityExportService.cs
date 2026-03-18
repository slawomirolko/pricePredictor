namespace PricePredictor.Application.Finance.Interfaces;

public interface IVolatilityExportService
{
    Task<VolatilityPeriodExportDto> GetPeriodAsync(DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default);
    Task<VolatilityLatestExportDto> GetNewestAsync(CancellationToken cancellationToken = default);
}
