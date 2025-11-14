using MarsVista.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarsVista.Api.Controllers.V1;

[ApiController]
[Route("api/v1/photos")]
public class PhotosController : ControllerBase
{
    private readonly IPhotoQueryService _photoQueryService;
    private readonly ILogger<PhotosController> _logger;

    public PhotosController(
        IPhotoQueryService photoQueryService,
        ILogger<PhotosController> logger)
    {
        _photoQueryService = photoQueryService;
        _logger = logger;
    }

    /// <summary>
    /// Get a specific photo by ID
    /// </summary>
    /// <param name="id">Photo ID</param>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPhoto(int id, CancellationToken cancellationToken)
    {
        var photo = await _photoQueryService.GetPhotoByIdAsync(id, cancellationToken);

        if (photo == null)
        {
            return NotFound(new { error = $"Photo {id} not found" });
        }

        return Ok(new { photo });
    }
}
