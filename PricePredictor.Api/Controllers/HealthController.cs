using Microsoft.AspNetCore.Mvc;

namespace PricePredictor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Check if the API is alive and running
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new 
        { 
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "PricePredictor.Api"
        });
    }
}


