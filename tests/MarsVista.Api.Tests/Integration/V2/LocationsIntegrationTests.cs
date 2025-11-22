using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MarsVista.Api.Entities;
using MarsVista.Api.Services.V2;

namespace MarsVista.Api.Tests.Integration.V2;

public class LocationsIntegrationTests : IntegrationTestBase
{
    private ILocationService _locationService = null!;

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<ILocationService, LocationService>();
    }

    protected override async Task SeedAdditionalDataAsync()
    {
        _locationService = ServiceProvider.GetRequiredService<ILocationService>();

        var now = DateTime.UtcNow;

        // Location 1: Site 79, Drive 1204 - 15 photos across multiple sols
        for (int i = 0; i < 15; i++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"NRF_1000_{i:D4}",
                Sol = 1000 + i,
                EarthDate = new DateTime(2015, 5, 30, 0, 0, 0, DateTimeKind.Utc).AddDays(i),
                DateTakenUtc = new DateTime(2015, 5, 30, 10, 0, 0, DateTimeKind.Utc).AddDays(i),
                Site = 79,
                Drive = 1204,
                Xyz = "{\"x\": 35.4362, \"y\": 22.5714, \"z\": -9.46445}",
                RoverId = 1,
                CameraId = 2,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        // Location 2: Site 79, Drive 1205 - 8 photos
        for (int i = 0; i < 8; i++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"NRF_1020_{i:D4}",
                Sol = 1020 + i,
                EarthDate = new DateTime(2015, 6, 20, 0, 0, 0, DateTimeKind.Utc).AddDays(i),
                DateTakenUtc = new DateTime(2015, 6, 20, 10, 0, 0, DateTimeKind.Utc).AddDays(i),
                Site = 79,
                Drive = 1205,
                Xyz = "{\"x\": 36.1234, \"y\": 23.4567, \"z\": -8.12345}",
                RoverId = 1,
                CameraId = 2,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        // Location 3: Site 80, Drive 1300 - 12 photos
        for (int i = 0; i < 12; i++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"NRF_1030_{i:D4}",
                Sol = 1030 + i,
                EarthDate = new DateTime(2015, 7, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i),
                DateTakenUtc = new DateTime(2015, 7, 1, 10, 0, 0, DateTimeKind.Utc).AddDays(i),
                Site = 80,
                Drive = 1300,
                Xyz = "{\"x\": 37.9876, \"y\": 24.6543, \"z\": -7.98765}",
                RoverId = 1,
                CameraId = 2,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        // Location 4: Site 85, Drive 1400 - 3 photos (for min_photos filter test)
        for (int i = 0; i < 3; i++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"NRF_1050_{i:D4}",
                Sol = 1050 + i,
                EarthDate = new DateTime(2015, 8, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i),
                DateTakenUtc = new DateTime(2015, 8, 1, 10, 0, 0, DateTimeKind.Utc).AddDays(i),
                Site = 85,
                Drive = 1400,
                Xyz = "{\"x\": 40.0, \"y\": 26.0, \"z\": -6.5}",
                RoverId = 1,
                CameraId = 2,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        // Perseverance location
        for (int i = 0; i < 10; i++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"NRP_500_{i:D4}",
                Sol = 500 + i,
                EarthDate = new DateTime(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i),
                DateTakenUtc = new DateTime(2021, 7, 1, 10, 0, 0, DateTimeKind.Utc).AddDays(i),
                Site = 1,
                Drive = 100,
                Xyz = "{\"x\": 10.0, \"y\": 20.0, \"z\": -5.0}",
                RoverId = 2,
                CameraId = 3,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetLocations_ReturnsAllLocations()
    {
        // Act
        var response = await _locationService.GetLocationsAsync(
            rovers: null,
            pageNumber: 1,
            pageSize: 25);

        // Assert
        response.Should().NotBeNull();
        response.Data.Should().NotBeNullOrEmpty();
        response.Data.Should().HaveCount(5); // 4 Curiosity locations + 1 Perseverance location
        response.Meta!.TotalCount.Should().Be(5);
    }

    [Fact]
    public async Task GetLocations_WithRoverFilter_FiltersCorrectly()
    {
        // Act
        var response = await _locationService.GetLocationsAsync(
            rovers: "curiosity",
            pageNumber: 1,
            pageSize: 25);

        // Assert
        response.Data.Should().HaveCount(4);
        response.Data.Should().OnlyContain(loc => loc.Attributes!.Rover == "curiosity");
    }

    [Fact]
    public async Task GetLocations_WithSolFilter_FiltersCorrectly()
    {
        // Act - Filter for sols 1020-1040
        var response = await _locationService.GetLocationsAsync(
            rovers: "curiosity",
            solMin: 1020,
            solMax: 1040,
            pageNumber: 1,
            pageSize: 25);

        // Assert
        // Should include locations 2 and 3 (sols 1020-1027 and 1030-1041)
        response.Data.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetLocations_WithMinPhotosFilter_FiltersCorrectly()
    {
        // Act - Require at least 10 photos
        var response = await _locationService.GetLocationsAsync(
            rovers: null,
            minPhotos: 10,
            pageNumber: 1,
            pageSize: 25);

        // Assert
        // Should only include locations with 10+ photos (locations 1, 3, and Perseverance)
        response.Data.Should().HaveCount(3);
        response.Data.Should().OnlyContain(loc => loc.Attributes!.PhotoCount >= 10);
    }

    [Fact]
    public async Task GetLocations_OrdersByPhotoCountDescending()
    {
        // Act
        var response = await _locationService.GetLocationsAsync(
            rovers: "curiosity",
            pageNumber: 1,
            pageSize: 25);

        // Assert
        response.Data.Should().NotBeEmpty();
        // First location should have the most photos (15)
        response.Data[0].Attributes!.PhotoCount.Should().Be(15);
        // Should be in descending order
        var photoCounts = response.Data.Select(loc => loc.Attributes!.PhotoCount).ToList();
        photoCounts.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GetLocations_ReturnsCompleteStructure()
    {
        // Act
        var response = await _locationService.GetLocationsAsync(
            rovers: "curiosity",
            pageNumber: 1,
            pageSize: 25);

        // Assert
        var location = response.Data.First();
        location.Should().NotBeNull();
        location.Id.Should().NotBeNullOrEmpty();
        location.Type.Should().Be("location");
        location.Attributes.Should().NotBeNull();
        location.Attributes!.Rover.Should().NotBeNullOrEmpty();
        location.Attributes.Site.Should().BeGreaterThan(0);
        location.Attributes.Drive.Should().BeGreaterThan(0);
        location.Attributes.PhotoCount.Should().BeGreaterThan(0);
        location.Attributes.FirstVisited.Should().NotBeNullOrEmpty();
        location.Attributes.LastVisited.Should().NotBeNullOrEmpty();
        location.Links.Should().NotBeNull();
        location.Links!.Photos.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetLocations_IncludesCoordinates()
    {
        // Act
        var response = await _locationService.GetLocationsAsync(
            rovers: "curiosity",
            pageNumber: 1,
            pageSize: 25);

        // Assert
        var location = response.Data.First();
        location.Attributes!.Coordinates.Should().NotBeNull();
        location.Attributes.Coordinates!.X.Should().NotBe(0);
        location.Attributes.Coordinates.Y.Should().NotBe(0);
        location.Attributes.Coordinates.Z.Should().NotBe(0);
    }

    [Fact]
    public async Task GetLocations_CalculatesVisitDates()
    {
        // Act
        var response = await _locationService.GetLocationsAsync(
            rovers: "curiosity",
            solMin: 1000,
            solMax: 1014,
            pageNumber: 1,
            pageSize: 25);

        // Assert
        var location = response.Data.First();
        location.Attributes!.FirstVisited.Should().Be("2015-05-30");
        location.Attributes.LastVisited.Should().Be("2015-06-13"); // 14 days later
    }

    [Fact]
    public async Task GetLocations_CalculatesVisitCount()
    {
        // Act
        var response = await _locationService.GetLocationsAsync(
            rovers: "curiosity",
            solMin: 1000,
            solMax: 1014,
            pageNumber: 1,
            pageSize: 25);

        // Assert
        var location = response.Data.First();
        // VisitCount = LastSol - FirstSol + 1 = 1014 - 1000 + 1 = 15
        location.Attributes!.VisitCount.Should().Be(15);
    }

    [Fact]
    public async Task GetLocations_GeneratesCorrectLocationId()
    {
        // Act
        var response = await _locationService.GetLocationsAsync(
            rovers: "curiosity",
            pageNumber: 1,
            pageSize: 25);

        // Assert
        response.Data.Should().Contain(loc => loc.Id == "curiosity_79_1204");
        response.Data.Should().Contain(loc => loc.Id == "curiosity_79_1205");
        response.Data.Should().Contain(loc => loc.Id == "curiosity_80_1300");
    }

    [Fact]
    public async Task GetLocations_IncludesPhotosLink()
    {
        // Act
        var response = await _locationService.GetLocationsAsync(
            rovers: "curiosity",
            pageNumber: 1,
            pageSize: 25);

        // Assert
        var location = response.Data.First();
        location.Links!.Photos.Should().Contain("/api/v2/photos?");
        location.Links.Photos.Should().Contain("site=");
        location.Links.Photos.Should().Contain("drive=");
        location.Links.Photos.Should().Contain("rovers=");
    }

    [Fact]
    public async Task GetLocations_WithPagination_ReturnsCorrectPage()
    {
        // Act - Get first page with 2 items
        var page1 = await _locationService.GetLocationsAsync(
            rovers: null,
            pageNumber: 1,
            pageSize: 2);

        // Assert
        page1.Data.Should().HaveCount(2);
        page1.Pagination!.Page.Should().Be(1);
        page1.Pagination.PerPage.Should().Be(2);
        page1.Pagination.TotalPages.Should().Be(3); // 5 total locations / 2 per page = 3 pages
    }

    [Fact]
    public async Task GetLocationById_WithValidId_ReturnsLocation()
    {
        // Arrange
        var locationId = "curiosity_79_1204";

        // Act
        var location = await _locationService.GetLocationByIdAsync(locationId);

        // Assert
        location.Should().NotBeNull();
        location!.Id.Should().Be(locationId);
        location.Type.Should().Be("location");
        location.Attributes!.Rover.Should().Be("curiosity");
        location.Attributes.Site.Should().Be(79);
        location.Attributes.Drive.Should().Be(1204);
        location.Attributes.PhotoCount.Should().Be(15);
    }

    [Fact]
    public async Task GetLocationById_WithInvalidId_ReturnsNull()
    {
        // Act
        var location = await _locationService.GetLocationByIdAsync("curiosity_999_9999");

        // Assert
        location.Should().BeNull();
    }

    [Fact]
    public async Task GetLocationById_ReturnsCompleteData()
    {
        // Arrange
        var locationId = "curiosity_79_1204";

        // Act
        var location = await _locationService.GetLocationByIdAsync(locationId);

        // Assert
        location.Should().NotBeNull();
        location!.Attributes!.FirstVisited.Should().NotBeNullOrEmpty();
        location.Attributes.LastVisited.Should().NotBeNullOrEmpty();
        location.Attributes.FirstSol.Should().BeGreaterThan(0);
        location.Attributes.LastSol.Should().BeGreaterThan(0);
        location.Attributes.VisitCount.Should().BeGreaterThan(0);
        location.Attributes.Coordinates.Should().NotBeNull();
        location.Links!.Photos.Should().NotBeNullOrEmpty();
    }
}
