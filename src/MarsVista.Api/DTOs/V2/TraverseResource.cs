using System.Text.Json.Serialization;

namespace MarsVista.Api.DTOs.V2;

/// <summary>
/// Traverse resource - deduplicated path data optimized for map visualization
/// </summary>
public record TraverseResource
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "traverse";

    [JsonPropertyName("attributes")]
    public TraverseAttributes Attributes { get; init; } = new();

    [JsonPropertyName("path")]
    public List<TraversePoint> Path { get; init; } = new();

    [JsonPropertyName("links")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TraverseLinks? Links { get; init; }

    [JsonPropertyName("meta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TraverseMeta? Meta { get; init; }
}

/// <summary>
/// Summary statistics for the traverse
/// </summary>
public record TraverseAttributes
{
    [JsonPropertyName("rover")]
    public string Rover { get; init; } = string.Empty;

    [JsonPropertyName("sol_range")]
    public SolRange SolRange { get; init; } = new();

    [JsonPropertyName("total_distance_m")]
    public float TotalDistanceM { get; init; }

    [JsonPropertyName("total_elevation_gain_m")]
    public float TotalElevationGainM { get; init; }

    [JsonPropertyName("total_elevation_loss_m")]
    public float TotalElevationLossM { get; init; }

    [JsonPropertyName("net_elevation_change_m")]
    public float NetElevationChangeM { get; init; }

    [JsonPropertyName("point_count")]
    public int PointCount { get; init; }

    [JsonPropertyName("simplified_point_count")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? SimplifiedPointCount { get; init; }

    [JsonPropertyName("bounding_box")]
    public BoundingBox BoundingBox { get; init; } = new();
}

/// <summary>
/// Sol range for the traverse
/// </summary>
public record SolRange
{
    [JsonPropertyName("start")]
    public int Start { get; init; }

    [JsonPropertyName("end")]
    public int End { get; init; }
}

/// <summary>
/// 3D bounding box
/// </summary>
public record BoundingBox
{
    [JsonPropertyName("min")]
    public PhotoCoordinates Min { get; init; } = new();

    [JsonPropertyName("max")]
    public PhotoCoordinates Max { get; init; } = new();
}

/// <summary>
/// A deduplicated point on the rover's traverse path
/// </summary>
public record TraversePoint
{
    [JsonPropertyName("x")]
    public float X { get; init; }

    [JsonPropertyName("y")]
    public float Y { get; init; }

    [JsonPropertyName("z")]
    public float Z { get; init; }

    [JsonPropertyName("sol_first")]
    public int SolFirst { get; init; }

    [JsonPropertyName("sol_last")]
    public int SolLast { get; init; }

    [JsonPropertyName("cumulative_distance_m")]
    public float CumulativeDistanceM { get; init; }

    [JsonPropertyName("segment")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TraverseSegment? Segment { get; init; }
}

/// <summary>
/// Per-segment distance and bearing data
/// </summary>
public record TraverseSegment
{
    [JsonPropertyName("distance_m")]
    public float DistanceM { get; init; }

    [JsonPropertyName("bearing_deg")]
    public float BearingDeg { get; init; }

    [JsonPropertyName("elevation_change_m")]
    public float ElevationChangeM { get; init; }
}

/// <summary>
/// Links related to the traverse
/// </summary>
public record TraverseLinks
{
    [JsonPropertyName("geojson")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? GeoJson { get; init; }

    [JsonPropertyName("kml")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Kml { get; init; }
}

/// <summary>
/// GeoJSON FeatureCollection for traverse path
/// </summary>
public record GeoJsonFeatureCollection
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "FeatureCollection";

    [JsonPropertyName("features")]
    public List<GeoJsonFeature> Features { get; init; } = new();
}

/// <summary>
/// GeoJSON Feature containing the traverse LineString
/// </summary>
public record GeoJsonFeature
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "Feature";

    [JsonPropertyName("geometry")]
    public GeoJsonLineString Geometry { get; init; } = new();

    [JsonPropertyName("properties")]
    public GeoJsonProperties Properties { get; init; } = new();
}

/// <summary>
/// GeoJSON LineString geometry
/// </summary>
public record GeoJsonLineString
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "LineString";

    /// <summary>
    /// Array of [x, y, z] coordinate arrays
    /// </summary>
    [JsonPropertyName("coordinates")]
    public List<float[]> Coordinates { get; init; } = new();
}

/// <summary>
/// Properties for the GeoJSON feature
/// </summary>
public record GeoJsonProperties
{
    [JsonPropertyName("rover")]
    public string Rover { get; init; } = string.Empty;

    [JsonPropertyName("sol_range")]
    public int[] SolRange { get; init; } = Array.Empty<int>();

    [JsonPropertyName("total_distance_m")]
    public float TotalDistanceM { get; init; }

    [JsonPropertyName("point_count")]
    public int PointCount { get; init; }
}

/// <summary>
/// Metadata about the traverse data source
/// </summary>
public record TraverseMeta
{
    [JsonPropertyName("data_source")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DataSource { get; init; }

    [JsonPropertyName("data_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DataUrl { get; init; }

    [JsonPropertyName("max_sol_in_data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int MaxSolInData { get; init; }

    [JsonPropertyName("coordinate_frame")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CoordinateFrame { get; init; }

    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; init; }
}
