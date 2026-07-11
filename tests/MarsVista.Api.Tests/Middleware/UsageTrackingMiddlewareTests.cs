using FluentAssertions;
using MarsVista.Api.Middleware;
using MarsVista.Core.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarsVista.Api.Tests.Middleware;

/// <summary>
/// Regression coverage for the production crash in
/// <see cref="UsageTrackingMiddleware"/> where the fire-and-forget usage-tracking
/// task dereferenced the HttpContext after the response completed. Once the
/// response is sent the framework recycles the context and its
/// <c>IFeatureCollection</c>; when the database write failed, the catch handler
/// logged <c>context.Request.Path</c> and threw a second
/// <c>ObjectDisposedException: IFeatureCollection has been disposed</c> that
/// escaped as an unobserved task exception (Sentry MARS-VISTA-API-G, 39 events).
///
/// The fix snapshots everything into a <see cref="UsageEvent"/> while the context
/// is alive and never touches the context on the detached task. These tests pin
/// that contract: the snapshot is built from the live request, and a persistence
/// failure is swallowed and logged from captured state alone.
/// </summary>
public class UsageTrackingMiddlewareTests
{
    [Fact]
    public async Task PersistenceFailure_IsSwallowedAndLogged_WithoutSurfacingException()
    {
        var logger = new Mock<ILogger<UsageTrackingMiddleware>>();
        var databaseFailure = new InvalidOperationException("simulated database failure");

        var middleware = new TestableUsageTrackingMiddleware(
            next: _ => Task.CompletedTask,
            logger: logger.Object,
            persist: _ => throw databaseFailure);

        var context = BuildAuthenticatedContext("/api/v2/photos/2863558");

        // The fire-and-forget tracking failure must never propagate into the
        // request pipeline.
        var invoke = () => middleware.InvokeAsync(context);
        await invoke.Should().NotThrowAsync();

        // The failure was caught and logged using the captured endpoint - proving
        // the catch handler did not dereference the (recycled) HttpContext, which
        // is exactly what threw the secondary ObjectDisposedException in production.
        VerifyLoggedError(logger, databaseFailure, "/api/v2/photos/2863558");
    }

    [Fact]
    public async Task BuildsUsageEventSnapshot_FromLiveRequestAndResponse()
    {
        UsageEvent? captured = null;

        var middleware = new TestableUsageTrackingMiddleware(
            next: ctx => { ctx.Response.StatusCode = 200; return Task.CompletedTask; },
            logger: Mock.Of<ILogger<UsageTrackingMiddleware>>(),
            persist: usageEvent => { captured = usageEvent; return Task.CompletedTask; });

        var context = BuildAuthenticatedContext(
            "/api/v2/photos/42", tier: "pro", query: "?include=rover,camera");
        context.Items["PhotosReturned"] = 1;

        await middleware.InvokeAsync(context);

        captured.Should().NotBeNull();
        captured!.UserEmail.Should().Be("test@marsvista.dev");
        captured.Tier.Should().Be("pro");
        captured.Endpoint.Should().Be("/api/v2/photos/42");
        captured.StatusCode.Should().Be(200);
        captured.QueryString.Should().Be("?include=rover,camera");
        captured.PhotosReturned.Should().Be(1);
    }

    [Fact]
    public async Task UnauthenticatedRequest_IsNotTracked()
    {
        var persisted = false;

        var middleware = new TestableUsageTrackingMiddleware(
            next: _ => Task.CompletedTask,
            logger: Mock.Of<ILogger<UsageTrackingMiddleware>>(),
            persist: _ => { persisted = true; return Task.CompletedTask; });

        // No UserEmail in Items -> the request is anonymous.
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v2/photos/42";

        await middleware.InvokeAsync(context);

        persisted.Should().BeFalse();
    }

    private static DefaultHttpContext BuildAuthenticatedContext(
        string path, string tier = "free", string? query = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        if (query is not null)
        {
            context.Request.QueryString = new QueryString(query);
        }

        context.Items["UserEmail"] = "test@marsvista.dev";
        context.Items["UserTier"] = tier;
        return context;
    }

    private static void VerifyLoggedError(
        Mock<ILogger<UsageTrackingMiddleware>> logger, Exception expected, string endpointFragment)
    {
        logger.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains(endpointFragment)),
                expected,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Overrides the database write with a supplied delegate so tests can capture
    /// the built event or simulate a persistence failure without a database.
    /// </summary>
    private sealed class TestableUsageTrackingMiddleware : UsageTrackingMiddleware
    {
        private readonly Func<UsageEvent, Task> _persist;

        public TestableUsageTrackingMiddleware(
            RequestDelegate next,
            ILogger<UsageTrackingMiddleware> logger,
            Func<UsageEvent, Task> persist)
            : base(next, logger, Mock.Of<IServiceScopeFactory>())
        {
            _persist = persist;
        }

        protected override Task PersistAsync(UsageEvent usageEvent) => _persist(usageEvent);
    }
}
