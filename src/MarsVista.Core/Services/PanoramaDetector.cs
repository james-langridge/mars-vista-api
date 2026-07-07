using MarsVista.Core.Entities;
using Microsoft.Extensions.Logging;

namespace MarsVista.Core.Services;

/// <summary>
/// Represents a detected panorama sequence: a set of photos that together form
/// a single- or multi-row panoramic observation.
/// </summary>
public class PanoramaSequence
{
    public List<Photo> Photos { get; set; } = new();
    public int Index { get; set; }
    public bool IsMultiRow { get; set; }
    public int ElevationTierCount { get; set; } = 1;
    public int AzimuthColumnCount { get; set; }
    public float MinElevation { get; set; }
    public float MaxElevation { get; set; }
}

/// <summary>
/// Pure panorama-detection logic shared by the API (request-time detection) and
/// the scraper (pre-compute into the panoramas table). Detects panoramic
/// sequences from candidate photos based on location, time contiguity, and
/// camera telemetry. Depends only on Core entities and ILogger - no DbContext,
/// no DTOs.
/// </summary>
public class PanoramaDetector
{
    // Panorama detection parameters
    public const float MinAzimuthRangeDegrees = 30.0f; // At least 30 degrees coverage
    public const int MinPhotosForPanorama = 3; // At least 3 photos
    public const int MinUniquePositions = 3; // At least 3 unique azimuth positions (stitchable)
    public const float MaxTimeDeltaSeconds = 300.0f; // Max 5 minutes between photos

    // Multi-row mosaic detection parameters
    public const float ElevationTierGapDegrees = 5.0f; // Min gap between sorted elevations to start a new tier
    public const float MinGridCompleteness = 0.40f; // Multi-row mosaic must fill 40% of grid cells

    // Only cameras designed for panoramic imaging — excludes spectrometers (ChemCam, SuperCam RMI),
    // hazard cameras (fixed FOV), arm cameras (MAHLI, SHERLOC), and descent/EDL cameras
    public static readonly HashSet<string> PanoramicCameras = new(StringComparer.OrdinalIgnoreCase)
    {
        "MAST", "NAVCAM",                               // Curiosity
        "MCZ_LEFT", "MCZ_RIGHT", "NAVCAM_LEFT", "NAVCAM_RIGHT", // Perseverance
        "PANCAM"                                         // Opportunity, Spirit
    };

    private readonly ILogger _logger;

    public PanoramaDetector(ILogger<PanoramaDetector> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Optimized panorama detection that processes sol batches.
    /// Groups photos by location/camera, splits on time gaps, then classifies
    /// as single-row or multi-row based on elevation tier clustering.
    /// </summary>
    public List<PanoramaSequence> DetectPanoramasOptimized(List<Photo> photos, int minPhotos, ref int panoramaIndex)
    {
        var panoramas = new List<PanoramaSequence>();

        // Group by rover, sol, site, drive, and camera
        var allGroups = photos
            .GroupBy(p => new
            {
                p.RoverId,
                p.Sol,
                Site = p.Site ?? 0,
                Drive = p.Drive ?? 0,
                p.CameraId
            })
            .ToList();

        _logger.LogDebug("DetectPanoramasOptimized: {PhotoCount} photos, {GroupCount} total groups, minPhotos={MinPhotos}",
            photos.Count, allGroups.Count, minPhotos);

        var groups = allGroups
            .Where(g => g.Count() >= minPhotos)
            .OrderBy(g => g.Key.RoverId)
            .ThenBy(g => g.Key.Sol)
            .ThenBy(g => g.Key.Site)
            .ThenBy(g => g.Key.Drive)
            .ThenBy(g => g.Key.CameraId)
            .ToList();

        _logger.LogDebug("DetectPanoramasOptimized: {QualifiedGroupCount} groups with >= {MinPhotos} photos",
            groups.Count, minPhotos);

        foreach (var group in groups)
        {
            var groupPhotos = group.OrderBy(p => p.SpacecraftClock).ThenBy(p => p.MastEl).ToList();

            _logger.LogDebug("Processing group: CameraId={CameraId}, Site={Site}, Drive={Drive}, {PhotoCount} photos",
                group.Key.CameraId, group.Key.Site, group.Key.Drive, groupPhotos.Count);

            // Step 1: Split into time-contiguous blocks (no elevation check)
            var blocks = BuildTimeContiguousBlocks(groupPhotos);

            // Step 2: For each block, cluster elevations and classify
            foreach (var block in blocks)
            {
                if (block.Count < minPhotos)
                    continue;

                var tiers = ClusterElevationTiers(block);

                if (tiers.Count <= 1)
                {
                    // Single-row: validate with existing criteria
                    if (IsValidPanorama(block))
                    {
                        var uniqueAz = block
                            .Select(p => Math.Round(p.MastAz ?? 0))
                            .Distinct()
                            .Count();

                        panoramas.Add(new PanoramaSequence
                        {
                            Photos = new List<Photo>(block),
                            Index = panoramaIndex++,
                            IsMultiRow = false,
                            ElevationTierCount = 1,
                            AzimuthColumnCount = uniqueAz
                        });
                    }
                }
                else
                {
                    // Multi-row candidate: validate with mosaic criteria
                    if (IsValidMosaic(block, tiers, out var mosaicMetrics))
                    {
                        var elevations = block.Select(p => p.MastEl ?? 0).ToList();

                        panoramas.Add(new PanoramaSequence
                        {
                            Photos = new List<Photo>(block),
                            Index = panoramaIndex++,
                            IsMultiRow = true,
                            ElevationTierCount = tiers.Count,
                            AzimuthColumnCount = mosaicMetrics!.MaxColumnsPerTier,
                            MinElevation = elevations.Min(),
                            MaxElevation = elevations.Max()
                        });
                    }
                    else
                    {
                        // Fallback: try each tier as a single-row panorama.
                        // A block with multiple elevation tiers that fails mosaic validation
                        // may still contain valid single-row panoramas within individual tiers.
                        foreach (var tier in tiers)
                        {
                            var tierPhotos = block
                                .Where(p => GetElevationTier(p.MastEl ?? 0, tiers) == tier)
                                .OrderBy(p => p.SpacecraftClock)
                                .ThenBy(p => p.MastEl)
                                .ToList();

                            if (tierPhotos.Count >= minPhotos && IsValidPanorama(tierPhotos))
                            {
                                var uniqueAz = tierPhotos
                                    .Select(p => Math.Round(p.MastAz ?? 0))
                                    .Distinct()
                                    .Count();

                                panoramas.Add(new PanoramaSequence
                                {
                                    Photos = new List<Photo>(tierPhotos),
                                    Index = panoramaIndex++,
                                    IsMultiRow = false,
                                    ElevationTierCount = 1,
                                    AzimuthColumnCount = uniqueAz
                                });
                            }
                        }
                    }
                }
            }
        }

        return panoramas;
    }

    /// <summary>
    /// Detect panorama sequences from a list of photos (single sol, index from 0).
    /// </summary>
    public List<PanoramaSequence> DetectPanoramas(List<Photo> photos, int minPhotos)
    {
        var panoramaIndex = 0;
        return DetectPanoramasOptimized(photos, minPhotos, ref panoramaIndex);
    }

    /// <summary>
    /// Split photos into time-contiguous blocks using only the time gap threshold.
    /// No elevation check — multi-row mosaics sweep across elevation tiers continuously.
    /// </summary>
    private List<List<Photo>> BuildTimeContiguousBlocks(List<Photo> orderedPhotos)
    {
        var blocks = new List<List<Photo>>();
        var current = new List<Photo>();

        for (int i = 0; i < orderedPhotos.Count; i++)
        {
            if (current.Count == 0)
            {
                current.Add(orderedPhotos[i]);
            }
            else
            {
                var timeDelta = (orderedPhotos[i].SpacecraftClock ?? 0) - (current[^1].SpacecraftClock ?? 0);
                if (timeDelta <= MaxTimeDeltaSeconds && timeDelta >= 0)
                {
                    current.Add(orderedPhotos[i]);
                }
                else
                {
                    blocks.Add(current);
                    current = new List<Photo> { orderedPhotos[i] };
                }
            }
        }

        if (current.Count > 0)
            blocks.Add(current);

        return blocks;
    }

    /// <summary>
    /// Cluster unique elevations into tiers using adaptive gap-based grouping.
    /// Sorts unique rounded elevations, splits when consecutive gap >= ElevationTierGapDegrees.
    /// Returns the center value of each tier.
    /// </summary>
    private static List<float> ClusterElevationTiers(List<Photo> photos)
    {
        var uniqueElevations = photos
            .Select(p => MathF.Round(p.MastEl ?? 0))
            .Distinct()
            .OrderBy(e => e)
            .ToList();

        if (uniqueElevations.Count == 0)
            return new List<float>();

        var tiers = new List<float>();
        var currentTier = new List<float> { uniqueElevations[0] };

        for (int i = 1; i < uniqueElevations.Count; i++)
        {
            if (uniqueElevations[i] - uniqueElevations[i - 1] >= ElevationTierGapDegrees)
            {
                // Gap found — finalize current tier, start new one
                tiers.Add(currentTier.Average());
                currentTier = new List<float> { uniqueElevations[i] };
            }
            else
            {
                currentTier.Add(uniqueElevations[i]);
            }
        }

        tiers.Add(currentTier.Average());
        return tiers;
    }

    /// <summary>
    /// Map a photo's elevation to its nearest tier center value.
    /// </summary>
    private static float GetElevationTier(float elevation, List<float> tiers)
    {
        return tiers.OrderBy(t => Math.Abs(t - elevation)).First();
    }

    private record MosaicMetrics(int MaxColumnsPerTier, int FilledCells, float Completeness);

    /// <summary>
    /// Validate a multi-row mosaic: azimuth range >= 30°, unique positions >= 3,
    /// and grid completeness >= 40%. Returns computed grid metrics on success.
    /// </summary>
    private bool IsValidMosaic(List<Photo> photos, List<float> tiers, out MosaicMetrics? metrics)
    {
        metrics = null;

        if (photos.Count < MinPhotosForPanorama)
            return false;

        var azimuths = photos.Select(p => p.MastAz ?? 0).ToList();
        var azimuthRange = azimuths.Max() - azimuths.Min();

        if (azimuthRange < MinAzimuthRangeDegrees)
        {
            _logger.LogDebug("IsValidMosaic: FAILED - azimuth range {Range}° < {Min}°", azimuthRange, MinAzimuthRangeDegrees);
            return false;
        }

        var uniquePositions = photos
            .Select(p => Math.Round(p.MastAz ?? 0))
            .Distinct()
            .Count();

        if (uniquePositions < MinUniquePositions)
        {
            _logger.LogDebug("IsValidMosaic: FAILED - {UniquePos} unique positions < {Min}", uniquePositions, MinUniquePositions);
            return false;
        }

        // Compute per-tier column counts in one pass
        var tierColumnCounts = tiers.Select(tier =>
        {
            var tierPhotos = photos.Where(p => GetElevationTier(p.MastEl ?? 0, tiers) == tier);
            return tierPhotos.Select(p => Math.Round(p.MastAz ?? 0)).Distinct().Count();
        }).ToList();

        var maxColumnsPerTier = tierColumnCounts.Max();

        if (maxColumnsPerTier < MinUniquePositions)
        {
            _logger.LogDebug("IsValidMosaic: FAILED - max {Cols} columns per tier < {Min} required for stitching",
                maxColumnsPerTier, MinUniquePositions);
            return false;
        }

        // At least 2 tiers must have meaningful column counts for a real multi-row mosaic.
        // One dense tier + sparse single-shot tiers is not a mosaic — it's a sweep with
        // unrelated observations at different elevations (e.g., bracketed bursts).
        var tiersWithEnoughColumns = tierColumnCounts.Count(c => c >= MinUniquePositions);
        if (tiersWithEnoughColumns < 2)
        {
            _logger.LogDebug("IsValidMosaic: FAILED - only {Count} tier(s) have >= {Min} columns (need at least 2)",
                tiersWithEnoughColumns, MinUniquePositions);
            return false;
        }

        // Grid completeness: how many (tier, azimuth) cells are filled vs total possible
        var totalCells = tiers.Count * maxColumnsPerTier;
        var filledCells = tierColumnCounts.Sum();
        var completeness = (float)filledCells / totalCells;

        if (completeness < MinGridCompleteness)
        {
            _logger.LogDebug("IsValidMosaic: FAILED - grid completeness {Completeness:P0} < {Min:P0}",
                completeness, MinGridCompleteness);
            return false;
        }

        _logger.LogDebug("IsValidMosaic: PASSED - {Count} photos, {Tiers} tiers, {Range}° azimuth, {Completeness:P0} grid fill",
            photos.Count, tiers.Count, azimuthRange, completeness);
        metrics = new MosaicMetrics(maxColumnsPerTier, filledCells, completeness);
        return true;
    }

    /// <summary>
    /// Check if a sequence qualifies as a panorama
    /// </summary>
    private bool IsValidPanorama(List<Photo> photos)
    {
        if (photos.Count < MinPhotosForPanorama)
        {
            _logger.LogDebug("IsValidPanorama: FAILED - {Count} photos < {Min} required",
                photos.Count, MinPhotosForPanorama);
            return false;
        }

        // Check azimuth range
        var azimuths = photos.Select(p => p.MastAz ?? 0).ToList();
        var azimuthRange = azimuths.Max() - azimuths.Min();

        if (azimuthRange < MinAzimuthRangeDegrees)
        {
            _logger.LogDebug("IsValidPanorama: FAILED - azimuth range {Range}° < {Min}° required",
                azimuthRange, MinAzimuthRangeDegrees);
            return false;
        }

        // Count unique positions (round to nearest degree to handle float precision)
        var uniquePositions = photos
            .Select(p => Math.Round(p.MastAz ?? 0))
            .Distinct()
            .Count();

        if (uniquePositions < MinUniquePositions)
        {
            _logger.LogDebug("IsValidPanorama: FAILED - {UniquePos} unique positions < {Min} required",
                uniquePositions, MinUniquePositions);
            return false;
        }

        _logger.LogDebug("IsValidPanorama: PASSED - {Count} photos, {Range}° coverage, {UniquePos} unique positions",
            photos.Count, azimuthRange, uniquePositions);
        return true;
    }

    /// <summary>
    /// Determine quality tier based on coverage and unique positions
    /// </summary>
    public static string GetQualityTier(float coverageDegrees, int uniquePositions)
    {
        if (coverageDegrees >= 300 && uniquePositions >= 10) return "full";
        if (coverageDegrees >= 200 && uniquePositions >= 7) return "wide";
        if (coverageDegrees >= 120 && uniquePositions >= 5) return "half";
        return "partial";
    }
}
