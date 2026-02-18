using MarsVista.Api.DTOs.V2;

namespace MarsVista.Api.Services.V2;

public interface IPanoramaStitchingService
{
    Task<StitchStatusResponse> GetStitchStatusAsync(string panoramaId, CancellationToken cancellationToken = default);
    Task<StitchStatusResponse> RequestStitchAsync(string panoramaId, CancellationToken cancellationToken = default);
    Task<string?> GetStitchedImagePathAsync(string panoramaId, CancellationToken cancellationToken = default);
}
