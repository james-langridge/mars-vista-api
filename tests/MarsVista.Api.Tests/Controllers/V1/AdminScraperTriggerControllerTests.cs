using FluentAssertions;
using MarsVista.Api.Controllers.V1;
using MarsVista.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MarsVista.Api.Tests.Controllers.V1;

public class AdminScraperTriggerControllerTests
{
    private readonly Mock<IAdminScraperTriggerService> _triggerServiceMock;
    private readonly Mock<IScraperJobTracker> _jobTrackerMock;
    private readonly Mock<ILogger<AdminScraperTriggerController>> _loggerMock;
    private readonly AdminScraperTriggerController _sut;

    public AdminScraperTriggerControllerTests()
    {
        _triggerServiceMock = new Mock<IAdminScraperTriggerService>();
        _jobTrackerMock = new Mock<IScraperJobTracker>();
        _loggerMock = new Mock<ILogger<AdminScraperTriggerController>>();
        _sut = new AdminScraperTriggerController(
            _triggerServiceMock.Object,
            _jobTrackerMock.Object,
            _loggerMock.Object);
    }

    #region TriggerSol Tests

    [Fact]
    public async Task TriggerSol_ShouldReturnBadRequest_WhenRoverIsEmpty()
    {
        // Arrange
        var request = new SolTriggerRequest { Rover = "", Sol = 100 };

        // Act
        var result = await _sut.TriggerSol(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = (BadRequestObjectResult)result;
        badRequest.Value.Should().BeEquivalentTo(new { error = "Rover name is required" });
    }

    [Fact]
    public async Task TriggerSol_ShouldReturnBadRequest_WhenSolIsNegative()
    {
        // Arrange
        var request = new SolTriggerRequest { Rover = "curiosity", Sol = -1 };

        // Act
        var result = await _sut.TriggerSol(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = (BadRequestObjectResult)result;
        badRequest.Value.Should().BeEquivalentTo(new { error = "Sol must be >= 0" });
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("opportunity")]
    [InlineData("spirit")]
    [InlineData("mars2020")]
    public async Task TriggerSol_ShouldReturnBadRequest_WhenRoverIsInvalid(string rover)
    {
        // Arrange
        var request = new SolTriggerRequest { Rover = rover, Sol = 100 };

        // Act
        var result = await _sut.TriggerSol(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Theory]
    [InlineData("perseverance")]
    [InlineData("curiosity")]
    [InlineData("PERSEVERANCE")]
    [InlineData("Curiosity")]
    public async Task TriggerSol_ShouldAcceptValidRovers(string rover)
    {
        // Arrange
        var request = new SolTriggerRequest { Rover = rover, Sol = 100 };
        var expectedJob = new ScraperJob { Id = "test123", Status = "started", Type = "sol", Rover = rover.ToLower() };
        _triggerServiceMock.Setup(s => s.TriggerSolAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(expectedJob);

        // Act
        var result = await _sut.TriggerSol(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region TriggerRange Tests

    [Fact]
    public async Task TriggerRange_ShouldReturnBadRequest_WhenRoverIsEmpty()
    {
        // Arrange
        var request = new RangeTriggerRequest { Rover = "", StartSol = 1, EndSol = 10 };

        // Act
        var result = await _sut.TriggerRange(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task TriggerRange_ShouldReturnBadRequest_WhenStartSolIsNegative()
    {
        // Arrange
        var request = new RangeTriggerRequest { Rover = "curiosity", StartSol = -1, EndSol = 10 };

        // Act
        var result = await _sut.TriggerRange(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = (BadRequestObjectResult)result;
        badRequest.Value.Should().BeEquivalentTo(new { error = "StartSol must be >= 0" });
    }

    [Fact]
    public async Task TriggerRange_ShouldReturnBadRequest_WhenEndSolLessThanStartSol()
    {
        // Arrange
        var request = new RangeTriggerRequest { Rover = "curiosity", StartSol = 100, EndSol = 50 };

        // Act
        var result = await _sut.TriggerRange(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = (BadRequestObjectResult)result;
        badRequest.Value.Should().BeEquivalentTo(new { error = "EndSol must be >= StartSol" });
    }

    [Fact]
    public async Task TriggerRange_ShouldReturnBadRequest_WhenRangeTooLarge()
    {
        // Arrange
        var request = new RangeTriggerRequest { Rover = "curiosity", StartSol = 1, EndSol = 1500 };

        // Act
        var result = await _sut.TriggerRange(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = (BadRequestObjectResult)result;
        badRequest.Value.Should().BeEquivalentTo(new { error = "Sol range too large. Max 1000 sols per request." });
    }

    [Fact]
    public async Task TriggerRange_ShouldAcceptValidRange()
    {
        // Arrange
        var request = new RangeTriggerRequest { Rover = "curiosity", StartSol = 1, EndSol = 100 };
        var expectedJob = new ScraperJob { Id = "test123", Status = "started", Type = "range", Rover = "curiosity" };
        _triggerServiceMock.Setup(s => s.TriggerRangeAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(expectedJob);

        // Act
        var result = await _sut.TriggerRange(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task TriggerRange_ShouldUseDefaultDelay()
    {
        // Arrange
        var request = new RangeTriggerRequest { Rover = "curiosity", StartSol = 1, EndSol = 10 };
        var expectedJob = new ScraperJob { Id = "test123", Status = "started", Type = "range", Rover = "curiosity" };
        _triggerServiceMock.Setup(s => s.TriggerRangeAsync("curiosity", 1, 10, 500))
            .ReturnsAsync(expectedJob);

        // Act
        var result = await _sut.TriggerRange(request);

        // Assert
        _triggerServiceMock.Verify(s => s.TriggerRangeAsync("curiosity", 1, 10, 500), Times.Once);
    }

    [Fact]
    public async Task TriggerRange_ShouldUseCustomDelay()
    {
        // Arrange
        var request = new RangeTriggerRequest { Rover = "curiosity", StartSol = 1, EndSol = 10, DelayMs = 1000 };
        var expectedJob = new ScraperJob { Id = "test123", Status = "started", Type = "range", Rover = "curiosity" };
        _triggerServiceMock.Setup(s => s.TriggerRangeAsync("curiosity", 1, 10, 1000))
            .ReturnsAsync(expectedJob);

        // Act
        var result = await _sut.TriggerRange(request);

        // Assert
        _triggerServiceMock.Verify(s => s.TriggerRangeAsync("curiosity", 1, 10, 1000), Times.Once);
    }

    #endregion

    #region TriggerFull Tests

    [Fact]
    public async Task TriggerFull_ShouldReturnBadRequest_WhenRoverIsEmpty()
    {
        // Arrange
        var request = new FullTriggerRequest { Rover = "", Confirm = true };

        // Act
        var result = await _sut.TriggerFull(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task TriggerFull_ShouldReturnBadRequest_WhenNotConfirmed()
    {
        // Arrange
        var request = new FullTriggerRequest { Rover = "curiosity", Confirm = false };

        // Act
        var result = await _sut.TriggerFull(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task TriggerFull_ShouldAcceptValidConfirmedRequest()
    {
        // Arrange
        var request = new FullTriggerRequest { Rover = "curiosity", Confirm = true };
        var expectedJob = new ScraperJob
        {
            Id = "test123",
            Status = "started",
            Type = "full",
            Rover = "curiosity",
            StartSol = 1,
            EndSol = 4500,
            TotalSols = 4500
        };
        _triggerServiceMock.Setup(s => s.TriggerFullAsync("curiosity"))
            .ReturnsAsync(expectedJob);

        // Act
        var result = await _sut.TriggerFull(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region Job Status Tests

    [Fact]
    public void GetJobStatus_ShouldReturnNotFound_WhenJobNotExists()
    {
        // Arrange
        _jobTrackerMock.Setup(j => j.GetJob("unknown")).Returns((ScraperJob?)null);

        // Act
        var result = _sut.GetJobStatus("unknown");

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public void GetJobStatus_ShouldReturnJob_WhenExists()
    {
        // Arrange
        var job = new ScraperJob
        {
            Id = "test123",
            Type = "sol",
            Rover = "curiosity",
            Status = "in_progress",
            StartSol = 100,
            EndSol = 100,
            CurrentSol = 100,
            PhotosAdded = 50
        };
        _jobTrackerMock.Setup(j => j.GetJob("test123")).Returns(job);

        // Act
        var result = _sut.GetJobStatus("test123");

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region Cancel Job Tests

    [Fact]
    public void CancelJob_ShouldReturnNotFound_WhenJobNotExists()
    {
        // Arrange
        _jobTrackerMock.Setup(j => j.GetJob("unknown")).Returns((ScraperJob?)null);

        // Act
        var result = _sut.CancelJob("unknown");

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public void CancelJob_ShouldReturnBadRequest_WhenJobAlreadyCompleted()
    {
        // Arrange
        var job = new ScraperJob { Id = "test123", Status = "completed" };
        _jobTrackerMock.Setup(j => j.GetJob("test123")).Returns(job);

        // Act
        var result = _sut.CancelJob("test123");

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void CancelJob_ShouldReturnOk_WhenJobIsActive()
    {
        // Arrange
        var job = new ScraperJob { Id = "test123", Status = "in_progress" };
        _jobTrackerMock.Setup(j => j.GetJob("test123")).Returns(job);
        _jobTrackerMock.Setup(j => j.RequestCancel("test123")).Returns(true);

        // Act
        var result = _sut.CancelJob("test123");

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region Mission Sol Tests

    [Theory]
    [InlineData("invalid")]
    [InlineData("opportunity")]
    [InlineData("spirit")]
    public async Task GetMissionSol_ShouldReturnBadRequest_WhenRoverIsInvalid(string rover)
    {
        // Act
        var result = await _sut.GetMissionSol(rover);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetMissionSol_ShouldReturnNotFound_WhenSolNotDetermined()
    {
        // Arrange
        _triggerServiceMock.Setup(s => s.GetCurrentMissionSolAsync("curiosity"))
            .ReturnsAsync((int?)null);

        // Act
        var result = await _sut.GetMissionSol("curiosity");

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetMissionSol_ShouldReturnOk_WhenSolDetermined()
    {
        // Arrange
        _triggerServiceMock.Setup(s => s.GetCurrentMissionSolAsync("perseverance"))
            .ReturnsAsync(1400);

        // Act
        var result = await _sut.GetMissionSol("perseverance");

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    #endregion
}
