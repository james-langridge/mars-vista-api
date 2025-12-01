using MarsVista.Api.DTOs.V2;

namespace MarsVista.Api.Services.V2;

/// <summary>
/// Service for retrieving deduplicated rover traverse path data
/// </summary>
public interface ITraverseService
{
    /// <summary>
    /// Get traverse data for a rover over a sol range
    /// </summary>
    /// <param name="rover">Rover slug</param>
    /// <param name="solMin">Minimum sol (inclusive)</param>
    /// <param name="solMax">Maximum sol (inclusive)</param>
    /// <param name="simplify">Douglas-Peucker tolerance in meters (0 = no simplification)</param>
    /// <param name="includeSegments">Include per-segment distance/bearing data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<TraverseResource> GetTraverseAsync(
        string rover,
        int? solMin = null,
        int? solMax = null,
        float simplify = 0,
        bool includeSegments = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get traverse data as GeoJSON
    /// </summary>
    Task<GeoJsonFeatureCollection> GetTraverseGeoJsonAsync(
        string rover,
        int? solMin = null,
        int? solMax = null,
        float simplify = 0,
        CancellationToken cancellationToken = default);
}
