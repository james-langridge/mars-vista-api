using FluentAssertions;
using MarsVista.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MarsVista.Api.Tests.Services;

public class ScraperJobTrackerTests
{
    private readonly ScraperJobTracker _sut;
    private readonly Mock<ILogger<ScraperJobTracker>> _loggerMock;

    public ScraperJobTrackerTests()
    {
        _loggerMock = new Mock<ILogger<ScraperJobTracker>>();
        _sut = new ScraperJobTracker(_loggerMock.Object);
    }

    [Fact]
    public void StartJob_ShouldCreateJobWithCorrectProperties()
    {
        // Act
        var job = _sut.StartJob("incremental", "perseverance", 100, 110, 7);

        // Assert
        job.Should().NotBeNull();
        job.Id.Should().NotBeNullOrEmpty();
        job.Id.Length.Should().Be(12);
        job.Type.Should().Be("incremental");
        job.Rover.Should().Be("perseverance");
        job.StartSol.Should().Be(100);
        job.EndSol.Should().Be(110);
        job.LookbackSols.Should().Be(7);
        job.Status.Should().Be("started");
        job.TotalSols.Should().Be(11);
        job.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void StartJob_ShouldGenerateUniqueJobIds()
    {
        // Act
        var job1 = _sut.StartJob("sol", "curiosity", 500, 500);
        var job2 = _sut.StartJob("sol", "curiosity", 501, 501);
        var job3 = _sut.StartJob("sol", "curiosity", 502, 502);

        // Assert
        job1.Id.Should().NotBe(job2.Id);
        job2.Id.Should().NotBe(job3.Id);
        job1.Id.Should().NotBe(job3.Id);
    }

    [Fact]
    public void GetJob_ShouldReturnNullForUnknownJobId()
    {
        // Act
        var job = _sut.GetJob("unknown-job-id");

        // Assert
        job.Should().BeNull();
    }

    [Fact]
    public void GetJob_ShouldReturnExistingJob()
    {
        // Arrange
        var createdJob = _sut.StartJob("range", "perseverance", 1, 100);

        // Act
        var retrievedJob = _sut.GetJob(createdJob.Id);

        // Assert
        retrievedJob.Should().NotBeNull();
        retrievedJob!.Id.Should().Be(createdJob.Id);
        retrievedJob.Type.Should().Be("range");
    }

    [Fact]
    public void GetAllJobs_ShouldReturnJobsOrderedByStartTimeDescending()
    {
        // Arrange
        var job1 = _sut.StartJob("sol", "curiosity", 1, 1);
        Thread.Sleep(10); // Ensure different timestamps
        var job2 = _sut.StartJob("sol", "curiosity", 2, 2);
        Thread.Sleep(10);
        var job3 = _sut.StartJob("sol", "curiosity", 3, 3);

        // Act
        var jobs = _sut.GetAllJobs();

        // Assert
        jobs.Should().HaveCount(3);
        jobs[0].Id.Should().Be(job3.Id); // Most recent first
        jobs[1].Id.Should().Be(job2.Id);
        jobs[2].Id.Should().Be(job1.Id);
    }

    [Fact]
    public void GetActiveJobs_ShouldOnlyReturnStartedOrInProgressJobs()
    {
        // Arrange
        var activeJob1 = _sut.StartJob("incremental", "all", null, null);
        var activeJob2 = _sut.StartJob("range", "curiosity", 1, 50);
        _sut.UpdateProgress(activeJob2.Id, 25, 100, 25, 0);

        var completedJob = _sut.StartJob("sol", "perseverance", 100, 100);
        _sut.CompleteJob(completedJob.Id, 50, 1, 0);

        // Act
        var activeJobs = _sut.GetActiveJobs();

        // Assert
        activeJobs.Should().HaveCount(2);
        activeJobs.Should().Contain(j => j.Id == activeJob1.Id);
        activeJobs.Should().Contain(j => j.Id == activeJob2.Id);
        activeJobs.Should().NotContain(j => j.Id == completedJob.Id);
    }

    [Fact]
    public void UpdateProgress_ShouldUpdateJobProgress()
    {
        // Arrange
        var job = _sut.StartJob("range", "curiosity", 1, 100);

        // Act
        _sut.UpdateProgress(job.Id, 50, 250, 50, 2);
        var updatedJob = _sut.GetJob(job.Id);

        // Assert
        updatedJob.Should().NotBeNull();
        updatedJob!.Status.Should().Be("in_progress");
        updatedJob.CurrentSol.Should().Be(50);
        updatedJob.PhotosAdded.Should().Be(250);
        updatedJob.SolsCompleted.Should().Be(50);
        updatedJob.SolsFailed.Should().Be(2);
    }

    [Fact]
    public void UpdateProgress_ShouldCalculateProgressPercent()
    {
        // Arrange
        var job = _sut.StartJob("range", "curiosity", 1, 100); // 100 total sols

        // Act
        _sut.UpdateProgress(job.Id, 50, 250, 50, 0);
        var updatedJob = _sut.GetJob(job.Id);

        // Assert
        updatedJob!.ProgressPercent.Should().Be(50.0);
    }

    [Fact]
    public void CompleteJob_ShouldSetCompletedStatus()
    {
        // Arrange
        var job = _sut.StartJob("range", "curiosity", 1, 10);

        // Act
        _sut.CompleteJob(job.Id, 500, 10, 0);
        var completedJob = _sut.GetJob(job.Id);

        // Assert
        completedJob!.Status.Should().Be("completed");
        completedJob.PhotosAdded.Should().Be(500);
        completedJob.SolsCompleted.Should().Be(10);
        completedJob.SolsFailed.Should().Be(0);
        completedJob.CompletedAt.Should().NotBeNull();
        completedJob.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void CompleteJob_ShouldSetPartialStatusWhenSolsFailed()
    {
        // Arrange
        var job = _sut.StartJob("range", "curiosity", 1, 10);

        // Act
        _sut.CompleteJob(job.Id, 400, 8, 2);
        var completedJob = _sut.GetJob(job.Id);

        // Assert
        completedJob!.Status.Should().Be("partial");
        completedJob.SolsFailed.Should().Be(2);
    }

    [Fact]
    public void CompleteJob_ShouldIncludeErrorMessage()
    {
        // Arrange
        var job = _sut.StartJob("incremental", "all", null, null);

        // Act
        _sut.CompleteJob(job.Id, 100, 5, 2, "Some sols timed out");
        var completedJob = _sut.GetJob(job.Id);

        // Assert
        completedJob!.ErrorMessage.Should().Be("Some sols timed out");
    }

    [Fact]
    public void FailJob_ShouldSetFailedStatus()
    {
        // Arrange
        var job = _sut.StartJob("full", "perseverance", 1, 1000);

        // Act
        _sut.FailJob(job.Id, "Database connection failed");
        var failedJob = _sut.GetJob(job.Id);

        // Assert
        failedJob!.Status.Should().Be("failed");
        failedJob.ErrorMessage.Should().Be("Database connection failed");
        failedJob.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void RequestCancel_ShouldReturnTrueForActiveJob()
    {
        // Arrange
        var job = _sut.StartJob("range", "curiosity", 1, 100);

        // Act
        var result = _sut.RequestCancel(job.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void RequestCancel_ShouldReturnFalseForCompletedJob()
    {
        // Arrange
        var job = _sut.StartJob("sol", "curiosity", 100, 100);
        _sut.CompleteJob(job.Id, 50, 1, 0);

        // Act
        var result = _sut.RequestCancel(job.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void RequestCancel_ShouldReturnFalseForUnknownJob()
    {
        // Act
        var result = _sut.RequestCancel("unknown-job-id");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsCancellationRequested_ShouldReturnTrueAfterCancel()
    {
        // Arrange
        var job = _sut.StartJob("range", "curiosity", 1, 100);
        _sut.RequestCancel(job.Id);

        // Act
        var isCancelled = _sut.IsCancellationRequested(job.Id);

        // Assert
        isCancelled.Should().BeTrue();
    }

    [Fact]
    public void IsCancellationRequested_ShouldReturnFalseForNonCancelledJob()
    {
        // Arrange
        var job = _sut.StartJob("range", "curiosity", 1, 100);

        // Act
        var isCancelled = _sut.IsCancellationRequested(job.Id);

        // Assert
        isCancelled.Should().BeFalse();
    }

    [Fact]
    public void IsCancellationRequested_ShouldReturnFalseForUnknownJob()
    {
        // Act
        var isCancelled = _sut.IsCancellationRequested("unknown-job-id");

        // Assert
        isCancelled.Should().BeFalse();
    }

    [Fact]
    public void CleanupOldJobs_ShouldKeepActiveJobs()
    {
        // Arrange
        var activeJob = _sut.StartJob("range", "curiosity", 1, 100);
        var completedJob = _sut.StartJob("sol", "perseverance", 100, 100);
        _sut.CompleteJob(completedJob.Id, 50, 1, 0);

        // Act
        _sut.CleanupOldJobs(keepCount: 0); // Remove all completed jobs

        // Assert
        _sut.GetJob(activeJob.Id).Should().NotBeNull();
        _sut.GetJob(completedJob.Id).Should().BeNull();
    }

    [Fact]
    public void CleanupOldJobs_ShouldKeepMostRecentCompletedJobs()
    {
        // Arrange - Create and complete 5 jobs
        var jobs = new List<ScraperJob>();
        for (int i = 0; i < 5; i++)
        {
            var job = _sut.StartJob("sol", "curiosity", i, i);
            _sut.CompleteJob(job.Id, 10, 1, 0);
            jobs.Add(job);
            Thread.Sleep(10); // Ensure different completion times
        }

        // Act - Keep only 2 most recent
        _sut.CleanupOldJobs(keepCount: 2);

        // Assert
        var remainingJobs = _sut.GetAllJobs();
        remainingJobs.Should().HaveCount(2);
        // Most recent 2 jobs should remain
        remainingJobs.Should().Contain(j => j.Id == jobs[4].Id);
        remainingJobs.Should().Contain(j => j.Id == jobs[3].Id);
    }

    [Fact]
    public void Job_ElapsedSeconds_ShouldCalculateCorrectly()
    {
        // Arrange
        var job = _sut.StartJob("range", "curiosity", 1, 100);
        Thread.Sleep(100); // Wait 100ms

        // Act
        var elapsed = job.ElapsedSeconds;

        // Assert
        elapsed.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void Job_EstimatedSecondsRemaining_ShouldReturnNullWhenNoProgress()
    {
        // Arrange
        var job = _sut.StartJob("range", "curiosity", 1, 100);

        // Assert
        job.EstimatedSecondsRemaining.Should().BeNull();
    }

    [Fact]
    public void Job_ProgressPercent_ShouldReturnZeroWhenTotalSolsIsZero()
    {
        // Arrange
        var job = _sut.StartJob("incremental", "all", null, null);

        // Assert
        job.ProgressPercent.Should().Be(0);
    }
}
