using FluentAssertions;
using MarsVista.Api.Middleware;
using MarsVista.Api.Services;
using MarsVista.Core.Data;
using MarsVista.Core.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace MarsVista.Api.Tests.Integration;

/// <summary>
/// Runs UserApiKeyAuthenticationMiddleware against a DbContext configured like
/// the one in Program.cs. Production makes no-tracking queries the default.
/// The shared test base does not, so these tests set it themselves.
/// </summary>
public class UserApiKeyAuthenticationMiddlewareTests : IntegrationTestBase
{
    private const string PlainApiKey = "mv_live_0123456789abcdef0123456789abcdef01234567";

    [Fact]
    public async Task A_request_records_the_first_use_of_a_key()
    {
        var keyId = await AddKeyAsync(lastUsedAt: null);
        var requestStart = DateTime.UtcNow;

        await SendRequestAsync();

        var lastUsedAt = await ReadLastUsedAtAsync(keyId);
        lastUsedAt.Should().NotBeNull();
        lastUsedAt!.Value.Should().BeCloseTo(requestStart, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task A_request_moves_last_used_forward_after_a_minute()
    {
        var twoMinutesAgo = DateTime.UtcNow.AddMinutes(-2);
        var keyId = await AddKeyAsync(lastUsedAt: twoMinutesAgo);
        var requestStart = DateTime.UtcNow;

        await SendRequestAsync();

        var lastUsedAt = await ReadLastUsedAtAsync(keyId);
        lastUsedAt!.Value.Should().BeCloseTo(requestStart, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task A_request_within_a_minute_of_the_last_use_writes_nothing()
    {
        var tenSecondsAgo = DateTime.UtcNow.AddSeconds(-10);
        var keyId = await AddKeyAsync(lastUsedAt: tenSecondsAgo);

        await SendRequestAsync();

        var lastUsedAt = await ReadLastUsedAtAsync(keyId);
        lastUsedAt!.Value.Should().BeCloseTo(tenSecondsAgo, TimeSpan.FromMilliseconds(1));
    }

    private async Task<Guid> AddKeyAsync(DateTime? lastUsedAt)
    {
        var apiKeyHash = new ApiKeyService().HashApiKey(PlainApiKey);
        var key = new ApiKey
        {
            Id = Guid.NewGuid(),
            UserEmail = "user@example.com",
            ApiKeyHash = apiKeyHash,
            Tier = "free",
            IsActive = true,
            LastUsedAt = lastUsedAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.ApiKeys.Add(key);
        await DbContext.SaveChangesAsync();
        return key.Id;
    }

    private async Task SendRequestAsync()
    {
        var connectionString = DbContext.Database.GetConnectionString();
        var services = new ServiceCollection();
        services.AddDbContext<MarsVistaDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning)));

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var requestDbContext = scope.ServiceProvider.GetRequiredService<MarsVistaDbContext>();

        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        context.Request.Path = "/api/v2/photos";
        context.Request.Headers["X-API-Key"] = PlainApiKey;

        var nextWasCalled = false;
        RequestDelegate next = _ =>
        {
            nextWasCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new UserApiKeyAuthenticationMiddleware(
            next,
            NullLogger<UserApiKeyAuthenticationMiddleware>.Instance);

        await middleware.InvokeAsync(context, requestDbContext, new ApiKeyService(), new AllowAllRateLimitService());

        nextWasCalled.Should().BeTrue();
    }

    private async Task<DateTime?> ReadLastUsedAtAsync(Guid keyId)
    {
        return await DbContext.ApiKeys
            .AsNoTracking()
            .Where(k => k.Id == keyId)
            .Select(k => k.LastUsedAt)
            .SingleAsync();
    }

    private sealed class AllowAllRateLimitService : IRateLimitService
    {
        public Task<(bool allowed, int hourlyRemaining, int dailyRemaining, long hourlyResetAt, long dailyResetAt)> CheckRateLimitAsync(
            string userEmail,
            string tier)
        {
            return Task.FromResult((true, 100, 1000, 0L, 0L));
        }

        public (int hourlyLimit, int dailyLimit) GetLimitsForTier(string tier)
        {
            return (10000, 100000);
        }
    }
}
