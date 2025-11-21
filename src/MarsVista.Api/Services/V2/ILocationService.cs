using MarsVista.Api.DTOs.V2;

namespace MarsVista.Api.Services.V2;

/// <summary>
/// Service for retrieving location timeline data
/// </summary>
public interface ILocationService
{
    /// <summary>
    /// Get all unique locations with optional filtering
    /// </summary>
    Task<ApiResponse<List<LocationResource>>> GetLocationsAsync(
        string? rovers = null,
        int? solMin = null,
        int? solMax = null,
        int? minPhotos = null,
        int pageNumber = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific location by ID
    /// </summary>
    Task<LocationResource?> GetLocationByIdAsync(
        string locationId,
        CancellationToken cancellationToken = default);
}
