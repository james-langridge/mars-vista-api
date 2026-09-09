using System.Net;
using FluentAssertions;
using MarsVista.Api.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MarsVista.Api.Tests.Middleware;

/// <summary>Drives the framework middleware with the options Program.cs registers.</summary>
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

    [Theory]
    [InlineData("100.64.0.0", true)]
    [InlineData("100.127.255.255", true)]
    [InlineData("100.63.255.255", false)]
    [InlineData("100.128.0.0", false)]
    public async Task OnlyTheWholeEdgeRangeIsTrusted(string remoteIp, bool trusted)
    {
        var context = Context(remoteIp, xRealIp: "203.0.113.9");

        await Apply(context);

        var expected = trusted ? "203.0.113.9" : remoteIp;
        context.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse(expected));
    }

    // Documents the fallback rather than guarding the policy: no change to
    // Configure can make these rows fail.
    [Theory]
    [InlineData(null)]
    [InlineData("garbage")]
    public async Task RequestFromEdge_WithUnusableHeader_KeepsEdgeAddress(string? xRealIp)
    {
        var context = Context(remoteIp: "100.64.0.7", xRealIp);

        await Apply(context);

        context.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse("100.64.0.7"));
    }

    [Fact]
    public async Task UntrustedSenderAssertingClientAddress_IsLoggedAsWarning()
    {
        var log = new CapturingLoggerProvider();
        using var host = await StartPipeline(log);

        await Send(host, remoteIp: "10.140.54.74", xRealIp: "203.0.113.9");

        log.Warnings.Should().ContainSingle().Which.Should().Contain("10.140.54.74").And.Contain("203.0.113.9");
    }

    [Theory]
    [InlineData("100.64.0.7", "203.0.113.9")]
    [InlineData("10.140.54.74", null)]
    public async Task TrustedEdgeOrHeaderlessRequest_LogsNothing(string remoteIp, string? xRealIp)
    {
        var log = new CapturingLoggerProvider();
        using var host = await StartPipeline(log);

        await Send(host, remoteIp, xRealIp);

        log.Warnings.Should().BeEmpty();
    }

    private static Task Apply(HttpContext context)
    {
        var options = new ForwardedHeadersOptions();
        ForwardedHeadersPolicy.Configure(options);
        var middleware = new ForwardedHeadersMiddleware(_ => Task.CompletedTask, NullLoggerFactory.Instance, Options.Create(options));
        return middleware.Invoke(context);
    }

    /// <summary>The forwarded-headers pair exactly as Program.cs registers it, over a trivial terminal handler.</summary>
    private static Task<IHost> StartPipeline(ILoggerProvider log) =>
        new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddLogging(logging => logging.AddProvider(log));
                    services.Configure<ForwardedHeadersOptions>(ForwardedHeadersPolicy.Configure);
                })
                .Configure(app =>
                {
                    app.UseForwardedHeaders();
                    app.Use(ForwardedHeadersPolicy.WarnOnUnconsumedClientAddress);
                    app.Run(context => context.Response.WriteAsync("ok"));
                }))
            .StartAsync();

    private static Task<HttpContext> Send(IHost host, string remoteIp, string? xRealIp) =>
        host.GetTestServer().SendAsync(context =>
        {
            context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
            if (xRealIp is not null)
            {
                context.Request.Headers["X-Real-IP"] = xRealIp;
            }
        });

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

    private sealed class CapturingLoggerProvider : ILoggerProvider, ILogger
    {
        public List<string> Warnings { get; } = new();

        public ILogger CreateLogger(string categoryName) => this;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                Warnings.Add(formatter(state, exception));
            }
        }

        public bool IsEnabled(LogLevel logLevel) => true;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public void Dispose() { }
    }
}
