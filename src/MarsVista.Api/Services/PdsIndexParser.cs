using System.Globalization;

namespace MarsVista.Api.Services;

/// <summary>
/// Parser for PDS (Planetary Data System) tab-delimited index files
/// Handles large files (300+ MB) via streaming and parses 55 metadata fields per row
/// </summary>
public class PdsIndexParser
{
    private readonly ILogger<PdsIndexParser> _logger;

    public PdsIndexParser(ILogger<PdsIndexParser> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parse a single tab-delimited line into a PdsIndexRow
    /// </summary>
    /// <param name="line">Tab-delimited line from edrindex.tab</param>
    /// <param name="lineNumber">Line number for error reporting</param>
    /// <returns>Parsed row or null if malformed</returns>
    public PdsIndexRow? ParseRow(string line, int lineNumber)
    {
        try
        {
            var fields = line.Split('\t');

            // MER index files have variable field counts:
            // - PANCAM, NAVCAM, HAZCAM, MI: 59 fields
            // - DESCENT: 52 fields
            // Minimum required: 51 fields (everything up to FlatFieldCorrection)
            if (fields.Length < 51)
            {
                _logger.LogWarning(
                    "Malformed row at line {LineNumber}: expected at least 51 fields, got {Count}",
                    lineNumber, fields.Length);
                return null;
            }

            return new PdsIndexRow
            {
                // Core identification (fields 0-8)
                VolumeId = Clean(fields[0]),
                DataSetId = Clean(fields[1]),
                InstrumentHostId = Clean(fields[2]),
                InstrumentId = Clean(fields[3]),
                PathName = Clean(fields[4]),
                FileName = Clean(fields[5]),
                ReleaseId = Clean(fields[6]),
                ProductId = Clean(fields[7]),
                ProductCreationTime = ParseDateTime(fields[8]),

                // Target and mission (fields 9-10)
                TargetName = Clean(fields[9]),
                MissionPhaseName = Clean(fields[10]),

                // Time data (fields 11-20)
                Sol = ParseInt(fields[11]) ?? 0,
                StartTime = ParseDateTime(fields[12]),
                StopTime = ParseDateTime(fields[13]),
                EarthReceivedStart = ParseDateTime(fields[14]),
                EarthReceivedStop = ParseDateTime(fields[15]),
                SpacecraftClockStart = Clean(fields[16]),
                SpacecraftClockStop = Clean(fields[17]),
                SequenceId = Clean(fields[18]),
                ObservationId = Clean(fields[19]),
                LocalTrueSolarTime = Clean(fields[20]),

                // Image dimensions (fields 21-24)
                Lines = ParseInt(fields[21]),
                LineSamples = ParseInt(fields[22]),
                FirstLine = ParseInt(fields[23]),
                FirstLineSample = ParseInt(fields[24]),

                // Instrument configuration (fields 25-30)
                InstrumentSerialNum = Clean(fields[25]),
                InstrumentModeId = Clean(fields[26]),
                InstCmprsRatio = ParseFloat(fields[27]),
                InstCmprsMode = Clean(fields[28]),
                InstCmprsFilter = Clean(fields[29]),

                // Image metadata (fields 30-38)
                ImageId = Clean(fields[30]),
                ImageType = Clean(fields[31]),
                ExposureDuration = ParseFloat(fields[32]),
                ErrorPixels = ParseInt(fields[33]),
                FilterName = Clean(fields[34]),
                FilterNumber = ParseInt(fields[35]),
                FrameId = Clean(fields[36]),
                FrameType = Clean(fields[37]),

                // Camera orientation (fields 38-44)
                AzimuthFov = ParseFloat(fields[38]),
                ElevationFov = ParseFloat(fields[39]),
                SiteInstrumentAzimuth = ParseFloat(fields[40]),
                SiteInstrumentElevation = ParseFloat(fields[41]),
                RoverInstrumentAzimuth = ParseFloat(fields[42]),
                RoverInstrumentElevation = ParseFloat(fields[43]),

                // Solar position (fields 44-46)
                SolarAzimuth = ParseFloat(fields[44]),
                SolarElevation = ParseFloat(fields[45]),
                SolarLongitude = ParseFloat(fields[46]),

                // Processing metadata (fields 47-50)
                ApplicationProcessId = Clean(fields[47]),
                ReferenceCoordSystem = Clean(fields[48]),
                TelemetrySourceName = Clean(fields[49]),
                RoverMotionCounter = ParseInt(fields[50]),

                // Calibration flags (fields 51-54)
                // DESCENT camera files only have 52 fields, so safely access optional fields
                FlatFieldCorrection = Clean(fields[51]),
                ShutterEffectCorrection = fields.Length > 52 ? Clean(fields[52]) : "",
                PixelAveragingHeight = fields.Length > 53 ? ParseInt(fields[53]) : null,
                PixelAveragingWidth = fields.Length > 54 ? ParseInt(fields[54]) : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing line {LineNumber}", lineNumber);
            return null;
        }
    }

    /// <summary>
    /// Stream parse a PDS index file, yielding rows asynchronously
    /// Efficient for large files (300+ MB)
    /// </summary>
    public async IAsyncEnumerable<PdsIndexRow> ParseStreamAsync(
        Stream stream,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream);
        var lineNumber = 0;

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var row = ParseRow(line, lineNumber);
            if (row != null)
            {
                yield return row;
            }
        }
    }

    // ============================================================================
    // PURE HELPER FUNCTIONS (No side effects)
    // ============================================================================

    /// <summary>
    /// Clean PDS field: trim whitespace and remove surrounding quotes
    /// PDS fields come as: "value with spaces  " → "value with spaces"
    /// </summary>
    private static string Clean(string field)
    {
        if (string.IsNullOrEmpty(field))
            return "";

        var trimmed = field.Trim();

        // Remove surrounding quotes if present
        if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[^1] == '"')
        {
            trimmed = trimmed[1..^1];
        }

        return trimmed.Trim();
    }

    /// <summary>
    /// Parse integer from PDS field, handling whitespace and errors gracefully
    /// </summary>
    private static int? ParseInt(string field)
    {
        var cleaned = Clean(field);
        if (string.IsNullOrEmpty(cleaned))
            return null;

        if (int.TryParse(cleaned, out var value))
            return value;

        return null;
    }

    /// <summary>
    /// Parse float from PDS field, handling whitespace and errors gracefully
    /// </summary>
    private static float? ParseFloat(string field)
    {
        var cleaned = Clean(field);
        if (string.IsNullOrEmpty(cleaned))
            return null;

        if (float.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            return value;

        return null;
    }

    /// <summary>
    /// Parse DateTime from PDS field (ISO 8601 format: 2004-01-25T07:18:28Z)
    /// Always treats as UTC
    /// </summary>
    private static DateTime? ParseDateTime(string field)
    {
        var cleaned = Clean(field);
        if (string.IsNullOrEmpty(cleaned))
            return null;

        if (DateTime.TryParse(cleaned, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var value))
        {
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        return null;
    }
}
