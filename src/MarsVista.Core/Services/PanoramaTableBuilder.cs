using MarsVista.Core.Data;
using MarsVista.Core.Entities;
using MarsVista.Core.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MarsVista.Core.Services;

/// <summary>
/// Populates the panoramas table by running <see cref="PanoramaDetector"/> over
/// a single (rover, sol) and materializing the presentation values that
/// ToPanoramaResource computes at request time. Used by the scraper's backfill
/// mode and its daily incremental refresh.
/// </summary>
public interface IPanoramaTableBuilder
{
    /// <summary>
    /// Rebuilds all panorama rows for one (rover, sol): deletes the existing rows
    /// and inserts freshly detected ones in a single transaction. Idempotent -
    /// rebuilding an unchanged sol produces identical rows. Returns the number of
    /// panoramas written.
    /// </summary>
    Task<int> RebuildSolAsync(int roverId, int sol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rebuilds every sol in [startSol, endSol] for one rover. Used by the daily
    /// scraper to refresh the sols it just scraped. Returns the total number of
    /// panoramas written across the window.
    /// </summary>
    Task<int> RebuildSolRangeAsync(int roverId, int startSol, int endSol, CancellationToken cancellationToken = default);
}

public class PanoramaTableBuilder : IPanoramaTableBuilder
{
    private readonly MarsVistaDbContext _context;
    private readonly PanoramaDetector _detector;
    private readonly ILogger<PanoramaTableBuilder> _logger;

    public PanoramaTableBuilder(
        MarsVistaDbContext context,
        PanoramaDetector detector,
        ILogger<PanoramaTableBuilder> logger)
    {
        _context = context;
        _detector = detector;
        _logger = logger;
    }

    public async Task<int> RebuildSolAsync(int roverId, int sol, CancellationToken cancellationToken = default)
    {
        // Candidate photos, ordered exactly as the request-time detail path
        // (DetectPanoramaSequenceByIdAsync) so sequence indices - and therefore
        // panorama ids - match the ids the stitch service and ratings resolve against.
        var photos = await _context.Photos
            .Where(p => p.RoverId == roverId &&
                        p.Sol == sol &&
                        p.Site.HasValue &&
                        p.Drive.HasValue &&
                        p.MastAz.HasValue &&
                        p.MastEl.HasValue &&
                        p.SpacecraftClock.HasValue &&
                        PanoramaDetector.PanoramicCameras.Contains(p.Camera.Name))
            .Include(p => p.Rover)
            .Include(p => p.Camera)
            .OrderBy(p => p.RoverId)
            .ThenBy(p => p.Site)
            .ThenBy(p => p.Drive)
            .ThenBy(p => p.SpacecraftClock)
            .ThenBy(p => p.MastEl)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var sequences = _detector.DetectPanoramas(photos, PanoramaDetector.MinPhotosForPanorama);
        var rows = sequences.Select(seq => MapToEntity(seq, roverId, sol)).ToList();

        await WarnOnOrphanedStitchesAsync(roverId, sol, rows, cancellationToken);

        // Idempotent replace: drop the sol's existing rows and insert the fresh
        // set atomically so a reader never sees a half-rebuilt sol. The whole
        // unit runs inside an execution strategy because the API/scraper
        // DbContext enables retry-on-failure, which forbids a bare
        // BeginTransactionAsync - the strategy re-runs the lambda on a transient
        // failure, so the change tracker is cleared each attempt to keep retries
        // clean.
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            _context.ChangeTracker.Clear();

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            await _context.Panoramas
                .Where(p => p.RoverId == roverId && p.Sol == sol)
                .ExecuteDeleteAsync(cancellationToken);

            if (rows.Count > 0)
            {
                _context.Panoramas.AddRange(rows);
                await _context.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        });

        _logger.LogDebug("Rebuilt {Count} panoramas for rover {RoverId} sol {Sol}", rows.Count, roverId, sol);
        return rows.Count;
    }

    public async Task<int> RebuildSolRangeAsync(int roverId, int startSol, int endSol, CancellationToken cancellationToken = default)
    {
        var total = 0;
        for (var sol = startSol; sol <= endSol; sol++)
        {
            total += await RebuildSolAsync(roverId, sol, cancellationToken);
        }
        return total;
    }

    /// <summary>
    /// Warn when a panorama id that an existing stitched_panoramas row points at
    /// no longer appears after a rebuild - the stitched image would be orphaned.
    /// </summary>
    private async Task WarnOnOrphanedStitchesAsync(
        int roverId, int sol, List<Panorama> rows, CancellationToken cancellationToken)
    {
        var priorStitchedIds = await _context.Panoramas
            .Where(p => p.RoverId == roverId && p.Sol == sol)
            .Join(_context.StitchedPanoramas, p => p.PanoramaId, s => s.PanoramaId, (p, s) => p.PanoramaId)
            .ToListAsync(cancellationToken);

        var newIds = rows.Select(r => r.PanoramaId).ToHashSet();
        foreach (var orphaned in priorStitchedIds.Where(id => !newIds.Contains(id)))
        {
            _logger.LogWarning(
                "Rebuild of rover {RoverId} sol {Sol} dropped panorama {PanoramaId}, which a stitched_panoramas row references - the stitched image is now orphaned",
                roverId, sol, orphaned);
        }
    }

    private static Panorama MapToEntity(PanoramaSequence sequence, int roverId, int sol)
    {
        var firstPhoto = sequence.Photos.First();
        var lastPhoto = sequence.Photos.Last();
        var rover = firstPhoto.Rover.Name.ToLowerInvariant();
        var panoramaId = $"pano_{rover}_{sol}_{sequence.Index}";

        var azimuths = sequence.Photos.Select(p => p.MastAz ?? 0).ToList();
        var coverageDegrees = azimuths.Max() - azimuths.Min();

        var uniqueAzimuths = sequence.Photos
            .Select(p => Math.Round(p.MastAz ?? 0))
            .Distinct()
            .OrderBy(a => a)
            .ToList();
        var uniquePositions = uniqueAzimuths.Count;

        float? avgPositionSpacing = null;
        if (uniquePositions > 1)
        {
            var totalSpacing = 0.0;
            for (int i = 1; i < uniqueAzimuths.Count; i++)
            {
                totalSpacing += uniqueAzimuths[i] - uniqueAzimuths[i - 1];
            }
            avgPositionSpacing = (float)(totalSpacing / (uniquePositions - 1));
        }

        var quality = PanoramaDetector.GetQualityTier(coverageDegrees, uniquePositions);

        // Mars time range, normalized so start <= end for reverse-sweep panoramas
        string? marsTimeStart = null;
        string? marsTimeEnd = null;
        TimeSpan parsedStart = default, parsedEnd = default;
        bool hasStart = !string.IsNullOrEmpty(firstPhoto.DateTakenMars) &&
            MarsTimeHelper.TryExtractTimeFromTimestamp(firstPhoto.DateTakenMars, out parsedStart);
        bool hasEnd = !string.IsNullOrEmpty(lastPhoto.DateTakenMars) &&
            MarsTimeHelper.TryExtractTimeFromTimestamp(lastPhoto.DateTakenMars, out parsedEnd);
        if (hasStart) marsTimeStart = MarsTimeHelper.FormatMarsTime(parsedStart);
        if (hasEnd) marsTimeEnd = MarsTimeHelper.FormatMarsTime(parsedEnd);
        if (hasStart && hasEnd && parsedStart > parsedEnd)
        {
            (marsTimeStart, marsTimeEnd) = (marsTimeEnd, marsTimeStart);
        }

        var avgElevation = sequence.Photos.Average(p => p.MastEl ?? 0);

        float? coordinateX = null, coordinateY = null, coordinateZ = null;
        if (!string.IsNullOrEmpty(firstPhoto.Xyz) &&
            MarsTimeHelper.TryParseXYZ(firstPhoto.Xyz, out var parsedXyz))
        {
            coordinateX = parsedXyz.X;
            coordinateY = parsedXyz.Y;
            coordinateZ = parsedXyz.Z;
        }

        return new Panorama
        {
            PanoramaId = panoramaId,
            RoverId = roverId,
            Sol = sol,
            SequenceIndex = sequence.Index,
            CameraId = firstPhoto.CameraId,
            MarsTimeStart = marsTimeStart,
            MarsTimeEnd = marsTimeEnd,
            TotalPhotos = sequence.Photos.Count,
            CoverageDegrees = coverageDegrees,
            AvgElevation = avgElevation,
            UniquePositions = uniquePositions,
            AvgPositionSpacing = avgPositionSpacing,
            QualityTier = quality,
            IsMultiRow = sequence.IsMultiRow,
            ElevationTierCount = sequence.ElevationTierCount,
            AzimuthColumnCount = sequence.AzimuthColumnCount,
            MinElevation = sequence.IsMultiRow ? sequence.MinElevation : null,
            MaxElevation = sequence.IsMultiRow ? sequence.MaxElevation : null,
            Site = firstPhoto.Site,
            Drive = firstPhoto.Drive,
            CoordinateX = coordinateX,
            CoordinateY = coordinateY,
            CoordinateZ = coordinateZ,
            PhotoIds = sequence.Photos.Select(p => p.Id).ToArray(),
        };
    }
}
