using MarsVista.Api.Data;
using MarsVista.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarsVista.Api.Repositories;

public class ScraperStateRepository : IScraperStateRepository
{
    private readonly MarsVistaDbContext _context;

    public ScraperStateRepository(MarsVistaDbContext context)
    {
        _context = context;
    }

    public async Task<ScraperState?> GetByRoverNameAsync(string roverName)
    {
        return await _context.ScraperStates
            .FirstOrDefaultAsync(s => s.RoverName.ToLower() == roverName.ToLower());
    }

    public async Task<List<ScraperState>> GetAllAsync()
    {
        return await _context.ScraperStates.ToListAsync();
    }

    public async Task<ScraperState> CreateAsync(ScraperState state)
    {
        state.CreatedAt = DateTime.UtcNow;
        state.UpdatedAt = DateTime.UtcNow;

        _context.ScraperStates.Add(state);
        await _context.SaveChangesAsync();

        return state;
    }

    public async Task<ScraperState> UpdateAsync(ScraperState state)
    {
        state.UpdatedAt = DateTime.UtcNow;

        _context.ScraperStates.Update(state);
        await _context.SaveChangesAsync();

        return state;
    }

    public async Task DeleteAsync(string roverName)
    {
        var state = await GetByRoverNameAsync(roverName);
        if (state != null)
        {
            _context.ScraperStates.Remove(state);
            await _context.SaveChangesAsync();
        }
    }
}
