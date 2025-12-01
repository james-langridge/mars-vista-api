using Microsoft.EntityFrameworkCore;
using MarsVista.Core.Data;
using MarsVista.Api.DTOs.V2;
using MarsVista.Core.Helpers;

namespace MarsVista.Api.Services.V2;

/// <summary>
/// Implementation of traverse service for deduplicated path data
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

        // Get rover ID for photo count (for cache invalidation)
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

        // Build query for photo count (used for cache invalidation)
        var photoQuery = _context.Photos
            .Where(p => p.RoverId == roverId && p.Xyz != null && p.Xyz != "");

        if (solMin.HasValue)
            photoQuery = photoQuery.Where(p => p.Sol >= solMin.Value);
        if (solMax.HasValue)
            photoQuery = photoQuery.Where(p => p.Sol <= solMax.Value);

        var photoCount = await photoQuery.CountAsync(cancellationToken);

        // Cache key includes rover, sol range, simplify, includeSegments, and photo count for auto-invalidation
        var cacheKey = _cachingService.GenerateCacheKey(
            "traverse", roverLower,
            solMin?.ToString() ?? "all",
            solMax?.ToString() ?? "all",
            simplify.ToString("F1"),
            includeSegments.ToString(),
            photoCount);

        var result = await _cachingService.GetOrSetAsync(
            cacheKey,
            async () => await ComputeTraverseAsync(roverLower, solMin, solMax, simplify, includeSegments, cancellationToken),
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
        int? solMin,
        int? solMax,
        float simplify,
        bool includeSegments,
        CancellationToken cancellationToken)
    {
        // Get unique coordinates grouped by xyz, ordered by first appearance
        var rawPoints = await GetUniqueCoordinatesAsync(roverLower, solMin, solMax, cancellationToken);

        if (rawPoints.Count == 0)
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

        // Parse coordinates
        var parsedPoints = ParsePoints(rawPoints);

        if (parsedPoints.Count == 0)
        {
            return new TraverseResource
            {
                Attributes = new TraverseAttributes
                {
                    Rover = roverLower,
                    SolRange = new SolRange
                    {
                        Start = rawPoints.Min(p => p.SolFirst),
                        End = rawPoints.Max(p => p.SolLast)
                    },
                    PointCount = 0
                }
            };
        }

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

    private async Task<List<RawPointData>> GetUniqueCoordinatesAsync(
        string roverLower,
        int? solMin,
        int? solMax,
        CancellationToken cancellationToken)
    {
        var query = _context.Photos
            .Where(p => p.Rover.Name.ToLower() == roverLower && p.Xyz != null && p.Xyz != "");

        if (solMin.HasValue)
            query = query.Where(p => p.Sol >= solMin.Value);

        if (solMax.HasValue)
            query = query.Where(p => p.Sol <= solMax.Value);

        // Group by xyz to deduplicate, get first and last sol for each unique position
        var grouped = await query
            .GroupBy(p => p.Xyz)
            .Select(g => new RawPointData
            {
                Xyz = g.Key!,
                SolFirst = g.Min(p => p.Sol),
                SolLast = g.Max(p => p.Sol)
            })
            .ToListAsync(cancellationToken);

        // Order by first sol appearance
        return grouped.OrderBy(p => p.SolFirst).ToList();
    }

    private List<ParsedPoint> ParsePoints(List<RawPointData> rawPoints)
    {
        var parsed = new List<ParsedPoint>();

        foreach (var raw in rawPoints)
        {
            if (MarsTimeHelper.TryParseXYZ(raw.Xyz, out var coords))
            {
                parsed.Add(new ParsedPoint
                {
                    X = coords.X,
                    Y = coords.Y,
                    Z = coords.Z,
                    SolFirst = raw.SolFirst,
                    SolLast = raw.SolLast
                });
            }
        }

        // Deduplicate by rounded coordinates (2 decimal places ~= 1cm precision)
        // Different xyz strings can represent same location due to precision differences
        var deduplicated = parsed
            .GroupBy(p => (
                X: MathF.Round(p.X, 2),
                Y: MathF.Round(p.Y, 2),
                Z: MathF.Round(p.Z, 2)))
            .Select(g => new ParsedPoint
            {
                X = g.Key.X,
                Y = g.Key.Y,
                Z = g.Key.Z,
                SolFirst = g.Min(p => p.SolFirst),
                SolLast = g.Max(p => p.SolLast)
            })
            .OrderBy(p => p.SolFirst)
            .ToList();

        return deduplicated;
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

    private record RawPointData
    {
        public required string Xyz { get; init; }
        public int SolFirst { get; init; }
        public int SolLast { get; init; }
    }

    private record ParsedPoint
    {
        public float X { get; init; }
        public float Y { get; init; }
        public float Z { get; init; }
        public int SolFirst { get; init; }
        public int SolLast { get; init; }
    }

    private record TraverseStats
    {
        public float TotalDistance { get; init; }
        public float ElevationGain { get; init; }
        public float ElevationLoss { get; init; }
        public required BoundingBox BoundingBox { get; init; }
    }
}
