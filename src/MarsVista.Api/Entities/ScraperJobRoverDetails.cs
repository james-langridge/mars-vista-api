namespace MarsVista.Api.Entities;

public class ScraperJobRoverDetails
{
    public int Id { get; set; }
    public int JobHistoryId { get; set; }
    public string RoverName { get; set; } = string.Empty;
    public int StartSol { get; set; }
    public int EndSol { get; set; }
    public int SolsAttempted { get; set; }
    public int SolsSucceeded { get; set; }
    public int SolsFailed { get; set; }
    public int PhotosAdded { get; set; }
    public int DurationSeconds { get; set; }
    public string Status { get; set; } = "success"; // 'success', 'failed', 'partial'
    public string? ErrorMessage { get; set; }
    public string? FailedSols { get; set; } // JSON array of failed sol numbers
    public DateTime CreatedAt { get; set; }

    // Navigation property
    public ScraperJobHistory JobHistory { get; set; } = null!;
}
