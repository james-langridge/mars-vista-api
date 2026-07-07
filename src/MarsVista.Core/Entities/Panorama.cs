namespace MarsVista.Core.Entities;

/// <summary>
/// A pre-computed panorama sequence. Materializes the result of
/// PanoramaDetector so the /api/v2/panoramas endpoints can filter, sort, and
/// paginate at the database level instead of re-detecting from the photos table
/// on every request. Rebuilt per (rover, sol) by the scraper; rows for a sol are
/// deleted and re-inserted idempotently.
///
/// Stored derived fields (coverage, quality tier, mosaic geometry, mars times)
/// are the presentation values ToPanoramaResource would compute at request time,
/// including the reverse-sweep mars-time normalization, so the table renders
/// byte-for-byte with the previous detection path.
/// </summary>
public class Panorama
{
    public int Id { get; set; }

    /// <summary>
    /// Stable identifier "pano_{rover}_{sol}_{sequenceIndex}". The stitch service
    /// and panorama ratings resolve against this string; it must match the IDs the
    /// request-time detection produced (rover-scoped sequence index).
    /// </summary>
    public string PanoramaId { get; set; } = string.Empty;

    public int RoverId { get; set; }
    public Rover Rover { get; set; } = null!;

    public int Sol { get; set; }

    /// <summary>Index of this panorama within its (rover, sol), starting at 0.</summary>
    public int SequenceIndex { get; set; }

    public int CameraId { get; set; }
    public Camera Camera { get; set; } = null!;

    // Presentation fields (normalized so start <= end for reverse sweeps)
    public string? MarsTimeStart { get; set; }
    public string? MarsTimeEnd { get; set; }

    public int TotalPhotos { get; set; }
    public float CoverageDegrees { get; set; }
    public float AvgElevation { get; set; }
    public int UniquePositions { get; set; }
    public float? AvgPositionSpacing { get; set; }

    /// <summary>full / wide / half / partial (PanoramaDetector.GetQualityTier).</summary>
    public string QualityTier { get; set; } = string.Empty;

    // Mosaic geometry
    public bool IsMultiRow { get; set; }
    public int ElevationTierCount { get; set; } = 1;
    public int AzimuthColumnCount { get; set; }

    /// <summary>Elevation extremes; populated only for multi-row mosaics.</summary>
    public float? MinElevation { get; set; }
    public float? MaxElevation { get; set; }

    // Location (from the first photo of the sequence)
    public int? Site { get; set; }
    public int? Drive { get; set; }
    public float? CoordinateX { get; set; }
    public float? CoordinateY { get; set; }
    public float? CoordinateZ { get; set; }

    /// <summary>Constituent photo ids, used to load photos for the detail endpoint.</summary>
    public int[] PhotoIds { get; set; } = Array.Empty<int>();

    public DateTime DetectedAt { get; set; }
}
