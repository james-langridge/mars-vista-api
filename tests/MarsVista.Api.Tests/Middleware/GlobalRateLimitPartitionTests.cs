using System.Net;
using System.Threading.RateLimiting;
using FluentAssertions;
using MarsVista.Api.Middleware;
using Microsoft.AspNetCore.Http;

namespace MarsVista.Api.Tests.Middleware;

/// <summary>
/// The global limiter is the anonymous-traffic defence. Requests carrying an
/// API key are governed by that key's hourly/daily quota instead, so the
/// per-minute IP bucket must not apply to them: the public frontend proxies
/// every visitor through one server IP and hit the 100/min ceiling in
/// production while a single user browsed the gallery (2026-09-09).
/// </summary>
public class GlobalRateLimitPartitionTests
{
    [Fact]
    public async Task KeyedRequest_IsNeverRateLimitedPerMinute()
    {
        var limiter = LimiterFor(Context("10.0.0.1", apiKey: "mv_live_key"));

        for (var i = 0; i < 500; i++)
        {
            using var lease = await limiter.AcquireAsync();
            lease.IsAcquired.Should().BeTrue($"request {i + 1} with an API key must not be limited");
        }
    }

    [Fact]
    public async Task AnonymousRequest_IsLimitedTo100PerMinutePerIp()
    {
        var limiter = LimiterFor(Context("10.0.0.1"));

        for (var i = 0; i < 100; i++)
        {
            using var lease = await limiter.AcquireAsync();
            lease.IsAcquired.Should().BeTrue($"request {i + 1} is within the limit");
        }

        using var rejected = await limiter.AcquireAsync();
        rejected.IsAcquired.Should().BeFalse("the 101st anonymous request in a minute is rejected");
    }

    [Fact]
    public void AnonymousRequests_FromDifferentIps_AreSeparatePartitions()
    {
        var a = GlobalRateLimitPartition.Resolve(Context("10.0.0.1"));
        var b = GlobalRateLimitPartition.Resolve(Context("10.0.0.2"));

        a.PartitionKey.Should().NotBe(b.PartitionKey);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/swagger/index.html")]
    [InlineData("/api/v1/internal/keys")]
    [InlineData("/api/scraper/perseverance")]
    [InlineData("/api/v1/statistics")]
    public void KeyedRequest_ToPathTheKeyQuotaSkips_StaysIpLimited(string path)
    {
        var partition = GlobalRateLimitPartition.Resolve(Context("10.0.0.1", apiKey: "junk", path: path));

        partition.PartitionKey.Should().Be("10.0.0.1", "the key middleware skips this path, so only the IP window applies");
    }

    [Theory]
    [InlineData("/api/v2/photos")]
    [InlineData("/api/v1/rovers/curiosity/photos")]
    [InlineData("/api/v1/manifests/curiosity")]
    public void KeyedRequest_ToQuotaGovernedPath_IsExempt(string path)
    {
        var partition = GlobalRateLimitPartition.Resolve(Context("10.0.0.1", apiKey: "mv_live_key", path: path));

        partition.PartitionKey.Should().Be("keyed");
    }

    [Fact]
    public void EmptyKeyHeader_IsAnonymous()
    {
        var partition = GlobalRateLimitPartition.Resolve(Context("10.0.0.1", apiKey: "", path: "/api/v2/photos"));

        partition.PartitionKey.Should().Be("10.0.0.1", "the key middleware treats an empty header as no key");
    }

    private static RateLimiter LimiterFor(HttpContext context)
    {
        var partition = GlobalRateLimitPartition.Resolve(context);
        return partition.Factory(partition.PartitionKey);
    }

    private static DefaultHttpContext Context(string ip, string? apiKey = null, string path = "/api/v2/photos")
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        context.Request.Path = path;
        if (apiKey is not null)
        {
            context.Request.Headers["X-API-Key"] = apiKey;
        }
        return context;
    }
}
