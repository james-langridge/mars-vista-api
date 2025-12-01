using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using MarsVista.Core.Entities;
using MarsVista.Api.Services.V2;
using MarsVista.Api.DTOs.V2;

namespace MarsVista.Api.Tests.Integration.V2;

public class TraverseIntegrationTests : IntegrationTestBase
{
    private ITraverseService _traverseService = null!;

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddMemoryCache();

        // Mock the caching service for tests (no Redis needed)
        var mockCaching = new Mock<ICachingServiceV2>();
        mockCaching
            .Setup(x => x.GenerateCacheKey(It.IsAny<object?[]>()))
            .Returns<object?[]>(parts => $"test:{string.Join(":", parts)}");
        mockCaching
            .Setup(x => x.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<TraverseResource>>>(),
                It.IsAny<CacheOptions?>()))
            .Returns<string, Func<Task<TraverseResource>>, CacheOptions?>(
                (key, factory, options) => factory());
        mockCaching
            .Setup(x => x.GetManifestCacheOptions(It.IsAny<bool>()))
            .Returns(new CacheOptions { RedisDuration = TimeSpan.FromHours(1) });

        services.AddSingleton(mockCaching.Object);
        services.AddScoped<ITraverseService, TraverseService>();
    }

    protected override async Task SeedAdditionalDataAsync()
    {
        _traverseService = ServiceProvider.GetRequiredService<ITraverseService>();

        var now = DateTime.UtcNow;

        // Create a traverse path for Curiosity with sequential waypoints
        var waypoints = new[]
        {
            new { Sol = 1000, Site = 79, Drive = 1200, X = 0.0f, Y = 0.0f, Z = 0.0f },
            new { Sol = 1001, Site = 79, Drive = 1202, X = 10.0f, Y = 0.0f, Z = 0.5f },   // 10m east
            new { Sol = 1002, Site = 79, Drive = 1204, X = 10.0f, Y = 10.0f, Z = 1.0f }, // 10m north
            new { Sol = 1003, Site = 80, Drive = 1206, X = 20.0f, Y = 10.0f, Z = 0.5f }, // 10m east
            new { Sol = 1004, Site = 80, Drive = 1208, X = 20.0f, Y = 20.0f, Z = 0.0f }, // 10m north
        };

        for (int i = 0; i < waypoints.Length; i++)
        {
            var wp = waypoints[i];

            // Add RoverWaypoint for traverse calculations (NASA PDS format)
            DbContext.RoverWaypoints.Add(new RoverWaypoint
            {
                RoverId = 1,
                Frame = "ROVER",
                Site = wp.Site,
                Drive = wp.Drive,
                Sol = wp.Sol,
                LandingX = wp.X,
                LandingY = wp.Y,
                LandingZ = wp.Z,
                CreatedAt = now,
                UpdatedAt = now
            });

            // Add 3 photos per waypoint
            for (int j = 0; j < 3; j++)
            {
                DbContext.Photos.Add(new Photo
                {
                    NasaId = $"NRF_{wp.Sol}_{j:D4}",
                    Sol = wp.Sol,
                    EarthDate = new DateTime(2015, 5, 30, 0, 0, 0, DateTimeKind.Utc).AddDays(i),
                    DateTakenUtc = new DateTime(2015, 5, 30, 10, j, 0, DateTimeKind.Utc).AddDays(i),
                    Site = wp.Site,
                    Drive = wp.Drive,
                    Xyz = $"{{\"x\": {wp.X}, \"y\": {wp.Y}, \"z\": {wp.Z}}}",
                    RoverId = 1,
                    CameraId = 2,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        // Add Perseverance traverse with duplicate locations (same XYZ on different sols)
        var persWaypoints = new[]
        {
            new { Sol = 500, X = -100.0f, Y = -50.0f, Z = -10.0f },
            new { Sol = 501, X = -100.0f, Y = -50.0f, Z = -10.0f }, // Same location (duplicate)
            new { Sol = 502, X = -110.0f, Y = -60.0f, Z = -9.0f },
            new { Sol = 503, X = -120.0f, Y = -70.0f, Z = -8.0f },
        };

        for (int i = 0; i < persWaypoints.Length; i++)
        {
            var wp = persWaypoints[i];

            // Add RoverWaypoint for traverse calculations
            DbContext.RoverWaypoints.Add(new RoverWaypoint
            {
                RoverId = 2,
                Frame = "ROVER",
                Site = 1,
                Drive = 100 + i * 2,
                Sol = wp.Sol,
                LandingX = wp.X,
                LandingY = wp.Y,
                LandingZ = wp.Z,
                CreatedAt = now,
                UpdatedAt = now
            });

            DbContext.Photos.Add(new Photo
            {
                NasaId = $"NRP_{wp.Sol}_0000",
                Sol = wp.Sol,
                EarthDate = new DateTime(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i),
                DateTakenUtc = new DateTime(2021, 7, 1, 10, 0, 0, DateTimeKind.Utc).AddDays(i),
                Site = 1,
                Drive = 100 + i * 2,
                Xyz = $"{{\"x\": {wp.X}, \"y\": {wp.Y}, \"z\": {wp.Z}}}",
                RoverId = 2,
                CameraId = 3,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await DbContext.SaveChangesAsync();
    }

    // ============================================================================
    // Basic Response Tests
    // ============================================================================

    [Fact]
    public async Task GetTraverse_WithValidRover_ReturnsTraverse()
    {
        // Act
        var response = await _traverseService.GetTraverseAsync("curiosity");

        // Assert
        response.Should().NotBeNull();
        response.Attributes.Should().NotBeNull();
        response.Attributes.Rover.Should().Be("curiosity");
        response.Path.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetTraverse_ReturnsDeduplicatedPoints()
    {
        // Act
        var response = await _traverseService.GetTraverseAsync("curiosity");

        // Assert
        // 5 unique locations
        response.Attributes.PointCount.Should().Be(5);
        response.Path.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetTraverse_OrdersPointsByFirstAppearance()
    {
        // Act
        var response = await _traverseService.GetTraverseAsync("curiosity");

        // Assert
        response.Path.Should().BeInAscendingOrder(p => p.SolFirst);
        response.Path[0].SolFirst.Should().Be(1000);
        response.Path[4].SolFirst.Should().Be(1004);
    }

    // ============================================================================
    // Distance Calculation Tests
    // ============================================================================

    [Fact]
    public async Task GetTraverse_CalculatesTotalDistance()
    {
        // Act
        var response = await _traverseService.GetTraverseAsync("curiosity");

        // Assert
        // Path: (0,0) -> (10,0) -> (10,10) -> (20,10) -> (20,20)
        // Distances: 10 + 10 + 10 + 10 = 40 meters (ignoring small Z changes)
        response.Attributes.TotalDistanceM.Should().BeGreaterThan(39);
        response.Attributes.TotalDistanceM.Should().BeLessThan(42);
    }

    [Fact]
    public async Task GetTraverse_IncludesCumulativeDistance()
    {
        // Act
        var response = await _traverseService.GetTraverseAsync("curiosity");

        // Assert
        response.Path[0].CumulativeDistanceM.Should().Be(0);
        response.Path.Should().BeInAscendingOrder(p => p.CumulativeDistanceM);
        response.Path[4].CumulativeDistanceM.Should().BeGreaterThan(39);
    }

    // ============================================================================
    // Elevation Calculation Tests
    // ============================================================================

    [Fact]
    public async Task GetTraverse_CalculatesElevationGain()
    {
        // Act
        var response = await _traverseService.GetTraverseAsync("curiosity");

        // Assert
        // Z values: 0 -> 0.5 -> 1.0 -> 0.5 -> 0
        // Gain: 0.5 + 0.5 = 1.0
        response.Attributes.TotalElevationGainM.Should().BeApproximately(1.0f, 0.1f);
    }

    [Fact]
    public async Task GetTraverse_CalculatesElevationLoss()
    {
        // Act
        var response = await _traverseService.GetTraverseAsync("curiosity");

        // Assert
        // Z values: 0 -> 0.5 -> 1.0 -> 0.5 -> 0
        // Loss: 0.5 + 0.5 = 1.0
        response.Attributes.TotalElevationLossM.Should().BeApproximately(1.0f, 0.1f);
    }

    [Fact]
    public async Task GetTraverse_CalculatesNetElevationChange()
    {
        // Act
        var response = await _traverseService.GetTraverseAsync("curiosity");

        // Assert
        // Start at 0, end at 0 -> net change is 0
        response.Attributes.NetElevationChangeM.Should().BeApproximately(0.0f, 0.1f);
    }

    // ============================================================================
    // Bounding Box Tests
    // ============================================================================

    [Fact]
    public async Task GetTraverse_CalculatesBoundingBox()
    {
        // Act
        var response = await _traverseService.GetTraverseAsync("curiosity");

        // Assert
        response.Attributes.BoundingBox.Should().NotBeNull();
        response.Attributes.BoundingBox!.Min.X.Should().BeApproximately(0.0f, 0.1f);
        response.Attributes.BoundingBox.Min.Y.Should().BeApproximately(0.0f, 0.1f);
        response.Attributes.BoundingBox.Max.X.Should().BeApproximately(20.0f, 0.1f);
        response.Attributes.BoundingBox.Max.Y.Should().BeApproximately(20.0f, 0.1f);
    }

    // ============================================================================
    // Sol Range Tests
    // ============================================================================

    [Fact]
    public async Task GetTraverse_CalculatesSolRange()
    {
        // Act
        var response = await _traverseService.GetTraverseAsync("curiosity");

        // Assert
        response.Attributes.SolRange.Start.Should().Be(1000);
        response.Attributes.SolRange.End.Should().Be(1004);
    }

    [Fact]
    public async Task GetTraverse_WithSolFilter_FiltersCorrectly()
    {
        // Act
        var response = await _traverseService.GetTraverseAsync(
            rover: "curiosity",
            solMin: 1001,
            solMax: 1003);

        // Assert
        response.Path.Should().HaveCount(3);
        response.Attributes.SolRange.Start.Should().Be(1001);
        response.Attributes.SolRange.End.Should().Be(1003);
    }

    // ============================================================================
    // Deduplication Tests
    // ============================================================================

    [Fact]
    public async Task GetTraverse_ReturnsAllWaypoints()
    {
        // Act - Perseverance has 4 waypoints (sols 500-503)
        var response = await _traverseService.GetTraverseAsync("perseverance");

        // Assert - Each waypoint is a distinct position from NASA PDS data
        // (no deduplication - NASA tactical data represents actual rover positions)
        response.Attributes.PointCount.Should().Be(4);
        response.Path.Should().HaveCount(4);
    }

    [Fact]
    public async Task GetTraverse_WaypointSolMatchesFirstAndLast()
    {
        // Act
        var response = await _traverseService.GetTraverseAsync("perseverance");

        // Assert - Each waypoint has a single sol (no merging with NASA waypoint data)
        var firstPoint = response.Path[0];
        firstPoint.SolFirst.Should().Be(500);
        firstPoint.SolLast.Should().Be(500);
    }

    // ============================================================================
    // Segment Data Tests
    // ============================================================================

    [Fact]
    public async Task GetTraverse_WithIncludeSegments_ReturnsSegmentData()
    {
        // Act
        var response = await _traverseService.GetTraverseAsync(
            rover: "curiosity",
            includeSegments: true);

        // Assert
        response.Path[0].Segment.Should().BeNull(); // First point has no segment
        response.Path[1].Segment.Should().NotBeNull();
        response.Path[1].Segment!.DistanceM.Should().BeGreaterThan(9);
        response.Path[1].Segment.BearingDeg.Should().BeInRange(0, 360);
    }

    [Fact]
    public async Task GetTraverse_WithoutIncludeSegments_HasNoSegmentData()
    {
        // Act
        var response = await _traverseService.GetTraverseAsync(
            rover: "curiosity",
            includeSegments: false);

        // Assert
        response.Path.Should().OnlyContain(p => p.Segment == null);
    }

    // ============================================================================
    // Simplification Tests
    // ============================================================================

    [Fact]
    public async Task GetTraverse_WithSimplify_ReducesPointCount()
    {
        // Act
        var response = await _traverseService.GetTraverseAsync(
            rover: "curiosity",
            simplify: 100); // Large tolerance to simplify

        // Assert
        response.Attributes.SimplifiedPointCount.Should().NotBeNull();
        response.Attributes.SimplifiedPointCount.Should().BeLessThan(response.Attributes.PointCount);
    }

    [Fact]
    public async Task GetTraverse_WithZeroSimplify_DoesNotSimplify()
    {
        // Act
        var response = await _traverseService.GetTraverseAsync(
            rover: "curiosity",
            simplify: 0);

        // Assert
        response.Attributes.SimplifiedPointCount.Should().BeNull();
    }

    // ============================================================================
    // GeoJSON Tests
    // ============================================================================

    [Fact]
    public async Task GetTraverseGeoJson_ReturnsValidGeoJson()
    {
        // Act
        var response = await _traverseService.GetTraverseGeoJsonAsync("curiosity");

        // Assert
        response.Should().NotBeNull();
        response.Type.Should().Be("FeatureCollection");
        response.Features.Should().HaveCount(1);
        response.Features[0].Type.Should().Be("Feature");
        response.Features[0].Geometry.Type.Should().Be("LineString");
    }

    [Fact]
    public async Task GetTraverseGeoJson_IncludesAllCoordinates()
    {
        // Act
        var response = await _traverseService.GetTraverseGeoJsonAsync("curiosity");

        // Assert
        var coordinates = response.Features[0].Geometry.Coordinates;
        coordinates.Should().HaveCount(5);

        // Each coordinate should have 3 values (x, y, z)
        coordinates[0].Should().HaveCount(3);
    }

    [Fact]
    public async Task GetTraverseGeoJson_IncludesProperties()
    {
        // Act
        var response = await _traverseService.GetTraverseGeoJsonAsync("curiosity");

        // Assert
        var properties = response.Features[0].Properties;
        properties.Rover.Should().Be("curiosity");
        properties.TotalDistanceM.Should().BeGreaterThan(0);
        properties.PointCount.Should().Be(5);
    }

    // ============================================================================
    // Empty/Invalid Data Tests
    // ============================================================================

    [Fact]
    public async Task GetTraverse_WithInvalidRover_ReturnsEmptyTraverse()
    {
        // Act
        var response = await _traverseService.GetTraverseAsync("nonexistent");

        // Assert
        response.Should().NotBeNull();
        response.Path.Should().BeEmpty();
        response.Attributes.PointCount.Should().Be(0);
    }

    [Fact]
    public async Task GetTraverse_WithNoMatchingData_ReturnsEmptyTraverse()
    {
        // Act
        var response = await _traverseService.GetTraverseAsync(
            rover: "curiosity",
            solMin: 9999,
            solMax: 9999);

        // Assert
        response.Path.Should().BeEmpty();
        response.Attributes.PointCount.Should().Be(0);
    }

    // ============================================================================
    // Links Tests
    // ============================================================================

    [Fact]
    public async Task GetTraverse_IncludesGeoJsonLink()
    {
        // Act
        var response = await _traverseService.GetTraverseAsync("curiosity");

        // Assert
        response.Links.Should().NotBeNull();
        response.Links!.GeoJson.Should().Contain("/api/v2/rovers/curiosity/traverse");
        response.Links.GeoJson.Should().Contain("format=geojson");
    }
}
