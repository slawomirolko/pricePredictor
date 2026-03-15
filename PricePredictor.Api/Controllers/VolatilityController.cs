using Microsoft.AspNetCore.Mvc;
using PricePredictor.Application.Finance;
using PricePredictor.Application.Finance.Interfaces;

namespace PricePredictor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VolatilityController : ControllerBase
{
    private readonly IVolatilityRepository _volatilityRepository;

    public VolatilityController(IVolatilityRepository volatilityRepository)
    {
        _volatilityRepository = volatilityRepository;
    }

    /// <summary>
    /// Get volatility data for a specific commodity within a time range
    /// </summary>
    /// <param name="commodity">Commodity type (Gold, Silver, NaturalGas, Oil)</param>
    /// <param name="startUtc">Start time (UTC)</param>
    /// <param name="endUtc">End time (UTC)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of volatility data points</returns>
    [HttpGet("{commodity}")]
    public async Task<ActionResult<IReadOnlyList<VolatilityPointDto>>> GetVolatility(
        VolatilityCommodity commodity,
        [FromQuery] DateTime startUtc,
        [FromQuery] DateTime endUtc,
        CancellationToken cancellationToken)
    {
        if (startUtc >= endUtc)
        {
            return BadRequest("startUtc must be before endUtc");
        }

        var points = await _volatilityRepository.GetVolatilityForPeriodAsync(
            commodity, 
            startUtc, 
            endUtc, 
            cancellationToken);

        return Ok(points);
    }

    /// <summary>
    /// Get latest volatility data point for a specific commodity
    /// </summary>
    /// <param name="commodity">Commodity type (Gold, Silver, NaturalGas, Oil)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Latest volatility data point</returns>
    [HttpGet("{commodity}/latest")]
    public async Task<ActionResult<VolatilityPointDto>> GetLatest(
        VolatilityCommodity commodity,
        CancellationToken cancellationToken)
    {
        var endUtc = DateTime.UtcNow;
        var startUtc = endUtc.AddMinutes(-5);

        var points = await _volatilityRepository.GetVolatilityForPeriodAsync(
            commodity, 
            startUtc, 
            endUtc, 
            cancellationToken);

        var latest = points.OrderByDescending(p => p.Timestamp).FirstOrDefault();

        if (latest == null)
        {
            return NotFound($"No data found for {commodity}");
        }

        return Ok(latest);
    }
}
