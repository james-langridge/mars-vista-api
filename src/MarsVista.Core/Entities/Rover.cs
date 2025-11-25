namespace MarsVista.Core.Entities;

public class Rover : ITimestamped
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? LandingDate { get; set; }
    public DateTime? LaunchDate { get; set; }
    public string Status { get; set; } = string.Empty;  // "active", "complete"
    public int? MaxSol { get; set; }
    public DateTime? MaxDate { get; set; }
    public int? TotalPhotos { get; set; }

    // Navigation properties
    public virtual ICollection<Camera> Cameras { get; set; } = new List<Camera>();
    public virtual ICollection<Photo> Photos { get; set; } = new List<Photo>();

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
