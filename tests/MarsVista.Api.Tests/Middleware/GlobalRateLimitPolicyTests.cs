using System.Net;
using System.Threading.RateLimiting;
using FluentAssertions;
using MarsVista.Api.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MarsVista.Api.Tests.Middleware;

/// <summary>
/// Anonymous traffic is bounded per IP per minute; traffic governed by the
/// per-key quota is not bounded here at all, so a first-party frontend whose
/// visitors all arrive from one server IP never shares a single bucket.
/// </summary>
public class GlobalRateLimitPolicyTests
{
    [Fact]
    public async Task KeyedRequest_IsNeverRateLimitedPerMinute()
    {
        using var limiter = Limiter();
        var request = Context("10.0.0.1", apiKey: "mv_live_key");

        for (var i = 0; i < 500; i++)
        {
            using var lease = await limiter.AcquireAsync(request);
            lease.IsAcquired.Should().BeTrue($"request {i + 1} with an API key must not be limited");
        }
    }

    [Fact]
    public async Task AnonymousRequest_IsLimitedTo100PerMinutePerIp()
    {
        using var limiter = Limiter();
        var request = Context("10.0.0.1");

        for (var i = 0; i < 100; i++)
        {
            using var lease = await limiter.AcquireAsync(request);
            lease.IsAcquired.Should().BeTrue($"request {i + 1} is within the limit");
        }

        using var rejected = await limiter.AcquireAsync(request);
        rejected.IsAcquired.Should().BeFalse("the 101st anonymous request in a minute is rejected");
        rejected.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter).Should().BeTrue();
        retryAfter.Should().Be(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task ExhaustedIp_StillServesOtherIpsAndKeyedRequests()
    {
        using var limiter = Limiter();
        await Exhaust(limiter, Context("10.0.0.1"));

        using var otherIp = await limiter.AcquireAsync(Context("10.0.0.2"));
        otherIp.IsAcquired.Should().BeTrue("each IP has its own window");

        using var keyed = await limiter.AcquireAsync(Context("10.0.0.1", apiKey: "mv_live_key"));
        keyed.IsAcquired.Should().BeTrue("a keyed request is governed by its quota, not the IP window");
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/swagger/index.html")]
    [InlineData("/api/v1/internal/keys")]
    [InlineData("/api/scraper/perseverance")]
    [InlineData("/api/v1/statistics")]
    public void KeyedRequest_ToPathTheKeyQuotaSkips_StaysIpLimited(string path)
    {
        var partition = GlobalRateLimitPolicy.PartitionFor(Context("10.0.0.1", apiKey: "junk", path: path));

        partition.PartitionKey.Should().Be("10.0.0.1", "the key middleware skips this path, so only the IP window applies");
    }

    [Theory]
    [InlineData("/api/v2/photos")]
    [InlineData("/api/v1/rovers/curiosity/photos")]
    [InlineData("/api/v1/manifests/curiosity")]
    public void KeyedRequest_ToQuotaGovernedPath_IsExempt(string path)
    {
        var partition = GlobalRateLimitPolicy.PartitionFor(Context("10.0.0.1", apiKey: "mv_live_key", path: path));

        partition.PartitionKey.Should().Be("keyed");
    }

    [Fact]
    public void EmptyKeyHeader_IsAnonymous()
    {
        var partition = GlobalRateLimitPolicy.PartitionFor(Context("10.0.0.1", apiKey: "", path: "/api/v2/photos"));

        partition.PartitionKey.Should().Be("10.0.0.1", "the key middleware treats an empty header as no key");
    }

    [Fact]
    public async Task Pipeline_RejectsAnonymousRequestsBeyondTheLimit_With429()
    {
        using var host = await StartPipeline();
        var client = host.GetTestClient();

        for (var i = 0; i < 100; i++)
        {
            (await client.GetAsync("/api/v2/photos")).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var rejected = await client.GetAsync("/api/v2/photos");
        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        (await rejected.Content.ReadAsStringAsync())
            .Should().Contain("Maximum 100 requests per minute per IP address without an API key");
    }

    [Fact]
    public async Task Pipeline_NeverRejectsKeyedRequests()
    {
        using var host = await StartPipeline();
        var client = host.GetTestClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "mv_live_key");

        for (var i = 0; i < 101; i++)
        {
            (await client.GetAsync("/api/v2/photos")).StatusCode.Should().Be(HttpStatusCode.OK, $"request {i + 1}");
        }
    }

    /// <summary>The limiter exactly as Program.cs builds it, over a trivial terminal handler.</summary>
    private static Task<IHost> StartPipeline() =>
        new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(services => services.AddRateLimiter(GlobalRateLimitPolicy.Configure))
                .Configure(app =>
                {
                    app.UseRateLimiter();
                    app.Run(context => context.Response.WriteAsync("ok"));
                }))
            .StartAsync();

    private static PartitionedRateLimiter<HttpContext> Limiter() =>
        PartitionedRateLimiter.Create<HttpContext, string>(GlobalRateLimitPolicy.PartitionFor);

    private static async Task Exhaust(PartitionedRateLimiter<HttpContext> limiter, HttpContext request)
    {
        for (var i = 0; i < GlobalRateLimitPolicy.AnonymousPermitLimit; i++)
        {
            (await limiter.AcquireAsync(request)).Dispose();
        }
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
