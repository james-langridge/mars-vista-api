using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MarsVista.Core.Entities;

namespace MarsVista.Api.Tests.Integration;

/// <summary>
/// Proves the panoramas table was created by the migration, round-trips the
/// int[] photo_ids array, and enforces the two uniqueness constraints the
/// pre-compute builder and stitch/rating resolution depend on.
/// </summary>
public class PanoramaTableMigrationTests : IntegrationTestBase
{
    private Panorama MakePanorama(string panoramaId, int sequenceIndex) => new()
    {
        PanoramaId = panoramaId,
        RoverId = 1,
        Sol = 1000,
        SequenceIndex = sequenceIndex,
        CameraId = 2,
        MarsTimeStart = "12:00:00",
        MarsTimeEnd = "12:04:00",
        TotalPhotos = 12,
        CoverageDegrees = 210f,
        AvgElevation = -5f,
        UniquePositions = 8,
        AvgPositionSpacing = 26f,
        QualityTier = "wide",
        IsMultiRow = false,
        ElevationTierCount = 1,
        AzimuthColumnCount = 8,
        PhotoIds = new[] { 10, 20, 30 },
        DetectedAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task RoundTripsARow_IncludingPhotoIdsArray()
    {
        DbContext.Panoramas.Add(MakePanorama("pano_curiosity_1000_0", 0));
        await DbContext.SaveChangesAsync();

        var row = await DbContext.Panoramas.AsNoTracking()
            .SingleAsync(p => p.PanoramaId == "pano_curiosity_1000_0");

        row.PhotoIds.Should().Equal(10, 20, 30);
        row.CoverageDegrees.Should().Be(210f);
        row.QualityTier.Should().Be("wide");
        row.Site.Should().BeNull("single-row panorama without location leaves site null");
    }

    [Fact]
    public async Task RejectsDuplicatePanoramaId()
    {
        DbContext.Panoramas.Add(MakePanorama("pano_curiosity_1000_0", 0));
        await DbContext.SaveChangesAsync();

        // Same panorama_id, different sequence index -> panorama_id unique index must reject.
        DbContext.Panoramas.Add(MakePanorama("pano_curiosity_1000_0", 1));
        var act = async () => await DbContext.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task RejectsDuplicateRoverSolSequenceIndex()
    {
        DbContext.Panoramas.Add(MakePanorama("pano_curiosity_1000_0", 0));
        await DbContext.SaveChangesAsync();

        // Different panorama_id but same (rover, sol, sequence index) -> canonical
        // identity unique index must reject.
        DbContext.Panoramas.Add(MakePanorama("pano_curiosity_1000_0_dup", 0));
        var act = async () => await DbContext.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }
}
