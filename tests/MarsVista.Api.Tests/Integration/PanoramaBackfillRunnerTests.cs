using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MarsVista.Core.Entities;
using MarsVista.Core.Services;

namespace MarsVista.Api.Tests.Integration;

/// <summary>
/// Verifies the backfill runner enumerates every (rover, sol) with candidate
/// panorama photos and rebuilds each, honouring an optional rover filter. The
/// runner touches only the database - there are no NASA HTTP calls in this path.
/// </summary>
public class PanoramaBackfillRunnerTests : IntegrationTestBase
{
    private PanoramaBackfillRunner _runner = null!;

    protected override async Task SeedAdditionalDataAsync()
    {
        var detector = new PanoramaDetector(NullLogger<PanoramaDetector>.Instance);
        var builder = new PanoramaTableBuilder(DbContext, detector, NullLogger<PanoramaTableBuilder>.Instance);
        _runner = new PanoramaBackfillRunner(DbContext, builder, NullLogger<PanoramaBackfillRunner>.Instance);

        var now = DateTime.UtcNow;

        // A detectable panorama on Curiosity (rover 1) sol 1000.
        AddPanoramaPhotos(roverId: 1, cameraId: 2, sol: 1000, prefix: "CUR1000");
        // A second on Curiosity sol 1001.
        AddPanoramaPhotos(roverId: 1, cameraId: 2, sol: 1001, prefix: "CUR1001");
        // One on Perseverance (rover 2) sol 50 - camera 3 is NAVCAM (panoramic).
        AddPanoramaPhotos(roverId: 2, cameraId: 3, sol: 50, prefix: "PER50");

        // A non-candidate photo (no telemetry) must not create a pair.
        DbContext.Photos.Add(new Photo
        {
            NasaId = "NO_TELEMETRY", Sol = 2000,
            EarthDate = new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DateTakenUtc = new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ImgSrcSmall = "s", ImgSrcMedium = "m", ImgSrcLarge = "l", ImgSrcFull = "f",
            RoverId = 1, CameraId = 2,
            CreatedAt = now, UpdatedAt = now,
        });

        await DbContext.SaveChangesAsync();
    }

    private void AddPanoramaPhotos(int roverId, int cameraId, int sol, string prefix)
    {
        var now = DateTime.UtcNow;
        for (int i = 0; i < 5; i++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"{prefix}_{i:D4}",
                Sol = sol,
                EarthDate = new DateTime(2015, 5, 30, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2015, 5, 30, 10, i, 0, DateTimeKind.Utc),
                DateTakenMars = $"Sol-{sol}M14:0{i}:00",
                ImgSrcSmall = "s", ImgSrcMedium = "m", ImgSrcLarge = "l", ImgSrcFull = "f",
                Site = 79, Drive = 1204,
                MastAz = 45.0f + (i * 10.0f),
                MastEl = -10.0f,
                SpacecraftClock = 813073000.0f + (i * 100.0f),
                RoverId = roverId, CameraId = cameraId,
                CreatedAt = now, UpdatedAt = now,
            });
        }
    }

    [Fact]
    public async Task RunAsync_RebuildsEveryCandidatePair()
    {
        var summary = await _runner.RunAsync();

        summary.TotalPairs.Should().Be(3, "three (rover, sol) pairs have candidate photos");
        summary.Processed.Should().Be(3);
        summary.Failures.Should().Be(0);
        summary.PanoramasWritten.Should().Be(3, "each pair forms one single-row panorama");

        DbContext.Panoramas.AsEnumerable().Select(p => p.Sol).OrderBy(s => s)
            .Should().Equal(50, 1000, 1001);
    }

    [Fact]
    public async Task RunAsync_WithRoverFilter_OnlyProcessesThatRover()
    {
        var summary = await _runner.RunAsync(new[] { 2 });

        summary.TotalPairs.Should().Be(1, "only Perseverance (rover 2) is in scope");
        summary.PanoramasWritten.Should().Be(1);
        DbContext.Panoramas.AsEnumerable().Should().OnlyContain(p => p.RoverId == 2);
    }
}
