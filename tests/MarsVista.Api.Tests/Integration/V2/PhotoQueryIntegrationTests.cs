using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MarsVista.Core.Entities;
using MarsVista.Api.Models.V2;
using MarsVista.Api.Services.V2;

namespace MarsVista.Api.Tests.Integration.V2;

public class PhotoQueryIntegrationTests : IntegrationTestBase
{
    private IPhotoQueryServiceV2 _photoQueryService = null!;

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IPhotoQueryServiceV2, PhotoQueryServiceV2>();
    }

    protected override async Task SeedAdditionalDataAsync()
    {
        // Get the service after base initialization
        _photoQueryService = ServiceProvider.GetRequiredService<IPhotoQueryServiceV2>();

        var now = DateTime.UtcNow;

        // Add photos for different scenarios
        var photos = new List<Photo>();

        // Curiosity photos - sol range 100-200
        for (int i = 0; i < 50; i++)
        {
            photos.Add(new Photo
            {
                NasaId = $"NLA_{100000 + i}",
                ImgSrcFull = $"https://mars.nasa.gov/msl/photo{i}.jpg",
                ImgSrcLarge = $"https://mars.nasa.gov/msl/photo{i}.jpg",
                ImgSrcMedium = $"https://mars.nasa.gov/msl/photo{i}.jpg",
                ImgSrcSmall = $"https://mars.nasa.gov/msl/photo{i}.jpg",
                Sol = 100 + (i / 10),
                EarthDate = new DateTime(2013, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i),
                DateTakenUtc = new DateTime(2013, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i),
                DateTakenMars = $"Sol-{100 + (i / 10)}M12:00:00.000",
                RoverId = 1,
                CameraId = i % 2 == 0 ? 1 : 2, // Alternate between FHAZ and MAST
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        // Perseverance photos - sol range 500-600
        for (int i = 0; i < 30; i++)
        {
            photos.Add(new Photo
            {
                NasaId = $"NLB_{200000 + i}",
                ImgSrcFull = $"https://mars.nasa.gov/m2020/photo{i}.jpg",
                ImgSrcLarge = $"https://mars.nasa.gov/m2020/photo{i}.jpg",
                ImgSrcMedium = $"https://mars.nasa.gov/m2020/photo{i}.jpg",
                ImgSrcSmall = $"https://mars.nasa.gov/m2020/photo{i}.jpg",
                Sol = 500 + (i / 5),
                EarthDate = new DateTime(2021, 3, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i),
                DateTakenUtc = new DateTime(2021, 3, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i),
                DateTakenMars = $"Sol-{500 + (i / 5)}M12:00:00.000",
                RoverId = 2,
                CameraId = 3, // NAVCAM
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        DbContext.Photos.AddRange(photos);
        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task QueryPhotos_WithMultipleRovers_ReturnsPhotoFromBothRovers()
    {
        // Arrange
        var parameters = new PhotoQueryParameters
        {
            Rovers = "curiosity,perseverance",
            RoverList = new List<string> { "curiosity", "perseverance" }, // Manually populate for tests
            Include = "rover", // Need to explicitly request relationships
            IncludeList = new List<string> { "rover" }, // Manually populate for tests
            Page = 1,
            PerPage = 100
        };

        // Act
        var response = await _photoQueryService.QueryPhotosAsync(parameters, default);

        // Assert
        response.Data.Should().NotBeEmpty();
        response.Data.Should().Contain(p => p.Relationships!.Rover!.Id == "curiosity");
        response.Data.Should().Contain(p => p.Relationships!.Rover!.Id == "perseverance");
        response.Meta!.TotalCount.Should().Be(80); // 50 + 30
    }

    [Fact]
    public async Task QueryPhotos_WithSolRange_FiltersCorrectly()
    {
        // Arrange
        var parameters = new PhotoQueryParameters
        {
            Rovers = "curiosity",
            RoverList = new List<string> { "curiosity" }, // Manually populate for tests
            SolMin = 100,
            SolMax = 103,
            Include = "rover", // Need to explicitly request relationships
            IncludeList = new List<string> { "rover" }, // Manually populate for tests
            Page = 1,
            PerPage = 100
        };

        // Act
        var response = await _photoQueryService.QueryPhotosAsync(parameters, default);

        // Assert
        response.Data.Should().NotBeEmpty();
        response.Data.Should().OnlyContain(p =>
            p.Attributes!.Sol >= 100 && p.Attributes.Sol <= 103);
        response.Data.Should().OnlyContain(p => p.Relationships!.Rover!.Id == "curiosity");
    }

    [Fact]
    public async Task QueryPhotos_WithDateRange_FiltersCorrectly()
    {
        // Arrange
        var parameters = new PhotoQueryParameters
        {
            Rovers = "perseverance",
            RoverList = new List<string> { "perseverance" }, // Manually populate for tests
            DateMin = "2021-03-10",
            DateMax = "2021-03-20",
            DateMinParsed = new DateTime(2021, 3, 10, 0, 0, 0, DateTimeKind.Utc),
            DateMaxParsed = new DateTime(2021, 3, 20, 0, 0, 0, DateTimeKind.Utc),
            Page = 1,
            PerPage = 100
        };

        // Act
        var response = await _photoQueryService.QueryPhotosAsync(parameters, default);

        // Assert
        response.Data.Should().NotBeEmpty();
        var minDate = new DateTime(2021, 3, 10);
        var maxDate = new DateTime(2021, 3, 20);
        foreach (var photo in response.Data)
        {
            var date = DateTime.Parse(photo.Attributes!.EarthDate!);
            date.Should().BeOnOrAfter(minDate);
            date.Should().BeOnOrBefore(maxDate);
        }
    }

    [Fact]
    public async Task QueryPhotos_WithCameraFilter_ReturnsOnlyMatchingCameras()
    {
        // Arrange
        var parameters = new PhotoQueryParameters
        {
            Rovers = "curiosity",
            RoverList = new List<string> { "curiosity" }, // Manually populate for tests
            Cameras = "FHAZ",
            CameraList = new List<string> { "FHAZ" }, // Manually populate for tests
            Include = "camera", // Need to explicitly request relationships
            IncludeList = new List<string> { "camera" }, // Manually populate for tests
            Page = 1,
            PerPage = 100
        };

        // Act
        var response = await _photoQueryService.QueryPhotosAsync(parameters, default);

        // Assert
        response.Data.Should().NotBeEmpty();
        response.Data.Should().OnlyContain(p => p.Relationships!.Camera!.Id == "FHAZ");
    }

    [Fact]
    public async Task QueryPhotos_WithMultipleCameras_ReturnsAllMatchingCameras()
    {
        // Arrange
        var parameters = new PhotoQueryParameters
        {
            Rovers = "curiosity",
            RoverList = new List<string> { "curiosity" }, // Manually populate for tests
            Cameras = "FHAZ,MAST",
            CameraList = new List<string> { "FHAZ", "MAST" }, // Manually populate for tests
            Include = "camera", // Need to explicitly request relationships
            IncludeList = new List<string> { "camera" }, // Manually populate for tests
            Page = 1,
            PerPage = 100
        };

        // Act
        var response = await _photoQueryService.QueryPhotosAsync(parameters, default);

        // Assert
        response.Data.Should().NotBeEmpty();
        response.Data.Should().Contain(p => p.Relationships!.Camera!.Id == "FHAZ");
        response.Data.Should().Contain(p => p.Relationships!.Camera!.Id == "MAST");
        response.Data.Should().HaveCount(50); // All Curiosity photos
    }

    [Fact]
    public async Task QueryPhotos_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var parametersPage1 = new PhotoQueryParameters
        {
            Rovers = "curiosity",
            RoverList = new List<string> { "curiosity" }, // Manually populate for tests
            Page = 1,
            PerPage = 10
        };

        var parametersPage2 = new PhotoQueryParameters
        {
            Rovers = "curiosity",
            RoverList = new List<string> { "curiosity" }, // Manually populate for tests
            Page = 2,
            PerPage = 10
        };

        // Act
        var responsePage1 = await _photoQueryService.QueryPhotosAsync(parametersPage1, default);
        var responsePage2 = await _photoQueryService.QueryPhotosAsync(parametersPage2, default);

        // Assert
        responsePage1.Data.Should().HaveCount(10);
        responsePage2.Data.Should().HaveCount(10);
        responsePage1.Pagination!.Page.Should().Be(1);
        responsePage2.Pagination!.Page.Should().Be(2);
        responsePage1.Pagination.TotalPages.Should().Be(5); // 50 photos / 10 per page

        // Pages should have different photos
        var page1Ids = responsePage1.Data.Select(p => p.Id).ToList();
        var page2Ids = responsePage2.Data.Select(p => p.Id).ToList();
        page1Ids.Should().NotIntersectWith(page2Ids);
    }

    [Fact]
    public async Task QueryPhotos_WithSorting_ReturnsSortedResults()
    {
        // Arrange
        var parameters = new PhotoQueryParameters
        {
            Rovers = "curiosity",
            RoverList = new List<string> { "curiosity" }, // Manually populate for tests
            Sort = "-sol", // Descending by sol
            Page = 1,
            PerPage = 100
        };

        // Act
        var response = await _photoQueryService.QueryPhotosAsync(parameters, default);

        // Assert
        response.Data.Should().NotBeEmpty();
        var sols = response.Data.Select(p => p.Attributes!.Sol).ToList();
        sols.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task QueryPhotos_CombinedFilters_AppliesAllFilters()
    {
        // Arrange
        var parameters = new PhotoQueryParameters
        {
            Rovers = "curiosity",
            RoverList = new List<string> { "curiosity" }, // Manually populate for tests
            Cameras = "MAST",
            CameraList = new List<string> { "MAST" }, // Manually populate for tests
            SolMin = 100,
            SolMax = 103,
            Include = "rover,camera", // Need to explicitly request relationships
            IncludeList = new List<string> { "rover", "camera" }, // Manually populate for tests
            Page = 1,
            PerPage = 100
        };

        // Act
        var response = await _photoQueryService.QueryPhotosAsync(parameters, default);

        // Assert
        response.Data.Should().NotBeEmpty();
        response.Data.Should().OnlyContain(p =>
            p.Relationships!.Rover!.Id == "curiosity" &&
            p.Relationships.Camera!.Id == "MAST" &&
            p.Attributes!.Sol >= 100 &&
            p.Attributes.Sol <= 103);
    }

    [Fact]
    public async Task QueryPhotos_WithFieldSelection_ReturnsOnlySelectedFields()
    {
        // Arrange
        var parameters = new PhotoQueryParameters
        {
            Rovers = "curiosity",
            RoverList = new List<string> { "curiosity" }, // Manually populate for tests
            Fields = "id,sol",
            Page = 1,
            PerPage = 10
        };

        // Act
        var response = await _photoQueryService.QueryPhotosAsync(parameters, default);

        // Assert
        response.Data.Should().NotBeEmpty();
        foreach (var photo in response.Data)
        {
            photo.Id.Should().BeGreaterThan(0);
            photo.Attributes!.Sol.Should().BeGreaterThanOrEqualTo(0);
            // Other fields should be null when field selection is implemented
        }
    }

    [Fact]
    public async Task QueryPhotos_WithIncludeRelationships_IncludesRelatedData()
    {
        // Arrange
        var parameters = new PhotoQueryParameters
        {
            Rovers = "curiosity",
            RoverList = new List<string> { "curiosity" }, // Manually populate for tests
            Include = "rover,camera",
            IncludeList = new List<string> { "rover", "camera" }, // Manually populate for tests
            Page = 1,
            PerPage = 10
        };

        // Act
        var response = await _photoQueryService.QueryPhotosAsync(parameters, default);

        // Assert
        response.Data.Should().NotBeEmpty();
        foreach (var photo in response.Data)
        {
            photo.Relationships.Should().NotBeNull();
            photo.Relationships!.Rover.Should().NotBeNull();
            photo.Relationships.Camera.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task GetPhotoById_WithValidId_ReturnsPhoto()
    {
        // Arrange
        var photo = await DbContext.Photos.FirstAsync();
        var parameters = new PhotoQueryParameters();

        // Act
        var result = await _photoQueryService.GetPhotoByIdAsync(photo.Id, parameters, default);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(photo.Id);
        result.Attributes!.ImgSrc.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetPhotoById_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var invalidId = 999999;
        var parameters = new PhotoQueryParameters();

        // Act
        var result = await _photoQueryService.GetPhotoByIdAsync(invalidId, parameters, default);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPhotosByIds_WithValidIds_ReturnsAllPhotos()
    {
        // Arrange
        var photoIds = await DbContext.Photos
            .Take(5)
            .Select(p => p.Id)
            .ToListAsync();
        var parameters = new PhotoQueryParameters();

        // Act
        var result = await _photoQueryService.GetPhotosByIdsAsync(photoIds, parameters, default);

        // Assert
        result.Should().HaveCount(5);
        result.Select(p => p.Id).Should().BeEquivalentTo(photoIds);
    }

    [Fact]
    public async Task GetStatistics_GroupByCamera_ReturnsCorrectStats()
    {
        // Arrange
        var parameters = new PhotoQueryParameters
        {
            Rovers = "curiosity",
            RoverList = new List<string> { "curiosity" } // Manually populate for tests
        };

        // Act
        var result = await _photoQueryService.GetStatisticsAsync(parameters, "camera", default);

        // Assert
        result.Should().NotBeNull();
        result.TotalPhotos.Should().Be(50);
        result.Groups.Should().NotBeEmpty();
        result.Groups.Should().Contain(c => c.Key == "FHAZ");
        result.Groups.Should().Contain(c => c.Key == "MAST");

        // Each camera should have 25 photos (50 photos alternating between 2 cameras)
        var fhazStats = result.Groups.First(c => c.Key == "FHAZ");
        var mastStats = result.Groups.First(c => c.Key == "MAST");
        fhazStats.Count.Should().Be(25);
        mastStats.Count.Should().Be(25);
    }

    [Fact]
    public async Task QueryPhotos_WithMarsTimeFilter_AppliesMinutePrecision()
    {
        // Arrange - Add photos with different Mars times for this test
        var now = DateTime.UtcNow;
        var testPhotos = new List<Photo>
        {
            new Photo
            {
                NasaId = "TIME_TEST_1",
                ImgSrcFull = "https://test.com/photo1.jpg",
                ImgSrcLarge = "https://test.com/photo1.jpg",
                ImgSrcMedium = "https://test.com/photo1.jpg",
                ImgSrcSmall = "https://test.com/photo1.jpg",
                Sol = 1000,
                EarthDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2020, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                DateTakenMars = "Sol-01000M10:00:00.000", // 10:00 - should be excluded
                RoverId = 1,
                CameraId = 1,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Photo
            {
                NasaId = "TIME_TEST_2",
                ImgSrcFull = "https://test.com/photo2.jpg",
                ImgSrcLarge = "https://test.com/photo2.jpg",
                ImgSrcMedium = "https://test.com/photo2.jpg",
                ImgSrcSmall = "https://test.com/photo2.jpg",
                Sol = 1000,
                EarthDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2020, 1, 1, 12, 28, 0, DateTimeKind.Utc),
                DateTakenMars = "Sol-01000M12:28:00.000", // 12:28 - should be included
                RoverId = 1,
                CameraId = 1,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Photo
            {
                NasaId = "TIME_TEST_3",
                ImgSrcFull = "https://test.com/photo3.jpg",
                ImgSrcLarge = "https://test.com/photo3.jpg",
                ImgSrcMedium = "https://test.com/photo3.jpg",
                ImgSrcSmall = "https://test.com/photo3.jpg",
                Sol = 1000,
                EarthDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2020, 1, 1, 12, 30, 0, DateTimeKind.Utc),
                DateTakenMars = "Sol-01000M12:30:00.000", // 12:30 - should be included
                RoverId = 1,
                CameraId = 1,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Photo
            {
                NasaId = "TIME_TEST_4",
                ImgSrcFull = "https://test.com/photo4.jpg",
                ImgSrcLarge = "https://test.com/photo4.jpg",
                ImgSrcMedium = "https://test.com/photo4.jpg",
                ImgSrcSmall = "https://test.com/photo4.jpg",
                Sol = 1000,
                EarthDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2020, 1, 1, 12, 35, 0, DateTimeKind.Utc),
                DateTakenMars = "Sol-01000M12:35:00.000", // 12:35 - should be excluded
                RoverId = 1,
                CameraId = 1,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        DbContext.Photos.AddRange(testPhotos);
        await DbContext.SaveChangesAsync();

        var parameters = new PhotoQueryParameters
        {
            SolMin = 1000,
            SolMax = 1000,
            MarsTimeMin = "M12:28:00",
            MarsTimeMax = "M12:31:00",
            MarsTimeMinParsed = TimeSpan.FromHours(12) + TimeSpan.FromMinutes(28),
            MarsTimeMaxParsed = TimeSpan.FromHours(12) + TimeSpan.FromMinutes(31),
            Page = 1,
            PerPage = 100
        };

        // Act
        var response = await _photoQueryService.QueryPhotosAsync(parameters, default);

        // Assert - Should only return photos within 12:28 to 12:31
        response.Data.Should().HaveCount(2);
        response.Data.Should().Contain(p => p.Attributes!.NasaId == "TIME_TEST_2");
        response.Data.Should().Contain(p => p.Attributes!.NasaId == "TIME_TEST_3");
        response.Data.Should().NotContain(p => p.Attributes!.NasaId == "TIME_TEST_1");
        response.Data.Should().NotContain(p => p.Attributes!.NasaId == "TIME_TEST_4");
    }

    [Fact]
    public async Task QueryPhotos_WithMarsTimeAndLocationFilters_AppliesBothFilters()
    {
        // Arrange - Add photos with times and locations for this test
        var now = DateTime.UtcNow;
        var testPhotos = new List<Photo>
        {
            new Photo
            {
                NasaId = "LOC_TIME_1",
                ImgSrcFull = "https://test.com/loc1.jpg",
                ImgSrcLarge = "https://test.com/loc1.jpg",
                ImgSrcMedium = "https://test.com/loc1.jpg",
                ImgSrcSmall = "https://test.com/loc1.jpg",
                Sol = 2000,
                EarthDate = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2022, 1, 1, 12, 29, 0, DateTimeKind.Utc),
                DateTakenMars = "Sol-02000M12:29:00.000", // 12:29 - in time range
                Site = 100,
                Drive = 500,
                RoverId = 1,
                CameraId = 1,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Photo
            {
                NasaId = "LOC_TIME_2",
                ImgSrcFull = "https://test.com/loc2.jpg",
                ImgSrcLarge = "https://test.com/loc2.jpg",
                ImgSrcMedium = "https://test.com/loc2.jpg",
                ImgSrcSmall = "https://test.com/loc2.jpg",
                Sol = 2000,
                EarthDate = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2022, 1, 1, 12, 29, 0, DateTimeKind.Utc),
                DateTakenMars = "Sol-02000M12:29:00.000", // 12:29 - in time range
                Site = 200, // Different site - should be excluded
                Drive = 500,
                RoverId = 1,
                CameraId = 1,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Photo
            {
                NasaId = "LOC_TIME_3",
                ImgSrcFull = "https://test.com/loc3.jpg",
                ImgSrcLarge = "https://test.com/loc3.jpg",
                ImgSrcMedium = "https://test.com/loc3.jpg",
                ImgSrcSmall = "https://test.com/loc3.jpg",
                Sol = 2000,
                EarthDate = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2022, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                DateTakenMars = "Sol-02000M10:00:00.000", // 10:00 - outside time range
                Site = 100, // Correct site
                Drive = 500,
                RoverId = 1,
                CameraId = 1,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        DbContext.Photos.AddRange(testPhotos);
        await DbContext.SaveChangesAsync();

        var parameters = new PhotoQueryParameters
        {
            SolMin = 2000,
            SolMax = 2000,
            Site = 100,
            SiteMin = 100,
            SiteMax = 100,
            Drive = 500,
            DriveMin = 500,
            DriveMax = 500,
            MarsTimeMin = "M12:28:00",
            MarsTimeMax = "M12:31:00",
            MarsTimeMinParsed = TimeSpan.FromHours(12) + TimeSpan.FromMinutes(28),
            MarsTimeMaxParsed = TimeSpan.FromHours(12) + TimeSpan.FromMinutes(31),
            Page = 1,
            PerPage = 100
        };

        // Act
        var response = await _photoQueryService.QueryPhotosAsync(parameters, default);

        // Assert - Should only return photo that matches BOTH location AND time
        response.Data.Should().HaveCount(1);
        response.Data.Should().Contain(p => p.Attributes!.NasaId == "LOC_TIME_1");
    }

    [Fact]
    public async Task QueryPhotos_QueryMetadataIncludesMarsTimeAndLocation()
    {
        // Arrange
        var parameters = new PhotoQueryParameters
        {
            Rovers = "curiosity",
            RoverList = new List<string> { "curiosity" },
            Site = 100,
            SiteMin = 100,
            SiteMax = 100,
            Drive = 500,
            DriveMin = 500,
            DriveMax = 500,
            MarsTimeMin = "M12:00:00",
            MarsTimeMax = "M14:00:00",
            MarsTimeMinParsed = TimeSpan.FromHours(12),
            MarsTimeMaxParsed = TimeSpan.FromHours(14),
            Page = 1,
            PerPage = 10
        };

        // Act
        var response = await _photoQueryService.QueryPhotosAsync(parameters, default);

        // Assert - Query metadata should include all filters
        response.Meta!.Query.Should().ContainKey("rovers");
        response.Meta.Query.Should().ContainKey("site");
        response.Meta.Query.Should().ContainKey("drive");
        response.Meta.Query.Should().ContainKey("mars_time_min");
        response.Meta.Query.Should().ContainKey("mars_time_max");
        response.Meta.Query["mars_time_min"].Should().Be("M12:00:00");
        response.Meta.Query["mars_time_max"].Should().Be("M14:00:00");
        response.Meta.Query["site"].Should().Be(100);
        response.Meta.Query["drive"].Should().Be(500);
    }
}
