using FluentAssertions;
using MarsVista.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MarsVista.Api.Tests.Services;

public class RateLimitServiceTests
{
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<RateLimitService>> _loggerMock;
    private readonly RateLimitService _sut;

    public RateLimitServiceTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<RateLimitService>>();
        _sut = new RateLimitService(_cache, _loggerMock.Object);
    }

    [Theory]
    [InlineData("free", 60, 500)]
    [InlineData("FREE", 60, 500)]
    [InlineData("pro", 5000, 100000)]
    [InlineData("PRO", 5000, 100000)]
    [InlineData("enterprise", 100000, -1)]
    [InlineData("ENTERPRISE", 100000, -1)]
    public void GetLimitsForTier_ShouldReturnCorrectLimits(string tier, int expectedHourly, int expectedDaily)
    {
        // Act
        var (hourlyLimit, dailyLimit) = _sut.GetLimitsForTier(tier);

        // Assert
        hourlyLimit.Should().Be(expectedHourly);
        dailyLimit.Should().Be(expectedDaily);
    }

    [Fact]
    public void GetLimitsForTier_ShouldReturnFreeTierForUnknownTier()
    {
        // Arrange
        var unknownTier = "unknown";

        // Act
        var (hourlyLimit, dailyLimit) = _sut.GetLimitsForTier(unknownTier);

        // Assert
        hourlyLimit.Should().Be(60);
        dailyLimit.Should().Be(500);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unknown tier")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckRateLimitAsync_FreeTier_FirstRequest_ShouldAllow()
    {
        // Arrange
        var userEmail = "test@example.com";
        var tier = "free";

        // Act
        var (allowed, hourlyRemaining, dailyRemaining, _, _) = await _sut.CheckRateLimitAsync(userEmail, tier);

        // Assert
        allowed.Should().BeTrue();
        hourlyRemaining.Should().Be(59); // 60 - 1
        dailyRemaining.Should().Be(499); // 500 - 1
    }

    [Fact]
    public async Task CheckRateLimitAsync_FreeTier_ShouldEnforceHourlyLimit()
    {
        // Arrange
        var userEmail = "hourly-limit-test@example.com";
        var tier = "free";

        // Act - Make 60 requests (the hourly limit)
        for (int i = 0; i < 60; i++)
        {
            var result = await _sut.CheckRateLimitAsync(userEmail, tier);
            result.allowed.Should().BeTrue($"request {i + 1} should be allowed");
        }

        // Act - 61st request should be blocked
        var (allowed, hourlyRemaining, dailyRemaining, _, _) = await _sut.CheckRateLimitAsync(userEmail, tier);

        // Assert
        allowed.Should().BeFalse();
        hourlyRemaining.Should().Be(0);
        // Daily still has room (500 limit, 60 requests made)
        dailyRemaining.Should().Be(440);
    }

    [Fact]
    public async Task CheckRateLimitAsync_FreeTier_ShouldEnforceDailyLimit()
    {
        // Arrange
        var userEmail = "daily-limit-test@example.com";
        var tier = "free";

        // Simulate 500 requests within daily limit but spread across different hours
        // We'll directly manipulate the cache to simulate this without making 500 actual requests
        var now = DateTime.UtcNow;
        var dayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
        var dailyKey = $"ratelimit:daily:{userEmail}:{dayStart:yyyyMMdd}";

        // Set daily count to 500 (at the limit)
        _cache.Set(dailyKey, 500, dayStart.AddDays(1));

        // Act - Next request should be blocked
        var (allowed, hourlyRemaining, dailyRemaining, _, _) = await _sut.CheckRateLimitAsync(userEmail, tier);

        // Assert
        allowed.Should().BeFalse();
        dailyRemaining.Should().Be(0);
    }

    [Fact]
    public async Task CheckRateLimitAsync_ProTier_ShouldHaveHigherLimits()
    {
        // Arrange
        var userEmail = "pro@example.com";
        var tier = "pro";

        // Act
        var (allowed, hourlyRemaining, dailyRemaining, _, _) = await _sut.CheckRateLimitAsync(userEmail, tier);

        // Assert
        allowed.Should().BeTrue();
        hourlyRemaining.Should().Be(4999); // 5000 - 1
        dailyRemaining.Should().Be(99999); // 100000 - 1
    }

    [Fact]
    public async Task CheckRateLimitAsync_EnterpriseTier_ShouldHaveUnlimitedDaily()
    {
        // Arrange
        var userEmail = "enterprise@example.com";
        var tier = "enterprise";

        // Act
        var (allowed, hourlyRemaining, dailyRemaining, _, _) = await _sut.CheckRateLimitAsync(userEmail, tier);

        // Assert
        allowed.Should().BeTrue();
        hourlyRemaining.Should().Be(99999); // 100000 - 1
        dailyRemaining.Should().Be(int.MaxValue); // Unlimited (-1 converts to MaxValue)
    }

    [Fact]
    public async Task CheckRateLimitAsync_ShouldReturnResetTimestamps()
    {
        // Arrange
        var userEmail = "timestamps@example.com";
        var tier = "free";
        var now = DateTime.UtcNow;

        // Act
        var (_, _, _, hourlyResetAt, dailyResetAt) = await _sut.CheckRateLimitAsync(userEmail, tier);

        // Assert
        hourlyResetAt.Should().BeGreaterThan(0);
        dailyResetAt.Should().BeGreaterThan(0);

        // Hourly reset should be within the next hour
        var hourlyResetTime = DateTimeOffset.FromUnixTimeSeconds(hourlyResetAt).UtcDateTime;
        hourlyResetTime.Should().BeAfter(now);
        hourlyResetTime.Should().BeBefore(now.AddHours(1).AddMinutes(1));

        // Daily reset should be within the next day
        var dailyResetTime = DateTimeOffset.FromUnixTimeSeconds(dailyResetAt).UtcDateTime;
        dailyResetTime.Should().BeAfter(now);
        dailyResetTime.Should().BeBefore(now.AddDays(1).AddMinutes(1));
    }

    [Fact]
    public async Task CheckRateLimitAsync_DifferentUsers_ShouldHaveIndependentLimits()
    {
        // Arrange
        var user1 = "user1@example.com";
        var user2 = "user2@example.com";
        var tier = "free";

        // Act - User 1 makes 60 requests
        for (int i = 0; i < 60; i++)
        {
            await _sut.CheckRateLimitAsync(user1, tier);
        }

        // User 1's 61st request should fail
        var user1Result = await _sut.CheckRateLimitAsync(user1, tier);

        // User 2's first request should succeed
        var user2Result = await _sut.CheckRateLimitAsync(user2, tier);

        // Assert
        user1Result.allowed.Should().BeFalse();
        user2Result.allowed.Should().BeTrue();
        user2Result.hourlyRemaining.Should().Be(59);
    }

    [Fact]
    public async Task CheckRateLimitAsync_WhenExceeded_ShouldLogWarning()
    {
        // Arrange
        var userEmail = "logger-test@example.com";
        var tier = "free";

        // Make 60 requests to hit the limit
        for (int i = 0; i < 60; i++)
        {
            await _sut.CheckRateLimitAsync(userEmail, tier);
        }

        // Act - 61st request should log warning
        await _sut.CheckRateLimitAsync(userEmail, tier);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Rate limit exceeded")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckRateLimitAsync_ShouldBeThreadSafe()
    {
        // Arrange
        var userEmail = "thread-test@example.com";
        var tier = "free";
        var concurrentRequests = 100;

        // Act - Make 100 concurrent requests
        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(_ => _sut.CheckRateLimitAsync(userEmail, tier))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert - Count how many were allowed
        var allowedCount = results.Count(r => r.allowed);

        // Since we have thread-safe locking, exactly 60 should be allowed (hourly limit)
        allowedCount.Should().Be(60);

        // The last allowed request should report 0 remaining
        var lastAllowedResult = results.Last(r => r.allowed);
        lastAllowedResult.hourlyRemaining.Should().Be(0);
    }
}
