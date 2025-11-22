using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MarsVista.Api.Entities;
using MarsVista.Api.Services.V2;
using MarsVista.Api.Tests.Integration;

namespace MarsVista.Api.Tests.Services.V2;

public class PanoramaServiceTests : IntegrationTestBase
{
    private Mock<ILogger<PanoramaService>> _mockLogger = null!;
    private Mock<IPhotoQueryServiceV2> _mockPhotoService = null!;
    private PanoramaService _service = null!;

    protected override void ConfigureServices(IServiceCollection services)
    {
        _mockLogger = new Mock<ILogger<PanoramaService>>();
        _mockPhotoService = new Mock<IPhotoQueryServiceV2>();

        services.AddSingleton(_mockLogger.Object);
        services.AddSingleton(_mockPhotoService.Object);
        services.AddScoped<PanoramaService>();
    }

    protected override async Task SeedAdditionalDataAsync()
    {
        // Get service after initialization
        _service = ServiceProvider.GetRequiredService<PanoramaService>();

        var now = DateTime.UtcNow;

        // Add panorama sequence (5 photos at same location with sequential azimuths)
        for (int i = 0; i < 5; i++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"NRF_1000_{i:D4}",
                Sol = 1000,
                EarthDate = new DateTime(2015, 5, 30, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2015, 5, 30, 10, i, 0, DateTimeKind.Utc),
                DateTakenMars = $"Sol-1000M14:0{i}:00",
                ImgSrcSmall = $"https://mars.nasa.gov/photo{i}_s.jpg",
                ImgSrcMedium = $"https://mars.nasa.gov/photo{i}_m.jpg",
                ImgSrcLarge = $"https://mars.nasa.gov/photo{i}_l.jpg",
                ImgSrcFull = $"https://mars.nasa.gov/photo{i}_f.jpg",
                Site = 79,
                Drive = 1204,
                MastAz = 45.0f + (i * 10.0f), // 45, 55, 65, 75, 85 degrees (40 degree range)
                MastEl = -10.0f, // Same elevation
                SpacecraftClock = 813073000.0f + (i * 100.0f), // 100 seconds apart (avoid float precision issues)
                Xyz = "{\"x\": 35.4362, \"y\": 22.5714, \"z\": -9.46445}",
                RoverId = 1,
                CameraId = 2, // MAST camera
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        // Add non-panorama photos (different elevation)
        DbContext.Photos.Add(new Photo
        {
            NasaId = "NRF_1000_9999",
            Sol = 1000,
            EarthDate = new DateTime(2015, 5, 30, 0, 0, 0, DateTimeKind.Utc),
            DateTakenUtc = new DateTime(2015, 5, 30, 11, 0, 0, DateTimeKind.Utc),
            DateTakenMars = "Sol-1000M15:00:00",
            ImgSrcSmall = "https://mars.nasa.gov/photo9999_s.jpg",
            ImgSrcMedium = "https://mars.nasa.gov/photo9999_m.jpg",
            ImgSrcLarge = "https://mars.nasa.gov/photo9999_l.jpg",
            ImgSrcFull = "https://mars.nasa.gov/photo9999_f.jpg",
            Site = 79,
            Drive = 1204,
            MastAz = 100.0f,
            MastEl = 30.0f, // Different elevation (not part of panorama)
            SpacecraftClock = 813074000.0f,
            RoverId = 1,
            CameraId = 2,
            CreatedAt = now,
            UpdatedAt = now
        });

        // Add photos without required telemetry (should be excluded)
        DbContext.Photos.Add(new Photo
        {
            NasaId = "NRF_1000_NOTELEMETRY",
            Sol = 1000,
            EarthDate = new DateTime(2015, 5, 30, 0, 0, 0, DateTimeKind.Utc),
            DateTakenUtc = new DateTime(2015, 5, 30, 12, 0, 0, DateTimeKind.Utc),
            DateTakenMars = "Sol-1000M16:00:00",
            ImgSrcSmall = "https://mars.nasa.gov/photonotel_s.jpg",
            ImgSrcMedium = "https://mars.nasa.gov/photonotel_m.jpg",
            ImgSrcLarge = "https://mars.nasa.gov/photonotel_l.jpg",
            ImgSrcFull = "https://mars.nasa.gov/photonotel_f.jpg",
            Site = 79,
            Drive = 1204,
            MastAz = null, // Missing telemetry
            MastEl = null,
            SpacecraftClock = null,
            RoverId = 1,
            CameraId = 2,
            CreatedAt = now,
            UpdatedAt = now
        });

        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetPanoramasAsync_WithValidData_DetectsPanorama()
    {
        // Act
        var result = await _service.GetPanoramasAsync(
            rovers: "curiosity",
            pageNumber: 1,
            pageSize: 25);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().NotBeNullOrEmpty();
        result.Data.Count.Should().BeGreaterThanOrEqualTo(1);

        var panorama = result.Data.First();
        panorama.Type.Should().Be("panorama");
        panorama.Attributes.Should().NotBeNull();
        panorama.Attributes!.Rover.Should().Be("curiosity");
        panorama.Attributes.Sol.Should().Be(1000);
        panorama.Attributes.TotalPhotos.Should().Be(5);
        panorama.Attributes.CoverageDegrees.Should().BeApproximately(40.0f, 0.1f);
    }

    [Fact]
    public async Task GetPanoramasAsync_WithSolFilter_FiltersCorrectly()
    {
        // Arrange - Add photos on different sol
        var now = DateTime.UtcNow;
        for (int i = 0; i < 3; i++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"NRF_2000_{i:D4}",
                Sol = 2000, // Different sol
                EarthDate = new DateTime(2015, 6, 30, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2015, 6, 30, 10, i, 0, DateTimeKind.Utc),
                ImgSrcSmall = $"https://mars.nasa.gov/photo2000{i}_s.jpg",
                ImgSrcMedium = $"https://mars.nasa.gov/photo2000{i}_m.jpg",
                ImgSrcLarge = $"https://mars.nasa.gov/photo2000{i}_l.jpg",
                ImgSrcFull = $"https://mars.nasa.gov/photo2000{i}_f.jpg",
                Site = 80,
                Drive = 1300,
                MastAz = 50.0f + (i * 15.0f),
                MastEl = -5.0f,
                SpacecraftClock = 913073000.0f + (i * 100.0f),
                RoverId = 1,
                CameraId = 2,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        await DbContext.SaveChangesAsync();

        // Act - Filter for sol 1000 only
        var result = await _service.GetPanoramasAsync(
            rovers: "curiosity",
            solMin: 1000,
            solMax: 1000,
            pageNumber: 1,
            pageSize: 25);

        // Assert
        result.Data.Should().NotBeNullOrEmpty();
        result.Data.Should().OnlyContain(p => p.Attributes!.Sol == 1000);
    }

    [Fact]
    public async Task GetPanoramasAsync_WithMinPhotosFilter_FiltersCorrectly()
    {
        // Act - Require at least 10 photos (should exclude our 5-photo panorama)
        var result = await _service.GetPanoramasAsync(
            rovers: "curiosity",
            minPhotos: 10,
            pageNumber: 1,
            pageSize: 25);

        // Assert
        result.Data.Should().BeEmpty();
        result.Meta!.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetPanoramasAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange - Add more panoramas
        var now = DateTime.UtcNow;
        for (int pano = 0; pano < 30; pano++)
        {
            for (int i = 0; i < 3; i++)
            {
                DbContext.Photos.Add(new Photo
                {
                    NasaId = $"NRF_{2000 + pano}_{i:D4}",
                    Sol = 2000 + pano,
                    EarthDate = new DateTime(2015, 6, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(pano),
                    DateTakenUtc = new DateTime(2015, 6, 1, 10, i, 0, DateTimeKind.Utc).AddDays(pano),
                    ImgSrcSmall = $"https://mars.nasa.gov/photo{pano}{i}_s.jpg",
                    ImgSrcMedium = $"https://mars.nasa.gov/photo{pano}{i}_m.jpg",
                    ImgSrcLarge = $"https://mars.nasa.gov/photo{pano}{i}_l.jpg",
                    ImgSrcFull = $"https://mars.nasa.gov/photo{pano}{i}_f.jpg",
                    Site = 80 + pano,
                    Drive = 1300,
                    MastAz = 50.0f + (i * 15.0f), // 30 degree range
                    MastEl = -5.0f,
                    SpacecraftClock = 913073000.0f + (i * 100.0f),
                    RoverId = 1,
                    CameraId = 2,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }
        await DbContext.SaveChangesAsync();

        // Act - Get page 2 with page size 10
        var result = await _service.GetPanoramasAsync(
            rovers: "curiosity",
            pageNumber: 2,
            pageSize: 10);

        // Assert
        result.Data.Should().HaveCount(10);
        result.Pagination.Should().NotBeNull();
        result.Pagination!.Page.Should().Be(2);
        result.Pagination.PerPage.Should().Be(10);
        result.Pagination.TotalPages.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task GetPanoramasAsync_WithNoTelemetry_ExcludesPhotos()
    {
        // Arrange - Clear existing data and add photos without telemetry
        DbContext.Photos.RemoveRange(DbContext.Photos);
        var now = DateTime.UtcNow;

        DbContext.Photos.Add(new Photo
        {
            NasaId = "NRF_3000_0000",
            Sol = 3000,
            EarthDate = new DateTime(2015, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            DateTakenUtc = new DateTime(2015, 7, 1, 10, 0, 0, DateTimeKind.Utc),
            ImgSrcSmall = "https://mars.nasa.gov/photo3000_s.jpg",
            ImgSrcMedium = "https://mars.nasa.gov/photo3000_m.jpg",
            ImgSrcLarge = "https://mars.nasa.gov/photo3000_l.jpg",
            ImgSrcFull = "https://mars.nasa.gov/photo3000_f.jpg",
            Site = 90,
            Drive = 1400,
            MastAz = null, // No telemetry
            MastEl = null,
            SpacecraftClock = null,
            RoverId = 1,
            CameraId = 2,
            CreatedAt = now,
            UpdatedAt = now
        });
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetPanoramasAsync(
            rovers: "curiosity",
            pageNumber: 1,
            pageSize: 25);

        // Assert
        result.Data.Should().BeEmpty();
        result.Meta!.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetPanoramaByIdAsync_WithValidId_ReturnsPanorama()
    {
        // Arrange
        var panoramaId = "pano_curiosity_1000_0";

        // First, get all panoramas to find the actual ID
        var allPanoramas = await _service.GetPanoramasAsync(
            rovers: "curiosity",
            pageNumber: 1,
            pageSize: 25);

        var actualPanoramaId = allPanoramas.Data.First().Id;

        // Act
        var result = await _service.GetPanoramaByIdAsync(actualPanoramaId);

        // Assert
        result.Should().NotBeNull();
        result!.Type.Should().Be("panorama");
        result.Attributes!.Sol.Should().Be(1000);
        result.Attributes.TotalPhotos.Should().Be(5);
    }

    [Fact]
    public async Task GetPanoramaByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var invalidId = "pano_curiosity_9999_0";

        // Act
        var result = await _service.GetPanoramaByIdAsync(invalidId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPanoramaByIdAsync_WithMalformedId_ReturnsNull()
    {
        // Arrange
        var malformedIds = new[]
        {
            "invalid",
            "pano_curiosity", // Missing parts
            "notpano_curiosity_1000_0", // Wrong prefix
            "pano_curiosity_abc_0", // Invalid sol
            "pano_curiosity_1000_xyz" // Invalid sequence
        };

        foreach (var malformedId in malformedIds)
        {
            // Act
            var result = await _service.GetPanoramaByIdAsync(malformedId);

            // Assert
            result.Should().BeNull($"ID '{malformedId}' should return null");
        }
    }

    [Fact]
    public async Task GetPanoramasAsync_DetectsMultiplePanoramasInSameSol()
    {
        // Arrange - Add second panorama sequence at different location on same sol
        var now = DateTime.UtcNow;
        for (int i = 0; i < 4; i++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"NRF_1000_SECOND_{i:D4}",
                Sol = 1000,
                EarthDate = new DateTime(2015, 5, 30, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2015, 5, 30, 14, i, 0, DateTimeKind.Utc),
                ImgSrcSmall = $"https://mars.nasa.gov/photo1000s{i}_s.jpg",
                ImgSrcMedium = $"https://mars.nasa.gov/photo1000s{i}_m.jpg",
                ImgSrcLarge = $"https://mars.nasa.gov/photo1000s{i}_l.jpg",
                ImgSrcFull = $"https://mars.nasa.gov/photo1000s{i}_f.jpg",
                Site = 79,
                Drive = 1205, // Different drive
                MastAz = 100.0f + (i * 15.0f), // 45 degree range
                MastEl = 5.0f,
                SpacecraftClock = 813080000.0f + (i * 100.0f),
                RoverId = 1,
                CameraId = 2,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetPanoramasAsync(
            rovers: "curiosity",
            solMin: 1000,
            solMax: 1000,
            pageNumber: 1,
            pageSize: 25);

        // Assert
        result.Data.Should().HaveCountGreaterThanOrEqualTo(2, "should detect both panorama sequences");
        result.Data.Should().Contain(p => p.Attributes!.TotalPhotos == 5);
        result.Data.Should().Contain(p => p.Attributes!.TotalPhotos == 4);
    }

    [Fact]
    public async Task GetPanoramasAsync_RequiresMinimumAzimuthRange()
    {
        // Arrange - Add photos with small azimuth range (< 30 degrees)
        var now = DateTime.UtcNow;
        for (int i = 0; i < 5; i++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"NRF_4000_{i:D4}",
                Sol = 4000,
                EarthDate = new DateTime(2015, 8, 1, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2015, 8, 1, 10, i, 0, DateTimeKind.Utc),
                ImgSrcSmall = $"https://mars.nasa.gov/photo4000{i}_s.jpg",
                ImgSrcMedium = $"https://mars.nasa.gov/photo4000{i}_m.jpg",
                ImgSrcLarge = $"https://mars.nasa.gov/photo4000{i}_l.jpg",
                ImgSrcFull = $"https://mars.nasa.gov/photo4000{i}_f.jpg",
                Site = 100,
                Drive = 1500,
                MastAz = 50.0f + (i * 2.0f), // Only 8 degree range (too small)
                MastEl = -10.0f,
                SpacecraftClock = 1013073000.0f + (i * 100.0f),
                RoverId = 1,
                CameraId = 2,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetPanoramasAsync(
            rovers: "curiosity",
            solMin: 4000,
            solMax: 4000,
            pageNumber: 1,
            pageSize: 25);

        // Assert
        result.Data.Should().BeEmpty("azimuth range is too small to qualify as panorama");
    }

    [Fact]
    public async Task GetPanoramasAsync_BreaksSequenceOnLargeTimeDelta()
    {
        // Arrange - Add photos with large time gap in the middle
        var now = DateTime.UtcNow;

        // First 3 photos (close together)
        for (int i = 0; i < 3; i++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"NRF_5000_A_{i:D4}",
                Sol = 5000,
                EarthDate = new DateTime(2015, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2015, 9, 1, 10, i, 0, DateTimeKind.Utc),
                ImgSrcSmall = $"https://mars.nasa.gov/photo5000a{i}_s.jpg",
                ImgSrcMedium = $"https://mars.nasa.gov/photo5000a{i}_m.jpg",
                ImgSrcLarge = $"https://mars.nasa.gov/photo5000a{i}_l.jpg",
                ImgSrcFull = $"https://mars.nasa.gov/photo5000a{i}_f.jpg",
                Site = 110,
                Drive = 1600,
                MastAz = 50.0f + (i * 15.0f),
                MastEl = -10.0f,
                SpacecraftClock = 1000000.0f + (i * 100.0f),
                RoverId = 1,
                CameraId = 2,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        // Photo with large time gap (400 seconds later)
        DbContext.Photos.Add(new Photo
        {
            NasaId = "NRF_5000_B_0000",
            Sol = 5000,
            EarthDate = new DateTime(2015, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            DateTakenUtc = new DateTime(2015, 9, 1, 10, 10, 0, DateTimeKind.Utc),
            ImgSrcSmall = "https://mars.nasa.gov/photo5000b_s.jpg",
            ImgSrcMedium = "https://mars.nasa.gov/photo5000b_m.jpg",
            ImgSrcLarge = "https://mars.nasa.gov/photo5000b_l.jpg",
            ImgSrcFull = "https://mars.nasa.gov/photo5000b_f.jpg",
            Site = 110,
            Drive = 1600,
            MastAz = 95.0f,
            MastEl = -10.0f,
            SpacecraftClock = 1000200.0f + 400.0f, // 400 seconds gap from last photo (> 300 max)
            RoverId = 1,
            CameraId = 2,
            CreatedAt = now,
            UpdatedAt = now
        });

        await DbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetPanoramasAsync(
            rovers: "curiosity",
            solMin: 5000,
            solMax: 5000,
            minPhotos: 3,
            pageNumber: 1,
            pageSize: 25);

        // Assert - Should detect one panorama (first 3 photos), the 4th photo is too far in time
        result.Data.Should().HaveCount(1, "time delta breaks the sequence after 3 photos");
        result.Data.First().Attributes!.TotalPhotos.Should().Be(3);
    }

    [Fact]
    public async Task GetPanoramasAsync_GroupsByCameraType()
    {
        // Arrange - Add NAVCAM photos at same location/time as MAST photos (NAVCAM is camera ID 3)
        var now = DateTime.UtcNow;
        for (int i = 0; i < 3; i++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"NRF_1000_NAV_{i:D4}",
                Sol = 1000,
                EarthDate = new DateTime(2015, 5, 30, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2015, 5, 30, 10, i, 0, DateTimeKind.Utc),
                ImgSrcSmall = $"https://mars.nasa.gov/photo1000nav{i}_s.jpg",
                ImgSrcMedium = $"https://mars.nasa.gov/photo1000nav{i}_m.jpg",
                ImgSrcLarge = $"https://mars.nasa.gov/photo1000nav{i}_l.jpg",
                ImgSrcFull = $"https://mars.nasa.gov/photo1000nav{i}_f.jpg",
                Site = 79,
                Drive = 1204,
                MastAz = 45.0f + (i * 15.0f), // 30 degree range
                MastEl = -10.0f,
                SpacecraftClock = 813073000.0f + (i * 100.0f),
                RoverId = 2, // Perseverance
                CameraId = 3, // NAVCAM
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetPanoramasAsync(
            solMin: 1000,
            solMax: 1000,
            pageNumber: 1,
            pageSize: 25);

        // Assert - Should detect separate panoramas for MAST (Curiosity) and NAVCAM (Perseverance)
        result.Data.Should().HaveCountGreaterThanOrEqualTo(2);
        result.Data.Should().Contain(p => p.Attributes!.Camera == "MAST");
        result.Data.Should().Contain(p => p.Attributes!.Camera == "NAVCAM");
    }

    [Fact]
    public async Task GetPanoramasAsync_WithEmptyDatabase_ReturnsEmptyList()
    {
        // Arrange - Clear all photos
        DbContext.Photos.RemoveRange(DbContext.Photos);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetPanoramasAsync(
            rovers: "curiosity",
            pageNumber: 1,
            pageSize: 25);

        // Assert
        result.Data.Should().BeEmpty();
        result.Meta!.TotalCount.Should().Be(0);
        result.Pagination!.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task GetPanoramasAsync_IncludesLocationInformation()
    {
        // Act
        var result = await _service.GetPanoramasAsync(
            rovers: "curiosity",
            pageNumber: 1,
            pageSize: 25);

        // Assert
        var panorama = result.Data.First();
        panorama.Attributes!.Location.Should().NotBeNull();
        panorama.Attributes.Location!.Site.Should().Be(79);
        panorama.Attributes.Location.Drive.Should().Be(1204);
        panorama.Attributes.Location.Coordinates.Should().NotBeNull();
        panorama.Attributes.Location.Coordinates!.X.Should().BeApproximately(35.4362f, 0.001f);
    }

    [Fact]
    public async Task GetPanoramasAsync_CalculatesAverageElevation()
    {
        // Act
        var result = await _service.GetPanoramasAsync(
            rovers: "curiosity",
            pageNumber: 1,
            pageSize: 25);

        // Assert
        var panorama = result.Data.First();
        panorama.Attributes!.AvgElevation.Should().BeApproximately(-10.0f, 0.1f);
    }

    [Fact]
    public async Task GetPanoramasAsync_IncludesDownloadLink()
    {
        // Act
        var result = await _service.GetPanoramasAsync(
            rovers: "curiosity",
            pageNumber: 1,
            pageSize: 25);

        // Assert
        var panorama = result.Data.First();
        panorama.Links.Should().NotBeNull();
        panorama.Links!.DownloadSet.Should().NotBeNullOrEmpty();
        panorama.Links.DownloadSet.Should().Contain("/api/v2/panoramas/");
        panorama.Links.DownloadSet.Should().Contain("/download");
    }
}
