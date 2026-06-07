using FluentAssertions;
using MarsVista.Api.Data;
using Microsoft.AspNetCore.Http;

namespace MarsVista.Api.Tests.Data;

/// <summary>
/// Regression coverage for the production crash in
/// <see cref="QueryTimingInterceptor"/> where overlapping EF Core callbacks
/// raced HttpContext.Items (a non-thread-safe Dictionary) and threw
/// "Operations that change non-concurrent collections must have exclusive
/// access...". Drives RecordStart/RecordStop concurrently from a high fan-out
/// of threads and asserts (a) no exception escapes and (b) the snapshot
/// published to HttpContext.Items is consistent.
/// </summary>
public class QueryTimingInterceptorConcurrencyTests
{
    [Fact]
    public async Task ConcurrentStartStop_DoesNotThrowAndPublishesConsistentTotals()
    {
        var httpContext = new DefaultHttpContext();
        var accessor = new StubHttpContextAccessor(httpContext);
        var interceptor = new QueryTimingInterceptor(accessor);

        const int iterations = 10_000;

        // Run many fake commands concurrently against the same interceptor
        // instance (mirrors the Scoped-per-request lifetime).
        await Parallel.ForEachAsync(
            Enumerable.Range(0, iterations),
            new ParallelOptions { MaxDegreeOfParallelism = 32 },
            async (_, _) =>
            {
                var id = Guid.NewGuid();
                interceptor.RecordStart(id);
                await Task.Yield(); // force off the original thread
                interceptor.RecordStop(id);
            });

        // (a) No exception escaped if we reached this point.
        // (b) The published total query count matches the iteration count
        //     exactly - Interlocked.Increment is the source of truth.
        httpContext.Items["__DbQueryCount"].Should().Be(iterations,
            "every RecordStop should have contributed to the total under concurrent load");

        // (c) Published total time is a positive TimeSpan - reading inside
        //     the lock means the last writer publishes the up-to-date sum.
        var publishedTime = httpContext.Items["__TotalDbTime"];
        publishedTime.Should().BeOfType<TimeSpan>();
        ((TimeSpan)publishedTime!).Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void RecordStopWithoutMatchingStart_IsIgnored()
    {
        var httpContext = new DefaultHttpContext();
        var interceptor = new QueryTimingInterceptor(new StubHttpContextAccessor(httpContext));

        // No RecordStart - calling RecordStop with an unknown CommandId must
        // be a no-op rather than throwing or polluting the totals.
        interceptor.RecordStop(Guid.NewGuid());

        httpContext.Items.Should().NotContainKey("__DbQueryCount");
        httpContext.Items.Should().NotContainKey("__TotalDbTime");
    }

    [Fact]
    public void RecordStartThenRecordStop_PublishesSingleQueryTotals()
    {
        var httpContext = new DefaultHttpContext();
        var interceptor = new QueryTimingInterceptor(new StubHttpContextAccessor(httpContext));

        var id = Guid.NewGuid();
        interceptor.RecordStart(id);
        Thread.Sleep(1); // ensure a non-zero elapsed time
        interceptor.RecordStop(id);

        httpContext.Items["__DbQueryCount"].Should().Be(1);
        httpContext.Items["__TotalDbTime"].Should().BeOfType<TimeSpan>();
        ((TimeSpan)httpContext.Items["__TotalDbTime"]!).Should().BeGreaterThan(TimeSpan.Zero);
    }

    private sealed class StubHttpContextAccessor : IHttpContextAccessor
    {
        public StubHttpContextAccessor(HttpContext context) => HttpContext = context;
        public HttpContext? HttpContext { get; set; }
    }
}
