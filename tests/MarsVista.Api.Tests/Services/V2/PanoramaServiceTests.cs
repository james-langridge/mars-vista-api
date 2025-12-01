using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MarsVista.Core.Entities;
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
    public async Task GetPanoramaByIdAsync_WithMultiplePanoramasOnSameSol_ReturnsCorrectOne()
    {
        // Arrange - Add two distinct panorama sequences on sol 6000
        var now = DateTime.UtcNow;

        // First panorama (different drive)
        for (int i = 0; i < 4; i++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"NRF_6000_A_{i:D4}",
                Sol = 6000,
                EarthDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2025, 1, 1, 10, i, 0, DateTimeKind.Utc),
                DateTakenMars = $"Sol-6000M10:0{i}:00",
                ImgSrcSmall = $"https://mars.nasa.gov/photo6000a{i}_s.jpg",
                ImgSrcMedium = $"https://mars.nasa.gov/photo6000a{i}_m.jpg",
                ImgSrcLarge = $"https://mars.nasa.gov/photo6000a{i}_l.jpg",
                ImgSrcFull = $"https://mars.nasa.gov/photo6000a{i}_f.jpg",
                Site = 200,
                Drive = 3000,
                MastAz = 10.0f + (i * 15.0f), // 45° range, 4 positions
                MastEl = -5.0f,
                SpacecraftClock = 1500000.0f + (i * 100.0f),
                RoverId = 1,
                CameraId = 2,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        // Second panorama (different drive)
        for (int i = 0; i < 3; i++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"NRF_6000_B_{i:D4}",
                Sol = 6000,
                EarthDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2025, 1, 1, 14, i, 0, DateTimeKind.Utc),
                DateTakenMars = $"Sol-6000M14:0{i}:00",
                ImgSrcSmall = $"https://mars.nasa.gov/photo6000b{i}_s.jpg",
                ImgSrcMedium = $"https://mars.nasa.gov/photo6000b{i}_m.jpg",
                ImgSrcLarge = $"https://mars.nasa.gov/photo6000b{i}_l.jpg",
                ImgSrcFull = $"https://mars.nasa.gov/photo6000b{i}_f.jpg",
                Site = 200,
                Drive = 3001, // Different drive
                MastAz = 90.0f + (i * 20.0f), // 40° range, 3 positions
                MastEl = -5.0f,
                SpacecraftClock = 1600000.0f + (i * 100.0f),
                RoverId = 1,
                CameraId = 2,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        await DbContext.SaveChangesAsync();

        // Act - Get panoramas list first
        var allPanoramas = await _service.GetPanoramasAsync(
            rovers: "curiosity",
            solMin: 6000,
            solMax: 6000,
            pageNumber: 1,
            pageSize: 25);

        // Should have 2 panoramas on sol 6000
        allPanoramas.Data.Should().HaveCount(2);

        // Both IDs should be per-sol indexed (0 and 1)
        var ids = allPanoramas.Data.Select(p => p.Id).ToList();
        ids.Should().Contain("pano_curiosity_6000_0");
        ids.Should().Contain("pano_curiosity_6000_1");

        // Now fetch each by ID
        var pano0 = await _service.GetPanoramaByIdAsync("pano_curiosity_6000_0");
        var pano1 = await _service.GetPanoramaByIdAsync("pano_curiosity_6000_1");

        // Both should be found
        pano0.Should().NotBeNull();
        pano1.Should().NotBeNull();

        // They should have different photo counts
        var counts = new[] { pano0!.Attributes!.TotalPhotos, pano1!.Attributes!.TotalPhotos };
        counts.Should().Contain(4);
        counts.Should().Contain(3);
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
    public async Task GetPanoramasAsync_WithBracketedExposures_CountsAllPhotos()
    {
        // Arrange - Add bracketed exposures (same spacecraft_clock at each position)
        DbContext.Photos.RemoveRange(DbContext.Photos);
        var now = DateTime.UtcNow;

        // 4 positions, 3 bracketed exposures each = 12 total photos
        var positions = new[] { 45.0f, 67.0f, 89.0f, 111.0f }; // ~22° spacing
        var baseSpacecraftClock = 813073000.0f;

        for (int pos = 0; pos < positions.Length; pos++)
        {
            var positionClock = baseSpacecraftClock + (pos * 60.0f); // 60 seconds between positions
            for (int exp = 0; exp < 3; exp++)
            {
                DbContext.Photos.Add(new Photo
                {
                    NasaId = $"BRACKETED_{pos}_{exp}",
                    Sol = 4278,
                    EarthDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    DateTakenUtc = new DateTime(2024, 1, 1, 10, pos, exp, DateTimeKind.Utc),
                    DateTakenMars = $"Sol-4278M14:{pos:D2}:{exp:D2}",
                    ImgSrcSmall = $"https://mars.nasa.gov/bracketed_{pos}_{exp}_s.jpg",
                    ImgSrcMedium = $"https://mars.nasa.gov/bracketed_{pos}_{exp}_m.jpg",
                    ImgSrcLarge = $"https://mars.nasa.gov/bracketed_{pos}_{exp}_l.jpg",
                    ImgSrcFull = $"https://mars.nasa.gov/bracketed_{pos}_{exp}_f.jpg",
                    Site = 100,
                    Drive = 1500,
                    MastAz = positions[pos],
                    MastEl = -10.0f,
                    SpacecraftClock = positionClock, // Same clock for all exposures at this position
                    RoverId = 1,
                    CameraId = 2,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetPanoramasAsync(
            rovers: "curiosity",
            solMin: 4278,
            solMax: 4278,
            pageNumber: 1,
            pageSize: 25);

        // Assert
        result.Data.Should().HaveCount(1);
        var panorama = result.Data.First();
        panorama.Attributes!.TotalPhotos.Should().Be(12, "all bracketed exposures should be counted");
        panorama.Attributes.UniquePositions.Should().Be(4, "should detect 4 unique azimuth positions");
    }

    [Fact]
    public async Task GetPanoramasAsync_WithVaryingElevation_DetectsFullSweep()
    {
        // Arrange - Photos with elevation varying up to 13° (within 15° tolerance)
        DbContext.Photos.RemoveRange(DbContext.Photos);
        var now = DateTime.UtcNow;

        // Simulate terrain-following sweep: 6 positions with varying elevation
        var elevations = new[] { -11.0f, -8.0f, -4.0f, 0.0f, 2.0f, -2.0f }; // 13° total range
        for (int i = 0; i < 6; i++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"ELEVATION_{i:D4}",
                Sol = 4279,
                EarthDate = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2024, 1, 2, 10, i, 0, DateTimeKind.Utc),
                DateTakenMars = $"Sol-4279M14:0{i}:00",
                ImgSrcSmall = $"https://mars.nasa.gov/elevation_{i}_s.jpg",
                ImgSrcMedium = $"https://mars.nasa.gov/elevation_{i}_m.jpg",
                ImgSrcLarge = $"https://mars.nasa.gov/elevation_{i}_l.jpg",
                ImgSrcFull = $"https://mars.nasa.gov/elevation_{i}_f.jpg",
                Site = 101,
                Drive = 1501,
                MastAz = 45.0f + (i * 30.0f), // 150° coverage
                MastEl = elevations[i],
                SpacecraftClock = 913073000.0f + (i * 100.0f),
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
            solMin: 4279,
            solMax: 4279,
            pageNumber: 1,
            pageSize: 25);

        // Assert - Should detect as single panorama despite 13° elevation change
        result.Data.Should().HaveCount(1);
        var panorama = result.Data.First();
        panorama.Attributes!.TotalPhotos.Should().Be(6);
        panorama.Attributes.CoverageDegrees.Should().BeApproximately(150.0f, 0.1f);
    }

    [Fact]
    public async Task GetPanoramasAsync_WithTwoPositions_RejectsAsPanorama()
    {
        // Arrange - Only 2 unique positions (not stitchable)
        DbContext.Photos.RemoveRange(DbContext.Photos);
        var now = DateTime.UtcNow;

        // 2 positions with 3 exposures each = 6 photos, 46° range
        var positions = new[] { 163.0f, 209.0f };
        for (int pos = 0; pos < positions.Length; pos++)
        {
            for (int exp = 0; exp < 3; exp++)
            {
                DbContext.Photos.Add(new Photo
                {
                    NasaId = $"TWOPOS_{pos}_{exp}",
                    Sol = 4280,
                    EarthDate = new DateTime(2024, 1, 3, 0, 0, 0, DateTimeKind.Utc),
                    DateTakenUtc = new DateTime(2024, 1, 3, 10, pos, exp, DateTimeKind.Utc),
                    DateTakenMars = $"Sol-4280M14:0{pos}:{exp:D2}",
                    ImgSrcSmall = $"https://mars.nasa.gov/twopos_{pos}_{exp}_s.jpg",
                    ImgSrcMedium = $"https://mars.nasa.gov/twopos_{pos}_{exp}_m.jpg",
                    ImgSrcLarge = $"https://mars.nasa.gov/twopos_{pos}_{exp}_l.jpg",
                    ImgSrcFull = $"https://mars.nasa.gov/twopos_{pos}_{exp}_f.jpg",
                    Site = 102,
                    Drive = 1502,
                    MastAz = positions[pos],
                    MastEl = -10.0f,
                    SpacecraftClock = 1013073000.0f + (pos * 60.0f),
                    RoverId = 1,
                    CameraId = 2,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetPanoramasAsync(
            rovers: "curiosity",
            solMin: 4280,
            solMax: 4280,
            pageNumber: 1,
            pageSize: 25);

        // Assert - Should reject: 46° range passes, but only 2 unique positions fails
        result.Data.Should().BeEmpty("2 positions is not stitchable");
    }

    [Fact]
    public async Task GetPanoramasAsync_ReturnsQualityMetadata()
    {
        // Act
        var result = await _service.GetPanoramasAsync(
            rovers: "curiosity",
            pageNumber: 1,
            pageSize: 25);

        // Assert - Original seed data has 5 photos at 5 positions (40° range)
        var panorama = result.Data.First();
        panorama.Attributes!.UniquePositions.Should().Be(5);
        panorama.Attributes.AvgPositionSpacing.Should().BeApproximately(10.0f, 0.1f);
        panorama.Attributes.Quality.Should().Be("partial"); // 40° coverage, 5 positions
    }

    [Fact]
    public async Task GetPanoramasAsync_WithHalfCoverage_ReturnsHalfQuality()
    {
        // Arrange - Add panorama with 120°+ coverage and 5+ positions
        DbContext.Photos.RemoveRange(DbContext.Photos);
        var now = DateTime.UtcNow;

        for (int i = 0; i < 6; i++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"HALF_{i:D4}",
                Sol = 4281,
                EarthDate = new DateTime(2024, 1, 4, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2024, 1, 4, 10, i, 0, DateTimeKind.Utc),
                ImgSrcSmall = $"https://mars.nasa.gov/half_{i}_s.jpg",
                ImgSrcMedium = $"https://mars.nasa.gov/half_{i}_m.jpg",
                ImgSrcLarge = $"https://mars.nasa.gov/half_{i}_l.jpg",
                ImgSrcFull = $"https://mars.nasa.gov/half_{i}_f.jpg",
                Site = 103,
                Drive = 1503,
                MastAz = 45.0f + (i * 25.0f), // 125° coverage
                MastEl = -10.0f,
                SpacecraftClock = 1113073000.0f + (i * 100.0f),
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
            solMin: 4281,
            solMax: 4281,
            pageNumber: 1,
            pageSize: 25);

        // Assert
        result.Data.Should().HaveCount(1);
        var panorama = result.Data.First();
        panorama.Attributes!.Quality.Should().Be("half"); // 125° >= 120°, 6 >= 5 positions
    }

    [Fact]
    public async Task GetPanoramasAsync_WithFullCoverage_ReturnsFullQuality()
    {
        // Arrange - Add panorama with 300°+ coverage and 10+ positions
        DbContext.Photos.RemoveRange(DbContext.Photos);
        var now = DateTime.UtcNow;

        for (int i = 0; i < 12; i++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"FULL_{i:D4}",
                Sol = 4282,
                EarthDate = new DateTime(2024, 1, 5, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2024, 1, 5, 10, i, 0, DateTimeKind.Utc),
                ImgSrcSmall = $"https://mars.nasa.gov/full_{i}_s.jpg",
                ImgSrcMedium = $"https://mars.nasa.gov/full_{i}_m.jpg",
                ImgSrcLarge = $"https://mars.nasa.gov/full_{i}_l.jpg",
                ImgSrcFull = $"https://mars.nasa.gov/full_{i}_f.jpg",
                Site = 104,
                Drive = 1504,
                MastAz = 15.0f + (i * 30.0f), // 330° coverage
                MastEl = -10.0f,
                SpacecraftClock = 1213073000.0f + (i * 100.0f),
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
            solMin: 4282,
            solMax: 4282,
            pageNumber: 1,
            pageSize: 25);

        // Assert
        result.Data.Should().HaveCount(1);
        var panorama = result.Data.First();
        panorama.Attributes!.Quality.Should().Be("full"); // 330° >= 300°, 12 >= 10 positions
        panorama.Attributes.UniquePositions.Should().Be(12);
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
