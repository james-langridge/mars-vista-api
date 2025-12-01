using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MarsVista.Core.Data;
using MarsVista.Core.Entities;

namespace MarsVista.Api.Services;

/// <summary>
/// Imports official NASA rover waypoint data from the Planetary Data System (PDS).
/// Source: https://pds-geosciences.wustl.edu/m2020/urn-nasa-pds-mars2020_rover_places/data_localizations/
/// </summary>
public class WaypointImportService
{
    private readonly MarsVistaDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly ILogger<WaypointImportService> _logger;

    // PDS localization data URLs by rover
    private static readonly Dictionary<string, string> PdsUrls = new()
    {
        ["perseverance"] = "https://pds-geosciences.wustl.edu/m2020/urn-nasa-pds-mars2020_rover_places/data_localizations/best_tactical.csv"
    };

    public WaypointImportService(
        MarsVistaDbContext context,
        HttpClient httpClient,
        ILogger<WaypointImportService> logger)
    {
        _context = context;
        _httpClient = httpClient;
        _logger = logger;
    }

    public record ImportResult(
        string Rover,
        int TotalRows,
        int Imported,
        int Updated,
        int Skipped,
        float TotalDistanceKm,
        int MaxSol);

    /// <summary>
    /// Import waypoints for a specific rover from NASA PDS
    /// </summary>
    public async Task<ImportResult> ImportWaypointsAsync(string roverName, CancellationToken cancellationToken = default)
    {
        var roverLower = roverName.ToLowerInvariant();

        if (!PdsUrls.TryGetValue(roverLower, out var pdsUrl))
        {
            throw new ArgumentException($"No PDS waypoint data available for rover: {roverName}");
        }

        // Get rover ID
        var rover = await _context.Rovers
            .FirstOrDefaultAsync(r => r.Name.ToLower() == roverLower, cancellationToken);

        if (rover == null)
        {
            throw new ArgumentException($"Rover not found in database: {roverName}");
        }

        _logger.LogInformation("Fetching waypoint data for {Rover} from {Url}", roverName, pdsUrl);

        // Fetch CSV from PDS
        var csvContent = await _httpClient.GetStringAsync(pdsUrl, cancellationToken);
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2)
        {
            throw new InvalidOperationException("PDS CSV file appears to be empty or malformed");
        }

        // Parse header to get column indices
        var header = lines[0].Split(',');
        var columnIndices = new Dictionary<string, int>();
        for (int i = 0; i < header.Length; i++)
        {
            columnIndices[header[i].Trim().ToLowerInvariant()] = i;
        }

        // Validate required columns
        var requiredColumns = new[] { "frame", "site", "drive", "landing_x", "landing_y", "landing_z", "sol" };
        foreach (var col in requiredColumns)
        {
            if (!columnIndices.ContainsKey(col))
            {
                throw new InvalidOperationException($"PDS CSV missing required column: {col}");
            }
        }

        // Parse waypoints
        var waypoints = new List<RoverWaypoint>();
        var totalRows = lines.Length - 1; // Exclude header

        for (int i = 1; i < lines.Length; i++)
        {
            var fields = lines[i].Split(',');
            if (fields.Length < header.Length)
            {
                continue; // Skip malformed rows
            }

            var waypoint = ParseWaypoint(fields, columnIndices, rover.Id);
            if (waypoint != null)
            {
                waypoints.Add(waypoint);
            }
        }

        _logger.LogInformation("Parsed {Count} waypoints from {TotalRows} rows", waypoints.Count, totalRows);

        // Upsert waypoints
        var imported = 0;
        var updated = 0;
        var skipped = 0;

        foreach (var waypoint in waypoints)
        {
            var existing = await _context.RoverWaypoints
                .FirstOrDefaultAsync(w =>
                    w.RoverId == waypoint.RoverId &&
                    w.Site == waypoint.Site &&
                    w.Drive == waypoint.Drive,
                    cancellationToken);

            if (existing != null)
            {
                // Update if coordinates changed
                if (existing.LandingX != waypoint.LandingX ||
                    existing.LandingY != waypoint.LandingY ||
                    existing.LandingZ != waypoint.LandingZ)
                {
                    existing.LandingX = waypoint.LandingX;
                    existing.LandingY = waypoint.LandingY;
                    existing.LandingZ = waypoint.LandingZ;
                    existing.Latitude = waypoint.Latitude;
                    existing.Longitude = waypoint.Longitude;
                    existing.Elevation = waypoint.Elevation;
                    existing.Sol = waypoint.Sol;
                    existing.UpdatedAt = DateTime.UtcNow;
                    updated++;
                }
                else
                {
                    skipped++;
                }
            }
            else
            {
                _context.RoverWaypoints.Add(waypoint);
                imported++;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Calculate total distance
        var orderedWaypoints = waypoints
            .OrderBy(w => w.Sol ?? 0)
            .ThenBy(w => w.Site)
            .ThenBy(w => w.Drive ?? 0)
            .ToList();

        float totalDistance = 0;
        for (int i = 1; i < orderedWaypoints.Count; i++)
        {
            var prev = orderedWaypoints[i - 1];
            var curr = orderedWaypoints[i];
            totalDistance += MathF.Sqrt(
                MathF.Pow(curr.LandingX - prev.LandingX, 2) +
                MathF.Pow(curr.LandingY - prev.LandingY, 2) +
                MathF.Pow(curr.LandingZ - prev.LandingZ, 2));
        }

        var maxSol = waypoints.Where(w => w.Sol.HasValue).Max(w => w.Sol) ?? 0;

        _logger.LogInformation(
            "Import complete for {Rover}: {Imported} new, {Updated} updated, {Skipped} unchanged. Total distance: {Distance:F2} km through Sol {Sol}",
            roverName, imported, updated, skipped, totalDistance / 1000, maxSol);

        return new ImportResult(
            roverName,
            totalRows,
            imported,
            updated,
            skipped,
            totalDistance / 1000,
            maxSol);
    }

    private RoverWaypoint? ParseWaypoint(string[] fields, Dictionary<string, int> indices, int roverId)
    {
        try
        {
            var frame = fields[indices["frame"]].Trim();
            var siteStr = fields[indices["site"]].Trim();
            var driveStr = fields[indices["drive"]].Trim();
            var landingXStr = fields[indices["landing_x"]].Trim();
            var landingYStr = fields[indices["landing_y"]].Trim();
            var landingZStr = fields[indices["landing_z"]].Trim();
            var solStr = fields[indices["sol"]].Trim();

            if (!int.TryParse(siteStr, out var site))
                return null;

            if (!float.TryParse(landingXStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var landingX))
                return null;
            if (!float.TryParse(landingYStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var landingY))
                return null;
            if (!float.TryParse(landingZStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var landingZ))
                return null;

            int? drive = null;
            if (!string.IsNullOrEmpty(driveStr) && driveStr != "-1" && int.TryParse(driveStr, out var d))
            {
                drive = d;
            }

            int? sol = null;
            if (!string.IsNullOrEmpty(solStr) && solStr != "-1" && int.TryParse(solStr, out var s))
            {
                sol = s;
            }

            // Optional: latitude, longitude, elevation
            double? latitude = null;
            double? longitude = null;
            float? elevation = null;

            if (indices.TryGetValue("planetocentric_latitude", out var latIdx) &&
                double.TryParse(fields[latIdx], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat))
            {
                latitude = lat;
            }

            if (indices.TryGetValue("longitude", out var lonIdx) &&
                double.TryParse(fields[lonIdx], NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
            {
                longitude = lon;
            }

            if (indices.TryGetValue("elevation", out var elevIdx) &&
                float.TryParse(fields[elevIdx], NumberStyles.Float, CultureInfo.InvariantCulture, out var elev))
            {
                elevation = elev;
            }

            return new RoverWaypoint
            {
                RoverId = roverId,
                Frame = frame,
                Site = site,
                Drive = drive,
                Sol = sol,
                LandingX = landingX,
                LandingY = landingY,
                LandingZ = landingZ,
                Latitude = latitude,
                Longitude = longitude,
                Elevation = elevation,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
        catch (Exception)
        {
            return null;
        }
    }
}
