using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MarsVista.Api.Entities;
using MarsVista.Api.Services.V2;
using MarsVista.Api.Tests.Integration;

namespace MarsVista.Api.Tests.Services.V2;

public class TimeMachineServiceTests : IntegrationTestBase
{
    private Mock<IPhotoQueryServiceV2> _mockPhotoService = null!;
    private Mock<ILogger<TimeMachineService>> _mockLogger = null!;
    private TimeMachineService _service = null!;

    protected override void ConfigureServices(IServiceCollection services)
    {
        _mockPhotoService = new Mock<IPhotoQueryServiceV2>();
        _mockLogger = new Mock<ILogger<TimeMachineService>>();

        services.AddSingleton(_mockPhotoService.Object);
        services.AddSingleton(_mockLogger.Object);
        services.AddScoped<TimeMachineService>();
    }

    protected override async Task SeedAdditionalDataAsync()
    {
        _service = ServiceProvider.GetRequiredService<TimeMachineService>();
        var now = DateTime.UtcNow;

        // Add photos at same location (site 79, drive 1204) across multiple sols
        // Sol 1000 - 14:00 Mars time
        DbContext.Photos.Add(new Photo
        {
            NasaId = "NRF_1000_0001",
            Sol = 1000,
            EarthDate = new DateTime(2015, 5, 30, 0, 0, 0, DateTimeKind.Utc),
            DateTakenUtc = new DateTime(2015, 5, 30, 10, 0, 0, DateTimeKind.Utc),
            DateTakenMars = "Sol-1000M14:00:00",
            ImgSrcSmall = "https://mars.nasa.gov/1000_s.jpg",
            ImgSrcMedium = "https://mars.nasa.gov/1000_m.jpg",
            ImgSrcLarge = "https://mars.nasa.gov/1000.jpg",
            ImgSrcFull = "https://mars.nasa.gov/1000_f.jpg",
            Site = 79,
            Drive = 1204,
            RoverId = 1,
            CameraId = 2, // MAST
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
            ImgSrcSmall = "https://mars.nasa.gov/1010_s.jpg",
            ImgSrcMedium = "https://mars.nasa.gov/1010_m.jpg",
            ImgSrcLarge = "https://mars.nasa.gov/1010.jpg",
            ImgSrcFull = "https://mars.nasa.gov/1010_f.jpg",
            Site = 79,
            Drive = 1204,
            RoverId = 1,
            CameraId = 2, // MAST
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
            ImgSrcSmall = "https://mars.nasa.gov/1020_s.jpg",
            ImgSrcMedium = "https://mars.nasa.gov/1020_m.jpg",
            ImgSrcLarge = "https://mars.nasa.gov/1020.jpg",
            ImgSrcFull = "https://mars.nasa.gov/1020_f.jpg",
            Site = 79,
            Drive = 1204,
            RoverId = 1,
            CameraId = 2, // MAST
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
            ImgSrcSmall = "https://mars.nasa.gov/1030_s.jpg",
            ImgSrcMedium = "https://mars.nasa.gov/1030_m.jpg",
            ImgSrcLarge = "https://mars.nasa.gov/1030.jpg",
            ImgSrcFull = "https://mars.nasa.gov/1030_f.jpg",
            Site = 79,
            Drive = 1204,
            RoverId = 1,
            CameraId = 2, // MAST
            CreatedAt = now,
            UpdatedAt = now
        });

        // Sol 1040 - NAVCAM at 14:00 Mars time (different camera)
        DbContext.Photos.Add(new Photo
        {
            NasaId = "NRF_1040_0005",
            Sol = 1040,
            EarthDate = new DateTime(2015, 7, 10, 0, 0, 0, DateTimeKind.Utc),
            DateTakenUtc = new DateTime(2015, 7, 10, 10, 0, 0, DateTimeKind.Utc),
            DateTakenMars = "Sol-1040M14:00:00",
            ImgSrcSmall = "https://mars.nasa.gov/1040_s.jpg",
            ImgSrcMedium = "https://mars.nasa.gov/1040_m.jpg",
            ImgSrcLarge = "https://mars.nasa.gov/1040.jpg",
            ImgSrcFull = "https://mars.nasa.gov/1040_f.jpg",
            Site = 79,
            Drive = 1204,
            RoverId = 1,
            CameraId = 1, // FHAZ (different camera)
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
            ImgSrcSmall = "https://mars.nasa.gov/diff_s.jpg",
            ImgSrcMedium = "https://mars.nasa.gov/diff_m.jpg",
            ImgSrcLarge = "https://mars.nasa.gov/diff.jpg",
            ImgSrcFull = "https://mars.nasa.gov/diff_f.jpg",
            Site = 80, // Different location
            Drive = 1300,
            RoverId = 1,
            CameraId = 2, // MAST
            CreatedAt = now,
            UpdatedAt = now
        });

        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetTimeMachinePhotosAsync_WithValidLocation_ReturnsPhotos()
    {
        // Act
        var result = await _service.GetTimeMachinePhotosAsync(
            site: 79,
            drive: 1204);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().NotBeNullOrEmpty();
        result.Location.Should().NotBeNull();
        result.Location!.Site.Should().Be(79);
        result.Location.Drive.Should().Be(1204);
    }

    [Fact]
    public async Task GetTimeMachinePhotosAsync_GroupsBySol()
    {
        // Act
        var result = await _service.GetTimeMachinePhotosAsync(
            site: 79,
            drive: 1204);

        // Assert
        result.Data.Should().HaveCount(5); // 5 unique sols
        result.Data.Should().OnlyHaveUniqueItems(x => x.Sol);
    }

    [Fact]
    public async Task GetTimeMachinePhotosAsync_OrdersBySol()
    {
        // Act
        var result = await _service.GetTimeMachinePhotosAsync(
            site: 79,
            drive: 1204);

        // Assert
        result.Data.Should().BeInAscendingOrder(x => x.Sol);
        result.Data[0].Sol.Should().Be(1000);
        result.Data[4].Sol.Should().Be(1040);
    }

    [Fact]
    public async Task GetTimeMachinePhotosAsync_WithMarsTimeFilter_FiltersCorrectly()
    {
        // Act - Filter for photos around 14:00 Mars time
        var result = await _service.GetTimeMachinePhotosAsync(
            site: 79,
            drive: 1204,
            marsTime: "M14:00:00");

        // Assert
        result.Data.Should().HaveCount(4); // Sols 1000, 1010, 1020, 1040 (not 1030 which is at 10:00)
        result.Data.Should().NotContain(x => x.Sol == 1030);
    }

    [Fact]
    public async Task GetTimeMachinePhotosAsync_WithMarsTimeFilter_RespectsTolerance()
    {
        // Act - Filter for 14:00, should include photos up to 30 minutes away
        var result = await _service.GetTimeMachinePhotosAsync(
            site: 79,
            drive: 1204,
            marsTime: "M14:00:00");

        // Assert
        result.Data.Should().Contain(x => x.Sol == 1010 && x.MarsTime == "M14:15:00"); // 15 min away
        result.Data.Should().Contain(x => x.Sol == 1020 && x.MarsTime == "M14:25:00"); // 25 min away
    }

    [Fact]
    public async Task GetTimeMachinePhotosAsync_WithCameraFilter_FiltersCorrectly()
    {
        // Act
        var result = await _service.GetTimeMachinePhotosAsync(
            site: 79,
            drive: 1204,
            camera: "MAST");

        // Assert
        result.Data.Should().HaveCount(4); // Excludes FHAZ photo
        result.Data.Should().OnlyContain(x => x.Photo.Relationships!.Camera!.Id == "MAST");
    }

    [Fact]
    public async Task GetTimeMachinePhotosAsync_WithRoverFilter_FiltersCorrectly()
    {
        // Arrange - Add Perseverance rover photos
        var now = DateTime.UtcNow;

        DbContext.Photos.Add(new Photo
        {
            NasaId = "NRP_100_0001",
            Sol = 100,
            EarthDate = new DateTime(2021, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            DateTakenUtc = new DateTime(2021, 3, 1, 10, 0, 0, DateTimeKind.Utc),
            DateTakenMars = "Sol-100M14:00:00",
            ImgSrcSmall = "https://mars.nasa.gov/pers100_s.jpg",
            ImgSrcMedium = "https://mars.nasa.gov/pers100_m.jpg",
            ImgSrcLarge = "https://mars.nasa.gov/pers100.jpg",
            ImgSrcFull = "https://mars.nasa.gov/pers100_f.jpg",
            Site = 79,
            Drive = 1204,
            RoverId = 2, // Perseverance
            CameraId = 3, // NAVCAM
            CreatedAt = now,
            UpdatedAt = now
        });
        await DbContext.SaveChangesAsync();

        // Act - Filter for Curiosity only
        var result = await _service.GetTimeMachinePhotosAsync(
            site: 79,
            drive: 1204,
            rover: "curiosity");

        // Assert
        result.Data.Should().HaveCount(5);
        result.Data.Should().OnlyContain(x => x.Photo.Relationships!.Rover!.Id == "curiosity");
    }

    [Fact]
    public async Task GetTimeMachinePhotosAsync_WithLimit_LimitsResults()
    {
        // Act
        var result = await _service.GetTimeMachinePhotosAsync(
            site: 79,
            drive: 1204,
            limit: 2);

        // Assert
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTimeMachinePhotosAsync_IncludesEarthDate()
    {
        // Act
        var result = await _service.GetTimeMachinePhotosAsync(
            site: 79,
            drive: 1204);

        // Assert
        result.Data.Should().OnlyContain(x => !string.IsNullOrEmpty(x.EarthDate));
        result.Data[0].EarthDate.Should().Be("2015-05-30");
    }

    [Fact]
    public async Task GetTimeMachinePhotosAsync_IncludesMarsTime()
    {
        // Act
        var result = await _service.GetTimeMachinePhotosAsync(
            site: 79,
            drive: 1204);

        // Assert
        var photosWithMarsTime = result.Data.Where(x => !string.IsNullOrEmpty(x.MarsTime)).ToList();
        photosWithMarsTime.Should().NotBeEmpty();
        photosWithMarsTime[0].MarsTime.Should().MatchRegex(@"M\d{2}:\d{2}:\d{2}");
    }

    [Fact]
    public async Task GetTimeMachinePhotosAsync_IncludesLightingConditions()
    {
        // Act
        var result = await _service.GetTimeMachinePhotosAsync(
            site: 79,
            drive: 1204);

        // Assert
        result.Data.Should().OnlyContain(x => !string.IsNullOrEmpty(x.LightingConditions));
    }

    [Fact]
    public async Task GetTimeMachinePhotosAsync_CalculatesLocationStats()
    {
        // Act
        var result = await _service.GetTimeMachinePhotosAsync(
            site: 79,
            drive: 1204);

        // Assert
        result.Location!.TotalVisits.Should().Be(5); // 5 unique sols
        result.Location.TotalPhotos.Should().Be(5); // 5 photos total at this location
    }

    [Fact]
    public async Task GetTimeMachinePhotosAsync_WithNoMatchingPhotos_ReturnsEmptyList()
    {
        // Act
        var result = await _service.GetTimeMachinePhotosAsync(
            site: 999,
            drive: 9999);

        // Assert
        result.Data.Should().BeEmpty();
        result.Location!.Site.Should().Be(999);
        result.Location.Drive.Should().Be(9999);
        result.Location.TotalVisits.Should().Be(0);
        result.Location.TotalPhotos.Should().Be(0);
    }

    [Fact]
    public async Task GetTimeMachinePhotosAsync_IncludesPhotoResource()
    {
        // Act
        var result = await _service.GetTimeMachinePhotosAsync(
            site: 79,
            drive: 1204);

        // Assert
        var entry = result.Data.First();
        entry.Photo.Should().NotBeNull();
        entry.Photo.Type.Should().Be("photo");
        entry.Photo.Attributes.Should().NotBeNull();
        entry.Photo.Attributes!.ImgSrc.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetTimeMachinePhotosAsync_PhotoIncludesRoverAndCamera()
    {
        // Act
        var result = await _service.GetTimeMachinePhotosAsync(
            site: 79,
            drive: 1204);

        // Assert
        var entry = result.Data.First();
        entry.Photo.Relationships.Should().NotBeNull();
        entry.Photo.Relationships!.Rover.Should().NotBeNull();
        entry.Photo.Relationships.Rover!.Id.Should().Be("curiosity");
        entry.Photo.Relationships.Camera.Should().NotBeNull();
        entry.Photo.Relationships.Camera!.Attributes.Should().NotBeNull();
        entry.Photo.Relationships.Camera.Attributes!.FullName.Should().Be("Mast Camera");
    }

    [Fact]
    public async Task GetTimeMachinePhotosAsync_HandlesPhotosWithoutMarsTime()
    {
        // Arrange - Add photo without Mars time
        var now = DateTime.UtcNow;
        DbContext.Photos.Add(new Photo
        {
            NasaId = "NRF_2000_NO_MARS_TIME",
            Sol = 2000,
            EarthDate = new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DateTakenUtc = new DateTime(2016, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            DateTakenMars = null, // No Mars time
            ImgSrcSmall = "https://mars.nasa.gov/2000_s.jpg",
            ImgSrcMedium = "https://mars.nasa.gov/2000_m.jpg",
            ImgSrcLarge = "https://mars.nasa.gov/2000.jpg",
            ImgSrcFull = "https://mars.nasa.gov/2000_f.jpg",
            Site = 79,
            Drive = 1204,
            RoverId = 1,
            CameraId = 2, // MAST
            CreatedAt = now,
            UpdatedAt = now
        });
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetTimeMachinePhotosAsync(
            site: 79,
            drive: 1204);

        // Assert
        result.Data.Should().Contain(x => x.Sol == 2000);
        var photoWithoutMarsTime = result.Data.First(x => x.Sol == 2000);
        photoWithoutMarsTime.MarsTime.Should().BeNull();
    }

    [Fact]
    public async Task GetTimeMachinePhotosAsync_WithMarsTimeFilter_ExcludesPhotosWithoutMarsTime()
    {
        // Arrange - Add photo without Mars time
        var now = DateTime.UtcNow;
        DbContext.Photos.Add(new Photo
        {
            NasaId = "NRF_2000_NO_MARS_TIME",
            Sol = 2000,
            EarthDate = new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DateTakenUtc = new DateTime(2016, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            DateTakenMars = null, // No Mars time
            ImgSrcSmall = "https://mars.nasa.gov/2000_s.jpg",
            ImgSrcMedium = "https://mars.nasa.gov/2000_m.jpg",
            ImgSrcLarge = "https://mars.nasa.gov/2000.jpg",
            ImgSrcFull = "https://mars.nasa.gov/2000_f.jpg",
            Site = 79,
            Drive = 1204,
            RoverId = 1,
            CameraId = 2, // MAST
            CreatedAt = now,
            UpdatedAt = now
        });
        await DbContext.SaveChangesAsync();

        // Act - Filter by Mars time
        var result = await _service.GetTimeMachinePhotosAsync(
            site: 79,
            drive: 1204,
            marsTime: "M14:00:00");

        // Assert
        result.Data.Should().NotContain(x => x.Sol == 2000);
    }

    [Fact]
    public async Task GetTimeMachinePhotosAsync_IncludesMetadata()
    {
        // Act
        var result = await _service.GetTimeMachinePhotosAsync(
            site: 79,
            drive: 1204);

        // Assert
        result.Meta.Should().NotBeNull();
        result.Meta!.TotalCount.Should().Be(5);
        result.Meta.ReturnedCount.Should().Be(5);
    }

    [Fact]
    public async Task GetTimeMachinePhotosAsync_DefaultLimitIs100()
    {
        // Arrange - Add many photos at same location
        var now = DateTime.UtcNow;
        for (int i = 0; i < 150; i++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"NRF_{5000 + i}_0000",
                Sol = 5000 + i,
                EarthDate = new DateTime(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i),
                DateTakenUtc = new DateTime(2017, 1, 1, 10, 0, 0, DateTimeKind.Utc).AddDays(i),
                DateTakenMars = $"Sol-{5000 + i}M14:00:00",
                ImgSrcSmall = $"https://mars.nasa.gov/{5000 + i}_s.jpg",
                ImgSrcMedium = $"https://mars.nasa.gov/{5000 + i}_m.jpg",
                ImgSrcLarge = $"https://mars.nasa.gov/{5000 + i}.jpg",
                ImgSrcFull = $"https://mars.nasa.gov/{5000 + i}_f.jpg",
                Site = 90,
                Drive = 1500,
                RoverId = 1,
                CameraId = 2, // MAST
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        await DbContext.SaveChangesAsync();

        // Act - No limit specified
        var result = await _service.GetTimeMachinePhotosAsync(
            site: 90,
            drive: 1500);

        // Assert - Should default to 100
        result.Data.Should().HaveCount(100);
    }

    [Fact]
    public async Task GetTimeMachinePhotosAsync_PhotoIncludesImages()
    {
        // Act
        var result = await _service.GetTimeMachinePhotosAsync(
            site: 79,
            drive: 1204);

        // Assert
        var entry = result.Data.First();
        entry.Photo.Attributes!.Images.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTimeMachinePhotosAsync_ExcludesOtherLocations()
    {
        // Act
        var result = await _service.GetTimeMachinePhotosAsync(
            site: 79,
            drive: 1204);

        // Assert
        result.Data.Should().NotContain(x =>
            x.Photo.Attributes != null &&
            x.Photo.Attributes.Location != null &&
            x.Photo.Attributes.Location.Site == 80); // Photo at site 80, drive 1300
    }
}
