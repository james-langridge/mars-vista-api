using System.Text.Json;
using FluentAssertions;
using MarsVista.Scraper.Helpers;
using MarsVista.Scraper.Tests.SampleData;

namespace MarsVista.Scraper.Tests.Services;

/// <summary>
/// Tests for Perseverance scraper parsing logic.
/// These tests verify that:
/// 1. Dimensions are correctly extracted from the dimension field (NOT scaleFactor!)
/// 2. All required fields are parsed correctly
/// 3. Image URLs at all resolutions are extracted
/// </summary>
public class PerseveranceScraperParsingTests
{
    // ============================================================================
    // CRITICAL: DIMENSION EXTRACTION TESTS
    // These tests would have caught the scaleFactor bug!
    // ============================================================================

    [Fact]
    public void ExtractDimensions_UsesDimensionField_NotScaleFactor()
    {
        // THIS TEST CATCHES THE BUG!
        // The original code incorrectly used:
        //   Width = TryGetInt(extended, "scaleFactor")  // WRONG!
        //   Height = TryGetInt(extended, "subframeRect") // WRONG!
        var json = JsonDocument.Parse(SampleNasaResponses.PerseveranceWithScaleFactor);
        var photo = json.RootElement;

        photo.TryGetProperty("extended", out var extended);
        var result = ScraperHelpers.ExtractPerseveranceDimensions(extended);

        // Width should be 1648, NOT 1 (scaleFactor value)
        result.width.Should().Be(1648, "dimension field contains 1648, not scaleFactor");
        result.height.Should().Be(1200, "dimension field contains 1200");
    }

    [Fact]
    public void ExtractDimensions_ParsesDimensionField()
    {
        var json = JsonDocument.Parse(SampleNasaResponses.PerseveranceFullPhoto);
        var photo = json.RootElement;

        photo.TryGetProperty("extended", out var extended);
        var result = ScraperHelpers.ExtractPerseveranceDimensions(extended);

        result.width.Should().Be(1648);
        result.height.Should().Be(1200);
    }

    [Fact]
    public void ExtractDimensions_ScaleFactorIsNotWidth()
    {
        // scaleFactor is typically 1, 2, or 4 - it's a scaling multiplier, not dimensions!
        var json = JsonDocument.Parse(SampleNasaResponses.PerseveranceOnlyScaleFactor);
        var photo = json.RootElement;

        photo.TryGetProperty("extended", out var extended);

        // Verify scaleFactor exists and is NOT a valid image dimension
        var scaleFactor = ScraperHelpers.TryGetInt(extended, "scaleFactor");
        scaleFactor.Should().Be(2, "scaleFactor is 2 in test data");

        // Our extraction should NOT return scaleFactor as width
        var result = ScraperHelpers.ExtractPerseveranceDimensions(extended);

        // Since dimension field is missing and subframeRect is invalid,
        // we should get null, NOT scaleFactor=2
        result.width.Should().NotBe(2, "scaleFactor should never be used as width");
    }

    [Fact]
    public void ExtractDimensions_FallsBackToSubframeRect()
    {
        // When dimension field is missing, fall back to subframeRect
        var json = JsonDocument.Parse("""
        {
            "extended": {
                "subframeRect": "(0,0,1024,768)"
            }
        }
        """);

        var result = ScraperHelpers.ExtractPerseveranceDimensions(
            json.RootElement.GetProperty("extended"));

        result.width.Should().Be(1024);
        result.height.Should().Be(768);
    }

    // ============================================================================
    // REQUIRED FIELD TESTS
    // ============================================================================

    [Fact]
    public void ParsePhoto_ExtractsAllRequiredFields()
    {
        var json = JsonDocument.Parse(SampleNasaResponses.PerseveranceFullPhoto);
        var photo = json.RootElement;

        // These fields MUST be present for a valid Perseverance photo
        var nasaId = ScraperHelpers.TryGetInt(photo, "imageid");
        var sol = ScraperHelpers.TryGetInt(photo, "sol");
        var dateTaken = ScraperHelpers.TryGetString(photo, "date_taken");

        // Camera is nested
        var camera = ScraperHelpers.TryGetString(photo, "camera", "instrument");

        // Image URL is nested
        var imgUrl = ScraperHelpers.TryGetString(photo, "image_files", "full_res");

        nasaId.Should().NotBeNull("imageid is required");
        sol.Should().NotBeNull("sol is required");
        dateTaken.Should().NotBeNull("date_taken is required");
        camera.Should().NotBeNull("camera instrument is required");
        imgUrl.Should().NotBeNull("full_res image URL is required");
    }

    [Fact]
    public void ParsePhoto_ExtractsDimensions()
    {
        var json = JsonDocument.Parse(SampleNasaResponses.PerseveranceFullPhoto);
        var photo = json.RootElement;

        photo.TryGetProperty("extended", out var extended);
        var result = ScraperHelpers.ExtractPerseveranceDimensions(extended);

        // THIS TEST WOULD HAVE CAUGHT THE BUG
        result.width.Should().NotBeNull("width should be extracted");
        result.height.Should().NotBeNull("height should be extracted");

        // Dimensions should be reasonable (not 1 from scaleFactor!)
        result.width.Should().BeGreaterThan(100, "width should be a valid image dimension");
        result.height.Should().BeGreaterThan(100, "height should be a valid image dimension");
    }

    // ============================================================================
    // IMAGE URL TESTS
    // ============================================================================

    [Fact]
    public void ParsePhoto_ExtractsAllImageSizes()
    {
        var json = JsonDocument.Parse(SampleNasaResponses.PerseveranceFullPhoto);
        var photo = json.RootElement;

        var fullRes = ScraperHelpers.TryGetString(photo, "image_files", "full_res");
        var small = ScraperHelpers.TryGetString(photo, "image_files", "small");
        var medium = ScraperHelpers.TryGetString(photo, "image_files", "medium");
        var large = ScraperHelpers.TryGetString(photo, "image_files", "large");

        fullRes.Should().NotBeNull("full_res URL should be present");
        small.Should().NotBeNull("small URL should be present for progressive loading");
        medium.Should().NotBeNull("medium URL should be present");
        large.Should().NotBeNull("large URL should be present");
    }

    [Fact]
    public void ParsePhoto_ImageUrlsAreValid()
    {
        var json = JsonDocument.Parse(SampleNasaResponses.PerseveranceFullPhoto);
        var photo = json.RootElement;

        var fullRes = ScraperHelpers.TryGetString(photo, "image_files", "full_res");

        fullRes.Should().StartWith("https://", "image URLs should use HTTPS");
        fullRes.Should().Contain("mars", "image URLs should be from NASA Mars domain");
    }

    // ============================================================================
    // METADATA EXTRACTION TESTS
    // ============================================================================

    [Fact]
    public void ParsePhoto_ExtractsLocationData()
    {
        var json = JsonDocument.Parse(SampleNasaResponses.PerseveranceFullPhoto);
        var photo = json.RootElement;

        var site = ScraperHelpers.TryGetInt(photo, "site");
        var drive = ScraperHelpers.TryGetInt(photo, "drive");

        site.Should().Be(25);
        drive.Should().Be(567);
    }

    [Fact]
    public void ParsePhoto_ExtractsMastOrientation()
    {
        var json = JsonDocument.Parse(SampleNasaResponses.PerseveranceFullPhoto);
        var photo = json.RootElement;

        photo.TryGetProperty("extended", out var extended);
        var mastAz = ScraperHelpers.TryGetFloatFromString(extended, "mast_az");
        var mastEl = ScraperHelpers.TryGetFloatFromString(extended, "mast_el");

        mastAz.Should().BeApproximately(90.0f, 0.1f);
        mastEl.Should().BeApproximately(-10.5f, 0.1f);
    }

    [Fact]
    public void ParsePhoto_ExtractsMarsLocalTime()
    {
        var json = JsonDocument.Parse(SampleNasaResponses.PerseveranceFullPhoto);
        var photo = json.RootElement;

        photo.TryGetProperty("extended", out var extended);
        var lmst = ScraperHelpers.TryGetString(extended, "lmst");

        lmst.Should().Be("Sol-01000M14:30:00.000");
    }

    // ============================================================================
    // DATA QUALITY ASSERTIONS
    // ============================================================================

    [Fact]
    public void FullPhoto_HasReasonableDimensions()
    {
        var json = JsonDocument.Parse(SampleNasaResponses.PerseveranceFullPhoto);
        var photo = json.RootElement;

        photo.TryGetProperty("extended", out var extended);
        var (width, height) = ScraperHelpers.ExtractPerseveranceDimensions(extended);

        // Full Perseverance photos should be high resolution
        width.Should().BeGreaterThanOrEqualTo(1000,
            "full Perseverance photos should be at least 1000px wide");
        height.Should().BeGreaterThanOrEqualTo(500,
            "full Perseverance photos should be at least 500px tall");
    }

    [Fact]
    public void SampleType_MatchesDimensions()
    {
        var json = JsonDocument.Parse(SampleNasaResponses.PerseveranceFullPhoto);
        var photo = json.RootElement;

        var sampleType = ScraperHelpers.TryGetString(photo, "sample_type");
        photo.TryGetProperty("extended", out var extended);
        var (width, height) = ScraperHelpers.ExtractPerseveranceDimensions(extended);

        // If sample_type is "full", dimensions should be >= 500px
        if (sampleType == "full")
        {
            width.Should().BeGreaterThanOrEqualTo(500,
                "full sample_type should have width >= 500px");
        }
    }
}
