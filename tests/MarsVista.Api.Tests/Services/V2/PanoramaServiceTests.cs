using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MarsVista.Core.Entities;
using MarsVista.Api.DTOs.V2;
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
        services.AddScoped<MarsVista.Core.Services.PanoramaDetector>();
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
        // Arrange - Add two panoramas where the LATER drive has EARLIER spacecraft_clock
        // This is a regression test for ordering consistency between list and lookup endpoints
        var now = DateTime.UtcNow;

        // Panorama A: Drive 3001 (higher) but spacecraft_clock 1400000 (LOWER)
        for (int i = 0; i < 3; i++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"NRF_6000_A_{i:D4}",
                Sol = 6000,
                EarthDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2025, 1, 1, 8, i, 0, DateTimeKind.Utc),
                DateTakenMars = $"Sol-6000M08:0{i}:00",
                ImgSrcSmall = $"https://mars.nasa.gov/photo6000a{i}_s.jpg",
                ImgSrcMedium = $"https://mars.nasa.gov/photo6000a{i}_m.jpg",
                ImgSrcLarge = $"https://mars.nasa.gov/photo6000a{i}_l.jpg",
                ImgSrcFull = $"https://mars.nasa.gov/photo6000a{i}_f.jpg",
                Site = 200,
                Drive = 3001, // Higher drive
                MastAz = 10.0f + (i * 20.0f), // 40° range, 3 positions
                MastEl = -5.0f,
                SpacecraftClock = 1400000.0f + (i * 100.0f), // LOWER clock
                RoverId = 1,
                CameraId = 2,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        // Panorama B: Drive 3000 (lower) but spacecraft_clock 1500000 (HIGHER)
        for (int i = 0; i < 4; i++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"NRF_6000_B_{i:D4}",
                Sol = 6000,
                EarthDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2025, 1, 1, 10, i, 0, DateTimeKind.Utc),
                DateTakenMars = $"Sol-6000M10:0{i}:00",
                ImgSrcSmall = $"https://mars.nasa.gov/photo6000b{i}_s.jpg",
                ImgSrcMedium = $"https://mars.nasa.gov/photo6000b{i}_m.jpg",
                ImgSrcLarge = $"https://mars.nasa.gov/photo6000b{i}_l.jpg",
                ImgSrcFull = $"https://mars.nasa.gov/photo6000b{i}_f.jpg",
                Site = 200,
                Drive = 3000, // Lower drive
                MastAz = 90.0f + (i * 15.0f), // 45° range, 4 positions
                MastEl = -5.0f,
                SpacecraftClock = 1500000.0f + (i * 100.0f), // HIGHER clock
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

        // Verify IDs match expected pattern
        var ids = allPanoramas.Data.Select(p => p.Id).ToList();
        ids.Should().Contain("pano_curiosity_6000_0");
        ids.Should().Contain("pano_curiosity_6000_1");

        // Critical: Fetch each panorama by ID and verify it matches the list
        foreach (var listedPano in allPanoramas.Data)
        {
            var fetchedPano = await _service.GetPanoramaByIdAsync(listedPano.Id);

            fetchedPano.Should().NotBeNull($"panorama {listedPano.Id} should be found");
            fetchedPano!.Id.Should().Be(listedPano.Id);
            fetchedPano.Attributes!.TotalPhotos.Should().Be(listedPano.Attributes!.TotalPhotos,
                $"photo count for {listedPano.Id} should match");
        }
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

    [Fact]
    public async Task GetPanoramasAsync_ReverseSweep_NormalizesMarsTimeRange()
    {
        // Arrange - Photos ordered by increasing spacecraft clock but DECREASING Mars time.
        // This simulates a reverse-sweep panorama where the camera sweeps in decreasing
        // azimuth order, so the first photo (by clock) has a later Mars time than the last.
        DbContext.Photos.RemoveRange(DbContext.Photos);
        var now = DateTime.UtcNow;

        for (int i = 0; i < 5; i++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"REVERSE_{i:D4}",
                Sol = 4500,
                EarthDate = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2024, 3, 1, 10, 0, 0, DateTimeKind.Utc).AddMinutes(i),
                DateTakenMars = $"Sol-4500M14:{(4 - i):D2}:00", // 14:04, 14:03, 14:02, 14:01, 14:00 (decreasing)
                ImgSrcSmall = $"https://mars.nasa.gov/reverse_{i}_s.jpg",
                ImgSrcMedium = $"https://mars.nasa.gov/reverse_{i}_m.jpg",
                ImgSrcLarge = $"https://mars.nasa.gov/reverse_{i}_l.jpg",
                ImgSrcFull = $"https://mars.nasa.gov/reverse_{i}_f.jpg",
                Site = 120,
                Drive = 2000,
                MastAz = 85.0f - (i * 10.0f), // Decreasing azimuth: 85, 75, 65, 55, 45
                MastEl = -10.0f,
                SpacecraftClock = 800000.0f + (i * 300.0f), // Increasing clock, float-safe gaps
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
            solMin: 4500,
            solMax: 4500,
            pageNumber: 1,
            pageSize: 25);

        // Assert - firstPhoto (lowest clock) has Mars time 14:04, lastPhoto has 14:00.
        // Without the fix, mars_time_start=M14:04:00 > mars_time_end=M14:00:00.
        // The swap should normalize to start=M14:00:00, end=M14:04:00.
        result.Data.Should().HaveCount(1);
        var panorama = result.Data.First();
        panorama.Attributes!.MarsTimeStart.Should().Be("M14:00:00");
        panorama.Attributes.MarsTimeEnd.Should().Be("M14:04:00");
    }

    // ===== Multi-Row Mosaic Detection Tests =====

    [Fact]
    public async Task GetPanoramasAsync_MultiRow_DetectsThreeRowMosaic()
    {
        // Arrange - 3 rows × 5 columns, elevation tiers 10° apart
        DbContext.Photos.RemoveRange(DbContext.Photos);
        var now = DateTime.UtcNow;

        var elevations = new[] { -20.0f, -10.0f, 0.0f }; // 3 tiers, 10° gaps
        var azimuths = new[] { 30.0f, 50.0f, 70.0f, 90.0f, 110.0f }; // 5 columns, 80° range
        var clock = 2000000.0f;

        for (int row = 0; row < elevations.Length; row++)
        {
            for (int col = 0; col < azimuths.Length; col++)
            {
                DbContext.Photos.Add(new Photo
                {
                    NasaId = $"MULTI_{row}_{col}",
                    Sol = 7000,
                    EarthDate = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                    DateTakenUtc = new DateTime(2025, 6, 1, 10, 0, 0, DateTimeKind.Utc).AddSeconds(clock - 2000000),
                    ImgSrcSmall = $"https://mars.nasa.gov/multi_{row}_{col}_s.jpg",
                    ImgSrcMedium = $"https://mars.nasa.gov/multi_{row}_{col}_m.jpg",
                    ImgSrcLarge = $"https://mars.nasa.gov/multi_{row}_{col}_l.jpg",
                    ImgSrcFull = $"https://mars.nasa.gov/multi_{row}_{col}_f.jpg",
                    Site = 300,
                    Drive = 5000,
                    MastAz = azimuths[col],
                    MastEl = elevations[row],
                    SpacecraftClock = clock,
                    RoverId = 1,
                    CameraId = 2,
                    CreatedAt = now,
                    UpdatedAt = now
                });
                clock += 20.0f; // 20 seconds between photos
            }
        }
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetPanoramasAsync(
            rovers: "curiosity",
            solMin: 7000,
            solMax: 7000,
            pageNumber: 1,
            pageSize: 25);

        // Assert - Should detect as 1 multi_row mosaic
        result.Data.Should().HaveCount(1);
        var panorama = result.Data.First();
        panorama.Attributes!.MosaicType.Should().Be("multi_row");
        panorama.Attributes.ElevationRows.Should().Be(3);
        panorama.Attributes.TotalPhotos.Should().Be(15);
        panorama.Attributes.GridDimensions.Should().Be("3x5");
        panorama.Attributes.ElevationRangeData.Should().NotBeNull();
        panorama.Attributes.ElevationRangeData!.Min.Should().BeApproximately(-20.0f, 0.1f);
        panorama.Attributes.ElevationRangeData.Max.Should().BeApproximately(0.0f, 0.1f);
        panorama.Attributes.VerticalCoverageDegrees.Should().BeApproximately(20.0f, 0.1f);
    }

    [Fact]
    public async Task GetPanoramasAsync_MultiRow_TimeGapSplitsTwoMosaics()
    {
        // Arrange - Two multi-row mosaics on same sol, >300s apart
        DbContext.Photos.RemoveRange(DbContext.Photos);
        var now = DateTime.UtcNow;

        var elevations = new[] { -15.0f, 0.0f }; // 2 tiers
        var azimuths = new[] { 30.0f, 60.0f, 90.0f }; // 3 columns
        var clock = 3000000.0f;

        // Mosaic A
        for (int row = 0; row < elevations.Length; row++)
        {
            for (int col = 0; col < azimuths.Length; col++)
            {
                DbContext.Photos.Add(new Photo
                {
                    NasaId = $"TIMESPLIT_A_{row}_{col}",
                    Sol = 7001,
                    EarthDate = new DateTime(2025, 6, 2, 0, 0, 0, DateTimeKind.Utc),
                    DateTakenUtc = new DateTime(2025, 6, 2, 10, 0, 0, DateTimeKind.Utc),
                    ImgSrcSmall = $"https://mars.nasa.gov/ts_a_{row}_{col}_s.jpg",
                    ImgSrcMedium = $"https://mars.nasa.gov/ts_a_{row}_{col}_m.jpg",
                    ImgSrcLarge = $"https://mars.nasa.gov/ts_a_{row}_{col}_l.jpg",
                    ImgSrcFull = $"https://mars.nasa.gov/ts_a_{row}_{col}_f.jpg",
                    Site = 301,
                    Drive = 5001,
                    MastAz = azimuths[col],
                    MastEl = elevations[row],
                    SpacecraftClock = clock,
                    RoverId = 1,
                    CameraId = 2,
                    CreatedAt = now,
                    UpdatedAt = now
                });
                clock += 20.0f;
            }
        }

        clock += 500.0f; // 500s gap (> 300s threshold)

        // Mosaic B
        for (int row = 0; row < elevations.Length; row++)
        {
            for (int col = 0; col < azimuths.Length; col++)
            {
                DbContext.Photos.Add(new Photo
                {
                    NasaId = $"TIMESPLIT_B_{row}_{col}",
                    Sol = 7001,
                    EarthDate = new DateTime(2025, 6, 2, 0, 0, 0, DateTimeKind.Utc),
                    DateTakenUtc = new DateTime(2025, 6, 2, 11, 0, 0, DateTimeKind.Utc),
                    ImgSrcSmall = $"https://mars.nasa.gov/ts_b_{row}_{col}_s.jpg",
                    ImgSrcMedium = $"https://mars.nasa.gov/ts_b_{row}_{col}_m.jpg",
                    ImgSrcLarge = $"https://mars.nasa.gov/ts_b_{row}_{col}_l.jpg",
                    ImgSrcFull = $"https://mars.nasa.gov/ts_b_{row}_{col}_f.jpg",
                    Site = 301,
                    Drive = 5001,
                    MastAz = azimuths[col] + 120.0f, // Different azimuth range
                    MastEl = elevations[row],
                    SpacecraftClock = clock,
                    RoverId = 1,
                    CameraId = 2,
                    CreatedAt = now,
                    UpdatedAt = now
                });
                clock += 20.0f;
            }
        }
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetPanoramasAsync(
            rovers: "curiosity",
            solMin: 7001,
            solMax: 7001,
            pageNumber: 1,
            pageSize: 25);

        // Assert - 2 separate multi-row mosaics
        result.Data.Should().HaveCount(2);
        result.Data.Should().OnlyContain(p => p.Attributes!.MosaicType == "multi_row");
        result.Data.Should().OnlyContain(p => p.Attributes!.TotalPhotos == 6);
    }

    [Fact]
    public async Task GetPanoramasAsync_SingleRow_HasCorrectNewFields()
    {
        // Act - Use seed data (5 photos, all at -10° elevation)
        var result = await _service.GetPanoramasAsync(
            rovers: "curiosity",
            solMin: 1000,
            solMax: 1000,
            pageNumber: 1,
            pageSize: 25);

        // Assert - Single-row fields
        result.Data.Should().NotBeNullOrEmpty();
        var panorama = result.Data.First();
        panorama.Attributes!.MosaicType.Should().Be("single_row");
        panorama.Attributes.ElevationRows.Should().Be(1);
        panorama.Attributes.ElevationRangeData.Should().BeNull();
        panorama.Attributes.GridDimensions.Should().BeNull();
        panorama.Attributes.VerticalCoverageDegrees.Should().BeNull();
    }

    [Fact]
    public async Task GetPanoramasAsync_MultiRow_RaggedGridUsesMaxColumns()
    {
        // Arrange - Ragged grid: row 0 has 5 columns, row 1 has 3 columns
        DbContext.Photos.RemoveRange(DbContext.Photos);
        var now = DateTime.UtcNow;
        var clock = 4000000.0f;

        // Row 0: 5 columns
        for (int col = 0; col < 5; col++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"RAGGED_0_{col}",
                Sol = 7002,
                EarthDate = new DateTime(2025, 6, 3, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2025, 6, 3, 10, 0, 0, DateTimeKind.Utc),
                ImgSrcSmall = $"https://mars.nasa.gov/ragged_0_{col}_s.jpg",
                ImgSrcMedium = $"https://mars.nasa.gov/ragged_0_{col}_m.jpg",
                ImgSrcLarge = $"https://mars.nasa.gov/ragged_0_{col}_l.jpg",
                ImgSrcFull = $"https://mars.nasa.gov/ragged_0_{col}_f.jpg",
                Site = 302,
                Drive = 5002,
                MastAz = 20.0f + (col * 20.0f), // 20, 40, 60, 80, 100
                MastEl = -15.0f,
                SpacecraftClock = clock,
                RoverId = 1,
                CameraId = 2,
                CreatedAt = now,
                UpdatedAt = now
            });
            clock += 20.0f;
        }

        // Row 1: 3 columns (subset of row 0's azimuth positions)
        for (int col = 0; col < 3; col++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"RAGGED_1_{col}",
                Sol = 7002,
                EarthDate = new DateTime(2025, 6, 3, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2025, 6, 3, 10, 5, 0, DateTimeKind.Utc),
                ImgSrcSmall = $"https://mars.nasa.gov/ragged_1_{col}_s.jpg",
                ImgSrcMedium = $"https://mars.nasa.gov/ragged_1_{col}_m.jpg",
                ImgSrcLarge = $"https://mars.nasa.gov/ragged_1_{col}_l.jpg",
                ImgSrcFull = $"https://mars.nasa.gov/ragged_1_{col}_f.jpg",
                Site = 302,
                Drive = 5002,
                MastAz = 40.0f + (col * 20.0f), // 40, 60, 80 (subset)
                MastEl = 0.0f, // 15° gap from row 0 → separate tier
                SpacecraftClock = clock,
                RoverId = 1,
                CameraId = 2,
                CreatedAt = now,
                UpdatedAt = now
            });
            clock += 20.0f;
        }
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetPanoramasAsync(
            rovers: "curiosity",
            solMin: 7002,
            solMax: 7002,
            pageNumber: 1,
            pageSize: 25);

        // Assert - Grid dimensions use max columns (5), completeness = 8/(2*5) = 80%
        result.Data.Should().HaveCount(1);
        var panorama = result.Data.First();
        panorama.Attributes!.MosaicType.Should().Be("multi_row");
        panorama.Attributes.GridDimensions.Should().Be("2x5");
        panorama.Attributes.TotalPhotos.Should().Be(8);
    }

    [Fact]
    public async Task GetPanoramasAsync_MultiRow_TooFewUniquePositionsRejected()
    {
        // Arrange - 2 elevation tiers but only 2 unique azimuth positions (< 3 required)
        DbContext.Photos.RemoveRange(DbContext.Photos);
        var now = DateTime.UtcNow;
        var clock = 5000000.0f;

        var elevations = new[] { -20.0f, 0.0f };
        var azimuths = new[] { 50.0f, 80.0f }; // Only 30° range, 2 positions
        for (int row = 0; row < elevations.Length; row++)
        {
            for (int col = 0; col < azimuths.Length; col++)
            {
                DbContext.Photos.Add(new Photo
                {
                    NasaId = $"FEWPOS_{row}_{col}",
                    Sol = 7003,
                    EarthDate = new DateTime(2025, 6, 4, 0, 0, 0, DateTimeKind.Utc),
                    DateTakenUtc = new DateTime(2025, 6, 4, 10, 0, 0, DateTimeKind.Utc),
                    ImgSrcSmall = $"https://mars.nasa.gov/fewpos_{row}_{col}_s.jpg",
                    ImgSrcMedium = $"https://mars.nasa.gov/fewpos_{row}_{col}_m.jpg",
                    ImgSrcLarge = $"https://mars.nasa.gov/fewpos_{row}_{col}_l.jpg",
                    ImgSrcFull = $"https://mars.nasa.gov/fewpos_{row}_{col}_f.jpg",
                    Site = 303,
                    Drive = 5003,
                    MastAz = azimuths[col],
                    MastEl = elevations[row],
                    SpacecraftClock = clock,
                    RoverId = 1,
                    CameraId = 2,
                    CreatedAt = now,
                    UpdatedAt = now
                });
                clock += 20.0f;
            }
        }
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetPanoramasAsync(
            rovers: "curiosity",
            solMin: 7003,
            solMax: 7003,
            pageNumber: 1,
            pageSize: 25);

        // Assert - Rejected: only 2 unique azimuth positions < 3 required
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPanoramasAsync_MultiRow_LowGridCompletenessRecoversAsSingleRow()
    {
        // Arrange - 4 elevation tiers with only 1 tier having >= 3 columns
        // Tier 0 (-30°): 8 azimuth columns → valid single-row panorama
        // Tier 1 (-15°): 1 column
        // Tier 2 (0°): 1 column
        // Tier 3 (15°): 1 column
        // Multi-row rejected: only 1 tier has >= 3 columns (need at least 2)
        // Fallback: tier 0 recovered as single-row panorama
        DbContext.Photos.RemoveRange(DbContext.Photos);
        var now = DateTime.UtcNow;
        var clock = 5100000.0f;

        // Tier 0: full row of 8 columns
        for (int col = 0; col < 8; col++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"SPARSE_0_{col}",
                Sol = 7010,
                EarthDate = new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2025, 6, 10, 10, 0, 0, DateTimeKind.Utc),
                ImgSrcSmall = $"https://mars.nasa.gov/sparse_0_{col}_s.jpg",
                ImgSrcMedium = $"https://mars.nasa.gov/sparse_0_{col}_m.jpg",
                ImgSrcLarge = $"https://mars.nasa.gov/sparse_0_{col}_l.jpg",
                ImgSrcFull = $"https://mars.nasa.gov/sparse_0_{col}_f.jpg",
                Site = 310,
                Drive = 5010,
                MastAz = 20.0f + (col * 15.0f), // 20..125° range
                MastEl = -30.0f,
                SpacecraftClock = clock,
                RoverId = 1,
                CameraId = 2,
                CreatedAt = now,
                UpdatedAt = now
            });
            clock += 20.0f;
        }

        // Tiers 1-3: only 1 column each (at azimuth 20°)
        var sparseElevations = new[] { -15.0f, 0.0f, 15.0f };
        for (int tier = 0; tier < sparseElevations.Length; tier++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"SPARSE_{tier + 1}_0",
                Sol = 7010,
                EarthDate = new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2025, 6, 10, 10, 5, 0, DateTimeKind.Utc),
                ImgSrcSmall = $"https://mars.nasa.gov/sparse_{tier + 1}_0_s.jpg",
                ImgSrcMedium = $"https://mars.nasa.gov/sparse_{tier + 1}_0_m.jpg",
                ImgSrcLarge = $"https://mars.nasa.gov/sparse_{tier + 1}_0_l.jpg",
                ImgSrcFull = $"https://mars.nasa.gov/sparse_{tier + 1}_0_f.jpg",
                Site = 310,
                Drive = 5010,
                MastAz = 20.0f, // Only 1 column per tier
                MastEl = sparseElevations[tier],
                SpacecraftClock = clock,
                RoverId = 1,
                CameraId = 2,
                CreatedAt = now,
                UpdatedAt = now
            });
            clock += 20.0f;
        }
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetPanoramasAsync(
            rovers: "curiosity",
            solMin: 7010,
            solMax: 7010,
            pageNumber: 1,
            pageSize: 25);

        // Assert - Multi-row rejected (only 1 tier has >= 3 columns), but tier 0 recovered as single-row
        result.Data.Should().HaveCount(1);
        var panorama = result.Data.First();
        panorama.Attributes!.MosaicType.Should().Be("single_row");
        panorama.Attributes.TotalPhotos.Should().Be(8);
        panorama.Attributes.ElevationRows.Should().Be(1);
    }

    [Fact]
    public async Task GetPanoramasAsync_MultiRow_SparseTierFallsBackToSingleRow()
    {
        // Arrange - Reproduces the sol 4324 false positive pattern:
        // Tier 0 (~5°): 4 unique azimuths spanning 220° (a real sweep)
        // Tier 1 (~17°): 15 bracketed exposures at 1 azimuth (stationary burst)
        // Multi-row rejected: only 1 tier has >= 3 columns. Tier 0 recovered as single-row.
        DbContext.Photos.RemoveRange(DbContext.Photos);
        var now = DateTime.UtcNow;
        var clock = 5200000.0f;

        // Tier 0: 4 unique positions
        var azimuths = new[] { 65.0f, 105.0f, 145.0f, 285.0f };
        foreach (var az in azimuths)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"FALLBACK_T0_{az}",
                Sol = 7011,
                EarthDate = new DateTime(2025, 6, 11, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2025, 6, 11, 10, 0, 0, DateTimeKind.Utc),
                ImgSrcSmall = $"https://mars.nasa.gov/fb_t0_{az}_s.jpg",
                ImgSrcMedium = $"https://mars.nasa.gov/fb_t0_{az}_m.jpg",
                ImgSrcLarge = $"https://mars.nasa.gov/fb_t0_{az}_l.jpg",
                ImgSrcFull = $"https://mars.nasa.gov/fb_t0_{az}_f.jpg",
                Site = 311,
                Drive = 5011,
                MastAz = az,
                MastEl = 5.0f,
                SpacecraftClock = clock,
                RoverId = 1,
                CameraId = 2,
                CreatedAt = now,
                UpdatedAt = now
            });
            clock += 60.0f;
        }

        // Tier 1: bracketed burst at single position (15 photos, 1 unique azimuth)
        for (int i = 0; i < 15; i++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"FALLBACK_T1_{i}",
                Sol = 7011,
                EarthDate = new DateTime(2025, 6, 11, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2025, 6, 11, 10, 5, 0, DateTimeKind.Utc),
                ImgSrcSmall = $"https://mars.nasa.gov/fb_t1_{i}_s.jpg",
                ImgSrcMedium = $"https://mars.nasa.gov/fb_t1_{i}_m.jpg",
                ImgSrcLarge = $"https://mars.nasa.gov/fb_t1_{i}_l.jpg",
                ImgSrcFull = $"https://mars.nasa.gov/fb_t1_{i}_f.jpg",
                Site = 311,
                Drive = 5011,
                MastAz = 115.0f, // Single position
                MastEl = 17.0f, // 12° gap from tier 0 → separate tier
                SpacecraftClock = clock,
                RoverId = 1,
                CameraId = 2,
                CreatedAt = now,
                UpdatedAt = now
            });
            clock += 10.0f;
        }
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetPanoramasAsync(
            rovers: "curiosity",
            solMin: 7011,
            solMax: 7011,
            pageNumber: 1,
            pageSize: 25);

        // Assert - Multi-row rejected (tier 1 has only 1 column), tier 0 recovered as single-row
        result.Data.Should().HaveCount(1);
        var panorama = result.Data.First();
        panorama.Attributes!.MosaicType.Should().Be("single_row");
        panorama.Attributes.TotalPhotos.Should().Be(4); // Just the 4 sweep photos
        panorama.Attributes.CoverageDegrees.Should().BeApproximately(220.0f, 0.1f);
        panorama.Attributes.ElevationRows.Should().Be(1);
    }

    [Fact]
    public async Task GetPanoramasAsync_ElevationClustering_PhotosWithin5DegreesStayOneTier()
    {
        // Arrange - Elevations with all gaps < 5° should cluster into one tier (single_row)
        DbContext.Photos.RemoveRange(DbContext.Photos);
        var now = DateTime.UtcNow;

        // Elevations: -12, -9, -5, -2, 1 → gaps: 3, 4, 3, 3 → all < 5 → 1 tier
        var elevations = new[] { -12.0f, -9.0f, -5.0f, -2.0f, 1.0f };
        for (int i = 0; i < elevations.Length; i++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"CLUSTER_{i}",
                Sol = 7004,
                EarthDate = new DateTime(2025, 6, 5, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2025, 6, 5, 10, i, 0, DateTimeKind.Utc),
                ImgSrcSmall = $"https://mars.nasa.gov/cluster_{i}_s.jpg",
                ImgSrcMedium = $"https://mars.nasa.gov/cluster_{i}_m.jpg",
                ImgSrcLarge = $"https://mars.nasa.gov/cluster_{i}_l.jpg",
                ImgSrcFull = $"https://mars.nasa.gov/cluster_{i}_f.jpg",
                Site = 304,
                Drive = 5004,
                MastAz = 30.0f + (i * 30.0f), // 120° range, 5 positions
                MastEl = elevations[i],
                SpacecraftClock = 6000000.0f + (i * 60.0f),
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
            solMin: 7004,
            solMax: 7004,
            pageNumber: 1,
            pageSize: 25);

        // Assert - Should be single_row (all elevations within one tier)
        result.Data.Should().HaveCount(1);
        var panorama = result.Data.First();
        panorama.Attributes!.MosaicType.Should().Be("single_row");
        panorama.Attributes.ElevationRows.Should().Be(1);
    }
}
