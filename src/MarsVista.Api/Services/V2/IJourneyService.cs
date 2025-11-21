using MarsVista.Api.DTOs.V2;

namespace MarsVista.Api.Services.V2;

/// <summary>
/// Service for retrieving rover journey data
/// </summary>
public interface IJourneyService
{
    /// <summary>
    /// Get journey data for a rover over a sol range
    /// </summary>
    Task<ApiResponse<JourneyResource>> GetJourneyAsync(
        string rover,
        int? solMin = null,
        int? solMax = null,
        CancellationToken cancellationToken = default);
}
