using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MarsVista.Api.Data;
using MarsVista.Api.Entities;
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

        services.AddDbContext<MarsVistaDbContext>((serviceProvider, options) =>
        {
            options.UseInMemoryDatabase($"MarsVistaTest_{Guid.NewGuid()}")
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCustomizer, InMemoryModelCustomizer>();
        });

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
                NasaId = $"NLA_{100000 + i}",
                ImgSrcFull = $"https://mars.nasa.gov/msl/photo{i}.jpg",
                ImgSrcLarge = $"https://mars.nasa.gov/msl/photo{i}.jpg",
                ImgSrcMedium = $"https://mars.nasa.gov/msl/photo{i}.jpg",
                ImgSrcSmall = $"https://mars.nasa.gov/msl/photo{i}.jpg",
                Sol = 100 + (i / 10),
                EarthDate = new DateTime(2013, 1, 1).AddDays(i),
                DateTakenUtc = new DateTime(2013, 1, 1).AddDays(i),
                DateTakenMars = $"Sol-{100 + (i / 10)}M12:00:00.000",
                RoverId = 1,
                CameraId = i % 2 == 0 ? 1 : 2 // Alternate between FHAZ and MAST
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
                EarthDate = new DateTime(2021, 3, 1).AddDays(i),
                DateTakenUtc = new DateTime(2021, 3, 1).AddDays(i),
                DateTakenMars = $"Sol-{500 + (i / 5)}M12:00:00.000",
                RoverId = 2,
                CameraId = 3 // NAVCAM
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
            SolMin = 105,
            SolMax = 108,
            Page = 1,
            PerPage = 100
        };

        // Act
        var response = await _photoQueryService.QueryPhotosAsync(parameters, default);

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
            Cameras = "FHAZ",
            Page = 1,
            PerPage = 100
        };

        // Act
        var response = await _photoQueryService.QueryPhotosAsync(parameters, default);

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
            Page = 1,
            PerPage = 100
        };

        // Act
        var response = await _photoQueryService.QueryPhotosAsync(parameters, default);

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
            Page = 1,
            PerPage = 10
        };

        var parametersPage2 = new PhotoQueryParameters
        {
            Rovers = "curiosity",
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
            Cameras = "MAST",
            SolMin = 100,
            SolMax = 105,
            Page = 1,
            PerPage = 100
        };

        // Act
        var response = await _photoQueryService.QueryPhotosAsync(parameters, default);

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
            Include = "rover,camera",
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
        var photo = await _dbContext.Photos.FirstAsync();
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
        var photoIds = await _dbContext.Photos
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
            Rovers = "curiosity"
        };

        // Act
        var result = await _photoQueryService.GetStatisticsAsync(parameters, "camera", default);

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

/// <summary>
/// Custom model customizer that ignores JsonDocument properties for in-memory database
/// </summary>
public class InMemoryModelCustomizer : Microsoft.EntityFrameworkCore.Infrastructure.ModelCustomizer
{
    public InMemoryModelCustomizer(Microsoft.EntityFrameworkCore.Infrastructure.ModelCustomizerDependencies dependencies)
        : base(dependencies)
    {
    }

    public override void Customize(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder, Microsoft.EntityFrameworkCore.DbContext context)
    {
        base.Customize(modelBuilder, context);

        // Ignore RawData property for in-memory database since JsonDocument is not supported
        modelBuilder.Entity<Photo>().Ignore(e => e.RawData);
    }
}
