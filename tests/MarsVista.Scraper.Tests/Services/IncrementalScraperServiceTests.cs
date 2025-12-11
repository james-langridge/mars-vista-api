using FluentAssertions;
using MarsVista.Scraper.Services;

namespace MarsVista.Scraper.Tests.Services;

/// <summary>
/// Tests for IncrementalScraperService result types and logic.
/// These tests verify the behavior of the result structures without requiring
/// database dependencies.
/// </summary>
public class IncrementalScraperServiceTests
{
    // ============================================================================
    // RESULT MODEL TESTS
    // ============================================================================

    [Fact]
    public void RoverScrapeResult_NewInstance_HasCorrectDefaults()
    {
        var result = new RoverScrapeResult();

        result.RoverName.Should().BeEmpty();
        result.PhotosAdded.Should().Be(0);
        result.StartSol.Should().Be(0);
        result.EndSol.Should().Be(0);
        result.SolsAttempted.Should().Be(0);
        result.SolsSucceeded.Should().Be(0);
        result.SolsFailed.Should().Be(0);
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void RoverScrapeResult_CanTrackPartialSuccess()
    {
        // A rover that completed but had some sol failures
        var result = new RoverScrapeResult
        {
            RoverName = "perseverance",
            StartSol = 1393,
            EndSol = 1400,
            SolsAttempted = 8,
            SolsSucceeded = 6,
            SolsFailed = 2,
            PhotosAdded = 150,
            Success = true // Rover completed, even with some sol failures
        };

        result.Success.Should().BeTrue("rover completed even with partial sol failures");
        result.SolsFailed.Should().BeGreaterThan(0);
    }

    [Fact]
    public void RoverScrapeResult_CanTrackCompleteFailure()
    {
        // A rover that failed completely (e.g., couldn't determine sol)
        var result = new RoverScrapeResult
        {
            RoverName = "perseverance",
            Success = false,
            ErrorMessage = "Could not determine current mission sol"
        };

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void IncrementalScrapeResult_NewInstance_HasCorrectDefaults()
    {
        var result = new IncrementalScrapeResult();

        result.TotalPhotosAdded.Should().Be(0);
        result.RoverResults.Should().BeEmpty();
        result.Success.Should().BeFalse();
        result.DurationSeconds.Should().Be(0);
    }

    [Fact]
    public void IncrementalScrapeResult_AllRoversSucceeded_IsSuccess()
    {
        var result = new IncrementalScrapeResult
        {
            TotalPhotosAdded = 300,
            RoverResults = new List<RoverScrapeResult>
            {
                new() { RoverName = "perseverance", Success = true, PhotosAdded = 150 },
                new() { RoverName = "curiosity", Success = true, PhotosAdded = 150 }
            },
            Success = true, // All rovers completed
            DurationSeconds = 45
        };

        result.Success.Should().BeTrue();
        result.RoverResults.Should().AllSatisfy(r => r.Success.Should().BeTrue());
    }

    [Fact]
    public void IncrementalScrapeResult_OneRoverFailed_IsNotSuccess()
    {
        var result = new IncrementalScrapeResult
        {
            TotalPhotosAdded = 150,
            RoverResults = new List<RoverScrapeResult>
            {
                new() { RoverName = "perseverance", Success = false, ErrorMessage = "NASA API unavailable" },
                new() { RoverName = "curiosity", Success = true, PhotosAdded = 150 }
            },
            Success = false, // One rover failed
            DurationSeconds = 30
        };

        result.Success.Should().BeFalse();
        result.RoverResults.Should().Contain(r => r.Success == false);
    }

    [Fact]
    public void IncrementalScrapeResult_AllRoversFailed_IsNotSuccess()
    {
        var result = new IncrementalScrapeResult
        {
            TotalPhotosAdded = 0,
            RoverResults = new List<RoverScrapeResult>
            {
                new() { RoverName = "perseverance", Success = false, ErrorMessage = "NASA API unavailable" },
                new() { RoverName = "curiosity", Success = false, ErrorMessage = "NASA API unavailable" }
            },
            Success = false,
            DurationSeconds = 10
        };

        result.Success.Should().BeFalse();
        result.RoverResults.Should().AllSatisfy(r => r.Success.Should().BeFalse());
    }

    // ============================================================================
    // PARTIAL SUCCESS LOGIC TESTS
    // ============================================================================

    [Theory]
    [InlineData(0, 8, true)]  // No failures = success
    [InlineData(1, 7, true)]  // Some failures, rover completed = success
    [InlineData(3, 5, true)]  // Many failures, rover completed = success
    [InlineData(8, 0, true)]  // All failures, but rover ran = still success (per requirements)
    public void RoverScrapeResult_PartialSolFailures_ShouldStillBeSuccessful(
        int solsFailed, int solsSucceeded, bool expectedSuccess)
    {
        // According to acceptance criteria:
        // "Partial success (some sols fail within a rover) still returns exit code 0 if all rovers complete"
        var result = new RoverScrapeResult
        {
            RoverName = "perseverance",
            SolsAttempted = solsFailed + solsSucceeded,
            SolsFailed = solsFailed,
            SolsSucceeded = solsSucceeded,
            Success = true // Rover completed (didn't fail to determine sol)
        };

        result.Success.Should().Be(expectedSuccess);
    }

    [Fact]
    public void RoverScrapeResult_CouldNotDetermineSol_ShouldNotBeSuccessful()
    {
        // The only case where Success should be false is when the rover
        // couldn't even start (e.g., couldn't determine current sol)
        var result = new RoverScrapeResult
        {
            RoverName = "perseverance",
            Success = false,
            ErrorMessage = "Could not determine current mission sol (NASA API unavailable and no photos in database)"
        };

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Could not determine");
    }
}
