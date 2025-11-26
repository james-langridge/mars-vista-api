namespace MarsVista.Scraper.Tests.SampleData;

/// <summary>
/// Sample NASA API responses for unit testing.
/// These represent real data structures from NASA's Mars rover APIs.
/// </summary>
public static class SampleNasaResponses
{
    // ============================================================================
    // CURIOSITY SAMPLES
    // ============================================================================

    /// <summary>
    /// Full Curiosity photo with all fields populated including subframe_rect dimensions.
    /// </summary>
    public const string CuriosityFullPhoto = """
    {
        "id": 123456,
        "sol": 4100,
        "instrument": "MAST_LEFT",
        "https_url": "https://mars.nasa.gov/msl-raw-images/proj/msl/redops/ods/surface/sol/04100/opgs/edr/mcam/4100ML0001234567890123456789.jpg",
        "date_taken": "2024-01-15T10:30:00Z",
        "camera_vector": "(0.123,0.456,0.789)",
        "camera_position": "(1.0,2.0,3.0)",
        "camera_model_type": "CAHVORE",
        "site": 95,
        "drive": 1234,
        "xyz": "(1.5,2.5,3.5)",
        "title": "Mars Surface Image",
        "description": "A view of the Martian surface",
        "image_credit": "NASA/JPL-Caltech/MSSS",
        "extended": {
            "subframe_rect": "(1,1,1024,1024)",
            "sample_type": "full",
            "lmst": "Sol-04100M14:30:00.000",
            "mast_az": "180.5",
            "mast_el": "-15.2",
            "filter_name": "FILTER_0"
        }
    }
    """;

    /// <summary>
    /// Curiosity thumbnail that should be skipped during scraping.
    /// </summary>
    public const string CuriosityThumbnail = """
    {
        "id": 123457,
        "sol": 4100,
        "instrument": "NAVCAM",
        "https_url": "https://mars.nasa.gov/msl-raw-images/proj/msl/redops/ods/surface/sol/04100/opgs/edr/ncam/thumbnail.jpg",
        "date_taken": "2024-01-15T10:30:00Z",
        "extended": {
            "subframe_rect": "(1,1,160,144)",
            "sample_type": "thumbnail",
            "lmst": "Sol-04100M14:30:00.000"
        }
    }
    """;

    /// <summary>
    /// Curiosity photo without subframe_rect - should infer dimensions from sample_type.
    /// </summary>
    public const string CuriosityNoSubframeRect = """
    {
        "id": 123458,
        "sol": 4100,
        "instrument": "CHEMCAM",
        "https_url": "https://mars.nasa.gov/msl-raw-images/proj/msl/redops/ods/surface/sol/04100/opgs/edr/ccam/image.jpg",
        "date_taken": "2024-01-15T10:30:00Z",
        "extended": {
            "sample_type": "chemcam prc",
            "lmst": "Sol-04100M14:30:00.000"
        }
    }
    """;

    // ============================================================================
    // PERSEVERANCE SAMPLES
    // ============================================================================

    /// <summary>
    /// Full Perseverance photo with dimension field (correct way to get dimensions).
    /// </summary>
    public const string PerseveranceFullPhoto = """
    {
        "imageid": 987654,
        "sol": 1000,
        "title": "Mars 2020 Surface Image",
        "caption": "A stunning view of Mars",
        "credit": "NASA/JPL-Caltech",
        "date_taken": "2024-01-15T10:30:00Z",
        "sample_type": "full",
        "site": 25,
        "drive": 567,
        "camera": {
            "instrument": "NAVCAM_LEFT"
        },
        "image_files": {
            "full_res": "https://mars.nasa.gov/mars2020-raw-images/pub/ods/surface/sol/01000/ids/edr/browse/ncam/full.jpg",
            "small": "https://mars.nasa.gov/mars2020-raw-images/pub/ods/surface/sol/01000/ids/edr/browse/ncam/small.jpg",
            "medium": "https://mars.nasa.gov/mars2020-raw-images/pub/ods/surface/sol/01000/ids/edr/browse/ncam/medium.jpg",
            "large": "https://mars.nasa.gov/mars2020-raw-images/pub/ods/surface/sol/01000/ids/edr/browse/ncam/large.jpg"
        },
        "extended": {
            "dimension": "(1648,1200)",
            "scaleFactor": 1,
            "subframeRect": "(1,1,1648,1200)",
            "lmst": "Sol-01000M14:30:00.000",
            "mast_az": "90.0",
            "mast_el": "-10.5",
            "filter_name": "FILTER_LEFT"
        }
    }
    """;

    /// <summary>
    /// Perseverance photo with scaleFactor that should NOT be used as width.
    /// This is the bug we're testing for!
    /// </summary>
    public const string PerseveranceWithScaleFactor = """
    {
        "imageid": 987655,
        "sol": 1000,
        "date_taken": "2024-01-15T10:30:00Z",
        "sample_type": "full",
        "camera": {
            "instrument": "MASTCAMZ_LEFT"
        },
        "image_files": {
            "full_res": "https://example.com/full.jpg"
        },
        "extended": {
            "scaleFactor": 1,
            "subframeRect": "(0,0,1648,1200)",
            "dimension": "(1648,1200)"
        }
    }
    """;

    /// <summary>
    /// Perseverance photo with ONLY scaleFactor (broken extraction would return wrong values).
    /// </summary>
    public const string PerseveranceOnlyScaleFactor = """
    {
        "imageid": 987656,
        "sol": 1000,
        "date_taken": "2024-01-15T10:30:00Z",
        "sample_type": "full",
        "camera": {
            "instrument": "SHERLOC_WATSON"
        },
        "image_files": {
            "full_res": "https://example.com/full.jpg"
        },
        "extended": {
            "scaleFactor": 2,
            "subframeRect": "invalid_format"
        }
    }
    """;

    // ============================================================================
    // OPPORTUNITY/SPIRIT SAMPLES
    // ============================================================================

    /// <summary>
    /// Legacy rover photo with thumbnail dimensions (64x64).
    /// </summary>
    public const string LegacyRoverThumbnail = """
    {
        "id": 555555,
        "sol": 5000,
        "instrument": "PANCAM",
        "https_url": "https://example.com/thumbnail.jpg",
        "date_taken": "2018-01-15T10:30:00Z",
        "extended": {
            "subframe_rect": "(0,0,64,64)",
            "sample_type": "thumbnail"
        }
    }
    """;

    // ============================================================================
    // RESPONSE WRAPPERS
    // ============================================================================

    /// <summary>
    /// Curiosity API response wrapper with items array.
    /// </summary>
    public static string WrapCuriosityItems(params string[] items)
    {
        return $$"""
        {
            "items": [{{string.Join(",", items)}}],
            "more": false,
            "total": {{items.Length}}
        }
        """;
    }

    /// <summary>
    /// Perseverance API response wrapper with images array.
    /// </summary>
    public static string WrapPerseveranceImages(params string[] images)
    {
        return $$"""
        {
            "images": [{{string.Join(",", images)}}],
            "total_images": {{images.Length}},
            "page": 0
        }
        """;
    }
}
