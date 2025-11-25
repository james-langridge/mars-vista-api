namespace MarsVista.Core.Entities;

public class ScraperState : ITimestamped
{
    public int Id { get; set; }
    public string RoverName { get; set; } = string.Empty;
    public int LastScrapedSol { get; set; }
    public DateTime LastScrapeTimestamp { get; set; }
    public string LastScrapeStatus { get; set; } = "success"; // 'success', 'failed', 'in_progress'
    public int PhotosAddedLastRun { get; set; }
    public string? ErrorMessage { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
