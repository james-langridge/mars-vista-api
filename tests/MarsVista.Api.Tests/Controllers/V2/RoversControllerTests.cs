using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MarsVista.Api.Controllers.V2;
using MarsVista.Api.DTOs.V2;
using MarsVista.Api.Services.V2;

namespace MarsVista.Api.Tests.Controllers.V2;

public class RoversControllerTests
{
    private readonly Mock<IRoverQueryServiceV2> _mockRoverQueryService;
    private readonly Mock<ICachingServiceV2> _mockCachingService;
    private readonly Mock<ILogger<RoversController>> _mockLogger;
    private readonly RoversController _controller;

    public RoversControllerTests()
    {
        _mockRoverQueryService = new Mock<IRoverQueryServiceV2>();
        _mockCachingService = new Mock<ICachingServiceV2>();
        _mockLogger = new Mock<ILogger<RoversController>>();
        _controller = new RoversController(
            _mockRoverQueryService.Object,
            _mockCachingService.Object,
            _mockLogger.Object
        );

        // Set up HTTP context
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.HttpContext.Request.Scheme = "https";
        _controller.HttpContext.Request.Host = new HostString("api.marsvista.io");
    }

    [Fact]
    public async Task GetRovers_ReturnsAllRovers()
    {
        // Arrange
        var expectedRovers = new List<RoverResource>
        {
            new RoverResource
            {
                Id = "curiosity",
                Name = "Curiosity",
                LandingDate = "2012-08-06",
                LaunchDate = "2011-11-26",
                Status = "active"
            },
            new RoverResource
            {
                Id = "perseverance",
                Name = "Perseverance",
                LandingDate = "2021-02-18",
                LaunchDate = "2020-07-30",
                Status = "active"
            }
        };

        _mockRoverQueryService
            .Setup(s => s.GetAllRoversAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRovers);

        _mockCachingService
            .Setup(s => s.GenerateETag(It.IsAny<object>()))
            .Returns("etag-12345");

        _mockCachingService
            .Setup(s => s.CheckETag(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        // Act
        var result = await _controller.GetRovers(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<List<RoverResource>>;
        response!.Data.Should().HaveCount(2);
        response.Meta!.ReturnedCount.Should().Be(2);
        response.Links!.Self.Should().Contain("/api/v2/rovers");
    }

    [Fact]
    public async Task GetRovers_WithMatchingETag_ReturnsNotModified()
    {
        // Arrange
        var expectedRovers = new List<RoverResource>();

        _mockRoverQueryService
            .Setup(s => s.GetAllRoversAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRovers);

        _mockCachingService
            .Setup(s => s.GenerateETag(It.IsAny<object>()))
            .Returns("etag-12345");

        _mockCachingService
            .Setup(s => s.CheckETag("\"etag-12345\"", "etag-12345"))
            .Returns(true);

        _controller.HttpContext.Request.Headers["If-None-Match"] = "\"etag-12345\"";

        // Act
        var result = await _controller.GetRovers(CancellationToken.None);

        // Assert
        result.Should().BeOfType<StatusCodeResult>();
        var statusCodeResult = result as StatusCodeResult;
        statusCodeResult!.StatusCode.Should().Be(304);
    }

    [Fact]
    public async Task GetRover_WithValidSlug_ReturnsRover()
    {
        // Arrange
        var slug = "curiosity";
        var expectedRover = new RoverResource
        {
            Id = slug,
            Name = "Curiosity",
            LandingDate = "2012-08-06",
            LaunchDate = "2011-11-26",
            Status = "active",
            MaxSol = 4102,
            MaxDate = "2024-11-20",
            TotalPhotos = 710000
        };

        _mockRoverQueryService
            .Setup(s => s.GetRoverBySlugAsync(slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRover);

        // Act
        var result = await _controller.GetRover(slug, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<RoverResource>;
        response!.Data.Id.Should().Be(slug);
        response.Data.Name.Should().Be("Curiosity");
        response.Links!.Self.Should().Contain($"/api/v2/rovers/{slug}");
    }

    [Fact]
    public async Task GetRover_WithInvalidSlug_ReturnsNotFound()
    {
        // Arrange
        var slug = "invalid_rover";

        _mockRoverQueryService
            .Setup(s => s.GetRoverBySlugAsync(slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoverResource?)null);

        // Act
        var result = await _controller.GetRover(slug, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var error = notFoundResult!.Value as ApiError;
        error!.Status.Should().Be(404);
        error.Detail.Should().Contain(slug);
        error.Errors.Should().ContainSingle(e => e.Field == "slug");
    }

    [Fact]
    public async Task GetManifest_WithValidSlug_ReturnsManifest()
    {
        // Arrange
        var slug = "curiosity";
        var expectedManifest = new RoverManifest
        {
            Name = "Curiosity",
            LandingDate = "2012-08-06",
            LaunchDate = "2011-11-26",
            Status = "active",
            MaxSol = 4102,
            MaxDate = "2024-11-20",
            TotalPhotos = 710000,
            Photos = new List<ManifestPhoto>
            {
                new ManifestPhoto
                {
                    Sol = 0,
                    TotalPhotos = 100,
                    Cameras = new List<string> { "FHAZ", "RHAZ", "NAVCAM" }
                }
            }
        };

        _mockRoverQueryService
            .Setup(s => s.GetRoverManifestAsync(slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedManifest);

        // Act
        var result = await _controller.GetManifest(slug, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<RoverManifest>;
        response!.Data.Name.Should().Be("Curiosity");
        response.Data.Photos.Should().NotBeEmpty();
        response.Links!.Self.Should().Contain($"/api/v2/rovers/{slug}/manifest");
    }

    [Fact]
    public async Task GetManifest_WithInvalidSlug_ReturnsNotFound()
    {
        // Arrange
        var slug = "invalid_rover";

        _mockRoverQueryService
            .Setup(s => s.GetRoverManifestAsync(slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoverManifest?)null);

        // Act
        var result = await _controller.GetManifest(slug, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var error = notFoundResult!.Value as ApiError;
        error!.Status.Should().Be(404);
        error.Detail.Should().Contain(slug);
    }

    [Fact]
    public async Task GetCameras_WithValidSlug_ReturnsCameras()
    {
        // Arrange
        var slug = "curiosity";
        var expectedCameras = new List<CameraResource>
        {
            new CameraResource
            {
                Id = "fhaz",
                Name = "FHAZ",
                FullName = "Front Hazard Avoidance Camera",
                RoverId = slug
            },
            new CameraResource
            {
                Id = "mast",
                Name = "MAST",
                FullName = "Mast Camera",
                RoverId = slug
            }
        };

        _mockRoverQueryService
            .Setup(s => s.GetRoverCamerasAsync(slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCameras);

        // Act
        var result = await _controller.GetCameras(slug, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<List<CameraResource>>;
        response!.Data.Should().HaveCount(2);
        response.Meta!.ReturnedCount.Should().Be(2);
        response.Links!.Self.Should().Contain($"/api/v2/rovers/{slug}/cameras");
    }

    [Fact]
    public async Task GetCameras_WithInvalidSlug_ReturnsNotFound()
    {
        // Arrange
        var slug = "invalid_rover";

        _mockRoverQueryService
            .Setup(s => s.GetRoverCamerasAsync(slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CameraResource>());

        _mockRoverQueryService
            .Setup(s => s.GetRoverBySlugAsync(slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoverResource?)null);

        // Act
        var result = await _controller.GetCameras(slug, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var error = notFoundResult!.Value as ApiError;
        error!.Status.Should().Be(404);
        error.Detail.Should().Contain(slug);
    }

    [Fact]
    public async Task GetRovers_SetsCacheControlHeader()
    {
        // Arrange
        var expectedRovers = new List<RoverResource>();

        _mockRoverQueryService
            .Setup(s => s.GetAllRoversAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRovers);

        _mockCachingService
            .Setup(s => s.GenerateETag(It.IsAny<object>()))
            .Returns("etag-12345");

        _mockCachingService
            .Setup(s => s.CheckETag(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        // Act
        var result = await _controller.GetRovers(CancellationToken.None);

        // Assert
        _controller.Response.Headers.Should().ContainKey("Cache-Control");
        _controller.Response.Headers["Cache-Control"].ToString().Should().Contain("public");
        _controller.Response.Headers["Cache-Control"].ToString().Should().Contain("max-age=86400");
    }

    [Theory]
    [InlineData("curiosity")]
    [InlineData("perseverance")]
    [InlineData("opportunity")]
    [InlineData("spirit")]
    public async Task GetRover_WithDifferentRoverSlugs_WorksCorrectly(string slug)
    {
        // Arrange
        var expectedRover = new RoverResource
        {
            Id = slug,
            Name = slug.First().ToString().ToUpper() + slug.Substring(1),
            Status = slug == "curiosity" || slug == "perseverance" ? "active" : "complete"
        };

        _mockRoverQueryService
            .Setup(s => s.GetRoverBySlugAsync(slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRover);

        // Act
        var result = await _controller.GetRover(slug, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<RoverResource>;
        response!.Data.Id.Should().Be(slug);
    }
}
