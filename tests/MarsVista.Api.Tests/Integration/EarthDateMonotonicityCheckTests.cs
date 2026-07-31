using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MarsVista.Core.Entities;
using MarsVista.Core.Services;

namespace MarsVista.Api.Tests.Integration;

/// <summary>
/// The daily scrape runs this check as a tripwire for the sol/earth_date
/// ordering invariant the API's sol-first earth_date sort depends on. These
/// tests prove the check actually fires on out-of-order data and on NULL
/// earth_dates - the failure mode it guards against is precisely a check that
/// stays silent - and that it does NOT fire on the shapes production data
/// takes every day (shared boundary dates, per-rover partitions).
/// </summary>
public class EarthDateMonotonicityCheckTests : IntegrationTestBase
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IEarthDateMonotonicityCheck, EarthDateMonotonicityCheck>();
    }

    private Photo MakePhoto(string nasaId, int sol, DateTime? earthDate, int roverId = 1) => new()
    {
        NasaId = nasaId, ImgSrcFull = "x", ImgSrcLarge = "x", ImgSrcMedium = "x", ImgSrcSmall = "x",
        Sol = sol, EarthDate = earthDate,
        DateTakenUtc = earthDate ?? new DateTime(2013, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        SampleType = "Full", RoverId = roverId, CameraId = 1,
        CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
    };

    private IEarthDateMonotonicityCheck Check =>
        ServiceProvider.GetRequiredService<IEarthDateMonotonicityCheck>();

    [Fact]
    public async Task ConcordantData_ReportsClean()
    {
        DbContext.Photos.AddRange(
            MakePhoto("MONO-A", 10, new DateTime(2013, 1, 10, 0, 0, 0, DateTimeKind.Utc)),
            MakePhoto("MONO-B", 11, new DateTime(2013, 1, 11, 0, 0, 0, DateTimeKind.Utc)),
            MakePhoto("MONO-C", 12, new DateTime(2013, 1, 12, 0, 0, 0, DateTimeKind.Utc)));
        await DbContext.SaveChangesAsync();

        var result = await Check.CheckAsync();

        result.Should().Be(new EarthDateInvariantResult(0, 0));
    }

    [Fact]
    public async Task LaterSolWithEarlierEarthDate_IsDetected()
    {
        // Sol 21's earth_date precedes sol 20's - the anomaly shape NASA has
        // produced before (mis-attributed timestamps around sol boundaries).
        DbContext.Photos.AddRange(
            MakePhoto("MONO-OK", 20, new DateTime(2013, 2, 20, 0, 0, 0, DateTimeKind.Utc)),
            MakePhoto("MONO-BAD", 21, new DateTime(2013, 2, 15, 0, 0, 0, DateTimeKind.Utc)));
        await DbContext.SaveChangesAsync();

        var result = await Check.CheckAsync();

        // Exactly one boundary violates, however many photos each sol holds.
        result.OrderingViolations.Should().Be(1);
    }

    [Fact]
    public async Task AdjacentSolsSharingABoundaryDate_AreNotViolations()
    {
        // The everyday production shape: a sol spans two Earth dates, so
        // consecutive sols routinely share the boundary date. The comparison
        // must stay strict - a > to >= regression would fire on thousands of
        // legitimate boundaries nightly.
        var shared = new DateTime(2013, 3, 5, 0, 0, 0, DateTimeKind.Utc);
        DbContext.Photos.AddRange(
            MakePhoto("MONO-EQ-A", 30, shared),
            MakePhoto("MONO-EQ-B", 31, shared),
            MakePhoto("MONO-EQ-C", 31, new DateTime(2013, 3, 6, 0, 0, 0, DateTimeKind.Utc)));
        await DbContext.SaveChangesAsync();

        var result = await Check.CheckAsync();

        result.OrderingViolations.Should().Be(0);
    }

    [Fact]
    public async Task RoversAreComparedIndependently()
    {
        // Rover missions overlap in time: one rover's late sols carry later
        // dates than another rover's early sols. The window must partition by
        // rover so mission boundaries never pair.
        DbContext.Photos.AddRange(
            MakePhoto("MONO-R1", 100, new DateTime(2020, 6, 1, 0, 0, 0, DateTimeKind.Utc), roverId: 1),
            MakePhoto("MONO-R2", 1, new DateTime(2004, 1, 5, 0, 0, 0, DateTimeKind.Utc), roverId: 2));
        await DbContext.SaveChangesAsync();

        var result = await Check.CheckAsync();

        result.OrderingViolations.Should().Be(0);
    }

    [Fact]
    public async Task NullEarthDates_AreCountedSeparately()
    {
        // NULLs silently drop out of the ordering comparison (an all-NULL sol
        // even masks a real violation across it), so they are alarmed on
        // directly rather than left invisible.
        DbContext.Photos.AddRange(
            MakePhoto("MONO-NULL-A", 40, new DateTime(2013, 4, 10, 0, 0, 0, DateTimeKind.Utc)),
            MakePhoto("MONO-NULL-B", 41, earthDate: null));
        await DbContext.SaveChangesAsync();

        var result = await Check.CheckAsync();

        result.NullEarthDates.Should().Be(1);
    }
}
