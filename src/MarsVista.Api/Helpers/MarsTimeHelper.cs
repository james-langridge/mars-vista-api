using System.Text.RegularExpressions;

namespace MarsVista.Api.Helpers;

/// <summary>
/// Helper methods for parsing and working with Mars local solar time
/// </summary>
public static class MarsTimeHelper
{
    // Mars time format: "Sol-01646M15:18:15.866" or just "M15:18:15" for queries
    private static readonly Regex MarsTimeRegex = new(@"^M?(\d{1,2}):(\d{2}):(\d{2})(?:\.(\d+))?$", RegexOptions.Compiled);

    // Golden hour on Mars: approximately 1 hour after sunrise and 1 hour before sunset
    // Mars sunrise/sunset varies, but roughly: 06:00-07:00 and 18:00-19:00 local time
    private static readonly TimeSpan GoldenHourMorningStart = TimeSpan.FromHours(5.5);
    private static readonly TimeSpan GoldenHourMorningEnd = TimeSpan.FromHours(7.5);
    private static readonly TimeSpan GoldenHourEveningStart = TimeSpan.FromHours(17.5);
    private static readonly TimeSpan GoldenHourEveningEnd = TimeSpan.FromHours(19.5);

    /// <summary>
    /// Parse Mars time string to TimeSpan
    /// Supports formats: "M14:23:45", "14:23:45", "M14:23:45.866"
    /// </summary>
    /// <param name="marsTime">Mars time string</param>
    /// <param name="result">Parsed TimeSpan</param>
    /// <returns>True if parsing succeeded</returns>
    public static bool TryParseMarsTime(string? marsTime, out TimeSpan result)
    {
        result = TimeSpan.Zero;

        if (string.IsNullOrWhiteSpace(marsTime))
            return false;

        var match = MarsTimeRegex.Match(marsTime.Trim());
        if (!match.Success)
            return false;

        try
        {
            var hours = int.Parse(match.Groups[1].Value);
            var minutes = int.Parse(match.Groups[2].Value);
            var seconds = int.Parse(match.Groups[3].Value);
            var milliseconds = 0;

            if (match.Groups[4].Success)
            {
                // Parse fractional seconds
                var fractionStr = match.Groups[4].Value.PadRight(3, '0').Substring(0, 3);
                milliseconds = int.Parse(fractionStr);
            }

            // Validate ranges
            if (hours < 0 || hours > 23 || minutes < 0 || minutes > 59 || seconds < 0 || seconds > 59)
                return false;

            result = new TimeSpan(0, hours, minutes, seconds, milliseconds);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Extract time component from full Mars timestamp
    /// Format: "Sol-01646M15:18:15.866" -> TimeSpan(15, 18, 15, 866)
    /// </summary>
    /// <param name="marsTimestamp">Full Mars timestamp</param>
    /// <param name="result">Extracted time component</param>
    /// <returns>True if extraction succeeded</returns>
    public static bool TryExtractTimeFromTimestamp(string? marsTimestamp, out TimeSpan result)
    {
        result = TimeSpan.Zero;

        if (string.IsNullOrWhiteSpace(marsTimestamp))
            return false;

        // Find the 'M' marker which precedes the time
        var mIndex = marsTimestamp.IndexOf('M');
        if (mIndex == -1)
            return false;

        var timePartial = marsTimestamp.Substring(mIndex);
        return TryParseMarsTime(timePartial, out result);
    }

    /// <summary>
    /// Check if a Mars time falls within golden hour
    /// Golden hour: ~1 hour after sunrise (06:00-07:00) and ~1 hour before sunset (18:00-19:00)
    /// </summary>
    /// <param name="marsTime">Mars local time</param>
    /// <returns>True if within golden hour</returns>
    public static bool IsGoldenHour(TimeSpan marsTime)
    {
        return (marsTime >= GoldenHourMorningStart && marsTime <= GoldenHourMorningEnd) ||
               (marsTime >= GoldenHourEveningStart && marsTime <= GoldenHourEveningEnd);
    }

    /// <summary>
    /// Check if a full Mars timestamp is within golden hour
    /// </summary>
    /// <param name="marsTimestamp">Full Mars timestamp (e.g., "Sol-01646M15:18:15.866")</param>
    /// <returns>True if within golden hour</returns>
    public static bool IsGoldenHour(string? marsTimestamp)
    {
        if (TryExtractTimeFromTimestamp(marsTimestamp, out var time))
            return IsGoldenHour(time);
        return false;
    }

    /// <summary>
    /// Determine lighting conditions based on Mars local time
    /// </summary>
    /// <param name="marsTime">Mars local time</param>
    /// <returns>Lighting condition string (golden_hour, midday, morning, afternoon, evening)</returns>
    public static string GetLightingConditions(TimeSpan marsTime)
    {
        if (IsGoldenHour(marsTime))
            return "golden_hour";

        var hours = marsTime.Hours;

        if (hours >= 11 && hours <= 13)
            return "midday";
        if (hours >= 6 && hours < 11)
            return "morning";
        if (hours >= 13 && hours < 17)
            return "afternoon";
        if (hours >= 17 && hours < 20)
            return "evening";

        return "night";
    }

    /// <summary>
    /// Determine lighting conditions from full Mars timestamp
    /// </summary>
    /// <param name="marsTimestamp">Full Mars timestamp</param>
    /// <returns>Lighting condition string</returns>
    public static string? GetLightingConditions(string? marsTimestamp)
    {
        if (TryExtractTimeFromTimestamp(marsTimestamp, out var time))
            return GetLightingConditions(time);
        return null;
    }

    /// <summary>
    /// Parse aspect ratio string to width/height tuple
    /// Format: "16:9", "4:3", "1:1", etc.
    /// </summary>
    /// <param name="aspectRatio">Aspect ratio string</param>
    /// <param name="result">Parsed (width, height) tuple</param>
    /// <returns>True if parsing succeeded</returns>
    public static bool TryParseAspectRatio(string? aspectRatio, out (int Width, int Height) result)
    {
        result = (0, 0);

        if (string.IsNullOrWhiteSpace(aspectRatio))
            return false;

        var parts = aspectRatio.Split(':');
        if (parts.Length != 2)
            return false;

        if (int.TryParse(parts[0].Trim(), out var width) &&
            int.TryParse(parts[1].Trim(), out var height) &&
            width > 0 && height > 0)
        {
            result = (width, height);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Check if image dimensions match the specified aspect ratio (with tolerance)
    /// </summary>
    /// <param name="width">Image width</param>
    /// <param name="height">Image height</param>
    /// <param name="aspectRatio">Target aspect ratio (width, height)</param>
    /// <param name="tolerance">Tolerance for matching (default 0.05 = 5%)</param>
    /// <returns>True if dimensions match aspect ratio</returns>
    public static bool MatchesAspectRatio(int width, int height, (int Width, int Height) aspectRatio, double tolerance = 0.05)
    {
        if (width <= 0 || height <= 0)
            return false;

        var imageRatio = (double)width / height;
        var targetRatio = (double)aspectRatio.Width / aspectRatio.Height;

        var difference = Math.Abs(imageRatio - targetRatio);
        var allowedDifference = targetRatio * tolerance;

        return difference <= allowedDifference;
    }

    /// <summary>
    /// Parse XYZ coordinate string to individual components
    /// Format: "(35.4362,22.5714,-9.46445)" or "35.4362,22.5714,-9.46445"
    /// </summary>
    /// <param name="xyz">XYZ coordinate string</param>
    /// <param name="result">Parsed (x, y, z) tuple</param>
    /// <returns>True if parsing succeeded</returns>
    public static bool TryParseXYZ(string? xyz, out (float X, float Y, float Z) result)
    {
        result = (0, 0, 0);

        if (string.IsNullOrWhiteSpace(xyz))
            return false;

        // Remove parentheses if present
        var cleaned = xyz.Trim().Trim('(', ')');
        var parts = cleaned.Split(',');

        if (parts.Length != 3)
            return false;

        if (float.TryParse(parts[0].Trim(), out var x) &&
            float.TryParse(parts[1].Trim(), out var y) &&
            float.TryParse(parts[2].Trim(), out var z))
        {
            result = (x, y, z);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Format TimeSpan as Mars time string
    /// </summary>
    /// <param name="time">TimeSpan to format</param>
    /// <param name="includePrefix">Include 'M' prefix</param>
    /// <returns>Formatted Mars time (e.g., "M14:23:45")</returns>
    public static string FormatMarsTime(TimeSpan time, bool includePrefix = true)
    {
        var prefix = includePrefix ? "M" : "";
        return $"{prefix}{time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
    }
}
