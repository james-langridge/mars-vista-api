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
            // - PANCAM, NAVCAM, HAZCAM, MI: 59 fields (standard format)
            // - DESCENT: 52 fields (missing PathName field - different structure!)
            // Detect DESCENT format by field count and instrument
            var isDescentFormat = fields.Length == 52 &&
                                  Clean(fields[3]).Contains("DESCAM", StringComparison.OrdinalIgnoreCase);

            // DESCENT has different field structure (PathName AND FileName are missing)
            // Standard: VolumeId, DataSetId, InstrumentHostId, InstrumentId, PathName, FileName, ReleaseId, ProductId...
            // DESCENT:  VolumeId, DataSetId, InstrumentHostId, InstrumentId, ReleaseId, ProductId, ProductCreationTime...
            var offset = isDescentFormat ? -2 : 0;  // Shift all fields after InstrumentId by -2 for DESCENT

            if (fields.Length < 51 && !isDescentFormat)
            {
                _logger.LogWarning(
                    "Malformed row at line {LineNumber}: expected at least 51 fields, got {Count}",
                    lineNumber, fields.Length);
                return null;
            }

            return new PdsIndexRow
            {
                // Core identification (fields 0-3 same for all)
                VolumeId = Clean(fields[0]),
                DataSetId = Clean(fields[1]),
                InstrumentHostId = Clean(fields[2]),
                InstrumentId = Clean(fields[3]),

                // DESCENT is missing PathName AND FileName - use empty strings
                PathName = isDescentFormat ? "" : Clean(fields[4]),
                FileName = isDescentFormat ? "" : Clean(fields[5]),
                ReleaseId = Clean(fields[6 + offset]),
                ProductId = Clean(fields[7 + offset]),
                ProductCreationTime = ParseDateTime(fields[8 + offset]),

                // Target and mission
                TargetName = Clean(fields[9 + offset]),
                MissionPhaseName = Clean(fields[10 + offset]),

                // Time data
                Sol = ParseInt(fields[11 + offset]) ?? 0,
                StartTime = ParseDateTime(fields[12 + offset]),
                StopTime = ParseDateTime(fields[13 + offset]),
                EarthReceivedStart = ParseDateTime(fields[14 + offset]),
                EarthReceivedStop = ParseDateTime(fields[15 + offset]),
                SpacecraftClockStart = Clean(fields[16 + offset]),
                SpacecraftClockStop = Clean(fields[17 + offset]),
                SequenceId = Clean(fields[18 + offset]),
                ObservationId = Clean(fields[19 + offset]),
                LocalTrueSolarTime = Clean(fields[20 + offset]),

                // Image dimensions
                Lines = ParseInt(fields[21 + offset]),
                LineSamples = ParseInt(fields[22 + offset]),
                FirstLine = ParseInt(fields[23 + offset]),
                FirstLineSample = ParseInt(fields[24 + offset]),

                // Instrument configuration
                InstrumentSerialNum = Clean(fields[25 + offset]),
                InstrumentModeId = Clean(fields[26 + offset]),
                InstCmprsRatio = ParseFloat(fields[27 + offset]),
                InstCmprsMode = Clean(fields[28 + offset]),
                InstCmprsFilter = Clean(fields[29 + offset]),

                // Image metadata
                ImageId = Clean(fields[30 + offset]),
                ImageType = Clean(fields[31 + offset]),
                ExposureDuration = ParseFloat(fields[32 + offset]),
                ErrorPixels = ParseInt(fields[33 + offset]),
                FilterName = Clean(fields[34 + offset]),
                FilterNumber = ParseInt(fields[35 + offset]),
                FrameId = Clean(fields[36 + offset]),
                FrameType = Clean(fields[37 + offset]),

                // Camera orientation
                AzimuthFov = ParseFloat(fields[38 + offset]),
                ElevationFov = ParseFloat(fields[39 + offset]),
                SiteInstrumentAzimuth = ParseFloat(fields[40 + offset]),
                SiteInstrumentElevation = ParseFloat(fields[41 + offset]),
                RoverInstrumentAzimuth = ParseFloat(fields[42 + offset]),
                RoverInstrumentElevation = ParseFloat(fields[43 + offset]),

                // Solar position
                SolarAzimuth = ParseFloat(fields[44 + offset]),
                SolarElevation = ParseFloat(fields[45 + offset]),
                SolarLongitude = ParseFloat(fields[46 + offset]),

                // Processing metadata
                ApplicationProcessId = Clean(fields[47 + offset]),
                ReferenceCoordSystem = Clean(fields[48 + offset]),
                TelemetrySourceName = Clean(fields[49 + offset]),
                RoverMotionCounter = ParseInt(fields[50 + offset]),

                // Calibration flags (last 4 fields)
                // DESCENT (52 fields): fields 48-51 (with offset -1)
                // Standard (59 fields): fields 51-54 (with offset 0)
                FlatFieldCorrection = Clean(fields[51 + offset]),
                ShutterEffectCorrection = (fields.Length > 52 + offset) ? Clean(fields[52 + offset]) : "",
                PixelAveragingHeight = (fields.Length > 53 + offset) ? ParseInt(fields[53 + offset]) : null,
                PixelAveragingWidth = (fields.Length > 54 + offset) ? ParseInt(fields[54 + offset]) : null
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
