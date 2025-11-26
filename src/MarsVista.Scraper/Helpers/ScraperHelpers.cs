using System.Text.Json;
using System.Text.RegularExpressions;

namespace MarsVista.Scraper.Helpers;

/// <summary>
/// Static helper methods for parsing NASA API responses.
/// Exposed as internal for unit testing.
/// </summary>
public static partial class ScraperHelpers
{
    // ============================================================================
    // DIMENSION PARSING
    // ============================================================================

    /// <summary>
    /// Parses dimensions from Curiosity's subframe_rect format: "(x, y, width, height)"
    /// Returns null if the format is invalid or not present.
    /// </summary>
    public static (int width, int height)? ParseSubframeRect(string? rect)
    {
        if (string.IsNullOrEmpty(rect))
            return null;

        // Format: "(x, y, width, height)" or "(x,y,width,height)" - we want the 3rd and 4th values
        var match = SubframeRectRegex().Match(rect);
        if (!match.Success)
            return null;

        return (int.Parse(match.Groups[3].Value), int.Parse(match.Groups[4].Value));
    }

    /// <summary>
    /// Parses dimensions from Perseverance's dimension field format: "(width,height)"
    /// Returns null if the format is invalid or not present.
    /// </summary>
    public static (int width, int height)? ParseDimensionField(string? dimension)
    {
        if (string.IsNullOrEmpty(dimension))
            return null;

        // Format: "(width,height)" - just two values
        var match = DimensionFieldRegex().Match(dimension);
        if (!match.Success)
            return null;

        return (int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value));
    }

    /// <summary>
    /// Infers dimensions from sample_type when actual dimensions are not available.
    /// These are conservative estimates based on typical NASA image sizes.
    /// </summary>
    public static (int? width, int? height) InferDimensionsFromSampleType(string? sampleType)
    {
        return sampleType?.ToLowerInvariant() switch
        {
            "thumbnail" => (160, 144),
            "subframe" => (1024, 1024),
            "full" => (1024, 1024),
            "downsampled" => (800, 600),
            "chemcam prc" => (1024, 1024),
            "mixed" => (1024, 1024),
            _ => (512, 512) // Conservative fallback
        };
    }

    // ============================================================================
    // THUMBNAIL DETECTION
    // ============================================================================

    /// <summary>
    /// Determines if a photo should be skipped based on sample_type.
    /// Thumbnails are too small to be useful (typically 160x144 or smaller).
    /// </summary>
    public static bool IsThumbnail(string? sampleType)
    {
        return sampleType?.Equals("thumbnail", StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// Determines if a photo should be skipped based on dimensions.
    /// Photos smaller than 200x200 are likely thumbnails.
    /// </summary>
    public static bool IsThumbnailByDimensions(int? width, int? height)
    {
        if (width == null || height == null)
            return false; // Can't determine without dimensions

        return width <= 200 || height <= 200;
    }

    // ============================================================================
    // JSON HELPERS
    // ============================================================================

    /// <summary>
    /// Safely extracts a string value from a JSON element.
    /// </summary>
    public static string? TryGetString(JsonElement element, string property)
    {
        if (element.ValueKind == JsonValueKind.Undefined)
            return null;

        if (element.TryGetProperty(property, out var value) &&
            value.ValueKind != JsonValueKind.Null &&
            value.ValueKind != JsonValueKind.Undefined)
        {
            return value.GetString();
        }

        return null;
    }

    /// <summary>
    /// Safely extracts a string value from a nested JSON element.
    /// </summary>
    public static string? TryGetString(JsonElement element, string property, string nestedProperty)
    {
        if (element.ValueKind == JsonValueKind.Undefined)
            return null;

        if (!element.TryGetProperty(property, out var value))
            return null;

        if (value.ValueKind != JsonValueKind.Object)
            return null;

        return TryGetString(value, nestedProperty);
    }

    /// <summary>
    /// Safely extracts an integer value from a JSON element.
    /// </summary>
    public static int? TryGetInt(JsonElement element, string property)
    {
        if (element.ValueKind == JsonValueKind.Undefined)
            return null;

        if (element.TryGetProperty(property, out var value) &&
            value.ValueKind == JsonValueKind.Number)
        {
            return value.GetInt32();
        }

        return null;
    }

    /// <summary>
    /// Safely extracts a float value from a JSON element.
    /// </summary>
    public static float? TryGetFloat(JsonElement element, string property)
    {
        if (element.ValueKind == JsonValueKind.Undefined)
            return null;

        if (element.TryGetProperty(property, out var value))
        {
            if (value.ValueKind == JsonValueKind.Number)
                return (float)value.GetDouble();

            if (value.ValueKind == JsonValueKind.String &&
                float.TryParse(value.GetString(), out var floatValue))
            {
                return floatValue;
            }
        }

        return null;
    }

    /// <summary>
    /// Safely extracts a float value from a string property in a JSON element.
    /// </summary>
    public static float? TryGetFloatFromString(JsonElement element, string property)
    {
        var str = TryGetString(element, property);
        if (str != null && float.TryParse(str, out var floatValue))
        {
            return floatValue;
        }
        return null;
    }

    /// <summary>
    /// Safely extracts a DateTime value from a JSON element.
    /// </summary>
    public static DateTime? TryGetDateTime(JsonElement element, string property)
    {
        var str = TryGetString(element, property);
        if (str != null && DateTime.TryParse(str, out var dateTime))
        {
            return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        }
        return null;
    }

    // ============================================================================
    // PERSEVERANCE DIMENSION EXTRACTION
    // ============================================================================

    /// <summary>
    /// Extracts dimensions from Perseverance extended metadata.
    /// Tries multiple fields in order of reliability:
    /// 1. dimension field: "(width,height)"
    /// 2. subframeRect field: "(x,y,width,height)"
    /// </summary>
    public static (int? width, int? height) ExtractPerseveranceDimensions(JsonElement extended)
    {
        if (extended.ValueKind == JsonValueKind.Undefined)
            return (null, null);

        // Try dimension field first - most reliable for Perseverance
        var dimension = TryGetString(extended, "dimension");
        var parsed = ParseDimensionField(dimension);
        if (parsed.HasValue)
            return (parsed.Value.width, parsed.Value.height);

        // Fall back to subframeRect
        var subframeRect = TryGetString(extended, "subframeRect");
        var subframeParsed = ParseSubframeRect(subframeRect);
        if (subframeParsed.HasValue)
            return (subframeParsed.Value.width, subframeParsed.Value.height);

        return (null, null);
    }

    /// <summary>
    /// Extracts dimensions from Curiosity extended metadata.
    /// Uses subframe_rect field: "(x,y,width,height)"
    /// Falls back to inference from sample_type.
    /// </summary>
    public static (int? width, int? height) ExtractCuriosityDimensions(
        JsonElement extended,
        string? sampleType)
    {
        var subframeRect = TryGetString(extended, "subframe_rect");
        var parsed = ParseSubframeRect(subframeRect);
        if (parsed.HasValue)
            return (parsed.Value.width, parsed.Value.height);

        // Fall back to inference
        return InferDimensionsFromSampleType(sampleType);
    }

    // ============================================================================
    // REGEX PATTERNS
    // ============================================================================

    [GeneratedRegex(@"\((\d+),\s*(\d+),\s*(\d+),\s*(\d+)\)")]
    private static partial Regex SubframeRectRegex();

    [GeneratedRegex(@"\((\d+),\s*(\d+)\)")]
    private static partial Regex DimensionFieldRegex();
}
