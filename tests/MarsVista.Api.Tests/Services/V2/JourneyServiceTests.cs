using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MarsVista.Core.Entities;
using MarsVista.Api.Services.V2;
using MarsVista.Api.Tests.Integration;

namespace MarsVista.Api.Tests.Services.V2;

public class JourneyServiceTests : IntegrationTestBase
{
    private Mock<ILogger<JourneyService>> _mockLogger = null!;
    private JourneyService _service = null!;

    protected override void ConfigureServices(IServiceCollection services)
    {
        _mockLogger = new Mock<ILogger<JourneyService>>();
        services.AddSingleton(_mockLogger.Object);
        services.AddScoped<JourneyService>();
    }

    protected override async Task SeedAdditionalDataAsync()
    {
        _service = ServiceProvider.GetRequiredService<JourneyService>();
        var now = DateTime.UtcNow;

        // Add journey waypoints (sequential sites and drives)
        var waypoints = new[]
        {
            new { Sol = 1000, Site = 79, Drive = 1200, Z = -9.0f },
            new { Sol = 1001, Site = 79, Drive = 1202, Z = -8.5f },
            new { Sol = 1002, Site = 79, Drive = 1204, Z = -8.0f },
            new { Sol = 1003, Site = 80, Drive = 1206, Z = -7.5f },
            new { Sol = 1004, Site = 80, Drive = 1208, Z = -7.0f }
        };

        for (int i = 0; i < waypoints.Length; i++)
        {
            var wp = waypoints[i];
            // Add multiple photos per waypoint
            for (int j = 0; j < 3; j++)
            {
                DbContext.Photos.Add(new Photo
                {
                    NasaId = $"NRF_{wp.Sol}_{j:D4}",
                    Sol = wp.Sol,
                    EarthDate = new DateTime(2015, 5, 30, 0, 0, 0, DateTimeKind.Utc).AddDays(i),
                    DateTakenUtc = new DateTime(2015, 5, 30, 10, j, 0, DateTimeKind.Utc).AddDays(i),
                    ImgSrcSmall = $"https://mars.nasa.gov/{wp.Sol}_{j}_s.jpg",
                    ImgSrcMedium = $"https://mars.nasa.gov/{wp.Sol}_{j}_m.jpg",
                    ImgSrcLarge = $"https://mars.nasa.gov/{wp.Sol}_{j}_l.jpg",
                    ImgSrcFull = $"https://mars.nasa.gov/{wp.Sol}_{j}_f.jpg",
                    Site = wp.Site,
                    Drive = wp.Drive,
                    Xyz = $"{{\"x\": {35.0 + i}, \"y\": {22.0 + i}, \"z\": {wp.Z}}}",
                    RoverId = 1,
                    CameraId = 2, // MAST
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        // Add photos without location (should be excluded)
        DbContext.Photos.Add(new Photo
        {
            NasaId = "NRF_1005_NO_LOCATION",
            Sol = 1005,
            EarthDate = new DateTime(2015, 6, 5, 0, 0, 0, DateTimeKind.Utc),
            DateTakenUtc = new DateTime(2015, 6, 5, 10, 0, 0, DateTimeKind.Utc),
            ImgSrcSmall = "https://mars.nasa.gov/1005_s.jpg",
            ImgSrcMedium = "https://mars.nasa.gov/1005_m.jpg",
            ImgSrcLarge = "https://mars.nasa.gov/1005_l.jpg",
            ImgSrcFull = "https://mars.nasa.gov/1005_f.jpg",
            Site = null,
            Drive = null,
            RoverId = 1,
            CameraId = 2, // MAST
            CreatedAt = now,
            UpdatedAt = now
        });

        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetJourneyAsync_WithValidData_ReturnsJourney()
    {
        // Act
        var result = await _service.GetJourneyAsync("curiosity");

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        result.Data.Type.Should().Be("journey");
        result.Data.Attributes.Should().NotBeNull();
        result.Data.Attributes!.Rover.Should().Be("curiosity");
    }

    [Fact]
    public async Task GetJourneyAsync_CalculatesCorrectWaypoints()
    {
        // Act
        var result = await _service.GetJourneyAsync("curiosity");

        // Assert
        result.Data.Path.Should().NotBeNullOrEmpty();
        result.Data.Path.Should().HaveCount(5); // 5 unique site/drive combinations
    }

    [Fact]
    public async Task GetJourneyAsync_OrdersWaypointsBySol()
    {
        // Act
        var result = await _service.GetJourneyAsync("curiosity");

        // Assert
        result.Data.Path.Should().BeInAscendingOrder(wp => wp.Sol);
        result.Data.Path[0].Sol.Should().Be(1000);
        result.Data.Path[4].Sol.Should().Be(1004);
    }

    [Fact]
    public async Task GetJourneyAsync_CalculatesTotalPhotos()
    {
        // Act
        var result = await _service.GetJourneyAsync("curiosity");

        // Assert
        result.Data.Attributes!.TotalPhotos.Should().Be(15); // 5 waypoints * 3 photos each
    }

    [Fact]
    public async Task GetJourneyAsync_CalculatesLocationsVisited()
    {
        // Act
        var result = await _service.GetJourneyAsync("curiosity");

        // Assert
        result.Data.Attributes!.LocationsVisited.Should().Be(5);
    }

    [Fact]
    public async Task GetJourneyAsync_CalculatesSolRange()
    {
        // Act
        var result = await _service.GetJourneyAsync("curiosity");

        // Assert
        result.Data.Attributes!.SolStart.Should().Be(1000);
        result.Data.Attributes.SolEnd.Should().Be(1004);
    }

    [Fact]
    public async Task GetJourneyAsync_CalculatesElevationChange()
    {
        // Act
        var result = await _service.GetJourneyAsync("curiosity");

        // Assert
        result.Data.Attributes!.ElevationChangeM.Should().NotBeNull();
        result.Data.Attributes.ElevationChangeM.Should().BeApproximately(2.0f, 0.1f); // -7.0 - (-9.0) = 2.0
    }

    [Fact]
    public async Task GetJourneyAsync_IncludesCoordinatesInWaypoints()
    {
        // Act
        var result = await _service.GetJourneyAsync("curiosity");

        // Assert
        var firstWaypoint = result.Data.Path.First();
        firstWaypoint.Coordinates.Should().NotBeNull();
        firstWaypoint.Coordinates!.X.Should().BeApproximately(35.0f, 0.1f);
        firstWaypoint.Coordinates.Y.Should().BeApproximately(22.0f, 0.1f);
        firstWaypoint.Coordinates.Z.Should().BeApproximately(-9.0f, 0.1f);
    }

    [Fact]
    public async Task GetJourneyAsync_IncludesPhotoCountPerWaypoint()
    {
        // Act
        var result = await _service.GetJourneyAsync("curiosity");

        // Assert
        result.Data.Path.Should().OnlyContain(wp => wp.PhotosTaken == 3);
    }

    [Fact]
    public async Task GetJourneyAsync_WithSolFilter_FiltersCorrectly()
    {
        // Act
        var result = await _service.GetJourneyAsync(
            rover: "curiosity",
            solMin: 1001,
            solMax: 1003);

        // Assert
        result.Data.Path.Should().HaveCount(3);
        result.Data.Path.Should().OnlyContain(wp => wp.Sol >= 1001 && wp.Sol <= 1003);
        result.Data.Attributes!.SolStart.Should().Be(1001);
        result.Data.Attributes.SolEnd.Should().Be(1003);
    }

    [Fact]
    public async Task GetJourneyAsync_WithNoMatchingData_ReturnsEmptyJourney()
    {
        // Act
        var result = await _service.GetJourneyAsync(
            rover: "curiosity",
            solMin: 9999,
            solMax: 9999);

        // Assert
        result.Data.Should().NotBeNull();
        result.Data.Path.Should().BeEmpty();
        result.Data.Attributes!.LocationsVisited.Should().Be(0);
        result.Data.Attributes.TotalPhotos.Should().Be(0);
    }

    [Fact]
    public async Task GetJourneyAsync_ExcludesPhotosWithoutLocation()
    {
        // Act - Query across all sols including the one without location
        var result = await _service.GetJourneyAsync("curiosity");

        // Assert
        result.Data.Path.Should().NotContain(wp => wp.Sol == 1005);
    }

    [Fact]
    public async Task GetJourneyAsync_IncludesVisualizationLinks()
    {
        // Act
        var result = await _service.GetJourneyAsync("curiosity");

        // Assert
        result.Data.Links.Should().NotBeNull();
        result.Data.Links!.MapVisualization.Should().NotBeNullOrEmpty();
        result.Data.Links.MapVisualization.Should().Contain("/api/v2/rovers/curiosity/journey/map");
        result.Data.Links.KmlExport.Should().NotBeNullOrEmpty();
        result.Data.Links.KmlExport.Should().Contain("/api/v2/rovers/curiosity/journey/export/kml");
    }

    [Fact]
    public async Task GetJourneyAsync_CalculatesApproximateDistance()
    {
        // Act
        var result = await _service.GetJourneyAsync("curiosity");

        // Assert
        result.Data.Attributes!.DistanceTraveledKm.Should().NotBeNull();
        result.Data.Attributes.DistanceTraveledKm.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetJourneyAsync_HandlesWaypointsWithoutCoordinates()
    {
        // Arrange - Add waypoint without XYZ
        var now = DateTime.UtcNow;
        DbContext.Photos.Add(new Photo
        {
            NasaId = "NRF_1010_NO_XYZ",
            Sol = 1010,
            EarthDate = new DateTime(2015, 6, 10, 0, 0, 0, DateTimeKind.Utc),
            DateTakenUtc = new DateTime(2015, 6, 10, 10, 0, 0, DateTimeKind.Utc),
            ImgSrcSmall = "https://mars.nasa.gov/1010_s.jpg",
            ImgSrcMedium = "https://mars.nasa.gov/1010_m.jpg",
            ImgSrcLarge = "https://mars.nasa.gov/1010_l.jpg",
            ImgSrcFull = "https://mars.nasa.gov/1010_f.jpg",
            Site = 85,
            Drive = 1300,
            Xyz = null, // No coordinates
            RoverId = 1,
            CameraId = 2, // MAST
            CreatedAt = now,
            UpdatedAt = now
        });
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetJourneyAsync(
            rover: "curiosity",
            solMin: 1010,
            solMax: 1010);

        // Assert
        result.Data.Path.Should().HaveCount(1);
        result.Data.Path[0].Coordinates.Should().BeNull();
    }

    [Fact]
    public async Task GetJourneyAsync_WithMixedCoordinates_CalculatesElevationFromAvailable()
    {
        // Arrange - Add waypoints with and without coordinates
        var now = DateTime.UtcNow;

        // Waypoint with coordinates at beginning
        DbContext.Photos.Add(new Photo
        {
            NasaId = "NRF_2000_START",
            Sol = 2000,
            EarthDate = new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DateTakenUtc = new DateTime(2016, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            ImgSrcSmall = "https://mars.nasa.gov/2000_s.jpg",
            ImgSrcMedium = "https://mars.nasa.gov/2000_m.jpg",
            ImgSrcLarge = "https://mars.nasa.gov/2000_l.jpg",
            ImgSrcFull = "https://mars.nasa.gov/2000_f.jpg",
            Site = 90,
            Drive = 1400,
            Xyz = "{\"x\": 50.0, \"y\": 30.0, \"z\": -10.0}",
            RoverId = 1,
            CameraId = 2, // MAST
            CreatedAt = now,
            UpdatedAt = now
        });

        // Waypoint without coordinates in middle
        DbContext.Photos.Add(new Photo
        {
            NasaId = "NRF_2001_MIDDLE",
            Sol = 2001,
            EarthDate = new DateTime(2016, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            DateTakenUtc = new DateTime(2016, 1, 2, 10, 0, 0, DateTimeKind.Utc),
            ImgSrcSmall = "https://mars.nasa.gov/2001_s.jpg",
            ImgSrcMedium = "https://mars.nasa.gov/2001_m.jpg",
            ImgSrcLarge = "https://mars.nasa.gov/2001_l.jpg",
            ImgSrcFull = "https://mars.nasa.gov/2001_f.jpg",
            Site = 91,
            Drive = 1405,
            Xyz = null,
            RoverId = 1,
            CameraId = 2, // MAST
            CreatedAt = now,
            UpdatedAt = now
        });

        // Waypoint with coordinates at end
        DbContext.Photos.Add(new Photo
        {
            NasaId = "NRF_2002_END",
            Sol = 2002,
            EarthDate = new DateTime(2016, 1, 3, 0, 0, 0, DateTimeKind.Utc),
            DateTakenUtc = new DateTime(2016, 1, 3, 10, 0, 0, DateTimeKind.Utc),
            ImgSrcSmall = "https://mars.nasa.gov/2002_s.jpg",
            ImgSrcMedium = "https://mars.nasa.gov/2002_m.jpg",
            ImgSrcLarge = "https://mars.nasa.gov/2002_l.jpg",
            ImgSrcFull = "https://mars.nasa.gov/2002_f.jpg",
            Site = 92,
            Drive = 1410,
            Xyz = "{\"x\": 52.0, \"y\": 32.0, \"z\": -5.0}",
            RoverId = 1,
            CameraId = 2, // MAST
            CreatedAt = now,
            UpdatedAt = now
        });

        await DbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetJourneyAsync(
            rover: "curiosity",
            solMin: 2000,
            solMax: 2002);

        // Assert
        result.Data.Attributes!.ElevationChangeM.Should().NotBeNull();
        result.Data.Attributes.ElevationChangeM.Should().BeApproximately(5.0f, 0.1f); // -5.0 - (-10.0) = 5.0
    }

    [Fact]
    public async Task GetJourneyAsync_WithMultiplePhotosPerLocation_CreatesOneWaypoint()
    {
        // The test data already has 3 photos per waypoint
        // Act
        var result = await _service.GetJourneyAsync("curiosity");

        // Assert - Should group by site/drive/sol
        result.Data.Path.Should().HaveCount(5); // Not 15 (would be if not grouped)
    }

    [Fact]
    public async Task GetJourneyAsync_IncludesEarthDateInWaypoints()
    {
        // Act
        var result = await _service.GetJourneyAsync("curiosity");

        // Assert
        result.Data.Path.Should().OnlyContain(wp => !string.IsNullOrEmpty(wp.EarthDate));
        result.Data.Path[0].EarthDate.Should().Be("2015-05-30");
    }

    [Fact]
    public async Task GetJourneyAsync_WithInvalidRover_ReturnsEmptyJourney()
    {
        // Act
        var result = await _service.GetJourneyAsync("nonexistent");

        // Assert
        result.Data.Path.Should().BeEmpty();
        result.Data.Attributes!.Rover.Should().Be("nonexistent");
        result.Data.Attributes.LocationsVisited.Should().Be(0);
    }

    [Fact]
    public async Task GetJourneyAsync_HandlesNegativeElevationChange()
    {
        // Arrange - Add waypoints with descending elevation
        var now = DateTime.UtcNow;

        DbContext.Photos.Add(new Photo
        {
            NasaId = "NRF_3000_HIGH",
            Sol = 3000,
            EarthDate = new DateTime(2016, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            DateTakenUtc = new DateTime(2016, 5, 1, 10, 0, 0, DateTimeKind.Utc),
            ImgSrcSmall = "https://mars.nasa.gov/3000_s.jpg",
            ImgSrcMedium = "https://mars.nasa.gov/3000_m.jpg",
            ImgSrcLarge = "https://mars.nasa.gov/3000_l.jpg",
            ImgSrcFull = "https://mars.nasa.gov/3000_f.jpg",
            Site = 100,
            Drive = 1500,
            Xyz = "{\"x\": 60.0, \"y\": 40.0, \"z\": 5.0}", // Higher elevation
            RoverId = 1,
            CameraId = 2, // MAST
            CreatedAt = now,
            UpdatedAt = now
        });

        DbContext.Photos.Add(new Photo
        {
            NasaId = "NRF_3001_LOW",
            Sol = 3001,
            EarthDate = new DateTime(2016, 5, 2, 0, 0, 0, DateTimeKind.Utc),
            DateTakenUtc = new DateTime(2016, 5, 2, 10, 0, 0, DateTimeKind.Utc),
            ImgSrcSmall = "https://mars.nasa.gov/3001_s.jpg",
            ImgSrcMedium = "https://mars.nasa.gov/3001_m.jpg",
            ImgSrcLarge = "https://mars.nasa.gov/3001_l.jpg",
            ImgSrcFull = "https://mars.nasa.gov/3001_f.jpg",
            Site = 101,
            Drive = 1505,
            Xyz = "{\"x\": 61.0, \"y\": 41.0, \"z\": -10.0}", // Lower elevation
            RoverId = 1,
            CameraId = 2, // MAST
            CreatedAt = now,
            UpdatedAt = now
        });

        await DbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetJourneyAsync(
            rover: "curiosity",
            solMin: 3000,
            solMax: 3001);

        // Assert
        result.Data.Attributes!.ElevationChangeM.Should().NotBeNull();
        result.Data.Attributes.ElevationChangeM.Should().BeApproximately(-15.0f, 0.1f); // -10.0 - 5.0 = -15.0
    }

    [Fact]
    public async Task GetJourneyAsync_ReturnsResponseWithCorrectStructure()
    {
        // Act
        var result = await _service.GetJourneyAsync("curiosity");

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        result.Data.Type.Should().Be("journey");
        result.Data.Attributes.Should().NotBeNull();
        result.Data.Path.Should().NotBeNull();
        result.Data.Links.Should().NotBeNull();
    }
}
