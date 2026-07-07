using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MarsVista.Core.Entities;

namespace MarsVista.Api.Tests.Integration;

/// <summary>
/// Proves the usage_events_monthly rollup table was created by the migration and
/// enforces the (month, user_email, endpoint, tier) unique key the retention
/// job's ON CONFLICT clause depends on. IntegrationTestBase runs MigrateAsync in
/// InitializeAsync, so reaching these assertions at all means the migration
/// applied cleanly.
/// </summary>
public class UsageEventMonthlyMigrationTests : IntegrationTestBase
{
    [Fact]
    public async Task RollupTable_RoundTripsARow()
    {
        var month = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DbContext.UsageEventsMonthly.Add(new UsageEventMonthly
        {
            Month = month,
            UserEmail = "someone@example.com",
            Endpoint = "/api/v2/photos",
            Tier = "free",
            RequestCount = 42,
            TotalResponseTimeMs = 8400,
            TotalPhotosReturned = 1008,
            ErrorCount = 3,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        await DbContext.SaveChangesAsync();

        var row = await DbContext.UsageEventsMonthly.AsNoTracking()
            .SingleAsync(r => r.UserEmail == "someone@example.com");

        row.RequestCount.Should().Be(42);
        row.TotalResponseTimeMs.Should().Be(8400);
        row.ErrorCount.Should().Be(3);
    }

    [Fact]
    public async Task RollupTable_RejectsDuplicateMonthUserEndpointTier()
    {
        var month = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        UsageEventMonthly MakeRow() => new()
        {
            Month = month,
            UserEmail = "dup@example.com",
            Endpoint = "/api/v2/rovers",
            Tier = "pro",
            RequestCount = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        DbContext.UsageEventsMonthly.Add(MakeRow());
        await DbContext.SaveChangesAsync();

        // A second row with the same (month, user, endpoint, tier) must violate
        // the unique index - this is what makes the retention job's upsert safe.
        DbContext.UsageEventsMonthly.Add(MakeRow());
        var act = async () => await DbContext.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }
}
