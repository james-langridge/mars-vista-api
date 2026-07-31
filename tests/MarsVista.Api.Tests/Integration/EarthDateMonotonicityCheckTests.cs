using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MarsVista.Core.Entities;
using MarsVista.Core.Services;

namespace MarsVista.Api.Tests.Integration;

/// <summary>
/// The daily scrape runs this check as a tripwire for the sol/earth_date
/// ordering invariant the API's sol-first earth_date sort depends on. These
/// tests prove the check actually fires on out-of-order data - the failure
/// mode it guards against is precisely a check that stays silent.
/// </summary>
public class EarthDateMonotonicityCheckTests : IntegrationTestBase
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IEarthDateMonotonicityCheck, EarthDateMonotonicityCheck>();
    }

    private Photo MakePhoto(string nasaId, int sol, DateTime earthDate) => new()
    {
        NasaId = nasaId, ImgSrcFull = "x", ImgSrcLarge = "x", ImgSrcMedium = "x", ImgSrcSmall = "x",
        Sol = sol, EarthDate = earthDate, DateTakenUtc = earthDate,
        SampleType = "Full", RoverId = 1, CameraId = 1,
        CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task ConcordantData_ReportsZeroViolations()
    {
        DbContext.Photos.AddRange(
            MakePhoto("MONO-A", 10, new DateTime(2013, 1, 10, 0, 0, 0, DateTimeKind.Utc)),
            MakePhoto("MONO-B", 11, new DateTime(2013, 1, 11, 0, 0, 0, DateTimeKind.Utc)),
            MakePhoto("MONO-C", 12, new DateTime(2013, 1, 12, 0, 0, 0, DateTimeKind.Utc)));
        await DbContext.SaveChangesAsync();

        var check = ServiceProvider.GetRequiredService<IEarthDateMonotonicityCheck>();

        (await check.CountViolationsAsync()).Should().Be(0);
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

        var check = ServiceProvider.GetRequiredService<IEarthDateMonotonicityCheck>();

        (await check.CountViolationsAsync()).Should().BeGreaterThan(0);
    }
}
