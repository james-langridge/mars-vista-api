namespace MarsVista.Core.Entities;

public class PanoramaRating
{
    public Guid Id { get; set; }
    public string PanoramaId { get; set; } = string.Empty;
    public int Rating { get; set; } // 1-5
    public string ClientId { get; set; } = string.Empty; // API key hash
    public DateTime CreatedAt { get; set; }
}
