namespace MarsVista.Core.Entities;

public class PhotoRating
{
    public Guid Id { get; set; }
    public int PhotoId { get; set; }
    public int Rating { get; set; } // 1-5
    public string ClientId { get; set; } = string.Empty; // API key hash
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Photo Photo { get; set; } = null!;
}
