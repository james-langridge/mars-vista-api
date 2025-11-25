using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MarsVista.Api.DTOs.V2;
using MarsVista.Core.Entities;
using MarsVista.Api.Services.V2;

namespace MarsVista.Api.Tests.Integration.V2;

public class PanoramasIntegrationTests : IntegrationTestBase
{
    private IPanoramaService _panoramaService = null!;
    private IPhotoQueryServiceV2 _photoQueryService = null!;

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IPhotoQueryServiceV2, PhotoQueryServiceV2>();
        services.AddScoped<IPanoramaService, PanoramaService>();
    }

    protected override async Task SeedAdditionalDataAsync()
    {
        _panoramaService = ServiceProvider.GetRequiredService<IPanoramaService>();
        _photoQueryService = ServiceProvider.GetRequiredService<IPhotoQueryServiceV2>();

        var now = DateTime.UtcNow;

        // Add panorama sequence for Curiosity at sol 1000
        for (int i = 0; i < 6; i++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"NRF_1000_{i:D4}",
                Sol = 1000,
                EarthDate = new DateTime(2015, 5, 30, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2015, 5, 30, 10, i, 0, DateTimeKind.Utc),
                DateTakenMars = $"Sol-1000M14:0{i}:00",
                Site = 79,
                Drive = 1204,
                MastAz = 45.0f + (i * 15.0f), // 75 degree range
                MastEl = -10.0f, // Same elevation
                SpacecraftClock = 813073000.0f + (i * 100.0f), // 100 seconds apart (avoid float precision)
                Xyz = "{\"x\": 35.4362, \"y\": 22.5714, \"z\": -9.46445}",
                ImgSrcSmall = $"https://mars.nasa.gov/pano_{i}_s.jpg",
                ImgSrcMedium = $"https://mars.nasa.gov/pano_{i}_m.jpg",
                ImgSrcLarge = $"https://mars.nasa.gov/pano_{i}.jpg",
                ImgSrcFull = $"https://mars.nasa.gov/pano_{i}.jpg",
                RoverId = 1,
                CameraId = 2, // MAST
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        // Add another panorama sequence at different location/sol
        for (int i = 0; i < 4; i++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"NRF_1010_{i:D4}",
                Sol = 1010,
                EarthDate = new DateTime(2015, 6, 10, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2015, 6, 10, 11, i, 0, DateTimeKind.Utc),
                DateTakenMars = $"Sol-1010M15:0{i}:00",
                Site = 80,
                Drive = 1205,
                MastAz = 100.0f + (i * 12.0f), // 36 degree range
                MastEl = 5.0f,
                SpacecraftClock = 823073000.0f + (i * 100.0f),
                Xyz = "{\"x\": 36.1234, \"y\": 23.5678, \"z\": -8.12345}",
                ImgSrcSmall = $"https://mars.nasa.gov/pano2_{i}_s.jpg",
                ImgSrcMedium = $"https://mars.nasa.gov/pano2_{i}_m.jpg",
                ImgSrcLarge = $"https://mars.nasa.gov/pano2_{i}.jpg",
                ImgSrcFull = $"https://mars.nasa.gov/pano2_{i}.jpg",
                RoverId = 1,
                CameraId = 2,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        // Add Perseverance panorama
        for (int i = 0; i < 5; i++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"NRP_500_{i:D4}",
                Sol = 500,
                EarthDate = new DateTime(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2021, 7, 1, 10, i, 0, DateTimeKind.Utc),
                DateTakenMars = $"Sol-500M14:0{i}:00",
                Site = 1,
                Drive = 100,
                MastAz = 200.0f + (i * 10.0f), // 40 degree range
                MastEl = 0.0f,
                SpacecraftClock = 900000000.0f + (i * 100.0f),
                Xyz = "{\"x\": 10.0, \"y\": 20.0, \"z\": -5.0}",
                ImgSrcSmall = $"https://mars.nasa.gov/pers_pano_{i}_s.jpg",
                ImgSrcMedium = $"https://mars.nasa.gov/pers_pano_{i}_m.jpg",
                ImgSrcLarge = $"https://mars.nasa.gov/pers_pano_{i}.jpg",
                ImgSrcFull = $"https://mars.nasa.gov/pers_pano_{i}.jpg",
                RoverId = 2,
                CameraId = 3, // NAVCAM for Perseverance
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetPanoramas_ReturnsDetectedPanoramas()
    {
        // Act
        var response = await _panoramaService.GetPanoramasAsync(
            rovers: null, // All rovers
            solMin: 0, // Explicit range to include all test data (overrides default 500-sol limit)
            solMax: 2000,
            pageNumber: 1,
            pageSize: 25);

        // Assert
        response.Should().NotBeNull();
        response.Data.Should().NotBeNullOrEmpty();
        response.Data.Should().HaveCountGreaterThanOrEqualTo(3); // At least 3 panoramas
        response.Meta!.TotalCount.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task GetPanoramas_WithRoverFilter_FiltersCorrectly()
    {
        // Act
        var response = await _panoramaService.GetPanoramasAsync(
            rovers: "curiosity",
            pageNumber: 1,
            pageSize: 25);

        // Assert
        response.Data.Should().NotBeEmpty();
        response.Data.Should().OnlyContain(p => p.Attributes!.Rover == "curiosity");
    }

    [Fact]
    public async Task GetPanoramas_WithSolFilter_FiltersCorrectly()
    {
        // Act
        var response = await _panoramaService.GetPanoramasAsync(
            rovers: "curiosity",
            solMin: 1000,
            solMax: 1000,
            pageNumber: 1,
            pageSize: 25);

        // Assert
        response.Data.Should().NotBeEmpty();
        response.Data.Should().OnlyContain(p => p.Attributes!.Sol == 1000);
    }

    [Fact]
    public async Task GetPanoramas_WithMinPhotosFilter_FiltersCorrectly()
    {
        // Act - Require at least 5 photos
        var response = await _panoramaService.GetPanoramasAsync(
            rovers: null,
            solMin: 0, // Explicit range to include all test data (overrides default 500-sol limit)
            solMax: 2000,
            minPhotos: 5,
            pageNumber: 1,
            pageSize: 25);

        // Assert
        response.Data.Should().NotBeEmpty();
        response.Data.Should().OnlyContain(p => p.Attributes!.TotalPhotos >= 5);
    }

    [Fact]
    public async Task GetPanoramas_ReturnsCompleteStructure()
    {
        // Act
        var response = await _panoramaService.GetPanoramasAsync(
            rovers: "curiosity",
            pageNumber: 1,
            pageSize: 25);

        // Assert
        var panorama = response.Data.First();
        panorama.Should().NotBeNull();
        panorama.Id.Should().NotBeNullOrEmpty();
        panorama.Type.Should().Be("panorama");
        panorama.Attributes.Should().NotBeNull();
        panorama.Attributes!.Rover.Should().NotBeNullOrEmpty();
        panorama.Attributes.Sol.Should().BeGreaterThan(0);
        panorama.Attributes.TotalPhotos.Should().BeGreaterThan(0);
        panorama.Attributes.CoverageDegrees.Should().BeGreaterThan(0);
        panorama.Links.Should().NotBeNull();
        panorama.Links!.DownloadSet.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetPanoramas_IncludesLocationData()
    {
        // Act
        var response = await _panoramaService.GetPanoramasAsync(
            rovers: "curiosity",
            pageNumber: 1,
            pageSize: 25);

        // Assert
        var panorama = response.Data.First();
        panorama.Attributes!.Location.Should().NotBeNull();
        panorama.Attributes.Location!.Site.Should().BeGreaterThan(0);
        panorama.Attributes.Location.Drive.Should().BeGreaterThan(0);
        panorama.Attributes.Location.Coordinates.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPanoramas_WithPagination_ReturnsCorrectPage()
    {
        // Act - Get first page
        var page1 = await _panoramaService.GetPanoramasAsync(
            rovers: null,
            solMin: 0, // Explicit range to include all test data (overrides default 500-sol limit)
            solMax: 2000,
            pageNumber: 1,
            pageSize: 2);

        // Assert
        page1.Data.Should().HaveCount(2);
        page1.Pagination!.Page.Should().Be(1);
        page1.Pagination.PerPage.Should().Be(2);
        page1.Pagination.TotalPages.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetPanoramaById_WithValidId_ReturnsPanorama()
    {
        // Arrange - Get all panoramas first to get a valid ID
        var allPanoramas = await _panoramaService.GetPanoramasAsync(
            rovers: "curiosity",
            pageNumber: 1,
            pageSize: 25);
        var panoramaId = allPanoramas.Data.First().Id;

        // Act
        var panorama = await _panoramaService.GetPanoramaByIdAsync(panoramaId);

        // Assert
        panorama.Should().NotBeNull();
        panorama!.Id.Should().Be(panoramaId);
        panorama.Type.Should().Be("panorama");
        panorama.Attributes.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPanoramaById_WithInvalidId_ReturnsNull()
    {
        // Act
        var panorama = await _panoramaService.GetPanoramaByIdAsync("pano_curiosity_9999_0");

        // Assert
        panorama.Should().BeNull();
    }

    [Fact]
    public async Task GetPanoramas_CalculatesCorrectCoverage()
    {
        // Act
        var response = await _panoramaService.GetPanoramasAsync(
            rovers: "curiosity",
            solMin: 1000,
            solMax: 1000,
            pageNumber: 1,
            pageSize: 25);

        // Assert
        var panorama = response.Data.First();
        // 6 photos from 45 to 120 degrees = 75 degree coverage
        panorama.Attributes!.CoverageDegrees.Should().BeApproximately(75.0f, 1.0f);
    }

    [Fact]
    public async Task GetPanoramas_IncludesMarsTimeRange()
    {
        // Act
        var response = await _panoramaService.GetPanoramasAsync(
            rovers: "curiosity",
            pageNumber: 1,
            pageSize: 25);

        // Assert
        var panorama = response.Data.First();
        panorama.Attributes!.MarsTimeStart.Should().NotBeNullOrEmpty();
        panorama.Attributes.MarsTimeEnd.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetPanoramas_IncludesAverageElevation()
    {
        // Act
        var response = await _panoramaService.GetPanoramasAsync(
            rovers: "curiosity",
            solMin: 1000,
            solMax: 1000,
            pageNumber: 1,
            pageSize: 25);

        // Assert
        var panorama = response.Data.First();
        panorama.Attributes!.AvgElevation.Should().BeApproximately(-10.0f, 0.5f);
    }

    [Fact]
    public async Task GetPanoramas_IncludesCameraName()
    {
        // Act
        var response = await _panoramaService.GetPanoramasAsync(
            rovers: "curiosity",
            pageNumber: 1,
            pageSize: 25);

        // Assert
        response.Data.Should().OnlyContain(p => !string.IsNullOrEmpty(p.Attributes!.Camera));
    }
}
