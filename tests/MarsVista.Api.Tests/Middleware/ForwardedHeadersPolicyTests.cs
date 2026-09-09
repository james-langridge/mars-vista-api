using System.Net;
using FluentAssertions;
using MarsVista.Api.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MarsVista.Api.Tests.Middleware;

/// <summary>
/// Behind Railway's edge every public request arrives from a proxy address,
/// so anything keyed on RemoteIpAddress (the anonymous rate limit, request
/// logs) must see the client address the edge reports, and only when the
/// connection really comes from the edge.
/// </summary>
public class ForwardedHeadersPolicyTests
{
    [Fact]
    public async Task RequestFromEdge_TakesClientIpFromXRealIp()
    {
        var context = Context(remoteIp: "100.64.0.7", xRealIp: "203.0.113.9");

        await Apply(context);

        context.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse("203.0.113.9"));
    }

    [Fact]
    public async Task RequestFromPrivateNetwork_IgnoresXRealIp()
    {
        var context = Context(remoteIp: "10.140.54.74", xRealIp: "203.0.113.9");

        await Apply(context);

        context.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse("10.140.54.74"),
            "only the edge proxy range is trusted to assert a client address");
    }

    [Fact]
    public async Task RequestFromEdge_WithoutHeader_KeepsEdgeAddress()
    {
        var context = Context(remoteIp: "100.64.0.7", xRealIp: null);

        await Apply(context);

        context.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse("100.64.0.7"));
    }

    private static Task Apply(HttpContext context)
    {
        var options = new ForwardedHeadersOptions();
        ForwardedHeadersPolicy.Configure(options);
        var middleware = new ForwardedHeadersMiddleware(_ => Task.CompletedTask, NullLoggerFactory.Instance, Options.Create(options));
        return middleware.Invoke(context);
    }

    private static DefaultHttpContext Context(string remoteIp, string? xRealIp)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
        if (xRealIp is not null)
        {
            context.Request.Headers["X-Real-IP"] = xRealIp;
        }
        return context;
    }
}
