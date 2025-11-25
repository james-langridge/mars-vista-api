using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MarsVista.Core.Entities;
using MarsVista.Api.Services.V2;

namespace MarsVista.Api.Tests.Integration.V2;

public class TimeMachineIntegrationTests : IntegrationTestBase
{
    private ITimeMachineService _timeMachineService = null!;
    private IPhotoQueryServiceV2 _photoQueryService = null!;

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IPhotoQueryServiceV2, PhotoQueryServiceV2>();
        services.AddScoped<ITimeMachineService, TimeMachineService>();
    }

    protected override async Task SeedAdditionalDataAsync()
    {
        _timeMachineService = ServiceProvider.GetRequiredService<ITimeMachineService>();
        _photoQueryService = ServiceProvider.GetRequiredService<IPhotoQueryServiceV2>();

        var now = DateTime.UtcNow;

        // Photos at location (site 79, drive 1204) across multiple sols at different times

        // Sol 1000 - 14:00 Mars time
        DbContext.Photos.Add(new Photo
        {
            NasaId = "NRF_1000_0001",
            Sol = 1000,
            EarthDate = new DateTime(2015, 5, 30, 0, 0, 0, DateTimeKind.Utc),
            DateTakenUtc = new DateTime(2015, 5, 30, 10, 0, 0, DateTimeKind.Utc),
            DateTakenMars = "Sol-1000M14:00:00",
            Site = 79,
            Drive = 1204,
            ImgSrcLarge = "https://mars.nasa.gov/1000.jpg",
            RoverId = 1,
            CameraId = 2,
            CreatedAt = now,
            UpdatedAt = now
        });

        // Sol 1010 - 14:15 Mars time (within 30 min tolerance)
        DbContext.Photos.Add(new Photo
        {
            NasaId = "NRF_1010_0002",
            Sol = 1010,
            EarthDate = new DateTime(2015, 6, 10, 0, 0, 0, DateTimeKind.Utc),
            DateTakenUtc = new DateTime(2015, 6, 10, 10, 15, 0, DateTimeKind.Utc),
            DateTakenMars = "Sol-1010M14:15:00",
            Site = 79,
            Drive = 1204,
            ImgSrcLarge = "https://mars.nasa.gov/1010.jpg",
            RoverId = 1,
            CameraId = 2,
            CreatedAt = now,
            UpdatedAt = now
        });

        // Sol 1020 - 14:25 Mars time (within 30 min tolerance)
        DbContext.Photos.Add(new Photo
        {
            NasaId = "NRF_1020_0003",
            Sol = 1020,
            EarthDate = new DateTime(2015, 6, 20, 0, 0, 0, DateTimeKind.Utc),
            DateTakenUtc = new DateTime(2015, 6, 20, 10, 25, 0, DateTimeKind.Utc),
            DateTakenMars = "Sol-1020M14:25:00",
            Site = 79,
            Drive = 1204,
            ImgSrcLarge = "https://mars.nasa.gov/1020.jpg",
            RoverId = 1,
            CameraId = 2,
            CreatedAt = now,
            UpdatedAt = now
        });

        // Sol 1030 - 10:00 Mars time (different time of day)
        DbContext.Photos.Add(new Photo
        {
            NasaId = "NRF_1030_0004",
            Sol = 1030,
            EarthDate = new DateTime(2015, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            DateTakenUtc = new DateTime(2015, 7, 1, 6, 0, 0, DateTimeKind.Utc),
            DateTakenMars = "Sol-1030M10:00:00",
            Site = 79,
            Drive = 1204,
            ImgSrcLarge = "https://mars.nasa.gov/1030.jpg",
            RoverId = 1,
            CameraId = 2,
            CreatedAt = now,
            UpdatedAt = now
        });

        // Sol 1040 - 06:00 Mars time (morning)
        DbContext.Photos.Add(new Photo
        {
            NasaId = "NRF_1040_0005",
            Sol = 1040,
            EarthDate = new DateTime(2015, 7, 10, 0, 0, 0, DateTimeKind.Utc),
            DateTakenUtc = new DateTime(2015, 7, 10, 2, 0, 0, DateTimeKind.Utc),
            DateTakenMars = "Sol-1040M06:00:00",
            Site = 79,
            Drive = 1204,
            ImgSrcLarge = "https://mars.nasa.gov/1040.jpg",
            RoverId = 1,
            CameraId = 2,
            CreatedAt = now,
            UpdatedAt = now
        });

        // Sol 1050 - 14:00 Mars time with NAVCAM (different camera)
        DbContext.Photos.Add(new Photo
        {
            NasaId = "NRF_1050_0006",
            Sol = 1050,
            EarthDate = new DateTime(2015, 7, 20, 0, 0, 0, DateTimeKind.Utc),
            DateTakenUtc = new DateTime(2015, 7, 20, 10, 0, 0, DateTimeKind.Utc),
            DateTakenMars = "Sol-1050M14:00:00",
            Site = 79,
            Drive = 1204,
            ImgSrcLarge = "https://mars.nasa.gov/1050.jpg",
            RoverId = 1,
            CameraId = 1, // FHAZ instead of MAST
            CreatedAt = now,
            UpdatedAt = now
        });

        // Photos at different location (should be excluded)
        DbContext.Photos.Add(new Photo
        {
            NasaId = "NRF_1000_DIFF_LOC",
            Sol = 1000,
            EarthDate = new DateTime(2015, 5, 30, 0, 0, 0, DateTimeKind.Utc),
            DateTakenUtc = new DateTime(2015, 5, 30, 10, 0, 0, DateTimeKind.Utc),
            DateTakenMars = "Sol-1000M14:00:00",
            Site = 80,
            Drive = 1300,
            ImgSrcLarge = "https://mars.nasa.gov/diff.jpg",
            RoverId = 1,
            CameraId = 2,
            CreatedAt = now,
            UpdatedAt = now
        });

        // Perseverance photo at same site/drive (different rover)
        DbContext.Photos.Add(new Photo
        {
            NasaId = "NRP_100_0001",
            Sol = 100,
            EarthDate = new DateTime(2021, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            DateTakenUtc = new DateTime(2021, 6, 1, 10, 0, 0, DateTimeKind.Utc),
            DateTakenMars = "Sol-100M14:00:00",
            Site = 79,
            Drive = 1204,
            ImgSrcLarge = "https://mars.nasa.gov/pers100.jpg",
            RoverId = 2,
            CameraId = 3,
            CreatedAt = now,
            UpdatedAt = now
        });

        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetTimeMachinePhotos_WithValidLocation_ReturnsPhotos()
    {
        // Act
        var response = await _timeMachineService.GetTimeMachinePhotosAsync(
            site: 79,
            drive: 1204);

        // Assert
        response.Should().NotBeNull();
        response.Data.Should().NotBeNullOrEmpty();
        response.Location.Should().NotBeNull();
        response.Location!.Site.Should().Be(79);
        response.Location.Drive.Should().Be(1204);
    }

    [Fact]
    public async Task GetTimeMachinePhotos_GroupsBySol()
    {
        // Act
        var response = await _timeMachineService.GetTimeMachinePhotosAsync(
            site: 79,
            drive: 1204);

        // Assert
        // Should have 7 unique sols (Curiosity: 1000, 1010, 1020, 1030, 1040, 1050; Perseverance: 100)
        response.Data.Should().HaveCount(7);
        response.Data.Should().OnlyHaveUniqueItems(x => x.Sol);
    }

    [Fact]
    public async Task GetTimeMachinePhotos_OrdersBySol()
    {
        // Act
        var response = await _timeMachineService.GetTimeMachinePhotosAsync(
            site: 79,
            drive: 1204);

        // Assert
        response.Data.Should().BeInAscendingOrder(x => x.Sol);
    }

    [Fact]
    public async Task GetTimeMachinePhotos_WithMarsTimeFilter_FiltersCorrectly()
    {
        // Act - Filter for photos around 14:00 Mars time
        var response = await _timeMachineService.GetTimeMachinePhotosAsync(
            site: 79,
            drive: 1204,
            marsTime: "M14:00:00");

        // Assert
        // Should include sols 1000, 1010, 1020, 1050 (Curiosity) and 100 (Perseverance)
        // Should exclude sol 1030 (10:00) and 1040 (06:00)
        response.Data.Should().NotContain(x => x.Sol == 1030);
        response.Data.Should().NotContain(x => x.Sol == 1040);
        response.Data.Should().Contain(x => x.Sol == 1000);
        response.Data.Should().Contain(x => x.Sol == 1010);
        response.Data.Should().Contain(x => x.Sol == 1020);
    }

    [Fact]
    public async Task GetTimeMachinePhotos_WithCameraFilter_FiltersCorrectly()
    {
        // Act
        var response = await _timeMachineService.GetTimeMachinePhotosAsync(
            site: 79,
            drive: 1204,
            camera: "MAST");

        // Assert
        // Should exclude sol 1050 (FHAZ) and sol 100 (Perseverance NAVCAM)
        response.Data.Should().NotContain(x => x.Sol == 1050);
        response.Data.Should().NotContain(x => x.Sol == 100);
    }

    [Fact]
    public async Task GetTimeMachinePhotos_WithRoverFilter_FiltersCorrectly()
    {
        // Act
        var response = await _timeMachineService.GetTimeMachinePhotosAsync(
            site: 79,
            drive: 1204,
            rover: "curiosity");

        // Assert
        // Should exclude Perseverance photo (sol 100)
        response.Data.Should().NotContain(x => x.Sol == 100);
        response.Data.Should().HaveCount(6); // Only Curiosity photos
    }

    [Fact]
    public async Task GetTimeMachinePhotos_WithLimit_LimitsResults()
    {
        // Act
        var response = await _timeMachineService.GetTimeMachinePhotosAsync(
            site: 79,
            drive: 1204,
            limit: 3);

        // Assert
        response.Data.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetTimeMachinePhotos_IncludesEarthDate()
    {
        // Act
        var response = await _timeMachineService.GetTimeMachinePhotosAsync(
            site: 79,
            drive: 1204);

        // Assert
        response.Data.Should().OnlyContain(x => !string.IsNullOrEmpty(x.EarthDate));
        response.Data.First().EarthDate.Should().MatchRegex(@"\d{4}-\d{2}-\d{2}");
    }

    [Fact]
    public async Task GetTimeMachinePhotos_IncludesMarsTime()
    {
        // Act
        var response = await _timeMachineService.GetTimeMachinePhotosAsync(
            site: 79,
            drive: 1204);

        // Assert
        var photosWithMarsTime = response.Data.Where(x => !string.IsNullOrEmpty(x.MarsTime)).ToList();
        photosWithMarsTime.Should().NotBeEmpty();
        photosWithMarsTime.Should().OnlyContain(x => x.MarsTime!.StartsWith("M"));
    }

    [Fact]
    public async Task GetTimeMachinePhotos_IncludesLightingConditions()
    {
        // Act
        var response = await _timeMachineService.GetTimeMachinePhotosAsync(
            site: 79,
            drive: 1204);

        // Assert
        response.Data.Should().OnlyContain(x => !string.IsNullOrEmpty(x.LightingConditions));
    }

    [Fact]
    public async Task GetTimeMachinePhotos_CalculatesLocationStats()
    {
        // Act
        var response = await _timeMachineService.GetTimeMachinePhotosAsync(
            site: 79,
            drive: 1204);

        // Assert
        response.Location!.TotalVisits.Should().Be(7); // 7 unique sols
        response.Location.TotalPhotos.Should().Be(7); // 7 photos total
    }

    [Fact]
    public async Task GetTimeMachinePhotos_WithNoMatchingPhotos_ReturnsEmptyList()
    {
        // Act
        var response = await _timeMachineService.GetTimeMachinePhotosAsync(
            site: 999,
            drive: 9999);

        // Assert
        response.Data.Should().BeEmpty();
        response.Location!.Site.Should().Be(999);
        response.Location.Drive.Should().Be(9999);
        response.Location.TotalVisits.Should().Be(0);
        response.Location.TotalPhotos.Should().Be(0);
    }

    [Fact]
    public async Task GetTimeMachinePhotos_IncludesPhotoResource()
    {
        // Act
        var response = await _timeMachineService.GetTimeMachinePhotosAsync(
            site: 79,
            drive: 1204);

        // Assert
        var entry = response.Data.First();
        entry.Photo.Should().NotBeNull();
        entry.Photo.Type.Should().Be("photo");
        entry.Photo.Attributes.Should().NotBeNull();
        entry.Photo.Attributes!.ImgSrc.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetTimeMachinePhotos_PhotoIncludesRoverAndCamera()
    {
        // Act
        var response = await _timeMachineService.GetTimeMachinePhotosAsync(
            site: 79,
            drive: 1204);

        // Assert
        var entry = response.Data.First();
        entry.Photo.Relationships.Should().NotBeNull();
        entry.Photo.Relationships!.Rover.Should().NotBeNull();
        entry.Photo.Relationships.Camera.Should().NotBeNull();
        entry.Photo.Relationships.Camera!.Attributes.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTimeMachinePhotos_IncludesMetadata()
    {
        // Act
        var response = await _timeMachineService.GetTimeMachinePhotosAsync(
            site: 79,
            drive: 1204);

        // Assert
        response.Meta.Should().NotBeNull();
        response.Meta!.TotalCount.Should().Be(7);
        response.Meta.ReturnedCount.Should().Be(7);
    }

    [Fact]
    public async Task GetTimeMachinePhotos_ExcludesOtherLocations()
    {
        // Act
        var response = await _timeMachineService.GetTimeMachinePhotosAsync(
            site: 79,
            drive: 1204);

        // Assert
        // Should not include photo from site 80, drive 1300
        response.Data.Should().OnlyContain(x =>
            x.Photo.Attributes!.NasaId != "NRF_1000_DIFF_LOC");
    }

    [Fact]
    public async Task GetTimeMachinePhotos_WithCombinedFilters_AppliesAllFilters()
    {
        // Act - Filter by rover, Mars time, and camera
        var response = await _timeMachineService.GetTimeMachinePhotosAsync(
            site: 79,
            drive: 1204,
            rover: "curiosity",
            marsTime: "M14:00:00",
            camera: "MAST");

        // Assert
        // Should only include Curiosity MAST photos around 14:00 Mars time
        // Should be sols 1000, 1010, 1020 (not 1050 which is FHAZ)
        response.Data.Should().HaveCount(3);
        response.Data.Should().Contain(x => x.Sol == 1000);
        response.Data.Should().Contain(x => x.Sol == 1010);
        response.Data.Should().Contain(x => x.Sol == 1020);
    }

    [Fact]
    public async Task GetTimeMachinePhotos_RespectsMarsTimeTolerance()
    {
        // Act - Filter for 14:00, should include photos up to 30 minutes away
        var response = await _timeMachineService.GetTimeMachinePhotosAsync(
            site: 79,
            drive: 1204,
            rover: "curiosity",
            marsTime: "M14:00:00");

        // Assert
        // Should include 14:15 and 14:25 (within 30 min tolerance)
        response.Data.Should().Contain(x => x.Sol == 1010); // 14:15
        response.Data.Should().Contain(x => x.Sol == 1020); // 14:25
    }

    [Fact]
    public async Task GetTimeMachinePhotos_ShowsTimeEvolutionAtLocation()
    {
        // Act
        var response = await _timeMachineService.GetTimeMachinePhotosAsync(
            site: 79,
            drive: 1204,
            rover: "curiosity");

        // Assert
        // Should show progression through different sols
        var sols = response.Data.Select(x => x.Sol).ToList();
        sols.Should().ContainInOrder(1000, 1010, 1020, 1030, 1040, 1050);

        // Should span multiple Earth dates
        var earthDates = response.Data.Select(x => x.EarthDate).Distinct().ToList();
        earthDates.Should().HaveCountGreaterThan(1);
    }
}
