using MarsVista.Core.Data;
using MarsVista.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarsVista.Core.Repositories;

public class SolCompletenessRepository : ISolCompletenessRepository
{
    private readonly MarsVistaDbContext _context;

    public SolCompletenessRepository(MarsVistaDbContext context)
    {
        _context = context;
    }

    public async Task<SolCompleteness?> GetByRoverAndSolAsync(int roverId, int sol)
    {
        return await _context.SolCompleteness
            .FirstOrDefaultAsync(s => s.RoverId == roverId && s.Sol == sol);
    }

    public async Task<List<SolCompleteness>> GetByRoverAsync(int roverId)
    {
        return await _context.SolCompleteness
            .Where(s => s.RoverId == roverId)
            .OrderBy(s => s.Sol)
            .ToListAsync();
    }

    public async Task<List<SolCompleteness>> GetByStatusAsync(int roverId, string status)
    {
        return await _context.SolCompleteness
            .Where(s => s.RoverId == roverId && s.ScrapeStatus == status)
            .OrderBy(s => s.Sol)
            .ToListAsync();
    }

    public async Task<List<SolCompleteness>> GetFailedSolsAsync(int roverId, int limit = 100)
    {
        return await _context.SolCompleteness
            .Where(s => s.RoverId == roverId && s.ScrapeStatus == "failed")
            .OrderByDescending(s => s.LastScrapeAttempt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<SolCompletenessSummary> GetSummaryAsync(int roverId)
    {
        var rover = await _context.Rovers.FindAsync(roverId);

        var stats = await _context.SolCompleteness
            .Where(s => s.RoverId == roverId)
            .GroupBy(s => s.ScrapeStatus)
            .Select(g => new { Status = g.Key, Count = g.Count(), Photos = g.Sum(s => s.PhotoCount) })
            .ToListAsync();

        var lastAttempt = await _context.SolCompleteness
            .Where(s => s.RoverId == roverId)
            .MaxAsync(s => (DateTime?)s.LastScrapeAttempt);

        return new SolCompletenessSummary
        {
            RoverId = roverId,
            RoverName = rover?.Name ?? "Unknown",
            TotalSols = stats.Sum(s => s.Count),
            SuccessSols = stats.FirstOrDefault(s => s.Status == "success")?.Count ?? 0,
            FailedSols = stats.FirstOrDefault(s => s.Status == "failed")?.Count ?? 0,
            PartialSols = stats.FirstOrDefault(s => s.Status == "partial")?.Count ?? 0,
            PendingSols = stats.FirstOrDefault(s => s.Status == "pending")?.Count ?? 0,
            EmptySols = stats.FirstOrDefault(s => s.Status == "empty")?.Count ?? 0,
            TotalPhotos = stats.Sum(s => s.Photos),
            LastScrapeAttempt = lastAttempt
        };
    }

    public async Task<SolCompleteness> UpsertAsync(SolCompleteness completeness)
    {
        var existing = await GetByRoverAndSolAsync(completeness.RoverId, completeness.Sol);

        if (existing == null)
        {
            completeness.CreatedAt = DateTime.UtcNow;
            completeness.UpdatedAt = DateTime.UtcNow;
            _context.SolCompleteness.Add(completeness);
        }
        else
        {
            existing.PhotoCount = completeness.PhotoCount;
            existing.NasaExpectedCount = completeness.NasaExpectedCount;
            existing.ScrapeStatus = completeness.ScrapeStatus;
            existing.LastScrapeAttempt = completeness.LastScrapeAttempt;
            existing.LastSuccessAt = completeness.LastSuccessAt;
            existing.AttemptCount = completeness.AttemptCount;
            existing.ConsecutiveFailures = completeness.ConsecutiveFailures;
            existing.LastError = completeness.LastError;
            existing.UpdatedAt = DateTime.UtcNow;
            completeness = existing;
        }

        await _context.SaveChangesAsync();
        return completeness;
    }

    public async Task RecordSuccessAsync(int roverId, int sol, int photoCount)
    {
        var existing = await GetByRoverAndSolAsync(roverId, sol);
        var now = DateTime.UtcNow;

        if (existing == null)
        {
            var completeness = new SolCompleteness
            {
                RoverId = roverId,
                Sol = sol,
                PhotoCount = photoCount,
                ScrapeStatus = photoCount > 0 ? "success" : "empty",
                LastScrapeAttempt = now,
                LastSuccessAt = now,
                AttemptCount = 1,
                ConsecutiveFailures = 0,
                CreatedAt = now,
                UpdatedAt = now
            };
            _context.SolCompleteness.Add(completeness);
        }
        else
        {
            existing.PhotoCount = photoCount;
            existing.ScrapeStatus = photoCount > 0 ? "success" : "empty";
            existing.LastScrapeAttempt = now;
            existing.LastSuccessAt = now;
            existing.AttemptCount++;
            existing.ConsecutiveFailures = 0;
            existing.LastError = null;
            existing.UpdatedAt = now;
        }

        await _context.SaveChangesAsync();
    }

    public async Task RecordFailureAsync(int roverId, int sol, string error)
    {
        var existing = await GetByRoverAndSolAsync(roverId, sol);
        var now = DateTime.UtcNow;

        if (existing == null)
        {
            var completeness = new SolCompleteness
            {
                RoverId = roverId,
                Sol = sol,
                PhotoCount = 0,
                ScrapeStatus = "failed",
                LastScrapeAttempt = now,
                AttemptCount = 1,
                ConsecutiveFailures = 1,
                LastError = error,
                CreatedAt = now,
                UpdatedAt = now
            };
            _context.SolCompleteness.Add(completeness);
        }
        else
        {
            existing.ScrapeStatus = "failed";
            existing.LastScrapeAttempt = now;
            existing.AttemptCount++;
            existing.ConsecutiveFailures++;
            existing.LastError = error;
            existing.UpdatedAt = now;
        }

        await _context.SaveChangesAsync();
    }

    public async Task BackfillFromPhotosAsync(int roverId)
    {
        // Get all sols with photo counts for this rover
        var solCounts = await _context.Photos
            .Where(p => p.RoverId == roverId)
            .GroupBy(p => p.Sol)
            .Select(g => new { Sol = g.Key, Count = g.Count() })
            .ToListAsync();

        var now = DateTime.UtcNow;

        foreach (var sc in solCounts)
        {
            var existing = await GetByRoverAndSolAsync(roverId, sc.Sol);

            if (existing == null)
            {
                var completeness = new SolCompleteness
                {
                    RoverId = roverId,
                    Sol = sc.Sol,
                    PhotoCount = sc.Count,
                    ScrapeStatus = "success",
                    LastSuccessAt = now,
                    AttemptCount = 1,
                    ConsecutiveFailures = 0,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _context.SolCompleteness.Add(completeness);
            }
            else
            {
                // Update photo count but don't change status if already set
                existing.PhotoCount = sc.Count;
                existing.UpdatedAt = now;
            }
        }

        await _context.SaveChangesAsync();
    }
}
