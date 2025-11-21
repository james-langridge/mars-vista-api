using MarsVista.Api.DTOs.V2;

namespace MarsVista.Api.Services.V2;

/// <summary>
/// Query service for v2 API rovers endpoint
/// </summary>
public interface IRoverQueryServiceV2
{
    /// <summary>
    /// Get all rovers
    /// </summary>
    Task<List<RoverResource>> GetAllRoversAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific rover by slug (name)
    /// </summary>
    Task<RoverResource?> GetRoverBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get rover manifest (photo history by sol)
    /// </summary>
    Task<RoverManifest?> GetRoverManifestAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get cameras for a specific rover
    /// </summary>
    Task<List<CameraResource>> GetRoverCamerasAsync(string slug, CancellationToken cancellationToken = default);
}
