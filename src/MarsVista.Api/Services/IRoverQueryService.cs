using MarsVista.Api.DTOs;

namespace MarsVista.Api.Services;

public interface IRoverQueryService
{
    Task<List<RoverDto>> GetAllRoversAsync(CancellationToken cancellationToken = default);
    Task<RoverDto?> GetRoverByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<PhotoManifestDto?> GetManifestAsync(string roverName, CancellationToken cancellationToken = default);
}
