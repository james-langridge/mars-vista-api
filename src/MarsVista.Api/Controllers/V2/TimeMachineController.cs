using Microsoft.AspNetCore.Mvc;
using MarsVista.Api.DTOs.V2;
using MarsVista.Api.Services.V2;

namespace MarsVista.Api.Controllers.V2;

/// <summary>
/// Controller for time machine queries (same location, different times)
/// </summary>
[ApiController]
[Route("api/v2/time-machine")]
[Tags("V2 - Advanced Features")]
public class TimeMachineController : ControllerBase
{
    private readonly ITimeMachineService _timeMachineService;
    private readonly ILogger<TimeMachineController> _logger;

    public TimeMachineController(
        ITimeMachineService timeMachineService,
        ILogger<TimeMachineController> logger)
    {
        _timeMachineService = timeMachineService;
        _logger = logger;
    }

    /// <summary>
    /// Get photos from the same location at different times
    /// </summary>
    /// <param name="site">Site number (required)</param>
    /// <param name="drive">Drive number (required)</param>
    /// <param name="rover">Rover name filter</param>
    /// <param name="mars_time">Mars local time filter (e.g., M14:00:00)</param>
    /// <param name="camera">Camera name filter</param>
    /// <param name="limit">Maximum number of results (default: 100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet]
    [ProducesResponseType(typeof(TimeMachineResponse), 200)]
    [ProducesResponseType(typeof(ApiError), 400)]
    public async Task<IActionResult> GetTimeMachinePhotos(
        [FromQuery] int? site = null,
        [FromQuery] int? drive = null,
        [FromQuery] string? rover = null,
        [FromQuery] string? mars_time = null,
        [FromQuery] string? camera = null,
        [FromQuery] int? limit = null,
        CancellationToken cancellationToken = default)
    {
        // Validate required parameters
        if (!site.HasValue)
        {
            return BadRequest(new ApiError
            {
                Type = "/errors/validation-error",
                Title = "Validation Error",
                Status = 400,
                Detail = "site parameter is required",
                Instance = Request.Path,
                Errors = new List<ValidationError>
                {
                    new ValidationError
                    {
                        Field = "site",
                        Message = "Required parameter",
                        Example = "site=79"
                    }
                }
            });
        }

        if (!drive.HasValue)
        {
            return BadRequest(new ApiError
            {
                Type = "/errors/validation-error",
                Title = "Validation Error",
                Status = 400,
                Detail = "drive parameter is required",
                Instance = Request.Path,
                Errors = new List<ValidationError>
                {
                    new ValidationError
                    {
                        Field = "drive",
                        Message = "Required parameter",
                        Example = "drive=1204"
                    }
                }
            });
        }

        var response = await _timeMachineService.GetTimeMachinePhotosAsync(
            site.Value,
            drive.Value,
            rover,
            mars_time,
            camera,
            limit,
            cancellationToken);

        return Ok(response);
    }
}
