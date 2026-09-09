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

    private static RateLimiter LimiterFor(HttpContext context)
    {
        var partition = GlobalRateLimitPartition.Resolve(context);
        return partition.Factory(partition.PartitionKey);
    }

    private static DefaultHttpContext Context(string ip, string? apiKey = null)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        if (apiKey is not null)
        {
            context.Request.Headers["X-API-Key"] = apiKey;
        }
        return context;
    }
}
