using MarsVista.Core.Entities;

namespace MarsVista.Core.Repositories;

public interface ISolCompletenessRepository
{
    Task<SolCompleteness?> GetByRoverAndSolAsync(int roverId, int sol);
    Task<List<SolCompleteness>> GetByRoverAsync(int roverId);
    Task<List<SolCompleteness>> GetByStatusAsync(int roverId, string status);
    Task<List<SolCompleteness>> GetFailedSolsAsync(int roverId, int limit = 100);
    Task<SolCompletenessSummary> GetSummaryAsync(int roverId);
    Task<SolCompleteness> UpsertAsync(SolCompleteness completeness);
    Task RecordSuccessAsync(int roverId, int sol, int photoCount);
    Task RecordFailureAsync(int roverId, int sol, string error);
    Task BackfillFromPhotosAsync(int roverId);
}

public class SolCompletenessSummary
{
    public int RoverId { get; set; }
    public string RoverName { get; set; } = "";
    public int TotalSols { get; set; }
    public int SuccessSols { get; set; }
    public int FailedSols { get; set; }
    public int PartialSols { get; set; }
    public int PendingSols { get; set; }
    public int EmptySols { get; set; }
    public int TotalPhotos { get; set; }
    public DateTime? LastScrapeAttempt { get; set; }
}
