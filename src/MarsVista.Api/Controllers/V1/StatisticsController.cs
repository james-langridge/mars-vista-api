using MarsVista.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarsVista.Api.Controllers.V1;

[ApiController]
[Route("api/v1/statistics")]
public class StatisticsController : ControllerBase
{
    private readonly IStatisticsService _statisticsService;
    private readonly ILogger<StatisticsController> _logger;

    public StatisticsController(
        IStatisticsService statisticsService,
        ILogger<StatisticsController> logger)
    {
        _statisticsService = statisticsService;
        _logger = logger;
    }

    /// <summary>
    /// Get database statistics including total photos, recent additions, and date ranges
    /// </summary>
    /// <remarks>
    /// This endpoint does not require authentication and is intended for public use
    /// to display statistics on the landing page.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetStatistics(CancellationToken cancellationToken)
    {
        try
        {
            var statistics = await _statisticsService.GetDatabaseStatisticsAsync(cancellationToken);
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve statistics");
            return StatusCode(500, new { error = "Failed to retrieve statistics" });
        }
    }
}
