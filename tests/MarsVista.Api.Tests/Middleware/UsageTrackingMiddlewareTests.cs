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
            next: WriteJsonResponse("""{"data":{"id":42,"type":"photo","attributes":{}}}"""),
            logger: Mock.Of<ILogger<UsageTrackingMiddleware>>(),
            persist: usageEvent => { captured = usageEvent; return Task.CompletedTask; });

        var context = BuildAuthenticatedContext(
            "/api/v2/photos/42", tier: "pro", query: "?include=rover,camera");

        await middleware.InvokeAsync(context);

        captured.Should().NotBeNull();
        captured!.UserEmail.Should().Be("test@marsvista.dev");
        captured.Tier.Should().Be("pro");
        captured.Endpoint.Should().Be("/api/v2/photos/42");
        captured.StatusCode.Should().Be(200);
        captured.QueryString.Should().Be("?include=rover,camera");
        captured.PhotosReturned.Should().Be(1);
    }

    // photos_returned is derived centrally from the buffered response body, not
    // set by controllers: nothing ever wrote HttpContext.Items["PhotosReturned"],
    // so every production row carried 0. These tests pin the counting rules for
    // each real response shape (v2 JSON:API, v1 NASA-compatible).

    [Theory]
    [InlineData("""{"data":[{"id":1,"type":"photo"},{"id":2,"type":"photo"},{"id":3,"type":"photo"}],"meta":{"returned_count":3}}""", 3)]
    [InlineData("""{"data":[]}""", 0)]
    [InlineData("""{"data":null}""", 0)]
    [InlineData("""{"data":{"id":42,"type":"photo","attributes":{}}}""", 1)]
    public async Task CountsPhotos_FromV2ResponseBody(string body, int expected)
    {
        var captured = await TrackRequest("/api/v2/photos", body);

        captured!.PhotosReturned.Should().Be(expected);
    }

    [Theory]
    [InlineData("""{"photos":[{"id":1},{"id":2}]}""", 2)]
    [InlineData("""{"photos":[]}""", 0)]
    [InlineData("""{"photo":{"id":42}}""", 1)]
    public async Task CountsPhotos_FromV1ResponseBody(string body, int expected)
    {
        var captured = await TrackRequest("/api/v1/rovers/curiosity/photos", body);

        captured!.PhotosReturned.Should().Be(expected);
    }

    [Fact]
    public async Task V1LatestEndpoint_PathWithoutPhotosSegment_StillCountsPhotos()
    {
        // /api/v1/rovers/{name}/latest returns the same {"photos":[...]} shape
        // as /photos and /latest_photos, but its path has no "photos" substring.
        // The v1 root keys are unambiguous, so counting must not be path-gated.
        var captured = await TrackRequest(
            "/api/v1/rovers/curiosity/latest",
            """{"photos":[{"id":1},{"id":2}],"pagination":{"page":1}}""");

        captured!.PhotosReturned.Should().Be(2);
    }

    [Fact]
    public async Task RatingEndpoint_FlatResponseOnPhotosPath_CountsZero()
    {
        // /api/v2/photos/{id}/rating lives under a photos path but returns a
        // flat RatingResponse with no data/photos/photo root key.
        var captured = await TrackRequest(
            "/api/v2/photos/42/rating",
            """{"average_rating":4.5,"rating_count":2,"user_rating":5}""");

        captured!.PhotosReturned.Should().Be(0);
    }

    [Fact]
    public async Task PanoramaDetail_WithRootPhotosArray_CountsMemberPhotos()
    {
        // /api/v2/panoramas/{id} embeds the panorama's member photos as a root
        // "photos" array (no "data" wrapper - verified against production).
        // Those are real photo payloads served to the caller, so they count;
        // the panoramas LIST response has no root "photos" key and counts 0.
        var captured = await TrackRequest(
            "/api/v2/panoramas/pano_curiosity_4919_1",
            """{"id":"pano_curiosity_4919_1","type":"panorama","attributes":{"total_photos":2},"photos":[{"id":1},{"id":2}],"links":{}}""");

        captured!.PhotosReturned.Should().Be(2);
    }

    [Fact]
    public async Task PanoramaList_DataWrapperWithoutRootPhotos_CountsZero()
    {
        // The panoramas LIST wraps panoramas in a "data" array and omits their
        // photos entirely (Photos is null and JsonIgnored), so nothing counts.
        var captured = await TrackRequest(
            "/api/v2/panoramas",
            """{"data":[{"id":"pano_curiosity_4919_1","type":"panorama","attributes":{"total_photos":6}}],"meta":{}}""");

        captured!.PhotosReturned.Should().Be(0);
    }

    [Fact]
    public async Task StatsResponse_DataObjectWithoutPhotoType_CountsZero()
    {
        var captured = await TrackRequest(
            "/api/v2/photos/stats",
            """{"data":{"total_photos":571,"groups":[{"key":"MAST","count":553}]}}""");

        captured!.PhotosReturned.Should().Be(0);
    }

    [Fact]
    public async Task NonPhotoEndpoint_WithDataArray_CountsZero()
    {
        var captured = await TrackRequest(
            "/api/v2/rovers",
            """{"data":[{"id":1,"type":"rover"},{"id":2,"type":"rover"}]}""");

        captured!.PhotosReturned.Should().Be(0);
    }

    [Fact]
    public async Task ErrorResponse_CountsZero()
    {
        var captured = await TrackRequest(
            "/api/v2/photos",
            """{"detail":"Validation Error","errors":[{"message":"bad sol"}]}""",
            statusCode: 400);

        captured!.PhotosReturned.Should().Be(0);
    }

    [Fact]
    public async Task EmptyBody_NotModifiedResponse_CountsZero()
    {
        var captured = await TrackRequest("/api/v2/photos", body: null, statusCode: 304);

        captured!.PhotosReturned.Should().Be(0);
    }

    [Fact]
    public async Task MalformedJsonBody_CountsZero_WithoutThrowing()
    {
        var captured = await TrackRequest("/api/v2/photos", "not json {");

        captured!.PhotosReturned.Should().Be(0);
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

    /// <summary>
    /// A pipeline terminal that writes <paramref name="json"/> as the response
    /// body, mimicking a controller result the middleware must count from.
    /// </summary>
    private static RequestDelegate WriteJsonResponse(string json, int statusCode = 200) =>
        async ctx =>
        {
            ctx.Response.StatusCode = statusCode;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(json);
        };

    /// <summary>
    /// Runs one authenticated request through the middleware with the given
    /// response body and returns the captured usage event.
    /// </summary>
    private static async Task<UsageEvent?> TrackRequest(
        string path, string? body, int statusCode = 200)
    {
        UsageEvent? captured = null;

        var middleware = new TestableUsageTrackingMiddleware(
            next: body is null
                ? ctx => { ctx.Response.StatusCode = statusCode; return Task.CompletedTask; }
                : WriteJsonResponse(body, statusCode),
            logger: Mock.Of<ILogger<UsageTrackingMiddleware>>(),
            persist: usageEvent => { captured = usageEvent; return Task.CompletedTask; });

        await middleware.InvokeAsync(BuildAuthenticatedContext(path));

        captured.Should().NotBeNull();
        return captured;
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
