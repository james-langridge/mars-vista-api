using System.Text.Json.Serialization;

namespace MarsVista.Api.DTOs.V2;

/// <summary>
/// Standardized API response envelope for v2 endpoints
/// Inspired by JSON:API specification for consistency
/// </summary>
public record ApiResponse<T>
{
    /// <summary>
    /// The primary data for this response
    /// </summary>
    [JsonPropertyName("data")]
    public T Data { get; init; }

    /// <summary>
    /// Metadata about the response
    /// </summary>
    [JsonPropertyName("meta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ResponseMeta? Meta { get; init; }

    /// <summary>
    /// Pagination information
    /// </summary>
    [JsonPropertyName("pagination")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PaginationInfo? Pagination { get; init; }

    /// <summary>
    /// Related resource links
    /// </summary>
    [JsonPropertyName("links")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ResponseLinks? Links { get; init; }

    public ApiResponse(T data)
    {
        Data = data;
    }
}

/// <summary>
/// Metadata about the response
/// </summary>
public record ResponseMeta
{
    /// <summary>
    /// Total count of resources matching the query (before pagination)
    /// </summary>
    [JsonPropertyName("total_count")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? TotalCount { get; init; }

    /// <summary>
    /// Number of resources returned in this response
    /// </summary>
    [JsonPropertyName("returned_count")]
    public int ReturnedCount { get; init; }

    /// <summary>
    /// The query parameters that were applied
    /// </summary>
    [JsonPropertyName("query")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Query { get; init; }

    /// <summary>
    /// Response timestamp
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Pagination information supporting both page-based and cursor-based pagination
/// </summary>
public record PaginationInfo
{
    /// <summary>
    /// Current page number (1-indexed)
    /// </summary>
    [JsonPropertyName("page")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Page { get; init; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    [JsonPropertyName("per_page")]
    public int PerPage { get; init; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    [JsonPropertyName("total_pages")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? TotalPages { get; init; }

    /// <summary>
    /// Cursor-based pagination information
    /// </summary>
    [JsonPropertyName("cursor")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CursorInfo? Cursor { get; init; }
}

/// <summary>
/// Cursor-based pagination pointers
/// </summary>
public record CursorInfo
{
    /// <summary>
    /// Cursor for the current page
    /// </summary>
    [JsonPropertyName("current")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Current { get; init; }

    /// <summary>
    /// Cursor for the next page
    /// </summary>
    [JsonPropertyName("next")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Next { get; init; }

    /// <summary>
    /// Cursor for the previous page
    /// </summary>
    [JsonPropertyName("previous")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Previous { get; init; }
}

/// <summary>
/// Related resource links for navigation
/// </summary>
public record ResponseLinks
{
    /// <summary>
    /// Link to the current resource/page
    /// </summary>
    [JsonPropertyName("self")]
    public string Self { get; init; } = string.Empty;

    /// <summary>
    /// Link to the next page
    /// </summary>
    [JsonPropertyName("next")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Next { get; init; }

    /// <summary>
    /// Link to the previous page
    /// </summary>
    [JsonPropertyName("previous")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Previous { get; init; }

    /// <summary>
    /// Link to the first page
    /// </summary>
    [JsonPropertyName("first")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? First { get; init; }

    /// <summary>
    /// Link to the last page
    /// </summary>
    [JsonPropertyName("last")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Last { get; init; }
}
