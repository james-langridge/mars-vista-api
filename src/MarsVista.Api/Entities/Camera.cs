namespace MarsVista.Api.Entities;

public class Camera : ITimestamped
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;      // "FHAZ", "NAVCAM", "MAST"
    public string FullName { get; set; } = string.Empty;  // "Front Hazard Avoidance Camera"
    public int RoverId { get; set; }

    // Navigation properties
    public virtual Rover Rover { get; set; } = null!;
    public virtual ICollection<Photo> Photos { get; set; } = new List<Photo>();

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
