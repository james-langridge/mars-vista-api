using MarsVista.Api.DTOs;

namespace MarsVista.Api.Services;

public interface IPhotoQueryService
{
    Task<(List<PhotoDto> Photos, int TotalCount)> QueryPhotosAsync(
        string roverName,
        int? sol = null,
        DateTime? earthDate = null,
        string? camera = null,
        int page = 1,
        int perPage = 25,
        CancellationToken cancellationToken = default);

    Task<(List<PhotoDto> Photos, int TotalCount)> GetLatestPhotosAsync(
        string roverName,
        int page = 1,
        int perPage = 25,
        CancellationToken cancellationToken = default);

    Task<PhotoDto?> GetPhotoByIdAsync(
        int id,
        CancellationToken cancellationToken = default);
}
