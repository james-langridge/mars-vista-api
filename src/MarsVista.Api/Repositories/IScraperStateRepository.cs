using MarsVista.Api.Entities;

namespace MarsVista.Api.Repositories;

public interface IScraperStateRepository
{
    Task<ScraperState?> GetByRoverNameAsync(string roverName);
    Task<List<ScraperState>> GetAllAsync();
    Task<ScraperState> CreateAsync(ScraperState state);
    Task<ScraperState> UpdateAsync(ScraperState state);
    Task DeleteAsync(string roverName);
}
