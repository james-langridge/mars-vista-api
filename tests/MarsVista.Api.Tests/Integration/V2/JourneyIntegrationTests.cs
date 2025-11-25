using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MarsVista.Core.Entities;
using MarsVista.Api.Services.V2;

namespace MarsVista.Api.Tests.Integration.V2;

public class JourneyIntegrationTests : IntegrationTestBase
{
    private IJourneyService _journeyService = null!;

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IJourneyService, JourneyService>();
    }

    protected override async Task SeedAdditionalDataAsync()
    {
        _journeyService = ServiceProvider.GetRequiredService<IJourneyService>();

        var now = DateTime.UtcNow;

        // Create a journey for Curiosity with sequential sites and drives
        var waypoints = new[]
        {
            new { Sol = 1000, Site = 79, Drive = 1200, X = 35.0f, Y = 22.0f, Z = -9.0f },
            new { Sol = 1001, Site = 79, Drive = 1202, X = 35.5f, Y = 22.2f, Z = -8.5f },
            new { Sol = 1002, Site = 79, Drive = 1204, X = 36.0f, Y = 22.4f, Z = -8.0f },
            new { Sol = 1003, Site = 80, Drive = 1206, X = 36.5f, Y = 22.6f, Z = -7.5f },
            new { Sol = 1004, Site = 80, Drive = 1208, X = 37.0f, Y = 22.8f, Z = -7.0f },
            new { Sol = 1005, Site = 80, Drive = 1210, X = 37.5f, Y = 23.0f, Z = -6.5f }
        };

        for (int i = 0; i < waypoints.Length; i++)
        {
            var wp = waypoints[i];
            // Add 5 photos per waypoint
            for (int j = 0; j < 5; j++)
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

        // Add Perseverance journey
        var persWaypoints = new[]
        {
            new { Sol = 500, Site = 1, Drive = 100, X = 10.0f, Y = 20.0f, Z = -5.0f },
            new { Sol = 501, Site = 1, Drive = 102, X = 10.5f, Y = 20.2f, Z = -4.8f },
            new { Sol = 502, Site = 1, Drive = 104, X = 11.0f, Y = 20.4f, Z = -4.6f }
        };

        for (int i = 0; i < persWaypoints.Length; i++)
        {
            var wp = persWaypoints[i];
            for (int j = 0; j < 3; j++)
            {
                DbContext.Photos.Add(new Photo
                {
                    NasaId = $"NRP_{wp.Sol}_{j:D4}",
                    Sol = wp.Sol,
                    EarthDate = new DateTime(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i),
                    DateTakenUtc = new DateTime(2021, 7, 1, 10, j, 0, DateTimeKind.Utc).AddDays(i),
                    Site = wp.Site,
                    Drive = wp.Drive,
                    Xyz = $"{{\"x\": {wp.X}, \"y\": {wp.Y}, \"z\": {wp.Z}}}",
                    RoverId = 2,
                    CameraId = 3,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetJourney_WithValidRover_ReturnsJourney()
    {
        // Act
        var response = await _journeyService.GetJourneyAsync("curiosity");

        // Assert
        response.Should().NotBeNull();
        response.Data.Should().NotBeNull();
        response.Data.Type.Should().Be("journey");
        response.Data.Attributes.Should().NotBeNull();
        response.Data.Attributes!.Rover.Should().Be("curiosity");
        response.Data.Path.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetJourney_ReturnsAllWaypoints()
    {
        // Act
        var response = await _journeyService.GetJourneyAsync("curiosity");

        // Assert
        response.Data.Path.Should().HaveCount(6); // 6 unique site/drive combinations
    }

    [Fact]
    public async Task GetJourney_OrdersWaypointsBySol()
    {
        // Act
        var response = await _journeyService.GetJourneyAsync("curiosity");

        // Assert
        response.Data.Path.Should().BeInAscendingOrder(wp => wp.Sol);
        response.Data.Path[0].Sol.Should().Be(1000);
        response.Data.Path[5].Sol.Should().Be(1005);
    }

    [Fact]
    public async Task GetJourney_CalculatesTotalPhotos()
    {
        // Act
        var response = await _journeyService.GetJourneyAsync("curiosity");

        // Assert
        response.Data.Attributes!.TotalPhotos.Should().Be(30); // 6 waypoints * 5 photos each
    }

    [Fact]
    public async Task GetJourney_CalculatesLocationsVisited()
    {
        // Act
        var response = await _journeyService.GetJourneyAsync("curiosity");

        // Assert
        response.Data.Attributes!.LocationsVisited.Should().Be(6);
    }

    [Fact]
    public async Task GetJourney_CalculatesSolRange()
    {
        // Act
        var response = await _journeyService.GetJourneyAsync("curiosity");

        // Assert
        response.Data.Attributes!.SolStart.Should().Be(1000);
        response.Data.Attributes.SolEnd.Should().Be(1005);
    }

    [Fact]
    public async Task GetJourney_CalculatesElevationChange()
    {
        // Act
        var response = await _journeyService.GetJourneyAsync("curiosity");

        // Assert
        response.Data.Attributes!.ElevationChangeM.Should().NotBeNull();
        // First waypoint: z = -9.0, Last waypoint: z = -6.5
        // Elevation change = -6.5 - (-9.0) = 2.5
        response.Data.Attributes.ElevationChangeM.Should().BeApproximately(2.5f, 0.1f);
    }

    [Fact]
    public async Task GetJourney_IncludesCoordinatesInWaypoints()
    {
        // Act
        var response = await _journeyService.GetJourneyAsync("curiosity");

        // Assert
        var firstWaypoint = response.Data.Path.First();
        firstWaypoint.Coordinates.Should().NotBeNull();
        firstWaypoint.Coordinates!.X.Should().BeApproximately(35.0f, 0.1f);
        firstWaypoint.Coordinates.Y.Should().BeApproximately(22.0f, 0.1f);
        firstWaypoint.Coordinates.Z.Should().BeApproximately(-9.0f, 0.1f);
    }

    [Fact]
    public async Task GetJourney_IncludesPhotoCountPerWaypoint()
    {
        // Act
        var response = await _journeyService.GetJourneyAsync("curiosity");

        // Assert
        response.Data.Path.Should().OnlyContain(wp => wp.PhotosTaken == 5);
    }

    [Fact]
    public async Task GetJourney_WithSolFilter_FiltersCorrectly()
    {
        // Act
        var response = await _journeyService.GetJourneyAsync(
            rover: "curiosity",
            solMin: 1001,
            solMax: 1003);

        // Assert
        response.Data.Path.Should().HaveCount(3);
        response.Data.Path.Should().OnlyContain(wp => wp.Sol >= 1001 && wp.Sol <= 1003);
        response.Data.Attributes!.SolStart.Should().Be(1001);
        response.Data.Attributes.SolEnd.Should().Be(1003);
    }

    [Fact]
    public async Task GetJourney_IncludesEarthDateInWaypoints()
    {
        // Act
        var response = await _journeyService.GetJourneyAsync("curiosity");

        // Assert
        response.Data.Path.Should().OnlyContain(wp => !string.IsNullOrEmpty(wp.EarthDate));
        response.Data.Path[0].EarthDate.Should().Be("2015-05-30");
    }

    [Fact]
    public async Task GetJourney_IncludesLocationInWaypoints()
    {
        // Act
        var response = await _journeyService.GetJourneyAsync("curiosity");

        // Assert
        var waypoint = response.Data.Path.First();
        waypoint.Site.Should().Be(79);
        waypoint.Drive.Should().Be(1200);
    }

    [Fact]
    public async Task GetJourney_IncludesVisualizationLinks()
    {
        // Act
        var response = await _journeyService.GetJourneyAsync("curiosity");

        // Assert
        response.Data.Links.Should().NotBeNull();
        response.Data.Links!.MapVisualization.Should().NotBeNullOrEmpty();
        response.Data.Links.MapVisualization.Should().Contain("/api/v2/rovers/curiosity/journey/map");
        response.Data.Links.KmlExport.Should().NotBeNullOrEmpty();
        response.Data.Links.KmlExport.Should().Contain("/api/v2/rovers/curiosity/journey/export/kml");
    }

    [Fact]
    public async Task GetJourney_CalculatesApproximateDistance()
    {
        // Act
        var response = await _journeyService.GetJourneyAsync("curiosity");

        // Assert
        response.Data.Attributes!.DistanceTraveledKm.Should().NotBeNull();
        response.Data.Attributes.DistanceTraveledKm.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetJourney_ForPerseverance_ReturnsCorrectData()
    {
        // Act
        var response = await _journeyService.GetJourneyAsync("perseverance");

        // Assert
        response.Data.Attributes!.Rover.Should().Be("perseverance");
        response.Data.Path.Should().HaveCount(3);
        response.Data.Attributes.TotalPhotos.Should().Be(9); // 3 waypoints * 3 photos each
        response.Data.Attributes.LocationsVisited.Should().Be(3);
    }

    [Fact]
    public async Task GetJourney_WithInvalidRover_ReturnsEmptyJourney()
    {
        // Act
        var response = await _journeyService.GetJourneyAsync("nonexistent");

        // Assert
        response.Data.Path.Should().BeEmpty();
        response.Data.Attributes!.Rover.Should().Be("nonexistent");
        response.Data.Attributes.LocationsVisited.Should().Be(0);
        response.Data.Attributes.TotalPhotos.Should().Be(0);
    }

    [Fact]
    public async Task GetJourney_WithNoMatchingData_ReturnsEmptyJourney()
    {
        // Act
        var response = await _journeyService.GetJourneyAsync(
            rover: "curiosity",
            solMin: 9999,
            solMax: 9999);

        // Assert
        response.Data.Path.Should().BeEmpty();
        response.Data.Attributes!.LocationsVisited.Should().Be(0);
        response.Data.Attributes.TotalPhotos.Should().Be(0);
    }

    [Fact]
    public async Task GetJourney_GroupsMultiplePhotosAtSameLocationCorrectly()
    {
        // Act
        var response = await _journeyService.GetJourneyAsync("curiosity");

        // Assert
        // Each waypoint has 5 photos, but they should be grouped into one waypoint
        response.Data.Path.Should().HaveCount(6); // Not 30
        response.Data.Path.Should().OnlyContain(wp => wp.PhotosTaken == 5);
    }

    [Fact]
    public async Task GetJourney_HandlesProgressionThroughMultipleSites()
    {
        // Act
        var response = await _journeyService.GetJourneyAsync("curiosity");

        // Assert
        var sites = response.Data.Path.Select(wp => wp.Site).Distinct().ToList();
        sites.Should().Contain(79);
        sites.Should().Contain(80);
        sites.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetJourney_CalculatesCorrectPhotoCountWithFiltering()
    {
        // Act
        var response = await _journeyService.GetJourneyAsync(
            rover: "curiosity",
            solMin: 1002,
            solMax: 1004);

        // Assert
        // 3 waypoints (1002, 1003, 1004) * 5 photos each = 15
        response.Data.Attributes!.TotalPhotos.Should().Be(15);
        response.Data.Path.Should().HaveCount(3);
    }
}
