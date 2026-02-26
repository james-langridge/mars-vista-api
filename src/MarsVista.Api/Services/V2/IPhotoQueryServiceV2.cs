using MarsVista.Api.DTOs.V2;
using MarsVista.Api.Models.V2;

namespace MarsVista.Api.Services.V2;

/// <summary>
/// Query service for v2 API photos endpoint
/// Handles complex filtering, pagination, and field selection
/// </summary>
public interface IPhotoQueryServiceV2
{
    /// <summary>
    /// Query photos with advanced filtering and pagination
    /// Returns ApiResponse with photos and pagination metadata
    /// </summary>
    Task<ApiResponse<List<PhotoResource>>> QueryPhotosAsync(
        PhotoQueryParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a single photo by ID
    /// </summary>
    Task<PhotoResource?> GetPhotoByIdAsync(int id, PhotoQueryParameters parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get multiple photos by IDs (batch retrieval)
    /// </summary>
    Task<List<PhotoResource>> GetPhotosByIdsAsync(List<int> ids, PhotoQueryParameters parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get total count of photos matching the query (for pagination metadata)
    /// </summary>
    Task<int> GetPhotoCountAsync(PhotoQueryParameters parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get photo statistics grouped by the specified dimension
    /// </summary>
    Task<PhotoStatisticsResponse> GetStatisticsAsync(
        PhotoQueryParameters parameters,
        string groupBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a photo exists by ID
    /// </summary>
    Task<bool> PhotoExistsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submit or update a photo rating
    /// </summary>
    Task<RatingResponse> UpsertRatingAsync(int photoId, string clientId, int rating, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get rating information for a photo
    /// </summary>
    Task<RatingResponse> GetRatingAsync(int photoId, string? clientId, CancellationToken cancellationToken = default);
}
