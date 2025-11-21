using MarsVista.Api.DTOs.V2;

namespace MarsVista.Api.Services.V2;

/// <summary>
/// Service for time machine queries (same location, different times)
/// </summary>
public interface ITimeMachineService
{
    /// <summary>
    /// Get photos from the same location at different times
    /// </summary>
    Task<TimeMachineResponse> GetTimeMachinePhotosAsync(
        int site,
        int drive,
        string? rover = null,
        string? marsTime = null,
        string? camera = null,
        int? limit = null,
        CancellationToken cancellationToken = default);
}
