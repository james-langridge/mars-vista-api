using System.Collections;
using FluentAssertions;
using MarsVista.Api.Middleware;
using MarsVista.Core.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
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
/// that contract: the snapshot is built from the live request; a persistence
/// failure is swallowed and logged from captured state; and, with the context
/// deliberately recycled mid-flight, the detached task never dereferences it.
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

        // The failure is swallowed and logged from the captured snapshot, so the
        // fire-and-forget task completes instead of faulting. (That the catch never
        // touches the recycled HttpContext is proven separately by
        // PersistenceFailureAfterContextRecycled_DoesNotDereferenceContext, which
        // runs the error path against a disposed feature collection.)
        VerifyLoggedError(logger, databaseFailure, "/api/v2/photos/2863558");
    }

    [Fact]
    public async Task PersistenceFailureAfterContextRecycled_DoesNotDereferenceContext()
    {
        UsageEvent? snapshot = null;
        var logger = new Mock<ILogger<UsageTrackingMiddleware>>();
        var databaseFailure = new InvalidOperationException("simulated database failure");

        var (context, features) = BuildRecyclableAuthenticatedContext(
            "/api/v2/photos/2863558", "?include=rover,camera");

        // Reproduce the production timing: the fire-and-forget task's error path
        // runs after the framework has recycled the HttpContext. The delegate
        // captures the (already materialized) snapshot, disposes the feature
        // collection, then fails the persistence.
        var middleware = new TestableUsageTrackingMiddleware(
            next: _ => Task.CompletedTask,
            logger: logger.Object,
            persist: usageEvent =>
            {
                snapshot = usageEvent;
                features.Recycle();
                throw databaseFailure;
            });

        var invoke = () => middleware.InvokeAsync(context);

        // The recycled context must not surface into the request pipeline...
        await invoke.Should().NotThrowAsync();

        // ...the feature collection really is disposed now (guards the test itself)...
        var touchRecycledContext = () => _ = context.Request.Path;
        touchRecycledContext.Should().Throw<ObjectDisposedException>();

        // ...the snapshot was fully materialized before recycling...
        snapshot.Should().NotBeNull();
        snapshot!.Endpoint.Should().Be("/api/v2/photos/2863558");
        snapshot.UserEmail.Should().Be("test@marsvista.dev");

        // ...and the failure was logged from that snapshot, never from the dead
        // context - which is exactly what threw the secondary ObjectDisposedException
        // in production.
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

    private static (DefaultHttpContext Context, RecyclableFeatureCollection Features) BuildRecyclableAuthenticatedContext(
        string path, string query)
    {
        var features = new FeatureCollection();
        features.Set<IHttpRequestFeature>(new HttpRequestFeature
        {
            Method = "GET",
            Path = path,
            QueryString = query,
            Headers = new HeaderDictionary()
        });
        features.Set<IHttpResponseFeature>(new HttpResponseFeature
        {
            StatusCode = 200,
            Headers = new HeaderDictionary()
        });
        features.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(Stream.Null));
        features.Set<IItemsFeature>(new ItemsFeature());

        var recyclable = new RecyclableFeatureCollection(features);
        var context = new DefaultHttpContext(recyclable);
        context.Items["UserEmail"] = "test@marsvista.dev";
        context.Items["UserTier"] = "free";
        return (context, recyclable);
    }

    /// <summary>
    /// An <see cref="IFeatureCollection"/> that throws <see cref="ObjectDisposedException"/>
    /// once <see cref="Recycle"/> is called, mimicking how the server tears down the
    /// request features after the response completes.
    /// </summary>
    private sealed class RecyclableFeatureCollection : IFeatureCollection
    {
        private readonly IFeatureCollection _inner;
        private bool _recycled;

        public RecyclableFeatureCollection(IFeatureCollection inner) => _inner = inner;

        public void Recycle() => _recycled = true;

        private void ThrowIfRecycled()
        {
            if (_recycled)
            {
                throw new ObjectDisposedException("Collection", "IFeatureCollection has been disposed.");
            }
        }

        public object? this[Type key]
        {
            get { ThrowIfRecycled(); return _inner[key]; }
            set { ThrowIfRecycled(); _inner[key] = value; }
        }

        public bool IsReadOnly
        {
            get { ThrowIfRecycled(); return _inner.IsReadOnly; }
        }

        public int Revision
        {
            get { ThrowIfRecycled(); return _inner.Revision; }
        }

        public TFeature? Get<TFeature>()
        {
            ThrowIfRecycled();
            return _inner.Get<TFeature>();
        }

        public void Set<TFeature>(TFeature? instance)
        {
            ThrowIfRecycled();
            _inner.Set(instance);
        }

        public IEnumerator<KeyValuePair<Type, object>> GetEnumerator()
        {
            ThrowIfRecycled();
            return _inner.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            ThrowIfRecycled();
            return _inner.GetEnumerator();
        }
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
