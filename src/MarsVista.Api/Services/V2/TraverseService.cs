using Microsoft.EntityFrameworkCore;
using MarsVista.Core.Data;
using MarsVista.Api.DTOs.V2;
using MarsVista.Core.Helpers;

namespace MarsVista.Api.Services.V2;

/// <summary>
/// Implementation of traverse service using official NASA PDS waypoint data.
/// Provides accurate distance calculations based on landing-relative coordinates.
///
/// Data source: https://pds-geosciences.wustl.edu/m2020/urn-nasa-pds-mars2020_rover_places/data_localizations/
///
/// IMPORTANT: The traverse data comes from NASA's tactical localization, NOT from photo XYZ coordinates.
/// Photo XYZ is in a local site frame; waypoint data is in a global landing-relative frame.
/// </summary>
public class TraverseService : ITraverseService
{
    private readonly MarsVistaDbContext _context;
    private readonly ICachingServiceV2 _cachingService;
    private readonly ILogger<TraverseService> _logger;

    public TraverseService(
        MarsVistaDbContext context,
        ICachingServiceV2 cachingService,
        ILogger<TraverseService> logger)
    {
        _context = context;
        _cachingService = cachingService;
        _logger = logger;
    }

    public async Task<TraverseResource> GetTraverseAsync(
        string rover,
        int? solMin = null,
        int? solMax = null,
        float simplify = 0,
        bool includeSegments = false,
        CancellationToken cancellationToken = default)
    {
        var roverLower = rover.ToLowerInvariant();

        // Get rover ID
        var roverId = await _context.Rovers
            .Where(r => r.Name.ToLower() == roverLower)
            .Select(r => r.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (roverId == 0)
        {
            return new TraverseResource
            {
                Attributes = new TraverseAttributes
                {
                    Rover = roverLower,
                    SolRange = new SolRange { Start = solMin ?? 0, End = solMax ?? 0 },
                    PointCount = 0
                }
            };
        }

        // Build query for waypoint count (used for cache invalidation)
        // Only count ROVER frames for consistency with traverse calculation
        var waypointQuery = _context.RoverWaypoints
            .Where(w => w.RoverId == roverId)
            .Where(w => w.Frame == "ROVER");

        if (solMin.HasValue)
            waypointQuery = waypointQuery.Where(w => w.Sol >= solMin.Value);
        if (solMax.HasValue)
            waypointQuery = waypointQuery.Where(w => w.Sol <= solMax.Value);

        var waypointCount = await waypointQuery.CountAsync(cancellationToken);

        // Cache key includes rover, sol range, simplify, includeSegments, and waypoint count
        var cacheKey = _cachingService.GenerateCacheKey(
            "traverse", roverLower,
            solMin?.ToString() ?? "all",
            solMax?.ToString() ?? "all",
            simplify.ToString("F1"),
            includeSegments.ToString(),
            waypointCount);

        var result = await _cachingService.GetOrSetAsync(
            cacheKey,
            async () => await ComputeTraverseAsync(roverLower, roverId, solMin, solMax, simplify, includeSegments, cancellationToken),
            _cachingService.GetManifestCacheOptions(isActiveRover: true));

        return result ?? new TraverseResource
        {
            Attributes = new TraverseAttributes
            {
                Rover = roverLower,
                SolRange = new SolRange { Start = solMin ?? 0, End = solMax ?? 0 },
                PointCount = 0
            }
        };
    }

    private async Task<TraverseResource> ComputeTraverseAsync(
        string roverLower,
        int roverId,
        int? solMin,
        int? solMax,
        float simplify,
        bool includeSegments,
        CancellationToken cancellationToken)
    {
        // Get waypoints from NASA PDS data (ordered by sol, then site, then drive)
        // Only use ROVER frames - these represent actual rover positions at specific sols
        // SITE frames are reference positions that would cause double-counting
        var waypointQuery = _context.RoverWaypoints
            .Where(w => w.RoverId == roverId)
            .Where(w => w.Frame == "ROVER");

        if (solMin.HasValue)
            waypointQuery = waypointQuery.Where(w => w.Sol >= solMin.Value);
        if (solMax.HasValue)
            waypointQuery = waypointQuery.Where(w => w.Sol <= solMax.Value);

        var waypoints = await waypointQuery
            .OrderBy(w => w.Sol ?? 0)
            .ThenBy(w => w.Site)
            .ThenBy(w => w.Drive ?? 0)
            .ToListAsync(cancellationToken);

        if (waypoints.Count == 0)
        {
            return new TraverseResource
            {
                Attributes = new TraverseAttributes
                {
                    Rover = roverLower,
                    SolRange = new SolRange { Start = solMin ?? 0, End = solMax ?? 0 },
                    PointCount = 0
                },
                Meta = new TraverseMeta
                {
                    DataSource = "nasa_pds",
                    Message = "No waypoint data available. Import waypoints using POST /api/v1/admin/waypoints/import/{rover}"
                }
            };
        }

        // Convert to parsed points for simplification/calculation
        var parsedPoints = waypoints.Select(w => new ParsedPoint
        {
            X = w.LandingX,
            Y = w.LandingY,
            Z = w.LandingZ,
            SolFirst = w.Sol ?? 0,
            SolLast = w.Sol ?? 0,
            Site = w.Site,
            Drive = w.Drive
        }).ToList();

        // Apply simplification if requested
        var originalCount = parsedPoints.Count;
        int? simplifiedCount = null;

        if (simplify > 0 && parsedPoints.Count > 2)
        {
            var pointsForSimplify = parsedPoints
                .Select((p, i) => (p.X, p.Y, p.Z, Index: i))
                .ToList();

            var keepIndices = PathSimplifier.Simplify(pointsForSimplify, simplify);
            var keepSet = new HashSet<int>(keepIndices);

            parsedPoints = parsedPoints
                .Where((_, i) => keepSet.Contains(i))
                .ToList();

            simplifiedCount = parsedPoints.Count;
        }

        // Calculate cumulative distances and statistics
        var (traversePoints, stats) = CalculateTraverseData(parsedPoints, includeSegments);

        var maxSol = waypoints.Where(w => w.Sol.HasValue).Max(w => w.Sol) ?? 0;

        return new TraverseResource
        {
            Attributes = new TraverseAttributes
            {
                Rover = roverLower,
                SolRange = new SolRange
                {
                    Start = parsedPoints.First().SolFirst,
                    End = parsedPoints.Max(p => p.SolLast)
                },
                TotalDistanceM = stats.TotalDistance,
                TotalElevationGainM = stats.ElevationGain,
                TotalElevationLossM = stats.ElevationLoss,
                NetElevationChangeM = stats.ElevationGain - stats.ElevationLoss,
                PointCount = originalCount,
                SimplifiedPointCount = simplifiedCount,
                BoundingBox = stats.BoundingBox
            },
            Path = traversePoints,
            Links = new TraverseLinks
            {
                GeoJson = $"/api/v2/rovers/{roverLower}/traverse?format=geojson"
            },
            Meta = new TraverseMeta
            {
                DataSource = "nasa_pds",
                DataUrl = "https://pds-geosciences.wustl.edu/m2020/urn-nasa-pds-mars2020_rover_places/data_localizations/best_tactical.csv",
                MaxSolInData = maxSol,
                CoordinateFrame = "landing_relative"
            }
        };
    }

    public async Task<GeoJsonFeatureCollection> GetTraverseGeoJsonAsync(
        string rover,
        int? solMin = null,
        int? solMax = null,
        float simplify = 0,
        CancellationToken cancellationToken = default)
    {
        var traverse = await GetTraverseAsync(rover, solMin, solMax, simplify, false, cancellationToken);

        var coordinates = traverse.Path
            .Select(p => new[] { p.X, p.Y, p.Z })
            .ToList();

        return new GeoJsonFeatureCollection
        {
            Features = new List<GeoJsonFeature>
            {
                new GeoJsonFeature
                {
                    Geometry = new GeoJsonLineString
                    {
                        Coordinates = coordinates
                    },
                    Properties = new GeoJsonProperties
                    {
                        Rover = traverse.Attributes.Rover,
                        SolRange = new[] { traverse.Attributes.SolRange.Start, traverse.Attributes.SolRange.End },
                        TotalDistanceM = traverse.Attributes.TotalDistanceM,
                        PointCount = traverse.Attributes.PointCount
                    }
                }
            }
        };
    }

    private (List<TraversePoint> Points, TraverseStats Stats) CalculateTraverseData(
        List<ParsedPoint> points,
        bool includeSegments)
    {
        var traversePoints = new List<TraversePoint>();
        float cumulative = 0;
        float elevationGain = 0;
        float elevationLoss = 0;

        float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;

        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];

            // Update bounding box
            minX = MathF.Min(minX, p.X);
            minY = MathF.Min(minY, p.Y);
            minZ = MathF.Min(minZ, p.Z);
            maxX = MathF.Max(maxX, p.X);
            maxY = MathF.Max(maxY, p.Y);
            maxZ = MathF.Max(maxZ, p.Z);

            TraverseSegment? segment = null;

            if (i > 0)
            {
                var prev = points[i - 1];
                var dist = PathSimplifier.Distance3D(prev.X, prev.Y, prev.Z, p.X, p.Y, p.Z);
                cumulative += dist;

                var elevChange = p.Z - prev.Z;
                if (elevChange > 0)
                    elevationGain += elevChange;
                else
                    elevationLoss += MathF.Abs(elevChange);

                if (includeSegments)
                {
                    segment = new TraverseSegment
                    {
                        DistanceM = MathF.Round(dist, 3),
                        BearingDeg = MathF.Round(PathSimplifier.Bearing2D(prev.X, prev.Y, p.X, p.Y), 1),
                        ElevationChangeM = MathF.Round(elevChange, 3)
                    };
                }
            }

            traversePoints.Add(new TraversePoint
            {
                X = MathF.Round(p.X, 3),
                Y = MathF.Round(p.Y, 3),
                Z = MathF.Round(p.Z, 3),
                SolFirst = p.SolFirst,
                SolLast = p.SolLast,
                CumulativeDistanceM = MathF.Round(cumulative, 1),
                Segment = segment
            });
        }

        var stats = new TraverseStats
        {
            TotalDistance = MathF.Round(cumulative, 1),
            ElevationGain = MathF.Round(elevationGain, 1),
            ElevationLoss = MathF.Round(elevationLoss, 1),
            BoundingBox = new BoundingBox
            {
                Min = new PhotoCoordinates
                {
                    X = MathF.Round(minX, 3),
                    Y = MathF.Round(minY, 3),
                    Z = MathF.Round(minZ, 3)
                },
                Max = new PhotoCoordinates
                {
                    X = MathF.Round(maxX, 3),
                    Y = MathF.Round(maxY, 3),
                    Z = MathF.Round(maxZ, 3)
                }
            }
        };

        return (traversePoints, stats);
    }

    private record ParsedPoint
    {
        public float X { get; init; }
        public float Y { get; init; }
        public float Z { get; init; }
        public int SolFirst { get; init; }
        public int SolLast { get; init; }
        public int Site { get; init; }
        public int? Drive { get; init; }
    }

    private record TraverseStats
    {
        public float TotalDistance { get; init; }
        public float ElevationGain { get; init; }
        public float ElevationLoss { get; init; }
        public required BoundingBox BoundingBox { get; init; }
    }
}
