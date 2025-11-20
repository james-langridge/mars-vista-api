namespace MarsVista.Api.Entities;

public class ScraperJobHistory
{
    public int Id { get; set; }
    public DateTime JobStartedAt { get; set; }
    public DateTime? JobCompletedAt { get; set; }
    public int? TotalDurationSeconds { get; set; }
    public int TotalRoversAttempted { get; set; }
    public int TotalRoversSucceeded { get; set; }
    public int TotalPhotosAdded { get; set; }
    public string Status { get; set; } = "in_progress"; // 'success', 'failed', 'partial', 'in_progress'
    public string? ErrorSummary { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation property
    public ICollection<ScraperJobRoverDetails> RoverDetails { get; set; } = new List<ScraperJobRoverDetails>();
}
