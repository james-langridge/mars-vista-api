using System.Text.Json.Serialization;

namespace MarsVista.Api.DTOs.V2;

public record RatingRequest
{
    [JsonPropertyName("rating")]
    public int Rating { get; init; }
}

public record RatingResponse
{
    [JsonPropertyName("average_rating")]
    public double AverageRating { get; init; }

    [JsonPropertyName("rating_count")]
    public int RatingCount { get; init; }

    [JsonPropertyName("user_rating")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? UserRating { get; init; }
}
