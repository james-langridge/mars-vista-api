using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MarsVista.Api.Entities;
using MarsVista.Api.Services.V2;
using MarsVista.Api.Tests.Integration;

namespace MarsVista.Api.Tests.Services.V2;

public class LocationServiceTests : IntegrationTestBase
{
    private Mock<ILogger<LocationService>> _mockLogger = null!;
    private LocationService _service = null!;

    protected override void ConfigureServices(IServiceCollection services)
    {
        _mockLogger = new Mock<ILogger<LocationService>>();
        services.AddSingleton(_mockLogger.Object);
        services.AddScoped<LocationService>();
    }

    protected override async Task SeedAdditionalDataAsync()
    {
        _service = ServiceProvider.GetRequiredService<LocationService>();
        var now = DateTime.UtcNow;

        // Add photos at location 1 (site 79, drive 1204) - 10 photos
        for (int i = 0; i < 10; i++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"NRF_1000_{i:D4}",
                Sol = 1000 + i,
                EarthDate = new DateTime(2015, 5, 30, 0, 0, 0, DateTimeKind.Utc).AddDays(i),
                DateTakenUtc = new DateTime(2015, 5, 30, 10, 0, 0, DateTimeKind.Utc).AddDays(i),
                ImgSrcSmall = $"https://mars.nasa.gov/1000_{i}_s.jpg",
                ImgSrcMedium = $"https://mars.nasa.gov/1000_{i}_m.jpg",
                ImgSrcLarge = $"https://mars.nasa.gov/1000_{i}_l.jpg",
                ImgSrcFull = $"https://mars.nasa.gov/1000_{i}_f.jpg",
                Site = 79,
                Drive = 1204,
                Xyz = "{\"x\": 35.4362, \"y\": 22.5714, \"z\": -9.46445}",
                RoverId = 1,
                CameraId = 2, // MAST
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        // Add photos at location 2 (site 79, drive 1205) - 5 photos
        for (int i = 0; i < 5; i++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"NRF_1010_{i:D4}",
                Sol = 1010 + i,
                EarthDate = new DateTime(2015, 6, 10, 0, 0, 0, DateTimeKind.Utc).AddDays(i),
                DateTakenUtc = new DateTime(2015, 6, 10, 10, 0, 0, DateTimeKind.Utc).AddDays(i),
                ImgSrcSmall = $"https://mars.nasa.gov/1010_{i}_s.jpg",
                ImgSrcMedium = $"https://mars.nasa.gov/1010_{i}_m.jpg",
                ImgSrcLarge = $"https://mars.nasa.gov/1010_{i}_l.jpg",
                ImgSrcFull = $"https://mars.nasa.gov/1010_{i}_f.jpg",
                Site = 79,
                Drive = 1205,
                Xyz = "{\"x\": 36.1234, \"y\": 23.4567, \"z\": -8.12345}",
                RoverId = 1,
                CameraId = 2, // MAST
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        // Add photos at location 3 (site 80, drive 1300) - 3 photos
        for (int i = 0; i < 3; i++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"NRF_1020_{i:D4}",
                Sol = 1020 + i,
                EarthDate = new DateTime(2015, 6, 20, 0, 0, 0, DateTimeKind.Utc).AddDays(i),
                DateTakenUtc = new DateTime(2015, 6, 20, 10, 0, 0, DateTimeKind.Utc).AddDays(i),
                ImgSrcSmall = $"https://mars.nasa.gov/1020_{i}_s.jpg",
                ImgSrcMedium = $"https://mars.nasa.gov/1020_{i}_m.jpg",
                ImgSrcLarge = $"https://mars.nasa.gov/1020_{i}_l.jpg",
                ImgSrcFull = $"https://mars.nasa.gov/1020_{i}_f.jpg",
                Site = 80,
                Drive = 1300,
                Xyz = "{\"x\": 37.9876, \"y\": 24.6543, \"z\": -7.98765}",
                RoverId = 1,
                CameraId = 2, // MAST
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        // Add photos without location data (should be excluded)
        DbContext.Photos.Add(new Photo
        {
            NasaId = "NRF_1030_NO_LOCATION",
            Sol = 1030,
            EarthDate = new DateTime(2015, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            DateTakenUtc = new DateTime(2015, 7, 1, 10, 0, 0, DateTimeKind.Utc),
            ImgSrcSmall = "https://mars.nasa.gov/1030_s.jpg",
            ImgSrcMedium = "https://mars.nasa.gov/1030_m.jpg",
            ImgSrcLarge = "https://mars.nasa.gov/1030_l.jpg",
            ImgSrcFull = "https://mars.nasa.gov/1030_f.jpg",
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
    public async Task GetLocationsAsync_WithValidData_ReturnsLocations()
    {
        // Act
        var result = await _service.GetLocationsAsync(
            rovers: "curiosity",
            pageNumber: 1,
            pageSize: 25);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().NotBeNullOrEmpty();
        result.Data.Should().HaveCount(3); // 3 unique locations
        result.Meta!.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetLocationsAsync_OrdersByPhotoCountDescending()
    {
        // Act
        var result = await _service.GetLocationsAsync(
            rovers: "curiosity",
            pageNumber: 1,
            pageSize: 25);

        // Assert
        result.Data.Should().NotBeNullOrEmpty();
        result.Data[0].Attributes!.PhotoCount.Should().Be(10); // Most photos
        result.Data[1].Attributes!.PhotoCount.Should().Be(5);
        result.Data[2].Attributes!.PhotoCount.Should().Be(3); // Least photos
    }

    [Fact]
    public async Task GetLocationsAsync_CalculatesVisitDates()
    {
        // Act
        var result = await _service.GetLocationsAsync(
            rovers: "curiosity",
            pageNumber: 1,
            pageSize: 25);

        // Assert
        var location = result.Data.First();
        location.Attributes!.FirstVisited.Should().Be("2015-05-30");
        location.Attributes.LastVisited.Should().Be("2015-06-08");
        location.Attributes.FirstSol.Should().Be(1000);
        location.Attributes.LastSol.Should().Be(1009);
    }

    [Fact]
    public async Task GetLocationsAsync_IncludesCoordinates()
    {
        // Act
        var result = await _service.GetLocationsAsync(
            rovers: "curiosity",
            pageNumber: 1,
            pageSize: 25);

        // Assert
        var location = result.Data.First();
        location.Attributes!.Coordinates.Should().NotBeNull();
        location.Attributes.Coordinates!.X.Should().BeApproximately(35.4362f, 0.001f);
        location.Attributes.Coordinates.Y.Should().BeApproximately(22.5714f, 0.001f);
        location.Attributes.Coordinates.Z.Should().BeApproximately(-9.46445f, 0.001f);
    }

    [Fact]
    public async Task GetLocationsAsync_GeneratesCorrectLocationId()
    {
        // Act
        var result = await _service.GetLocationsAsync(
            rovers: "curiosity",
            pageNumber: 1,
            pageSize: 25);

        // Assert
        result.Data.Should().Contain(loc => loc.Id == "curiosity_79_1204");
        result.Data.Should().Contain(loc => loc.Id == "curiosity_79_1205");
        result.Data.Should().Contain(loc => loc.Id == "curiosity_80_1300");
    }

    [Fact]
    public async Task GetLocationsAsync_IncludesPhotosLink()
    {
        // Act
        var result = await _service.GetLocationsAsync(
            rovers: "curiosity",
            pageNumber: 1,
            pageSize: 25);

        // Assert
        var location = result.Data.First();
        location.Links.Should().NotBeNull();
        location.Links!.Photos.Should().NotBeNullOrEmpty();
        location.Links.Photos.Should().Contain("/api/v2/photos?");
        location.Links.Photos.Should().Contain("site=79");
        location.Links.Photos.Should().Contain("drive=1204");
    }

    [Fact]
    public async Task GetLocationsAsync_WithSolFilter_FiltersCorrectly()
    {
        // Act
        var result = await _service.GetLocationsAsync(
            rovers: "curiosity",
            solMin: 1015,
            solMax: 1025,
            pageNumber: 1,
            pageSize: 25);

        // Assert
        result.Data.Should().HaveCount(1); // Only location 3 in this range (1020-1022)
        result.Data.Should().Contain(loc => loc.Attributes!.Site == 80 && loc.Attributes.Drive == 1300);
    }

    [Fact]
    public async Task GetLocationsAsync_WithMinPhotosFilter_FiltersCorrectly()
    {
        // Act - Require at least 5 photos
        var result = await _service.GetLocationsAsync(
            rovers: "curiosity",
            minPhotos: 5,
            pageNumber: 1,
            pageSize: 25);

        // Assert
        result.Data.Should().HaveCount(2); // Only locations with 10 and 5 photos
        result.Data.Should().OnlyContain(loc => loc.Attributes!.PhotoCount >= 5);
    }

    [Fact]
    public async Task GetLocationsAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange - Add more locations
        var now = DateTime.UtcNow;
        for (int i = 0; i < 30; i++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"NRF_{2000 + i}_0000",
                Sol = 2000 + i,
                EarthDate = new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i),
                DateTakenUtc = new DateTime(2016, 1, 1, 10, 0, 0, DateTimeKind.Utc).AddDays(i),
                ImgSrcSmall = $"https://mars.nasa.gov/{2000 + i}_s.jpg",
                ImgSrcMedium = $"https://mars.nasa.gov/{2000 + i}_m.jpg",
                ImgSrcLarge = $"https://mars.nasa.gov/{2000 + i}_l.jpg",
                ImgSrcFull = $"https://mars.nasa.gov/{2000 + i}_f.jpg",
                Site = 100 + i,
                Drive = 2000,
                Xyz = $"{{\"x\": {40.0 + i}, \"y\": 30.0, \"z\": -5.0}}",
                RoverId = 1,
                CameraId = 2, // MAST
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        await DbContext.SaveChangesAsync();

        // Act - Get page 2 with page size 10
        var result = await _service.GetLocationsAsync(
            rovers: "curiosity",
            pageNumber: 2,
            pageSize: 10);

        // Assert
        result.Data.Should().HaveCount(10);
        result.Pagination!.Page.Should().Be(2);
        result.Pagination.PerPage.Should().Be(10);
        result.Pagination.TotalPages.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task GetLocationsAsync_ExcludesPhotosWithoutLocation()
    {
        // Act
        var result = await _service.GetLocationsAsync(
            rovers: "curiosity",
            pageNumber: 1,
            pageSize: 25);

        // Assert
        result.Data.Should().HaveCount(3); // Photo with no location excluded
        result.Data.Should().OnlyContain(loc =>
            loc.Attributes!.Site > 0 && loc.Attributes.Drive > 0);
    }

    [Fact]
    public async Task GetLocationByIdAsync_WithValidId_ReturnsLocation()
    {
        // Arrange
        var locationId = "curiosity_79_1204";

        // Act
        var result = await _service.GetLocationByIdAsync(locationId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(locationId);
        result.Type.Should().Be("location");
        result.Attributes!.Rover.Should().Be("curiosity");
        result.Attributes.Site.Should().Be(79);
        result.Attributes.Drive.Should().Be(1204);
        result.Attributes.PhotoCount.Should().Be(10);
    }

    [Fact]
    public async Task GetLocationByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var invalidId = "curiosity_999_9999";

        // Act
        var result = await _service.GetLocationByIdAsync(invalidId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLocationByIdAsync_WithMalformedId_ReturnsNull()
    {
        // Arrange
        var malformedIds = new[]
        {
            "invalid",
            "curiosity_79", // Missing drive
            "curiosity_abc_1204", // Invalid site
            "curiosity_79_xyz", // Invalid drive
            "curiosity_79_1204_extra" // Too many parts
        };

        foreach (var malformedId in malformedIds)
        {
            // Act
            var result = await _service.GetLocationByIdAsync(malformedId);

            // Assert
            result.Should().BeNull($"ID '{malformedId}' should return null");
        }
    }

    [Fact]
    public async Task GetLocationsAsync_CalculatesVisitCount()
    {
        // Act
        var result = await _service.GetLocationsAsync(
            rovers: "curiosity",
            pageNumber: 1,
            pageSize: 25);

        // Assert
        var location = result.Data.First();
        // Visit count = LastSol - FirstSol + 1
        location.Attributes!.VisitCount.Should().Be(10); // 1009 - 1000 + 1
    }

    [Fact]
    public async Task GetLocationsAsync_WithMultipleRovers_GroupsSeparately()
    {
        // Arrange - Add Perseverance photos at same site/drive
        var now = DateTime.UtcNow;

        // Add Perseverance photos at same site/drive as Curiosity
        for (int i = 0; i < 7; i++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"NRP_1000_{i:D4}",
                Sol = 1000 + i,
                EarthDate = new DateTime(2021, 3, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i),
                DateTakenUtc = new DateTime(2021, 3, 1, 10, 0, 0, DateTimeKind.Utc).AddDays(i),
                ImgSrcSmall = $"https://mars.nasa.gov/pers1000_{i}_s.jpg",
                ImgSrcMedium = $"https://mars.nasa.gov/pers1000_{i}_m.jpg",
                ImgSrcLarge = $"https://mars.nasa.gov/pers1000_{i}_l.jpg",
                ImgSrcFull = $"https://mars.nasa.gov/pers1000_{i}_f.jpg",
                Site = 79, // Same site as Curiosity
                Drive = 1204, // Same drive as Curiosity
                Xyz = "{\"x\": 10.0, \"y\": 20.0, \"z\": -5.0}",
                RoverId = 2, // Perseverance
                CameraId = 3, // NAVCAM
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        await DbContext.SaveChangesAsync();

        // Act - Query both rovers
        var result = await _service.GetLocationsAsync(
            rovers: "curiosity,perseverance",
            pageNumber: 1,
            pageSize: 25);

        // Assert - Should have separate locations for each rover at same site/drive
        result.Data.Should().Contain(loc => loc.Id == "curiosity_79_1204");
        result.Data.Should().Contain(loc => loc.Id == "perseverance_79_1204");

        var curiosityLocation = result.Data.First(loc => loc.Attributes!.Rover == "curiosity" &&
            loc.Attributes.Site == 79 && loc.Attributes.Drive == 1204);
        var perseveranceLocation = result.Data.First(loc => loc.Attributes!.Rover == "perseverance" &&
            loc.Attributes.Site == 79 && loc.Attributes.Drive == 1204);

        curiosityLocation.Attributes!.PhotoCount.Should().Be(10);
        perseveranceLocation.Attributes!.PhotoCount.Should().Be(7);
    }

    [Fact]
    public async Task GetLocationsAsync_WithEmptyDatabase_ReturnsEmptyList()
    {
        // Arrange - Clear all photos
        DbContext.Photos.RemoveRange(DbContext.Photos);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetLocationsAsync(
            rovers: "curiosity",
            pageNumber: 1,
            pageSize: 25);

        // Assert
        result.Data.Should().BeEmpty();
        result.Meta!.TotalCount.Should().Be(0);
        result.Pagination!.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task GetLocationsAsync_HandlesLocationWithoutCoordinates()
    {
        // Arrange - Add location without XYZ coordinates
        var now = DateTime.UtcNow;
        DbContext.Photos.Add(new Photo
        {
            NasaId = "NRF_3000_NO_XYZ",
            Sol = 3000,
            EarthDate = new DateTime(2015, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            DateTakenUtc = new DateTime(2015, 8, 1, 10, 0, 0, DateTimeKind.Utc),
            ImgSrcSmall = "https://mars.nasa.gov/3000_s.jpg",
            ImgSrcMedium = "https://mars.nasa.gov/3000_m.jpg",
            ImgSrcLarge = "https://mars.nasa.gov/3000_l.jpg",
            ImgSrcFull = "https://mars.nasa.gov/3000_f.jpg",
            Site = 90,
            Drive = 1500,
            Xyz = null, // No coordinates
            RoverId = 1,
            CameraId = 2, // MAST
            CreatedAt = now,
            UpdatedAt = now
        });
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetLocationsAsync(
            rovers: "curiosity",
            solMin: 3000,
            solMax: 3000,
            pageNumber: 1,
            pageSize: 25);

        // Assert
        result.Data.Should().HaveCount(1);
        var location = result.Data.First();
        location.Attributes!.Coordinates.Should().BeNull();
    }

    [Fact]
    public async Task GetLocationsAsync_SetsCorrectResourceType()
    {
        // Act
        var result = await _service.GetLocationsAsync(
            rovers: "curiosity",
            pageNumber: 1,
            pageSize: 25);

        // Assert
        result.Data.Should().OnlyContain(loc => loc.Type == "location");
    }
}
