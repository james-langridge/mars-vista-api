using MarsVista.Api.DTOs;

namespace MarsVista.Api.Services;

public interface IStatisticsService
{
    Task<DatabaseStatisticsDto> GetDatabaseStatisticsAsync(CancellationToken cancellationToken = default);
}
