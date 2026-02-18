namespace MarsVista.Core.Entities;

public class StitchedPanorama
{
    public Guid Id { get; set; }
    public string PanoramaId { get; set; } = string.Empty;
    public string Status { get; set; } = "processing"; // processing, completed, failed
    public string? ImagePath { get; set; }
    public int? ImageWidth { get; set; }
    public int? ImageHeight { get; set; }
    public long? ImageSizeBytes { get; set; }
    public int? SourcePhotoCount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
