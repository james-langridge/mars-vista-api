using FluentAssertions;
using MarsVista.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace MarsVista.Api.Tests.Services;

public class RedisRateLimitServiceTests
{
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<RedisRateLimitService>> _loggerMock;

    public RedisRateLimitServiceTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<RedisRateLimitService>>();
    }

    private RedisRateLimitService CreateServiceWithoutRedis()
    {
        return new RedisRateLimitService(null, _cache, _loggerMock.Object);
    }

    private RedisRateLimitService CreateServiceWithRedis(Mock<IConnectionMultiplexer> redisMock)
    {
        return new RedisRateLimitService(redisMock.Object, _cache, _loggerMock.Object);
    }

    #region GetLimitsForTier Tests

    [Theory]
    [InlineData("free", 1000, 10000)]
    [InlineData("FREE", 1000, 10000)]
    [InlineData("pro", 10000, 100000)]
    [InlineData("PRO", 10000, 100000)]
    [InlineData("unlimited", -1, -1)]
    public void GetLimitsForTier_ShouldReturnCorrectLimits(string tier, int expectedHourly, int expectedDaily)
    {
        // Arrange
        var sut = CreateServiceWithoutRedis();

        // Act
        var (hourlyLimit, dailyLimit) = sut.GetLimitsForTier(tier);

        // Assert
        hourlyLimit.Should().Be(expectedHourly);
        dailyLimit.Should().Be(expectedDaily);
    }

    [Fact]
    public void GetLimitsForTier_ShouldReturnFreeTierForUnknownTier()
    {
        // Arrange
        var sut = CreateServiceWithoutRedis();
        var unknownTier = "unknown";

        // Act
        var (hourlyLimit, dailyLimit) = sut.GetLimitsForTier(unknownTier);

        // Assert
        hourlyLimit.Should().Be(1000);
        dailyLimit.Should().Be(10000);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unknown tier")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Memory Fallback Tests (Redis unavailable)

    [Fact]
    public async Task CheckRateLimitAsync_MemoryFallback_FirstRequest_ShouldAllow()
    {
        // Arrange
        var sut = CreateServiceWithoutRedis();
        var userEmail = "test@example.com";
        var tier = "free";

        // Act
        var (allowed, hourlyRemaining, dailyRemaining, _, _) = await sut.CheckRateLimitAsync(userEmail, tier);

        // Assert
        allowed.Should().BeTrue();
        hourlyRemaining.Should().Be(999); // 1000 - 1
        dailyRemaining.Should().Be(9999); // 10000 - 1
    }

    [Fact]
    public async Task CheckRateLimitAsync_MemoryFallback_ShouldEnforceHourlyLimit()
    {
        // Arrange
        var sut = CreateServiceWithoutRedis();
        var userEmail = "hourly-limit-test@example.com";
        var tier = "free";

        // Act - Make 1000 requests (the hourly limit)
        for (int i = 0; i < 1000; i++)
        {
            var result = await sut.CheckRateLimitAsync(userEmail, tier);
            result.allowed.Should().BeTrue($"request {i + 1} should be allowed");
        }

        // Act - 1001st request should be blocked
        var (allowed, hourlyRemaining, dailyRemaining, _, _) = await sut.CheckRateLimitAsync(userEmail, tier);

        // Assert
        allowed.Should().BeFalse();
        hourlyRemaining.Should().Be(0);
        dailyRemaining.Should().Be(9000);
    }

    [Fact]
    public async Task CheckRateLimitAsync_MemoryFallback_ShouldEnforceDailyLimit()
    {
        // Arrange
        var sut = CreateServiceWithoutRedis();
        var userEmail = "daily-limit-test@example.com";
        var tier = "free";

        // Simulate hitting daily limit by setting cache directly
        var now = DateTime.UtcNow;
        var dayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
        var dailyKey = $"ratelimit:daily:{userEmail}:{dayStart:yyyyMMdd}";
        _cache.Set(dailyKey, 10000, dayStart.AddDays(1));

        // Act - Next request should be blocked
        var (allowed, hourlyRemaining, dailyRemaining, _, _) = await sut.CheckRateLimitAsync(userEmail, tier);

        // Assert
        allowed.Should().BeFalse();
        dailyRemaining.Should().Be(0);
    }

    [Fact]
    public async Task CheckRateLimitAsync_MemoryFallback_ShouldReturnResetTimestamps()
    {
        // Arrange
        var sut = CreateServiceWithoutRedis();
        var userEmail = "timestamps@example.com";
        var tier = "free";
        var now = DateTime.UtcNow;

        // Act
        var (_, _, _, hourlyResetAt, dailyResetAt) = await sut.CheckRateLimitAsync(userEmail, tier);

        // Assert
        hourlyResetAt.Should().BeGreaterThan(0);
        dailyResetAt.Should().BeGreaterThan(0);

        var hourlyResetTime = DateTimeOffset.FromUnixTimeSeconds(hourlyResetAt).UtcDateTime;
        hourlyResetTime.Should().BeAfter(now);
        hourlyResetTime.Should().BeBefore(now.AddHours(1).AddMinutes(1));

        var dailyResetTime = DateTimeOffset.FromUnixTimeSeconds(dailyResetAt).UtcDateTime;
        dailyResetTime.Should().BeAfter(now);
        dailyResetTime.Should().BeBefore(now.AddDays(1).AddMinutes(1));
    }

    [Fact]
    public async Task CheckRateLimitAsync_MemoryFallback_DifferentUsers_ShouldHaveIndependentLimits()
    {
        // Arrange
        var sut = CreateServiceWithoutRedis();
        var user1 = "user1@example.com";
        var user2 = "user2@example.com";
        var tier = "free";

        // Act - User 1 makes 1000 requests
        for (int i = 0; i < 1000; i++)
        {
            await sut.CheckRateLimitAsync(user1, tier);
        }

        var user1Result = await sut.CheckRateLimitAsync(user1, tier);
        var user2Result = await sut.CheckRateLimitAsync(user2, tier);

        // Assert
        user1Result.allowed.Should().BeFalse();
        user2Result.allowed.Should().BeTrue();
        user2Result.hourlyRemaining.Should().Be(999);
    }

    [Fact]
    public async Task CheckRateLimitAsync_MemoryFallback_ShouldBeThreadSafe()
    {
        // Arrange
        var sut = CreateServiceWithoutRedis();
        var userEmail = "thread-test@example.com";
        var tier = "free";
        var concurrentRequests = 100;

        // Act
        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(_ => sut.CheckRateLimitAsync(userEmail, tier))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        var allowedCount = results.Count(r => r.allowed);
        allowedCount.Should().Be(100);
    }

    #endregion

    #region Redis Tests

    [Fact]
    public async Task CheckRateLimitAsync_Redis_FirstRequest_ShouldAllow()
    {
        // Arrange
        var redisMock = new Mock<IConnectionMultiplexer>();
        var dbMock = new Mock<IDatabase>();

        redisMock.Setup(x => x.IsConnected).Returns(true);
        redisMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(dbMock.Object);

        // First call returns 1 (first increment)
        dbMock.Setup(x => x.StringIncrementAsync(It.IsAny<RedisKey>(), 1, It.IsAny<CommandFlags>()))
            .ReturnsAsync(1);

        var sut = CreateServiceWithRedis(redisMock);
        var userEmail = "test@example.com";
        var tier = "free";

        // Act
        var (allowed, hourlyRemaining, dailyRemaining, _, _) = await sut.CheckRateLimitAsync(userEmail, tier);

        // Assert
        allowed.Should().BeTrue();
        hourlyRemaining.Should().Be(999); // 1000 - 1
        dailyRemaining.Should().Be(9999); // 10000 - 1
    }

    [Fact]
    public async Task CheckRateLimitAsync_Redis_AtHourlyLimit_ShouldBlock()
    {
        // Arrange
        var redisMock = new Mock<IConnectionMultiplexer>();
        var dbMock = new Mock<IDatabase>();

        redisMock.Setup(x => x.IsConnected).Returns(true);
        redisMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(dbMock.Object);

        // Return 1001 for hourly (over limit), 1001 for daily (under limit)
        var callCount = 0;
        dbMock.Setup(x => x.StringIncrementAsync(It.IsAny<RedisKey>(), 1, It.IsAny<CommandFlags>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1 ? 1001 : 1001; // hourly over, daily under
            });

        var sut = CreateServiceWithRedis(redisMock);
        var userEmail = "test@example.com";
        var tier = "free";

        // Act
        var (allowed, hourlyRemaining, _, _, _) = await sut.CheckRateLimitAsync(userEmail, tier);

        // Assert
        allowed.Should().BeFalse();
        hourlyRemaining.Should().Be(0);
    }

    [Fact]
    public async Task CheckRateLimitAsync_Redis_SetsExpiration_OnFirstRequest()
    {
        // Arrange
        var redisMock = new Mock<IConnectionMultiplexer>();
        var dbMock = new Mock<IDatabase>();

        redisMock.Setup(x => x.IsConnected).Returns(true);
        redisMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(dbMock.Object);

        dbMock.Setup(x => x.StringIncrementAsync(It.IsAny<RedisKey>(), 1, It.IsAny<CommandFlags>()))
            .ReturnsAsync(1);
        dbMock.Setup(x => x.KeyExpireAsync(It.IsAny<RedisKey>(), It.IsAny<TimeSpan>(), It.IsAny<ExpireWhen>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var sut = CreateServiceWithRedis(redisMock);

        // Act
        await sut.CheckRateLimitAsync("test@example.com", "free");

        // Assert - Expiration should be set for both hourly and daily keys
        dbMock.Verify(x => x.KeyExpireAsync(
            It.Is<RedisKey>(k => k.ToString().Contains("hourly")),
            It.Is<TimeSpan>(t => t.TotalMinutes > 0 && t.TotalMinutes <= 60),
            It.IsAny<ExpireWhen>(),
            It.IsAny<CommandFlags>()), Times.Once);

        dbMock.Verify(x => x.KeyExpireAsync(
            It.Is<RedisKey>(k => k.ToString().Contains("daily")),
            It.Is<TimeSpan>(t => t.TotalHours > 0 && t.TotalHours <= 24),
            It.IsAny<ExpireWhen>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task CheckRateLimitAsync_Redis_UnlimitedTier_ShouldAlwaysAllow()
    {
        // Arrange
        var redisMock = new Mock<IConnectionMultiplexer>();
        var dbMock = new Mock<IDatabase>();

        redisMock.Setup(x => x.IsConnected).Returns(true);
        redisMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(dbMock.Object);

        // Even with very high counts
        dbMock.Setup(x => x.StringIncrementAsync(It.IsAny<RedisKey>(), 1, It.IsAny<CommandFlags>()))
            .ReturnsAsync(999999);

        var sut = CreateServiceWithRedis(redisMock);

        // Act
        var (allowed, hourlyRemaining, dailyRemaining, _, _) = await sut.CheckRateLimitAsync("test@example.com", "unlimited");

        // Assert
        allowed.Should().BeTrue();
        hourlyRemaining.Should().Be(int.MaxValue);
        dailyRemaining.Should().Be(int.MaxValue);
    }

    #endregion

    #region Graceful Degradation Tests

    [Fact]
    public async Task CheckRateLimitAsync_Redis_OnError_FallsBackToMemory()
    {
        // Arrange
        var redisMock = new Mock<IConnectionMultiplexer>();
        var dbMock = new Mock<IDatabase>();

        redisMock.Setup(x => x.IsConnected).Returns(true);
        redisMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(dbMock.Object);

        // Simulate Redis failure
        dbMock.Setup(x => x.StringIncrementAsync(It.IsAny<RedisKey>(), 1, It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection lost"));

        var sut = CreateServiceWithRedis(redisMock);

        // Act
        var (allowed, hourlyRemaining, _, _, _) = await sut.CheckRateLimitAsync("test@example.com", "free");

        // Assert - Should fall back to memory and allow
        allowed.Should().BeTrue();
        hourlyRemaining.Should().Be(999);

        // Verify warning was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Redis rate limit check failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckRateLimitAsync_Redis_NotConnected_UsesMemory()
    {
        // Arrange
        var redisMock = new Mock<IConnectionMultiplexer>();
        redisMock.Setup(x => x.IsConnected).Returns(false);

        var sut = CreateServiceWithRedis(redisMock);

        // Act
        var (allowed, hourlyRemaining, _, _, _) = await sut.CheckRateLimitAsync("test@example.com", "free");

        // Assert - Should use memory fallback
        allowed.Should().BeTrue();
        hourlyRemaining.Should().Be(999);
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task CheckRateLimitAsync_WhenExceeded_ShouldLogWarning()
    {
        // Arrange
        var sut = CreateServiceWithoutRedis();
        var userEmail = "logger-test@example.com";
        var tier = "free";

        // Make 1000 requests to hit the limit
        for (int i = 0; i < 1000; i++)
        {
            await sut.CheckRateLimitAsync(userEmail, tier);
        }

        // Act - 1001st request should log warning
        await sut.CheckRateLimitAsync(userEmail, tier);

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

    #endregion
}
