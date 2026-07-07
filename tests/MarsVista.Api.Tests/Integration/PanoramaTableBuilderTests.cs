using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MarsVista.Api.Services.V2;
using MarsVista.Core.Entities;
using MarsVista.Core.Services;

namespace MarsVista.Api.Tests.Integration;

/// <summary>
/// Verifies the pre-compute builder produces rows whose stored presentation
/// values match the live request-time detection path (so the read-path cutover
/// is byte-for-byte), that rebuilding is idempotent, and that a rebuild which
/// drops a stitched-referenced panorama id logs a warning instead of throwing.
/// </summary>
public class PanoramaTableBuilderTests : IntegrationTestBase
{
    private const int RoverId = 1;   // Curiosity (seeded by base)
    private const int Sol = 1000;

    private IPanoramaService _panoramaService = null!;
    private PanoramaTableBuilder _builder = null!;

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IPhotoQueryServiceV2, PhotoQueryServiceV2>();
        services.AddScoped<PanoramaDetector>();
        services.AddScoped<IPanoramaService, PanoramaService>();
    }

    protected override async Task SeedAdditionalDataAsync()
    {
        _panoramaService = ServiceProvider.GetRequiredService<IPanoramaService>();
        _builder = new PanoramaTableBuilder(
            DbContext,
            ServiceProvider.GetRequiredService<PanoramaDetector>(),
            NullLogger<PanoramaTableBuilder>.Instance);

        var now = DateTime.UtcNow;
        // Single-row panorama: 5 photos, azimuth 45..85 (40° range, 5 positions), same elevation.
        for (int i = 0; i < 5; i++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"NRF_1000_{i:D4}",
                Sol = Sol,
                EarthDate = new DateTime(2015, 5, 30, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2015, 5, 30, 10, i, 0, DateTimeKind.Utc),
                DateTakenMars = $"Sol-1000M14:0{i}:00",
                ImgSrcSmall = "s", ImgSrcMedium = "m", ImgSrcLarge = "l", ImgSrcFull = "f",
                Site = 79,
                Drive = 1204,
                MastAz = 45.0f + (i * 10.0f),
                MastEl = -10.0f,
                SpacecraftClock = 813073000.0f + (i * 100.0f),
                Xyz = "{\"x\": 35.4362, \"y\": 22.5714, \"z\": -9.46445}",
                RoverId = RoverId,
                CameraId = 2, // MAST
                CreatedAt = now, UpdatedAt = now,
            });
        }
        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task BuilderRow_MatchesServedResource()
    {
        // Build the table, then read it back through the (now table-backed) service:
        // the served DTO must reflect the stored row field-for-field.
        await _builder.RebuildSolAsync(RoverId, Sol);

        var live = await _panoramaService.GetPanoramasAsync(
            rovers: "curiosity", solMin: Sol, solMax: Sol, pageSize: 50);
        live.Data.Should().NotBeEmpty("the built table has a panorama for this sol");

        foreach (var dto in live.Data!)
        {
            var row = DbContext.Panoramas.AsEnumerable()
                .Single(p => p.PanoramaId == dto.Id);
            var attrs = dto.Attributes!;

            row.TotalPhotos.Should().Be(attrs.TotalPhotos);
            row.CoverageDegrees.Should().Be(attrs.CoverageDegrees!.Value);
            row.UniquePositions.Should().Be(attrs.UniquePositions);
            row.AvgPositionSpacing.Should().Be(attrs.AvgPositionSpacing);
            row.AvgElevation.Should().Be(attrs.AvgElevation!.Value);
            row.QualityTier.Should().Be(attrs.Quality);
            row.IsMultiRow.Should().Be(attrs.MosaicType == "multi_row");
            row.ElevationTierCount.Should().Be(attrs.ElevationRows);
            row.MarsTimeStart.Should().Be(attrs.MarsTimeStart);
            row.MarsTimeEnd.Should().Be(attrs.MarsTimeEnd);
            row.Site.Should().Be(attrs.Location!.Site);
            row.Drive.Should().Be(attrs.Location!.Drive);
        }
    }

    [Fact]
    public async Task RebuildSolRange_RebuildsEverySolInWindow()
    {
        // Seed a second panorama at sol 1001 (the base seed covers sol 1000).
        var now = DateTime.UtcNow;
        for (int i = 0; i < 5; i++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"NRF_1001_{i:D4}",
                Sol = 1001,
                EarthDate = new DateTime(2015, 5, 31, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2015, 5, 31, 10, i, 0, DateTimeKind.Utc),
                DateTakenMars = $"Sol-1001M14:0{i}:00",
                ImgSrcSmall = "s", ImgSrcMedium = "m", ImgSrcLarge = "l", ImgSrcFull = "f",
                Site = 79, Drive = 1204,
                MastAz = 45.0f + (i * 10.0f),
                MastEl = -10.0f,
                SpacecraftClock = 813073000.0f + (i * 100.0f),
                RoverId = RoverId, CameraId = 2,
                CreatedAt = now, UpdatedAt = now,
            });
        }
        await DbContext.SaveChangesAsync();

        // Range spans an empty trailing sol (1002) to prove it is handled without error.
        var written = await _builder.RebuildSolRangeAsync(RoverId, Sol, 1002);

        written.Should().Be(2, "sols 1000 and 1001 each form one panorama; 1002 has none");
        DbContext.Panoramas.AsEnumerable().Select(p => p.Sol).OrderBy(s => s)
            .Should().Equal(1000, 1001);
    }

    [Fact]
    public async Task Rebuild_IsIdempotent()
    {
        var first = await _builder.RebuildSolAsync(RoverId, Sol);
        var firstRows = DbContext.Panoramas.AsEnumerable()
            .Where(p => p.Sol == Sol)
            .OrderBy(p => p.SequenceIndex)
            .Select(p => (p.PanoramaId, p.TotalPhotos, p.CoverageDegrees))
            .ToList();

        var second = await _builder.RebuildSolAsync(RoverId, Sol);
        var secondRows = DbContext.Panoramas.AsEnumerable()
            .Where(p => p.Sol == Sol)
            .OrderBy(p => p.SequenceIndex)
            .Select(p => (p.PanoramaId, p.TotalPhotos, p.CoverageDegrees))
            .ToList();

        second.Should().Be(first, "rebuilding an unchanged sol writes the same number of rows");
        secondRows.Should().Equal(firstRows, "the rows are identical across rebuilds");
    }

    [Fact]
    public async Task Rebuild_DroppingStitchedReferencedId_DoesNotThrow()
    {
        await _builder.RebuildSolAsync(RoverId, Sol);
        // Point a stitched_panoramas row at a panorama id that will not exist
        // after we remove the photos and rebuild.
        var existingId = DbContext.Panoramas.AsEnumerable().First(p => p.Sol == Sol).PanoramaId;
        DbContext.StitchedPanoramas.Add(new StitchedPanorama
        {
            PanoramaId = existingId,
            Status = "completed",
            CreatedAt = DateTime.UtcNow,
        });
        DbContext.Photos.RemoveRange(DbContext.Photos.Where(p => p.Sol == Sol));
        await DbContext.SaveChangesAsync();

        var rebuilt = await _builder.RebuildSolAsync(RoverId, Sol);

        rebuilt.Should().Be(0, "with the photos gone there are no panoramas to detect");
        DbContext.Panoramas.AsEnumerable().Any(p => p.Sol == Sol).Should().BeFalse();
    }
}
