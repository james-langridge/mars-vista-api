using MarsVista.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarsVista.Api.Controllers.V1;

[ApiController]
[Route("api/v1/manifests")]
public class ManifestsController : ControllerBase
{
    private readonly IRoverQueryService _roverQueryService;
    private readonly ILogger<ManifestsController> _logger;

    public ManifestsController(
        IRoverQueryService roverQueryService,
        ILogger<ManifestsController> logger)
    {
        _roverQueryService = roverQueryService;
        _logger = logger;
    }

    /// <summary>
    /// Get photo manifest for a rover (photos grouped by sol)
    /// </summary>
    /// <param name="name">Rover name</param>
    [HttpGet("{name}")]
    public async Task<IActionResult> GetManifest(string name, CancellationToken cancellationToken)
    {
        var manifest = await _roverQueryService.GetManifestAsync(name, cancellationToken);

        if (manifest == null)
        {
            return NotFound(new { error = $"Rover '{name}' not found" });
        }

        return Ok(new { photo_manifest = manifest });
    }
}
