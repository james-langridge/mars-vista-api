using Microsoft.AspNetCore.Mvc;
using MarsVista.Api.DTOs.V2;
using MarsVista.Api.Services.V2;
using System.Text.Json.Serialization;

namespace MarsVista.Api.Controllers.V1;

/// <summary>
/// Internal API for rating submissions from trusted frontends.
/// Protected by InternalApiMiddleware (X-Internal-Secret header).
/// Accepts clientId as a parameter so frontends don't need per-user API keys.
/// </summary>
[ApiController]
[Route("api/v1/internal/ratings")]
[ApiExplorerSettings(IgnoreApi = true)]
public class RatingInternalController : ControllerBase
{
    private readonly IPhotoQueryServiceV2 _photoQueryService;
    private readonly IPanoramaService _panoramaService;
    private readonly ILogger<RatingInternalController> _logger;

    public RatingInternalController(
        IPhotoQueryServiceV2 photoQueryService,
        IPanoramaService panoramaService,
        ILogger<RatingInternalController> logger)
    {
        _photoQueryService = photoQueryService;
        _panoramaService = panoramaService;
        _logger = logger;
    }

    [HttpPost("photos/{id}")]
    public async Task<IActionResult> RatePhoto(
        int id,
        [FromBody] InternalRatingRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.ClientId))
            return BadRequest(new { error = "client_id is required" });

        if (request.Rating < 1 || request.Rating > 5)
            return BadRequest(new { error = "Rating must be between 1 and 5" });

        if (!await _photoQueryService.PhotoExistsAsync(id, cancellationToken))
            return NotFound(new { error = $"Photo with ID {id} not found" });

        var result = await _photoQueryService.UpsertRatingAsync(id, request.ClientId, request.Rating, cancellationToken);
        return Ok(result);
    }

    [HttpGet("photos/{id}")]
    public async Task<IActionResult> GetPhotoRating(
        int id,
        [FromQuery(Name = "client_id")] string? clientId,
        CancellationToken cancellationToken)
    {
        if (!await _photoQueryService.PhotoExistsAsync(id, cancellationToken))
            return NotFound(new { error = $"Photo with ID {id} not found" });

        var result = await _photoQueryService.GetRatingAsync(id, clientId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("panoramas/{id}")]
    public async Task<IActionResult> RatePanorama(
        string id,
        [FromBody] InternalRatingRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.ClientId))
            return BadRequest(new { error = "client_id is required" });

        if (request.Rating < 1 || request.Rating > 5)
            return BadRequest(new { error = "Rating must be between 1 and 5" });

        if (!IsValidPanoramaIdFormat(id))
            return BadRequest(new { error = "Invalid panorama ID format. Expected: pano_{rover}_{sol}_{index}" });

        var result = await _panoramaService.UpsertRatingAsync(id, request.ClientId, request.Rating, cancellationToken);
        return Ok(result);
    }

    [HttpGet("panoramas/{id}")]
    public async Task<IActionResult> GetPanoramaRating(
        string id,
        [FromQuery(Name = "client_id")] string? clientId,
        CancellationToken cancellationToken)
    {
        var result = await _panoramaService.GetRatingAsync(id, clientId, cancellationToken);
        return Ok(result);
    }

    private static bool IsValidPanoramaIdFormat(string id)
    {
        var parts = id.Split('_');
        return parts.Length >= 4 &&
               parts[0] == "pano" &&
               int.TryParse(parts[^2], out _) &&
               int.TryParse(parts[^1], out _);
    }
}

public record InternalRatingRequest
{
    [JsonPropertyName("rating")]
    public int Rating { get; init; }

    [JsonPropertyName("client_id")]
    public string ClientId { get; init; } = string.Empty;
}
