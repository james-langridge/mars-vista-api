using Microsoft.EntityFrameworkCore;
using MarsVista.Core.Data;
using MarsVista.Api.DTOs.V2;
using MarsVista.Core.Helpers;

namespace MarsVista.Api.Services.V2;

/// <summary>
/// Implementation of journey tracking service
/// Provides data about rover's path and progress
/// </summary>
public class JourneyService : IJourneyService
{
    private readonly MarsVistaDbContext _context;
    private readonly ILogger<JourneyService> _logger;

    public JourneyService(
        MarsVistaDbContext context,
        ILogger<JourneyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<JourneyResource>> GetJourneyAsync(
        string rover,
        int? solMin = null,
        int? solMax = null,
        CancellationToken cancellationToken = default)
    {
        // Build query for rover's photos with location data
        var query = _context.Photos
            .Where(p => p.Rover.Name.ToLower() == rover.ToLowerInvariant() &&
                       p.Site.HasValue &&
                       p.Drive.HasValue);

        // Apply sol filters
        if (solMin.HasValue)
        {
            query = query.Where(p => p.Sol >= solMin.Value);
        }

        if (solMax.HasValue)
        {
            query = query.Where(p => p.Sol <= solMax.Value);
        }

        // Get waypoints (unique site/drive combinations per sol)
        var waypoints = await query
            .GroupBy(p => new
            {
                p.Sol,
                p.EarthDate,
                Site = p.Site!.Value,
                Drive = p.Drive!.Value,
                p.Xyz
            })
            .Select(g => new
            {
                g.Key.Sol,
                g.Key.EarthDate,
                g.Key.Site,
                g.Key.Drive,
                g.Key.Xyz,
                PhotoCount = g.Count()
            })
            .OrderBy(w => w.Sol)
            .ThenBy(w => w.Drive)
            .ToListAsync(cancellationToken);

        if (waypoints.Count == 0)
        {
            return new ApiResponse<JourneyResource>(new JourneyResource
            {
                Type = "journey",
                Attributes = new JourneyAttributes
                {
                    Rover = rover.ToLowerInvariant(),
                    SolStart = solMin ?? 0,
                    SolEnd = solMax ?? 0,
                    LocationsVisited = 0,
                    TotalPhotos = 0
                },
                Path = new List<JourneyWaypoint>()
            });
        }

        // Calculate journey statistics
        var totalPhotos = waypoints.Sum(w => w.PhotoCount);
        var locationsVisited = waypoints.Count;
        var solStart = waypoints.First().Sol;
        var solEnd = waypoints.Last().Sol;

        // Calculate approximate distance (sum of drive increments)
        // This is a rough approximation - actual distance would require 3D coordinate analysis
        var driveDistance = waypoints.Last().Drive - waypoints.First().Drive;
        var distanceKm = driveDistance * 0.01f; // Very rough approximation

        // Calculate elevation change if we have XYZ coordinates
        float? elevationChange = null;
        var firstWaypoint = waypoints.FirstOrDefault(w => !string.IsNullOrEmpty(w.Xyz));
        var lastWaypoint = waypoints.LastOrDefault(w => !string.IsNullOrEmpty(w.Xyz));

        if (firstWaypoint != null && lastWaypoint != null &&
            MarsTimeHelper.TryParseXYZ(firstWaypoint.Xyz, out var firstCoords) &&
            MarsTimeHelper.TryParseXYZ(lastWaypoint.Xyz, out var lastCoords))
        {
            elevationChange = lastCoords.Z - firstCoords.Z;
        }

        // Build waypoint list
        var journeyWaypoints = waypoints.Select(w =>
        {
            PhotoCoordinates? coordinates = null;
            if (!string.IsNullOrEmpty(w.Xyz) &&
                MarsTimeHelper.TryParseXYZ(w.Xyz, out var parsed))
            {
                coordinates = new PhotoCoordinates
                {
                    X = parsed.X,
                    Y = parsed.Y,
                    Z = parsed.Z
                };
            }

            return new JourneyWaypoint
            {
                Sol = w.Sol,
                EarthDate = w.EarthDate?.ToString("yyyy-MM-dd"),
                Site = w.Site,
                Drive = w.Drive,
                Coordinates = coordinates,
                PhotosTaken = w.PhotoCount
            };
        }).ToList();

        var journey = new JourneyResource
        {
            Type = "journey",
            Attributes = new JourneyAttributes
            {
                Rover = rover.ToLowerInvariant(),
                SolStart = solStart,
                SolEnd = solEnd,
                DistanceTraveledKm = distanceKm > 0 ? distanceKm : null,
                LocationsVisited = locationsVisited,
                ElevationChangeM = elevationChange,
                TotalPhotos = totalPhotos
            },
            Path = journeyWaypoints,
            Links = new JourneyLinks
            {
                MapVisualization = $"/api/v2/rovers/{rover}/journey/map",
                KmlExport = $"/api/v2/rovers/{rover}/journey/export/kml"
            }
        };

        return new ApiResponse<JourneyResource>(journey);
    }
}
