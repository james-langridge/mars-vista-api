using MarsVista.Api.DTOs.V2;

namespace MarsVista.Api.Services.V2;

/// <summary>
/// Service for detecting and retrieving panoramic sequences
/// </summary>
public interface IPanoramaService
{
    /// <summary>
    /// Get all detected panoramas with optional filtering
    /// </summary>
    Task<ApiResponse<List<PanoramaResource>>> GetPanoramasAsync(
        string? rovers = null,
        int? solMin = null,
        int? solMax = null,
        int? minPhotos = null,
        string? stitchStatus = null,
        string? stitchMethod = null,
        string? mosaicType = null,
        string? quality = null,
        double? minRating = null,
        string? sort = null,
        string? order = null,
        int pageNumber = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific panorama by ID
    /// </summary>
    Task<PanoramaResource?> GetPanoramaByIdAsync(
        string panoramaId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create or update a rating for a panorama
    /// </summary>
    Task<RatingResponse> UpsertRatingAsync(
        string panoramaId,
        string clientId,
        int rating,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get rating information for a panorama
    /// </summary>
    Task<RatingResponse> GetRatingAsync(
        string panoramaId,
        string? clientId = null,
        CancellationToken cancellationToken = default);
}
