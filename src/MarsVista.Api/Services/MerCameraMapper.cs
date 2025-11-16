namespace MarsVista.Api.Services;

/// <summary>
/// Maps PDS instrument names (PANCAM_LEFT, NAVCAM_RIGHT) to database camera names (PANCAM, NAVCAM)
/// MER rovers have LEFT/RIGHT camera variants that map to generic camera entities in our database
/// </summary>
public static class MerCameraMapper
{
    /// <summary>
    /// PDS to database camera name mapping
    /// PDS uses specific instrument IDs (PANCAM_LEFT, PANCAM_RIGHT)
    /// Database uses generic camera names (PANCAM, NAVCAM, etc.)
    /// </summary>
    private static readonly Dictionary<string, string> CameraMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        // Panoramic Camera (PANCAM)
        { "PANCAM_LEFT", "PANCAM" },
        { "PANCAM_RIGHT", "PANCAM" },
        { "PANCAM", "PANCAM" },  // Fallback for generic name

        // Navigation Camera (NAVCAM)
        { "NAVCAM_LEFT", "NAVCAM" },
        { "NAVCAM_RIGHT", "NAVCAM" },
        { "NAVCAM", "NAVCAM" },  // Fallback

        // Front Hazard Avoidance Camera (FHAZ)
        { "FHAZ_LEFT", "FHAZ" },
        { "FHAZ_RIGHT", "FHAZ" },
        { "FHAZ", "FHAZ" },  // Fallback
        { "FRONT_HAZCAM_LEFT", "FHAZ" },  // MER PDS format
        { "FRONT_HAZCAM_RIGHT", "FHAZ" },

        // Rear Hazard Avoidance Camera (RHAZ)
        { "RHAZ_LEFT", "RHAZ" },
        { "RHAZ_RIGHT", "RHAZ" },
        { "RHAZ", "RHAZ" },  // Fallback
        { "REAR_HAZCAM_LEFT", "RHAZ" },  // MER PDS format
        { "REAR_HAZCAM_RIGHT", "RHAZ" },

        // Microscopic Imager (MI)
        // Database uses "MINITES" for this camera
        { "MI", "MINITES" },
        { "MICROSCOPIC_IMAGER", "MINITES" },

        // Descent/Entry Camera
        // Various possible names from PDS
        { "DESCENT", "ENTRY" },
        { "DESCENT_IMAGER", "ENTRY" },
        { "DESCAM", "ENTRY" },  // MER PDS format
        { "EDL", "ENTRY" },  // Entry, Descent, Landing
        { "ENTRY", "ENTRY" }
    };

    /// <summary>
    /// Map PDS instrument name to database camera name
    /// </summary>
    /// <param name="pdsInstrumentName">Instrument ID from PDS index file</param>
    /// <returns>Database camera name, or original name if no mapping found</returns>
    public static string MapToDbName(string pdsInstrumentName)
    {
        if (string.IsNullOrWhiteSpace(pdsInstrumentName))
            return pdsInstrumentName;

        var trimmed = pdsInstrumentName.Trim();

        return CameraMapping.TryGetValue(trimmed, out var dbName)
            ? dbName
            : trimmed; // Return original if no mapping (for unknown cameras)
    }

    /// <summary>
    /// Check if a PDS instrument name has a known mapping
    /// </summary>
    public static bool HasMapping(string pdsInstrumentName)
    {
        if (string.IsNullOrWhiteSpace(pdsInstrumentName))
            return false;

        return CameraMapping.ContainsKey(pdsInstrumentName.Trim());
    }

    /// <summary>
    /// Get all supported PDS instrument names
    /// </summary>
    public static IReadOnlyCollection<string> GetSupportedInstruments()
    {
        return CameraMapping.Keys;
    }
}
