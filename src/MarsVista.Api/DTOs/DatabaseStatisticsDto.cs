using System.Text.Json.Serialization;

namespace MarsVista.Api.DTOs;

public record DatabaseStatisticsDto
{
    [JsonPropertyName("total_photos")]
    public long TotalPhotos { get; init; }

    [JsonPropertyName("photos_added_last_7_days")]
    public long PhotosAddedLast7Days { get; init; }

    [JsonPropertyName("rover_count")]
    public int RoverCount { get; init; }

    [JsonPropertyName("earliest_photo_date")]
    public string? EarliestPhotoDate { get; init; }

    [JsonPropertyName("latest_photo_date")]
    public string? LatestPhotoDate { get; init; }

    [JsonPropertyName("last_scrape_at")]
    public DateTime? LastScrapeAt { get; init; }
}
