using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MarsVista.Api.Controllers.V2;
using MarsVista.Api.DTOs.V2;
using MarsVista.Api.Models.V2;
using MarsVista.Api.Services.V2;

namespace MarsVista.Api.Tests.Controllers.V2;

public class PhotosControllerTests
{
    private readonly Mock<IPhotoQueryServiceV2> _mockPhotoQueryService;
    private readonly Mock<ICachingServiceV2> _mockCachingService;
    private readonly Mock<ILogger<PhotosController>> _mockLogger;
    private readonly PhotosController _controller;

    public PhotosControllerTests()
    {
        _mockPhotoQueryService = new Mock<IPhotoQueryServiceV2>();
        _mockCachingService = new Mock<ICachingServiceV2>();
        _mockLogger = new Mock<ILogger<PhotosController>>();
        _controller = new PhotosController(
            _mockPhotoQueryService.Object,
            _mockCachingService.Object,
            _mockLogger.Object
        );

        // Set up HTTP context for URL generation
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.HttpContext.Request.Scheme = "https";
        _controller.HttpContext.Request.Host = new HostString("api.marsvista.io");
        _controller.HttpContext.Request.Path = "/api/v2/photos";
    }

    [Fact]
    public async Task QueryPhotos_WithValidParameters_ReturnsOkResult()
    {
        // Arrange
        var parameters = new PhotoQueryParameters
        {
            Rovers = "curiosity",
            PageNumber = 1,
            PageSize = 25
        };

        var expectedPhotos = new List<PhotoResource>
        {
            new PhotoResource
            {
                Id = 123456,
                Attributes = new PhotoAttributes
                {
                    ImgSrc = "https://mars.nasa.gov/msl/123456.jpg",
                    Sol = 1000,
                    EarthDate = "2015-05-30"
                }
            }
        };

        var expectedResponse = new ApiResponse<List<PhotoResource>>(expectedPhotos)
        {
            Pagination = new PaginationInfo
            {
                Page = 1,
                PerPage = 25,
                TotalPages = 1,
                TotalCount = 1
            },
            Meta = new ResponseMeta
            {
                TotalCount = 1,
                ReturnedCount = 1
            }
        };

        _mockPhotoQueryService
            .Setup(s => s.QueryPhotosAsync(It.IsAny<PhotoQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        _mockCachingService
            .Setup(s => s.GenerateETag(It.IsAny<object>()))
            .Returns("etag-12345");

        _mockCachingService
            .Setup(s => s.CheckETag(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        _mockCachingService
            .Setup(s => s.GetCacheControlHeader(It.IsAny<bool>()))
            .Returns("public, max-age=3600");

        // Act
        var result = await _controller.QueryPhotos(parameters, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeOfType<ApiResponse<List<PhotoResource>>>();

        var response = okResult.Value as ApiResponse<List<PhotoResource>>;
        response!.Data.Should().HaveCount(1);
        response.Links.Should().NotBeNull();
        response.Links!.Self.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task QueryPhotos_WithInvalidRover_ReturnsBadRequest()
    {
        // Arrange
        var parameters = new PhotoQueryParameters
        {
            Rovers = "invalid_rover",
            PageNumber = 1,
            PageSize = 25
        };

        // Act
        var result = await _controller.QueryPhotos(parameters, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeOfType<ApiError>();

        var error = badRequestResult.Value as ApiError;
        error!.Status.Should().Be(400);
        error.Type.Should().Be("/errors/validation-error");
    }

    [Fact]
    public async Task QueryPhotos_WithMatchingETag_ReturnsNotModified()
    {
        // Arrange
        var parameters = new PhotoQueryParameters
        {
            Rovers = "curiosity",
            PageNumber = 1,
            PageSize = 25
        };

        var expectedResponse = new ApiResponse<List<PhotoResource>>(new List<PhotoResource>())
        {
            Pagination = new PaginationInfo { Page = 1, PerPage = 25 }
        };

        _mockPhotoQueryService
            .Setup(s => s.QueryPhotosAsync(It.IsAny<PhotoQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        _mockCachingService
            .Setup(s => s.GenerateETag(It.IsAny<object>()))
            .Returns("etag-12345");

        _mockCachingService
            .Setup(s => s.CheckETag("\"etag-12345\"", "etag-12345"))
            .Returns(true);

        _controller.HttpContext.Request.Headers["If-None-Match"] = "\"etag-12345\"";

        // Act
        var result = await _controller.QueryPhotos(parameters, CancellationToken.None);

        // Assert
        result.Should().BeOfType<StatusCodeResult>();
        var statusCodeResult = result as StatusCodeResult;
        statusCodeResult!.StatusCode.Should().Be(304);
    }

    [Fact]
    public async Task QueryPhotos_WithMultipleRovers_FiltersCorrectly()
    {
        // Arrange
        var parameters = new PhotoQueryParameters
        {
            Rovers = "curiosity,perseverance",
            PageNumber = 1,
            PageSize = 25
        };

        var expectedPhotos = new List<PhotoResource>
        {
            new PhotoResource
            {
                Id = 1,
                Relationships = new PhotoRelationships
                {
                    Rover = new RoverRelationship { Id = "curiosity" }
                }
            },
            new PhotoResource
            {
                Id = 2,
                Relationships = new PhotoRelationships
                {
                    Rover = new RoverRelationship { Id = "perseverance" }
                }
            }
        };

        var expectedResponse = new ApiResponse<List<PhotoResource>>(expectedPhotos)
        {
            Pagination = new PaginationInfo { Page = 1, PerPage = 25, TotalCount = 2 }
        };

        _mockPhotoQueryService
            .Setup(s => s.QueryPhotosAsync(It.IsAny<PhotoQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        _mockCachingService
            .Setup(s => s.GenerateETag(It.IsAny<object>()))
            .Returns("etag-12345");

        _mockCachingService
            .Setup(s => s.CheckETag(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        _mockCachingService
            .Setup(s => s.GetCacheControlHeader(It.IsAny<bool>()))
            .Returns("public, max-age=3600");

        // Act
        var result = await _controller.QueryPhotos(parameters, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<List<PhotoResource>>;
        response!.Data.Should().HaveCount(2);
        response.Data[0].Relationships!.Rover!.Id.Should().Be("curiosity");
        response.Data[1].Relationships!.Rover!.Id.Should().Be("perseverance");
    }

    [Fact]
    public async Task GetPhoto_WithValidId_ReturnsPhoto()
    {
        // Arrange
        var photoId = 123456;
        var expectedPhoto = new PhotoResource
        {
            Id = photoId,
            Attributes = new PhotoAttributes
            {
                ImgSrc = "https://mars.nasa.gov/msl/123456.jpg",
                Sol = 1000,
                EarthDate = "2015-05-30"
            }
        };

        _mockPhotoQueryService
            .Setup(s => s.GetPhotoByIdAsync(photoId, It.IsAny<PhotoQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPhoto);

        _mockCachingService
            .Setup(s => s.GenerateETag(It.IsAny<object>()))
            .Returns("etag-12345");

        _mockCachingService
            .Setup(s => s.CheckETag(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        _mockCachingService
            .Setup(s => s.GetCacheControlHeader(It.IsAny<bool>()))
            .Returns("public, max-age=3600");

        // Act
        var result = await _controller.GetPhoto(photoId, new PhotoQueryParameters(), CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<PhotoResource>;
        response!.Data.Id.Should().Be(photoId);
        response.Links!.Self.Should().Contain($"/api/v2/photos/{photoId}");
    }

    [Fact]
    public async Task GetPhoto_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var photoId = 999999;

        _mockPhotoQueryService
            .Setup(s => s.GetPhotoByIdAsync(photoId, It.IsAny<PhotoQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PhotoResource?)null);

        // Act
        var result = await _controller.GetPhoto(photoId, new PhotoQueryParameters(), CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var error = notFoundResult!.Value as ApiError;
        error!.Status.Should().Be(404);
        error.Detail.Should().Contain(photoId.ToString());
    }

    [Fact]
    public async Task GetStatistics_WithValidParameters_ReturnsStats()
    {
        // Arrange
        var parameters = new PhotoQueryParameters { Rovers = "curiosity" };
        var groupBy = "camera";

        var expectedStats = new PhotoStatisticsResponse
        {
            TotalPhotos = 1000,
            ByCamera = new List<CameraStatistics>
            {
                new CameraStatistics
                {
                    Camera = "MAST",
                    Count = 500,
                    Percentage = 50.0,
                    AvgPerSol = 10.5
                }
            }
        };

        _mockPhotoQueryService
            .Setup(s => s.GetStatisticsAsync(It.IsAny<PhotoQueryParameters>(), groupBy, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStats);

        _mockCachingService
            .Setup(s => s.GenerateETag(It.IsAny<object>()))
            .Returns("etag-12345");

        _mockCachingService
            .Setup(s => s.CheckETag(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        _mockCachingService
            .Setup(s => s.GetCacheControlHeader(It.IsAny<bool>()))
            .Returns("public, max-age=3600");

        // Act
        var result = await _controller.GetStatistics(parameters, groupBy, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<PhotoStatisticsResponse>;
        response!.Data.TotalPhotos.Should().Be(1000);
        response.Data.ByCamera.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetStatistics_WithInvalidGroupBy_ReturnsBadRequest()
    {
        // Arrange
        var parameters = new PhotoQueryParameters { Rovers = "curiosity" };
        var groupBy = "invalid_field";

        // Act
        var result = await _controller.GetStatistics(parameters, groupBy, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var error = badRequestResult!.Value as ApiError;
        error!.Status.Should().Be(400);
        error.Errors.Should().ContainSingle(e => e.Field == "group_by");
    }

    [Fact]
    public async Task BatchGetPhotos_WithValidIds_ReturnsPhotos()
    {
        // Arrange
        var request = new BatchPhotoRequest
        {
            Ids = new List<int> { 1, 2, 3 }
        };

        var expectedPhotos = new List<PhotoResource>
        {
            new PhotoResource { Id = 1 },
            new PhotoResource { Id = 2 },
            new PhotoResource { Id = 3 }
        };

        _mockPhotoQueryService
            .Setup(s => s.GetPhotosByIdsAsync(request.Ids, It.IsAny<PhotoQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPhotos);

        _mockCachingService
            .Setup(s => s.GenerateETag(It.IsAny<object>()))
            .Returns("etag-12345");

        _mockCachingService
            .Setup(s => s.CheckETag(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        // Act
        var result = await _controller.BatchGetPhotos(request, new PhotoQueryParameters(), CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<List<PhotoResource>>;
        response!.Data.Should().HaveCount(3);
        response.Meta!.Query!["ids_requested"].Should().Be(3);
        response.Meta.Query["ids_found"].Should().Be(3);
    }

    [Fact]
    public async Task BatchGetPhotos_WithEmptyIds_ReturnsBadRequest()
    {
        // Arrange
        var request = new BatchPhotoRequest
        {
            Ids = new List<int>()
        };

        // Act
        var result = await _controller.BatchGetPhotos(request, new PhotoQueryParameters(), CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var error = badRequestResult!.Value as ApiError;
        error!.Errors.Should().ContainSingle(e => e.Field == "ids");
    }

    [Fact]
    public async Task BatchGetPhotos_WithTooManyIds_ReturnsBadRequest()
    {
        // Arrange
        var request = new BatchPhotoRequest
        {
            Ids = Enumerable.Range(1, 101).ToList() // 101 IDs exceeds the 100 limit
        };

        // Act
        var result = await _controller.BatchGetPhotos(request, new PhotoQueryParameters(), CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var error = badRequestResult!.Value as ApiError;
        error!.Detail.Should().Contain("Too many photo IDs");
    }

    [Fact]
    public async Task QueryPhotos_WithSolRange_FiltersCorrectly()
    {
        // Arrange
        var parameters = new PhotoQueryParameters
        {
            Rovers = "curiosity",
            SolMin = 100,
            SolMax = 200,
            PageNumber = 1,
            PageSize = 25
        };

        var expectedResponse = new ApiResponse<List<PhotoResource>>(new List<PhotoResource>())
        {
            Pagination = new PaginationInfo { Page = 1, PerPage = 25 },
            Meta = new ResponseMeta
            {
                Query = new Dictionary<string, object>
                {
                    ["sol_min"] = 100,
                    ["sol_max"] = 200
                }
            }
        };

        _mockPhotoQueryService
            .Setup(s => s.QueryPhotosAsync(It.Is<PhotoQueryParameters>(p =>
                p.SolMin == 100 && p.SolMax == 200), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        _mockCachingService
            .Setup(s => s.GenerateETag(It.IsAny<object>()))
            .Returns("etag-12345");

        _mockCachingService
            .Setup(s => s.CheckETag(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        _mockCachingService
            .Setup(s => s.GetCacheControlHeader(It.IsAny<bool>()))
            .Returns("public, max-age=3600");

        // Act
        var result = await _controller.QueryPhotos(parameters, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockPhotoQueryService.Verify(s => s.QueryPhotosAsync(
            It.Is<PhotoQueryParameters>(p => p.SolMin == 100 && p.SolMax == 200),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
