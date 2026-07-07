using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MarsVista.Core.Entities;
using MarsVista.Core.Services;

namespace MarsVista.Api.Tests.Integration;

/// <summary>
/// Verifies the retention job deletes usage_events older than the retention
/// window while conserving their statistics in the monthly rollup. The
/// aggregate-then-delete runs as one atomic statement, so a crash cannot lose
/// events without also having rolled them up.
/// </summary>
public class UsageEventRetentionServiceTests : IntegrationTestBase
{
    private UsageEventRetentionService _service = null!;

    protected override Task SeedAdditionalDataAsync()
    {
        _service = new UsageEventRetentionService(DbContext);
        return Task.CompletedTask;
    }

    private void AddEvent(DateTime createdAt, string email, string endpoint, string tier,
        int responseMs, int photos, int status)
    {
        DbContext.UsageEvents.Add(new UsageEvent
        {
            UserEmail = email,
            Endpoint = endpoint,
            Tier = tier,
            ResponseTimeMs = responseMs,
            PhotosReturned = photos,
            StatusCode = status,
            CreatedAt = DateTime.SpecifyKind(createdAt, DateTimeKind.Utc),
        });
    }

    [Fact]
    public async Task DeletesOldEvents_KeepsRecent_AndRollsUpStatistics()
    {
        var now = DateTime.UtcNow;
        // Two old events in the same month/user/endpoint/tier -> one rollup row, count 2.
        AddEvent(now.AddDays(-120), "old@x.dev", "/api/v2/photos", "free", 100, 10, 200);
        AddEvent(now.AddDays(-118), "old@x.dev", "/api/v2/photos", "free", 300, 20, 500);
        // Recent event must be left untouched.
        AddEvent(now.AddDays(-5), "new@x.dev", "/api/v2/photos", "free", 50, 5, 200);
        await DbContext.SaveChangesAsync();

        var deleted = await _service.PurgeAndRollUpAsync(retentionDays: 90);

        deleted.Should().Be(2, "both events older than 90 days are purged");

        var remaining = await DbContext.UsageEvents.AsNoTracking().ToListAsync();
        remaining.Should().ContainSingle(e => e.UserEmail == "new@x.dev");
        remaining.Should().NotContain(e => e.UserEmail == "old@x.dev");

        var rollup = await DbContext.UsageEventsMonthly.AsNoTracking()
            .SingleAsync(r => r.UserEmail == "old@x.dev");
        rollup.RequestCount.Should().Be(2);
        rollup.TotalResponseTimeMs.Should().Be(400, "100 + 300");
        rollup.TotalPhotosReturned.Should().Be(30, "10 + 20");
        rollup.ErrorCount.Should().Be(1, "only the status 500 event counts as an error");
        rollup.Month.Should().Be(new DateTime(now.AddDays(-120).Year, now.AddDays(-120).Month, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task IsIdempotent_SecondRunPurgesNothingAndDoesNotDoubleCount()
    {
        var now = DateTime.UtcNow;
        AddEvent(now.AddDays(-200), "a@x.dev", "/api/v2/rovers", "pro", 40, 0, 200);
        await DbContext.SaveChangesAsync();

        var firstDeleted = await _service.PurgeAndRollUpAsync(retentionDays: 90);
        var secondDeleted = await _service.PurgeAndRollUpAsync(retentionDays: 90);

        firstDeleted.Should().Be(1);
        secondDeleted.Should().Be(0, "nothing older than the window remains after the first run");

        var rollup = await DbContext.UsageEventsMonthly.AsNoTracking()
            .SingleAsync(r => r.UserEmail == "a@x.dev");
        rollup.RequestCount.Should().Be(1, "the second empty run must not re-accumulate");
    }

    [Fact]
    public async Task AccumulatesIntoExistingRollupRow_ViaOnConflict()
    {
        var now = DateTime.UtcNow;
        var oldMonth = now.AddDays(-120);

        // Pre-existing rollup row for the same (month, user, endpoint, tier).
        DbContext.UsageEventsMonthly.Add(new UsageEventMonthly
        {
            Month = new DateTime(oldMonth.Year, oldMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            UserEmail = "acc@x.dev",
            Endpoint = "/api/v2/photos",
            Tier = "free",
            RequestCount = 5,
            TotalResponseTimeMs = 500,
            TotalPhotosReturned = 50,
            ErrorCount = 1,
            CreatedAt = now,
            UpdatedAt = now,
        });
        AddEvent(oldMonth, "acc@x.dev", "/api/v2/photos", "free", 200, 20, 404);
        await DbContext.SaveChangesAsync();

        await _service.PurgeAndRollUpAsync(retentionDays: 90);

        var rollup = await DbContext.UsageEventsMonthly.AsNoTracking()
            .SingleAsync(r => r.UserEmail == "acc@x.dev");
        rollup.RequestCount.Should().Be(6, "5 existing + 1 purged");
        rollup.TotalResponseTimeMs.Should().Be(700, "500 + 200");
        rollup.ErrorCount.Should().Be(2, "1 existing + 1 new 404");
    }

    [Fact]
    public async Task ConservesTotalRequestCount_AcrossPurge()
    {
        var now = DateTime.UtcNow;
        for (var i = 0; i < 7; i++)
            AddEvent(now.AddDays(-100 - i), "c@x.dev", $"/api/v2/e{i % 3}", "free", 10, 1, 200);
        AddEvent(now.AddDays(-1), "c@x.dev", "/api/v2/live", "free", 10, 1, 200);
        await DbContext.SaveChangesAsync();

        var preTotal = await DbContext.UsageEvents.AsNoTracking().CountAsync();

        await _service.PurgeAndRollUpAsync(retentionDays: 90);

        var remaining = await DbContext.UsageEvents.AsNoTracking().CountAsync();
        var rolledUp = await DbContext.UsageEventsMonthly.AsNoTracking().SumAsync(r => r.RequestCount);
        (remaining + rolledUp).Should().Be(preTotal, "no event may be lost or double-counted");
    }
}
