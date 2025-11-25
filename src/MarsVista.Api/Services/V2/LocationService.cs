using Microsoft.EntityFrameworkCore;
using MarsVista.Core.Data;
using MarsVista.Api.DTOs.V2;
using MarsVista.Core.Helpers;

namespace MarsVista.Api.Services.V2;

/// <summary>
/// Implementation of location timeline service
/// Provides data about unique locations visited by rovers
/// </summary>
public class LocationService : ILocationService
{
    private readonly MarsVistaDbContext _context;
    private readonly ILogger<LocationService> _logger;

    public LocationService(
        MarsVistaDbContext context,
        ILogger<LocationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<List<LocationResource>>> GetLocationsAsync(
        string? rovers = null,
        int? solMin = null,
        int? solMax = null,
        int? minPhotos = null,
        int pageNumber = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        // Build query for photos with location data
        var query = _context.Photos
            .Where(p => p.Site.HasValue && p.Drive.HasValue);

        // Apply filters
        if (!string.IsNullOrWhiteSpace(rovers))
        {
            var roverList = rovers.Split(',')
                .Select(r => r.Trim().ToLowerInvariant())
                .ToList();
            query = query.Where(p => roverList.Contains(p.Rover.Name.ToLower()));
        }

        if (solMin.HasValue)
        {
            query = query.Where(p => p.Sol >= solMin.Value);
        }

        if (solMax.HasValue)
        {
            query = query.Where(p => p.Sol <= solMax.Value);
        }

        // Group by rover, site, and drive
        var locationGroups = await query
            .GroupBy(p => new
            {
                Rover = p.Rover.Name,
                Site = p.Site!.Value,
                Drive = p.Drive!.Value
            })
            .Select(g => new
            {
                g.Key.Rover,
                g.Key.Site,
                g.Key.Drive,
                PhotoCount = g.Count(),
                FirstVisited = g.Min(p => p.EarthDate),
                LastVisited = g.Max(p => p.EarthDate),
                FirstSol = g.Min(p => p.Sol),
                LastSol = g.Max(p => p.Sol),
                Xyz = g.Select(p => p.Xyz).FirstOrDefault()
            })
            .Where(g => !minPhotos.HasValue || g.PhotoCount >= minPhotos.Value)
            .OrderByDescending(g => g.PhotoCount)
            .ToListAsync(cancellationToken);

        // Apply pagination
        var totalCount = locationGroups.Count;
        var paginatedLocations = locationGroups
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Convert to resources
        var resources = paginatedLocations.Select(loc =>
        {
            var locationId = $"{loc.Rover.ToLowerInvariant()}_{loc.Site}_{loc.Drive}";

            PhotoCoordinates? coordinates = null;
            if (!string.IsNullOrEmpty(loc.Xyz) &&
                MarsTimeHelper.TryParseXYZ(loc.Xyz, out var parsed))
            {
                coordinates = new PhotoCoordinates
                {
                    X = parsed.X,
                    Y = parsed.Y,
                    Z = parsed.Z
                };
            }

            return new LocationResource
            {
                Id = locationId,
                Type = "location",
                Attributes = new LocationAttributes
                {
                    Rover = loc.Rover.ToLowerInvariant(),
                    Site = loc.Site,
                    Drive = loc.Drive,
                    FirstVisited = loc.FirstVisited?.ToString("yyyy-MM-dd"),
                    LastVisited = loc.LastVisited?.ToString("yyyy-MM-dd"),
                    FirstSol = loc.FirstSol,
                    LastSol = loc.LastSol,
                    PhotoCount = loc.PhotoCount,
                    VisitCount = loc.LastSol - loc.FirstSol + 1, // Approximate
                    Coordinates = coordinates
                },
                Links = new LocationLinks
                {
                    Photos = $"/api/v2/photos?site={loc.Site}&drive={loc.Drive}&rovers={loc.Rover.ToLowerInvariant()}"
                }
            };
        }).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new ApiResponse<List<LocationResource>>(resources)
        {
            Meta = new ResponseMeta
            {
                TotalCount = totalCount,
                ReturnedCount = resources.Count
            },
            Pagination = new PaginationInfo
            {
                Page = pageNumber,
                PerPage = pageSize,
                TotalPages = totalPages
            }
        };
    }

    public async Task<LocationResource?> GetLocationByIdAsync(
        string locationId,
        CancellationToken cancellationToken = default)
    {
        // Parse location ID (format: "curiosity_79_1204")
        var parts = locationId.Split('_');
        if (parts.Length != 3)
            return null;

        var rover = parts[0];
        if (!int.TryParse(parts[1], out var site))
            return null;
        if (!int.TryParse(parts[2], out var drive))
            return null;

        // Get location data
        var locationData = await _context.Photos
            .Where(p => p.Rover.Name.ToLower() == rover &&
                       p.Site == site &&
                       p.Drive == drive)
            .GroupBy(p => new { p.Rover.Name, p.Site, p.Drive })
            .Select(g => new
            {
                Rover = g.Key.Name,
                Site = g.Key.Site!.Value,
                Drive = g.Key.Drive!.Value,
                PhotoCount = g.Count(),
                FirstVisited = g.Min(p => p.EarthDate),
                LastVisited = g.Max(p => p.EarthDate),
                FirstSol = g.Min(p => p.Sol),
                LastSol = g.Max(p => p.Sol),
                Xyz = g.Select(p => p.Xyz).FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (locationData == null)
            return null;

        PhotoCoordinates? coordinates = null;
        if (!string.IsNullOrEmpty(locationData.Xyz) &&
            MarsTimeHelper.TryParseXYZ(locationData.Xyz, out var parsed))
        {
            coordinates = new PhotoCoordinates
            {
                X = parsed.X,
                Y = parsed.Y,
                Z = parsed.Z
            };
        }

        return new LocationResource
        {
            Id = locationId,
            Type = "location",
            Attributes = new LocationAttributes
            {
                Rover = locationData.Rover.ToLowerInvariant(),
                Site = locationData.Site,
                Drive = locationData.Drive,
                FirstVisited = locationData.FirstVisited?.ToString("yyyy-MM-dd"),
                LastVisited = locationData.LastVisited?.ToString("yyyy-MM-dd"),
                FirstSol = locationData.FirstSol,
                LastSol = locationData.LastSol,
                PhotoCount = locationData.PhotoCount,
                VisitCount = locationData.LastSol - locationData.FirstSol + 1,
                Coordinates = coordinates
            },
            Links = new LocationLinks
            {
                Photos = $"/api/v2/photos?site={locationData.Site}&drive={locationData.Drive}&rovers={locationData.Rover.ToLowerInvariant()}"
            }
        };
    }
}
