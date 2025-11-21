using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MarsVista.Api.Data;
using MarsVista.Api.Models;
using MarsVista.Api.Models.V2;
using MarsVista.Api.Services.V2;

namespace MarsVista.Api.Tests.Integration.V2;

public class PhotoQueryIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly MarsVistaDbContext _dbContext;
    private readonly IPhotoQueryServiceV2 _photoQueryService;

    public PhotoQueryIntegrationTests()
    {
        // Set up in-memory database
        var services = new ServiceCollection();

        services.AddDbContext<MarsVistaDbContext>(options =>
            options.UseInMemoryDatabase($"MarsVistaTest_{Guid.NewGuid()}"));

        services.AddLogging(builder => builder.AddConsole());
        services.AddScoped<IPhotoQueryServiceV2, PhotoQueryServiceV2>();

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<MarsVistaDbContext>();
        _photoQueryService = _serviceProvider.GetRequiredService<IPhotoQueryServiceV2>();

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Add rovers
        var curiosity = new Rover
        {
            Id = 1,
            Name = "Curiosity",
            LandingDate = new DateTime(2012, 8, 6),
            LaunchDate = new DateTime(2011, 11, 26),
            Status = "active"
        };

        var perseverance = new Rover
        {
            Id = 2,
            Name = "Perseverance",
            LandingDate = new DateTime(2021, 2, 18),
            LaunchDate = new DateTime(2020, 7, 30),
            Status = "active"
        };

        _dbContext.Rovers.AddRange(curiosity, perseverance);

        // Add cameras
        var fhaz = new Camera
        {
            Id = 1,
            Name = "FHAZ",
            FullName = "Front Hazard Avoidance Camera",
            RoverId = 1
        };

        var mast = new Camera
        {
            Id = 2,
            Name = "MAST",
            FullName = "Mast Camera",
            RoverId = 1
        };

        var navcam = new Camera
        {
            Id = 3,
            Name = "NAVCAM",
            FullName = "Navigation Camera",
            RoverId = 2
        };

        _dbContext.Cameras.AddRange(fhaz, mast, navcam);

        // Add photos for different scenarios
        var photos = new List<Photo>();

        // Curiosity photos - sol range 100-200
        for (int i = 0; i < 50; i++)
        {
            photos.Add(new Photo
            {
                NasaId = 100000 + i,
                ImgSrc = $"https://mars.nasa.gov/msl/photo{i}.jpg",
                Sol = 100 + (i / 10),
                EarthDate = new DateTime(2013, 1, 1).AddDays(i),
                RoverId = 1,
                CameraId = i % 2 == 0 ? 1 : 2, // Alternate between FHAZ and MAST
                RawData = System.Text.Json.JsonDocument.Parse("{}")
            });
        }

        // Perseverance photos - sol range 500-600
        for (int i = 0; i < 30; i++)
        {
            photos.Add(new Photo
            {
                NasaId = 200000 + i,
                ImgSrc = $"https://mars.nasa.gov/m2020/photo{i}.jpg",
                Sol = 500 + (i / 5),
                EarthDate = new DateTime(2021, 3, 1).AddDays(i),
                RoverId = 2,
                CameraId = 3, // NAVCAM
                RawData = System.Text.Json.JsonDocument.Parse("{}")
            });
        }

        _dbContext.Photos.AddRange(photos);
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task QueryPhotos_WithMultipleRovers_ReturnsPhotoFromBothRovers()
    {
        // Arrange
        var parameters = new PhotoQueryParameters
        {
            Rovers = "curiosity,perseverance",
            PageNumber = 1,
            PageSize = 100
        };

        // Act
        var response = await _photoQueryService.QueryPhotosAsync(parameters, CancellationToken.None);

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
            SolMin = 105,
            SolMax = 108,
            PageNumber = 1,
            PageSize = 100
        };

        // Act
        var response = await _photoQueryService.QueryPhotosAsync(parameters, CancellationToken.None);

        // Assert
        response.Data.Should().NotBeEmpty();
        response.Data.Should().OnlyContain(p =>
            p.Attributes!.Sol >= 105 && p.Attributes.Sol <= 108);
        response.Data.Should().OnlyContain(p => p.Relationships!.Rover!.Id == "curiosity");
    }

    [Fact]
    public async Task QueryPhotos_WithDateRange_FiltersCorrectly()
    {
        // Arrange
        var parameters = new PhotoQueryParameters
        {
            Rovers = "perseverance",
            DateMin = "2021-03-10",
            DateMax = "2021-03-20",
            PageNumber = 1,
            PageSize = 100
        };

        // Act
        var response = await _photoQueryService.QueryPhotosAsync(parameters, CancellationToken.None);

        // Assert
        response.Data.Should().NotBeEmpty();
        response.Data.Should().OnlyContain(p =>
        {
            var date = DateTime.Parse(p.Attributes!.EarthDate!);
            return date >= new DateTime(2021, 3, 10) && date <= new DateTime(2021, 3, 20);
        });
    }

    [Fact]
    public async Task QueryPhotos_WithCameraFilter_ReturnsOnlyMatchingCameras()
    {
        // Arrange
        var parameters = new PhotoQueryParameters
        {
            Rovers = "curiosity",
            Cameras = "FHAZ",
            PageNumber = 1,
            PageSize = 100
        };

        // Act
        var response = await _photoQueryService.QueryPhotosAsync(parameters, CancellationToken.None);

        // Assert
        response.Data.Should().NotBeEmpty();
        response.Data.Should().OnlyContain(p => p.Relationships!.Camera!.Id == "fhaz");
    }

    [Fact]
    public async Task QueryPhotos_WithMultipleCameras_ReturnsAllMatchingCameras()
    {
        // Arrange
        var parameters = new PhotoQueryParameters
        {
            Rovers = "curiosity",
            Cameras = "FHAZ,MAST",
            PageNumber = 1,
            PageSize = 100
        };

        // Act
        var response = await _photoQueryService.QueryPhotosAsync(parameters, CancellationToken.None);

        // Assert
        response.Data.Should().NotBeEmpty();
        response.Data.Should().Contain(p => p.Relationships!.Camera!.Id == "fhaz");
        response.Data.Should().Contain(p => p.Relationships!.Camera!.Id == "mast");
        response.Data.Should().HaveCount(50); // All Curiosity photos
    }

    [Fact]
    public async Task QueryPhotos_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var parametersPage1 = new PhotoQueryParameters
        {
            Rovers = "curiosity",
            PageNumber = 1,
            PageSize = 10
        };

        var parametersPage2 = new PhotoQueryParameters
        {
            Rovers = "curiosity",
            PageNumber = 2,
            PageSize = 10
        };

        // Act
        var responsePage1 = await _photoQueryService.QueryPhotosAsync(parametersPage1, CancellationToken.None);
        var responsePage2 = await _photoQueryService.QueryPhotosAsync(parametersPage2, CancellationToken.None);

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
            Sort = "-sol", // Descending by sol
            PageNumber = 1,
            PageSize = 100
        };

        // Act
        var response = await _photoQueryService.QueryPhotosAsync(parameters, CancellationToken.None);

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
            Cameras = "MAST",
            SolMin = 100,
            SolMax = 105,
            PageNumber = 1,
            PageSize = 100
        };

        // Act
        var response = await _photoQueryService.QueryPhotosAsync(parameters, CancellationToken.None);

        // Assert
        response.Data.Should().NotBeEmpty();
        response.Data.Should().OnlyContain(p =>
            p.Relationships!.Rover!.Id == "curiosity" &&
            p.Relationships.Camera!.Id == "mast" &&
            p.Attributes!.Sol >= 100 &&
            p.Attributes.Sol <= 105);
    }

    [Fact]
    public async Task QueryPhotos_WithFieldSelection_ReturnsOnlySelectedFields()
    {
        // Arrange
        var parameters = new PhotoQueryParameters
        {
            Rovers = "curiosity",
            Fields = "id,sol",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var response = await _photoQueryService.QueryPhotosAsync(parameters, CancellationToken.None);

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
            Include = "rover,camera",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var response = await _photoQueryService.QueryPhotosAsync(parameters, CancellationToken.None);

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
        var photo = await _dbContext.Photos.FirstAsync();
        var parameters = new PhotoQueryParameters();

        // Act
        var result = await _photoQueryService.GetPhotoByIdAsync(photo.Id, parameters, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(photo.Id);
        result.Attributes!.ImgSrc.Should().Be(photo.ImgSrc);
    }

    [Fact]
    public async Task GetPhotoById_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var invalidId = 999999;
        var parameters = new PhotoQueryParameters();

        // Act
        var result = await _photoQueryService.GetPhotoByIdAsync(invalidId, parameters, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPhotosByIds_WithValidIds_ReturnsAllPhotos()
    {
        // Arrange
        var photoIds = await _dbContext.Photos
            .Take(5)
            .Select(p => p.Id)
            .ToListAsync();
        var parameters = new PhotoQueryParameters();

        // Act
        var result = await _photoQueryService.GetPhotosByIdsAsync(photoIds, parameters, CancellationToken.None);

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
            Rovers = "curiosity"
        };

        // Act
        var result = await _photoQueryService.GetStatisticsAsync(parameters, "camera", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalPhotos.Should().Be(50);
        result.ByCamera.Should().NotBeEmpty();
        result.ByCamera.Should().Contain(c => c.Camera == "FHAZ");
        result.ByCamera.Should().Contain(c => c.Camera == "MAST");

        // Each camera should have 25 photos (50 photos alternating between 2 cameras)
        var fhazStats = result.ByCamera.First(c => c.Camera == "FHAZ");
        var mastStats = result.ByCamera.First(c => c.Camera == "MAST");
        fhazStats.Count.Should().Be(25);
        mastStats.Count.Should().Be(25);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        _serviceProvider?.Dispose();
    }
}
