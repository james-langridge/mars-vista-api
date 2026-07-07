using MarsVista.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MarsVista.Core.Services;

/// <summary>
/// Summary of a backfill run.
/// </summary>
public record PanoramaBackfillSummary(int TotalPairs, int Processed, int PanoramasWritten, int Failures);

/// <summary>
/// One-off backfill of the panoramas table: enumerates every (rover, sol) that
/// has candidate panorama photos and rebuilds each. Idempotent and resumable -
/// re-running simply rebuilds every sol again to the same result. Run from the
/// scraper's PANORAMA_BACKFILL mode.
/// </summary>
public class PanoramaBackfillRunner
{
    private readonly MarsVistaDbContext _context;
    private readonly IPanoramaTableBuilder _builder;
    private readonly ILogger<PanoramaBackfillRunner> _logger;

    public PanoramaBackfillRunner(
        MarsVistaDbContext context,
        IPanoramaTableBuilder builder,
        ILogger<PanoramaBackfillRunner> logger)
    {
        _context = context;
        _builder = builder;
        _logger = logger;
    }

    public async Task<PanoramaBackfillSummary> RunAsync(
        IReadOnlyList<int>? roverIds = null,
        CancellationToken cancellationToken = default)
    {
        var candidates = _context.Photos
            .Where(p => p.Site.HasValue &&
                        p.Drive.HasValue &&
                        p.MastAz.HasValue &&
                        p.MastEl.HasValue &&
                        p.SpacecraftClock.HasValue &&
                        PanoramaDetector.PanoramicCameras.Contains(p.Camera.Name));

        if (roverIds is { Count: > 0 })
        {
            candidates = candidates.Where(p => roverIds.Contains(p.RoverId));
        }

        var pairs = await candidates
            .Select(p => new { p.RoverId, p.Sol })
            .Distinct()
            .OrderBy(x => x.RoverId)
            .ThenBy(x => x.Sol)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Panorama backfill starting: {PairCount} (rover, sol) pairs to process", pairs.Count);

        var processed = 0;
        var panoramas = 0;
        var failures = 0;

        foreach (var pair in pairs)
        {
            try
            {
                panoramas += await _builder.RebuildSolAsync(pair.RoverId, pair.Sol, cancellationToken);
                processed++;

                if (processed % 100 == 0)
                {
                    _logger.LogInformation(
                        "Backfill progress: {Processed}/{Total} sols, {Panoramas} panoramas written",
                        processed, pairs.Count, panoramas);
                }
            }
            catch (Exception ex)
            {
                // One bad sol must not abort the whole backfill; it can be retried by
                // simply re-running (idempotent per sol).
                failures++;
                _logger.LogError(ex, "Backfill failed for rover {RoverId} sol {Sol}", pair.RoverId, pair.Sol);
            }
        }

        _logger.LogInformation(
            "Panorama backfill complete: {Processed}/{Total} sols processed, {Panoramas} panoramas written, {Failures} failures",
            processed, pairs.Count, panoramas, failures);

        return new PanoramaBackfillSummary(pairs.Count, processed, panoramas, failures);
    }
}
