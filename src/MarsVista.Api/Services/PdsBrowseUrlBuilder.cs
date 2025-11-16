namespace MarsVista.Api.Services;

/// <summary>
/// Builds browse JPG URLs from PDS index file data
/// Converts data paths to browse paths and appends .jpg extension
/// </summary>
public static class PdsBrowseUrlBuilder
{
    private const string BaseUrl = "https://planetarydata.jpl.nasa.gov/img/data/mer";

    /// <summary>
    /// Build browse JPG URL from PDS index file data
    /// </summary>
    /// <param name="rover">Rover name (e.g., "opportunity", "spirit")</param>
    /// <param name="pathName">Path from index file (e.g., "/mer1po_0xxx/data/sol0001/edr/")</param>
    /// <param name="fileName">Filename from index file (e.g., "1p128287181eff0000p2303l2m1.img")</param>
    /// <param name="sol">Sol number for path validation</param>
    /// <returns>Complete browse JPG URL</returns>
    /// <example>
    /// Input:
    ///   rover = "opportunity"
    ///   pathName = "/mer1po_0xxx/data/sol0001/edr/"
    ///   fileName = "1p128287181eff0000p2303l2m1.img"
    ///   sol = 1
    ///
    /// Output:
    ///   https://planetarydata.jpl.nasa.gov/img/data/mer/opportunity/mer1po_0xxx/browse/sol0001/edr/1p128287181eff0000p2303l2m1.img.jpg
    /// </example>
    public static string BuildBrowseUrl(string rover, string pathName, string fileName, int sol)
    {
        // Convert data path to browse path
        // /mer1po_0xxx/data/sol0001/edr/ → /mer1po_0xxx/browse/sol0001/edr/
        var browsePath = pathName.Replace("/data/", "/browse/");

        // Ensure sol number is 4-digit padded in path
        // Some index files may have inconsistent formatting
        var solPattern = $"sol{sol}";
        var solPadded = $"sol{sol:D4}";

        if (browsePath.Contains(solPattern) && !browsePath.Contains(solPadded))
        {
            browsePath = browsePath.Replace(solPattern, solPadded);
        }

        // Construct full URL
        // Note: pathName usually includes leading slash, but handle both cases
        var path = browsePath.TrimStart('/');
        var fullUrl = $"{BaseUrl}/{rover.ToLower()}/{path}{fileName}.jpg";

        return fullUrl;
    }

    /// <summary>
    /// Extract volume name from path
    /// </summary>
    /// <param name="pathName">Path from index file</param>
    /// <returns>Volume name (e.g., "mer1po_0xxx")</returns>
    /// <example>
    /// "/mer1po_0xxx/data/sol0001/edr/" → "mer1po_0xxx"
    /// </example>
    public static string? ExtractVolumeName(string pathName)
    {
        if (string.IsNullOrWhiteSpace(pathName))
            return null;

        // Volume name is first directory in path
        var parts = pathName.Trim('/').Split('/');
        return parts.Length > 0 ? parts[0] : null;
    }

    /// <summary>
    /// Build index file URL for a specific volume
    /// </summary>
    /// <param name="rover">Rover name ("opportunity" or "spirit")</param>
    /// <param name="volumeName">Volume name (e.g., "mer1po_0xxx")</param>
    /// <returns>URL to edrindex.tab file</returns>
    public static string BuildIndexUrl(string rover, string volumeName)
    {
        return $"{BaseUrl}/{rover.ToLower()}/{volumeName}/index/edrindex.tab";
    }

    /// <summary>
    /// Get all volume names for a rover
    /// </summary>
    /// <param name="rover">Rover name ("opportunity" or "spirit")</param>
    /// <returns>List of volume names for that rover</returns>
    public static List<string> GetVolumesForRover(string rover)
    {
        var roverLower = rover.ToLower();

        return roverLower switch
        {
            "opportunity" => new List<string>
            {
                "mer1po_0xxx",  // PANCAM
                "mer1no_0xxx",  // NAVCAM
                "mer1ho_0xxx",  // HAZCAM
                "mer1mo_0xxx",  // MI (Microscopic Imager)
                "mer1do_0xxx"   // DESCENT
            },
            "spirit" => new List<string>
            {
                "mer2po_0xxx",  // PANCAM
                "mer2no_0xxx",  // NAVCAM
                "mer2ho_0xxx",  // HAZCAM
                "mer2mo_0xxx",  // MI
                "mer2do_0xxx"   // DESCENT
            },
            _ => new List<string>()
        };
    }

    /// <summary>
    /// Decode camera type from volume name
    /// </summary>
    /// <param name="volumeName">Volume name (e.g., "mer1po_0xxx")</param>
    /// <returns>Camera type code (p=PANCAM, n=NAVCAM, h=HAZCAM, m=MI, d=DESCENT)</returns>
    public static string? GetCameraCodeFromVolume(string volumeName)
    {
        if (string.IsNullOrWhiteSpace(volumeName) || volumeName.Length < 5)
            return null;

        // Format: mer{rover}{camera}{suffix}_0xxx
        // mer1po_0xxx -> p (PANCAM)
        // mer1no_0xxx -> n (NAVCAM)
        return volumeName[4].ToString().ToLower();
    }
}
