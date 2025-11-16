namespace MarsVista.Api.Services;

/// <summary>
/// Represents a single row from a PDS tab-delimited index file
/// Contains all 55 metadata fields from MER (Opportunity/Spirit) edrindex.tab files
/// </summary>
public record PdsIndexRow
{
    // Core identification
    public string VolumeId { get; init; } = "";
    public string DataSetId { get; init; } = "";
    public string InstrumentHostId { get; init; } = "";  // "MER1" or "MER2"
    public string InstrumentId { get; init; } = "";      // "PANCAM_LEFT", "NAVCAM_RIGHT", etc.
    public string PathName { get; init; } = "";
    public string FileName { get; init; } = "";
    public string ReleaseId { get; init; } = "";
    public string ProductId { get; init; } = "";         // Unique NASA ID
    public DateTime? ProductCreationTime { get; init; }

    // Target and mission
    public string TargetName { get; init; } = "";
    public string MissionPhaseName { get; init; } = "";

    // Time data (⭐ Essential fields)
    public int Sol { get; init; }                        // PLANET_DAY_NUMBER
    public DateTime? StartTime { get; init; }            // Earth date/time (UTC)
    public DateTime? StopTime { get; init; }
    public DateTime? EarthReceivedStart { get; init; }
    public DateTime? EarthReceivedStop { get; init; }
    public string SpacecraftClockStart { get; init; } = "";
    public string SpacecraftClockStop { get; init; } = "";
    public string SequenceId { get; init; } = "";
    public string ObservationId { get; init; } = "";
    public string LocalTrueSolarTime { get; init; } = ""; // Mars local time

    // Image dimensions (⭐ Essential fields)
    public int? Lines { get; init; }                      // Height
    public int? LineSamples { get; init; }                // Width
    public int? FirstLine { get; init; }
    public int? FirstLineSample { get; init; }

    // Instrument configuration
    public string InstrumentSerialNum { get; init; } = "";
    public string InstrumentModeId { get; init; } = "";
    public float? InstCmprsRatio { get; init; }
    public string InstCmprsMode { get; init; } = "";
    public string InstCmprsFilter { get; init; } = "";

    // Image metadata
    public string ImageId { get; init; } = "";
    public string ImageType { get; init; } = "";
    public float? ExposureDuration { get; init; }        // ⭐ Exposure time (ms)
    public int? ErrorPixels { get; init; }
    public string FilterName { get; init; } = "";        // ⭐ Camera filter
    public int? FilterNumber { get; init; }
    public string FrameId { get; init; } = "";
    public string FrameType { get; init; } = "";

    // Camera orientation (⭐ Essential telemetry)
    public float? AzimuthFov { get; init; }
    public float? ElevationFov { get; init; }
    public float? SiteInstrumentAzimuth { get; init; }   // ⭐ Mast azimuth
    public float? SiteInstrumentElevation { get; init; } // ⭐ Mast elevation
    public float? RoverInstrumentAzimuth { get; init; }
    public float? RoverInstrumentElevation { get; init; }

    // Solar position (⭐ Valuable metadata)
    public float? SolarAzimuth { get; init; }
    public float? SolarElevation { get; init; }
    public float? SolarLongitude { get; init; }

    // Processing metadata
    public string ApplicationProcessId { get; init; } = "";
    public string ReferenceCoordSystem { get; init; } = "";
    public string TelemetrySourceName { get; init; } = "";
    public int? RoverMotionCounter { get; init; }        // ⭐ Odometry

    // Calibration flags
    public string FlatFieldCorrection { get; init; } = "";
    public string ShutterEffectCorrection { get; init; } = "";
    public int? PixelAveragingHeight { get; init; }
    public int? PixelAveragingWidth { get; init; }
}
