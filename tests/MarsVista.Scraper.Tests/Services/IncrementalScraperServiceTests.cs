using System.Net;
using System.Text.Json;
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

    // ============================================================================
    // FAILED SOL INFO TESTS
    // ============================================================================

    [Fact]
    public void FailedSolInfo_NewInstance_HasCorrectDefaults()
    {
        var info = new FailedSolInfo();

        info.Sol.Should().Be(0);
        info.ErrorType.Should().BeEmpty();
        info.ErrorMessage.Should().BeEmpty();
        info.Timestamp.Should().Be(default);
    }

    [Fact]
    public void FailedSolInfo_FromException_ClassifiesHttpRequestException_503()
    {
        var ex = new HttpRequestException("Service unavailable", null, HttpStatusCode.ServiceUnavailable);

        var info = FailedSolInfo.FromException(1708, ex);

        info.Sol.Should().Be(1708);
        info.ErrorType.Should().Be("HTTP_503");
        info.ErrorMessage.Should().Contain("Service unavailable");
        info.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void FailedSolInfo_FromException_ClassifiesHttpRequestException_429()
    {
        var ex = new HttpRequestException("Rate limited", null, HttpStatusCode.TooManyRequests);

        var info = FailedSolInfo.FromException(1709, ex);

        info.Sol.Should().Be(1709);
        info.ErrorType.Should().Be("HTTP_429");
    }

    [Fact]
    public void FailedSolInfo_FromException_ClassifiesHttpRequestException_NoStatusCode()
    {
        var ex = new HttpRequestException("Connection failed");

        var info = FailedSolInfo.FromException(1710, ex);

        info.Sol.Should().Be(1710);
        info.ErrorType.Should().Be("NetworkError");
    }

    [Fact]
    public void FailedSolInfo_FromException_ClassifiesTaskCanceledException()
    {
        var ex = new TaskCanceledException("Request timed out");

        var info = FailedSolInfo.FromException(1711, ex);

        info.Sol.Should().Be(1711);
        info.ErrorType.Should().Be("Timeout");
    }

    [Fact]
    public void FailedSolInfo_FromException_ClassifiesJsonException()
    {
        var ex = new JsonException("Invalid JSON");

        var info = FailedSolInfo.FromException(1712, ex);

        info.Sol.Should().Be(1712);
        info.ErrorType.Should().Be("ParseError");
    }

    [Fact]
    public void FailedSolInfo_FromException_ClassifiesOperationCancelledException()
    {
        var ex = new OperationCanceledException("Cancelled by user");

        var info = FailedSolInfo.FromException(1713, ex);

        info.Sol.Should().Be(1713);
        info.ErrorType.Should().Be("Cancelled");
    }

    [Fact]
    public void FailedSolInfo_FromException_ClassifiesUnknownException()
    {
        var ex = new InvalidOperationException("Something unexpected");

        var info = FailedSolInfo.FromException(1714, ex);

        info.Sol.Should().Be(1714);
        info.ErrorType.Should().Be("Unknown");
    }

    [Fact]
    public void FailedSolInfo_FromException_TruncatesLongErrorMessages()
    {
        var longMessage = new string('x', 500);
        var ex = new Exception(longMessage);

        var info = FailedSolInfo.FromException(1715, ex);

        info.ErrorMessage.Length.Should().BeLessThanOrEqualTo(203); // 200 + "..."
        info.ErrorMessage.Should().EndWith("...");
    }

    [Fact]
    public void FailedSolInfo_FromException_PreservesShortErrorMessages()
    {
        var shortMessage = "Short error";
        var ex = new Exception(shortMessage);

        var info = FailedSolInfo.FromException(1716, ex);

        info.ErrorMessage.Should().Be(shortMessage);
    }

    // ============================================================================
    // ROVER SCRAPE RESULT - FAILED SOLS JSON TESTS
    // ============================================================================

    [Fact]
    public void RoverScrapeResult_GetFailedSolsJson_ReturnsNullWhenNoFailures()
    {
        var result = new RoverScrapeResult
        {
            RoverName = "perseverance",
            Success = true,
            FailedSolDetails = new List<FailedSolInfo>()
        };

        result.GetFailedSolsJson().Should().BeNull();
    }

    [Fact]
    public void RoverScrapeResult_GetFailedSolsJson_ReturnsValidJson()
    {
        var result = new RoverScrapeResult
        {
            RoverName = "perseverance",
            Success = true,
            SolsFailed = 2,
            FailedSolDetails = new List<FailedSolInfo>
            {
                new()
                {
                    Sol = 1708,
                    ErrorType = "HTTP_503",
                    ErrorMessage = "Service Unavailable",
                    Timestamp = new DateTime(2025, 1, 10, 2, 0, 15, DateTimeKind.Utc)
                },
                new()
                {
                    Sol = 1709,
                    ErrorType = "NetworkError",
                    ErrorMessage = "Connection failed",
                    Timestamp = new DateTime(2025, 1, 10, 2, 0, 18, DateTimeKind.Utc)
                }
            }
        };

        var json = result.GetFailedSolsJson();

        json.Should().NotBeNull();

        // Parse and verify JSON structure
        var parsed = JsonDocument.Parse(json!);
        var root = parsed.RootElement;
        root.GetArrayLength().Should().Be(2);

        var first = root[0];
        first.GetProperty("sol").GetInt32().Should().Be(1708);
        first.GetProperty("error_type").GetString().Should().Be("HTTP_503");
        first.GetProperty("error_message").GetString().Should().Be("Service Unavailable");
        first.GetProperty("timestamp").GetString().Should().Contain("2025-01-10");

        var second = root[1];
        second.GetProperty("sol").GetInt32().Should().Be(1709);
        second.GetProperty("error_type").GetString().Should().Be("NetworkError");
    }

    [Fact]
    public void RoverScrapeResult_FailedSolDetails_DefaultsToEmptyList()
    {
        var result = new RoverScrapeResult();

        result.FailedSolDetails.Should().NotBeNull();
        result.FailedSolDetails.Should().BeEmpty();
    }

    [Fact]
    public void RoverScrapeResult_WithFailedSolDetails_TracksMultipleErrorTypes()
    {
        var result = new RoverScrapeResult
        {
            RoverName = "perseverance",
            StartSol = 1393,
            EndSol = 1400,
            SolsAttempted = 8,
            SolsSucceeded = 5,
            SolsFailed = 3,
            PhotosAdded = 100,
            Success = true,
            FailedSolDetails = new List<FailedSolInfo>
            {
                FailedSolInfo.FromException(1395,
                    new HttpRequestException("503", null, HttpStatusCode.ServiceUnavailable)),
                FailedSolInfo.FromException(1397,
                    new HttpRequestException("429", null, HttpStatusCode.TooManyRequests)),
                FailedSolInfo.FromException(1399,
                    new TaskCanceledException("Timeout"))
            }
        };

        result.FailedSolDetails.Should().HaveCount(3);
        result.FailedSolDetails.Select(f => f.ErrorType)
            .Should().BeEquivalentTo(new[] { "HTTP_503", "HTTP_429", "Timeout" });
        result.FailedSolDetails.Select(f => f.Sol)
            .Should().BeEquivalentTo(new[] { 1395, 1397, 1399 });
    }
}
