namespace MarsVista.Core.Entities;

public class SolCompleteness : ITimestamped
{
    public Guid Id { get; set; }
    public int RoverId { get; set; }
    public int Sol { get; set; }

    // Our data
    public int PhotoCount { get; set; }

    // NASA data (null if unknown - hard to get for Perseverance)
    public int? NasaExpectedCount { get; set; }

    // Status: pending, success, partial, failed, empty
    public string ScrapeStatus { get; set; } = "pending";

    // Tracking
    public DateTime? LastScrapeAttempt { get; set; }
    public DateTime? LastSuccessAt { get; set; }
    public int AttemptCount { get; set; }
    public int ConsecutiveFailures { get; set; }
    public string? LastError { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public Rover? Rover { get; set; }
}
