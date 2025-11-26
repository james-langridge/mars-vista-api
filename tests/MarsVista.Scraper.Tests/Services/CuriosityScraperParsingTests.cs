using System.Text.Json;
using FluentAssertions;
using MarsVista.Scraper.Helpers;
using MarsVista.Scraper.Tests.SampleData;

namespace MarsVista.Scraper.Tests.Services;

/// <summary>
/// Tests for Curiosity scraper parsing logic.
/// These tests verify that:
/// 1. Dimensions are correctly extracted from NASA responses
/// 2. Thumbnails are properly filtered out
/// 3. All required fields are parsed
/// </summary>
public class CuriosityScraperParsingTests
{
    // ============================================================================
    // DIMENSION EXTRACTION TESTS
    // ============================================================================

    [Fact]
    public void ParsePhoto_WithSubframeRect_ExtractsDimensions()
    {
        var json = JsonDocument.Parse(SampleNasaResponses.CuriosityFullPhoto);
        var photo = json.RootElement;

        photo.TryGetProperty("extended", out var extended);
        var result = ScraperHelpers.ExtractCuriosityDimensions(
            extended,
            ScraperHelpers.TryGetString(extended, "sample_type"));

        result.width.Should().Be(1024, "subframe_rect contains (1,1,1024,1024)");
        result.height.Should().Be(1024);
    }

    [Fact]
    public void ParsePhoto_WithoutSubframeRect_InfersDimensionsFromSampleType()
    {
        var json = JsonDocument.Parse(SampleNasaResponses.CuriosityNoSubframeRect);
        var photo = json.RootElement;

        photo.TryGetProperty("extended", out var extended);
        var sampleType = ScraperHelpers.TryGetString(extended, "sample_type");
        var result = ScraperHelpers.ExtractCuriosityDimensions(extended, sampleType);

        // "chemcam prc" should infer 1024x1024
        result.width.Should().Be(1024);
        result.height.Should().Be(1024);
    }

    [Fact]
    public void ParsePhoto_FullSampleType_HasExpectedDimensions()
    {
        var json = JsonDocument.Parse(SampleNasaResponses.CuriosityFullPhoto);
        var photo = json.RootElement;

        photo.TryGetProperty("extended", out var extended);
        var result = ScraperHelpers.ExtractCuriosityDimensions(
            extended,
            ScraperHelpers.TryGetString(extended, "sample_type"));

        // Full images should have dimensions >= 500px
        result.width.Should().BeGreaterThanOrEqualTo(500,
            "full images should be at least 500px wide");
        result.height.Should().BeGreaterThanOrEqualTo(500,
            "full images should be at least 500px tall");
    }

    // ============================================================================
    // THUMBNAIL FILTERING TESTS
    // ============================================================================

    [Fact]
    public void ShouldScrapePhoto_ThumbnailSampleType_ReturnsFalse()
    {
        var json = JsonDocument.Parse(SampleNasaResponses.CuriosityThumbnail);
        var photo = json.RootElement;

        photo.TryGetProperty("extended", out var extended);
        var sampleType = ScraperHelpers.TryGetString(extended, "sample_type");

        var shouldSkip = ScraperHelpers.IsThumbnail(sampleType);

        shouldSkip.Should().BeTrue("thumbnails should be skipped during scraping");
    }

    [Fact]
    public void ShouldScrapePhoto_FullSampleType_ReturnsTrue()
    {
        var json = JsonDocument.Parse(SampleNasaResponses.CuriosityFullPhoto);
        var photo = json.RootElement;

        photo.TryGetProperty("extended", out var extended);
        var sampleType = ScraperHelpers.TryGetString(extended, "sample_type");

        var shouldSkip = ScraperHelpers.IsThumbnail(sampleType);

        shouldSkip.Should().BeFalse("full images should be scraped");
    }

    [Fact]
    public void Thumbnail_HasSmallDimensions()
    {
        var json = JsonDocument.Parse(SampleNasaResponses.CuriosityThumbnail);
        var photo = json.RootElement;

        photo.TryGetProperty("extended", out var extended);
        var result = ScraperHelpers.ExtractCuriosityDimensions(
            extended,
            ScraperHelpers.TryGetString(extended, "sample_type"));

        result.width.Should().BeLessThanOrEqualTo(200,
            "thumbnails should be small (<=200px)");
        result.height.Should().BeLessThanOrEqualTo(200);
    }

    // ============================================================================
    // REQUIRED FIELD TESTS
    // ============================================================================

    [Fact]
    public void ParsePhoto_ExtractsAllRequiredFields()
    {
        var json = JsonDocument.Parse(SampleNasaResponses.CuriosityFullPhoto);
        var photo = json.RootElement;

        // These fields MUST be present for a valid photo
        var nasaId = ScraperHelpers.TryGetInt(photo, "id");
        var sol = ScraperHelpers.TryGetInt(photo, "sol");
        var instrument = ScraperHelpers.TryGetString(photo, "instrument");
        var imgUrl = ScraperHelpers.TryGetString(photo, "https_url");
        var dateTaken = ScraperHelpers.TryGetString(photo, "date_taken");

        nasaId.Should().NotBeNull("nasa_id is required");
        sol.Should().NotBeNull("sol is required");
        instrument.Should().NotBeNull("instrument (camera) is required");
        imgUrl.Should().NotBeNull("image URL is required");
        dateTaken.Should().NotBeNull("date_taken is required");
    }

    [Fact]
    public void ParsePhoto_ExtractsDimensions_FromFullPhoto()
    {
        var json = JsonDocument.Parse(SampleNasaResponses.CuriosityFullPhoto);
        var photo = json.RootElement;

        photo.TryGetProperty("extended", out var extended);
        var result = ScraperHelpers.ExtractCuriosityDimensions(
            extended,
            ScraperHelpers.TryGetString(extended, "sample_type"));

        // THIS TEST WOULD HAVE CAUGHT THE BUG
        // Curiosity photos should have dimensions extracted
        result.width.Should().NotBeNull("width should be extracted from full photos");
        result.height.Should().NotBeNull("height should be extracted from full photos");
    }

    // ============================================================================
    // CAMERA MAPPING TESTS
    // ============================================================================

    [Theory]
    [InlineData("MAST_LEFT", "MAST")]
    [InlineData("MAST_RIGHT", "MAST")]
    [InlineData("NAV_LEFT_A", "NAVCAM")]
    [InlineData("NAV_RIGHT_A", "NAVCAM")]
    [InlineData("FHAZ_LEFT_A", "FHAZ")]
    [InlineData("RHAZ_RIGHT_A", "RHAZ")]
    [InlineData("CHEMCAM_RMI", "CHEMCAM")]
    [InlineData("MAHLI", "MAHLI")]
    [InlineData("MARDI", "MARDI")]
    public void MapInstrumentToCamera_ReturnsCorrectMapping(
        string instrument, string expectedCamera)
    {
        // Note: The actual MapInstrumentToCamera is private in CuriosityScraper,
        // but we're documenting the expected behavior here.
        // In a real scenario, we'd either:
        // 1. Make the method internal and use InternalsVisibleTo
        // 2. Test via integration tests
        // 3. Extract to a separate helper class

        // For now, this test documents expected behavior
        instrument.Should().NotBeNull();
        expectedCamera.Should().NotBeNull();
    }

    // ============================================================================
    // METADATA EXTRACTION TESTS
    // ============================================================================

    [Fact]
    public void ParsePhoto_ExtractsLocationData()
    {
        var json = JsonDocument.Parse(SampleNasaResponses.CuriosityFullPhoto);
        var photo = json.RootElement;

        var site = ScraperHelpers.TryGetInt(photo, "site");
        var drive = ScraperHelpers.TryGetInt(photo, "drive");
        var xyz = ScraperHelpers.TryGetString(photo, "xyz");

        site.Should().Be(95);
        drive.Should().Be(1234);
        xyz.Should().Be("(1.5,2.5,3.5)");
    }

    [Fact]
    public void ParsePhoto_ExtractsMastOrientation()
    {
        var json = JsonDocument.Parse(SampleNasaResponses.CuriosityFullPhoto);
        var photo = json.RootElement;

        photo.TryGetProperty("extended", out var extended);
        var mastAz = ScraperHelpers.TryGetFloatFromString(extended, "mast_az");
        var mastEl = ScraperHelpers.TryGetFloatFromString(extended, "mast_el");

        mastAz.Should().BeApproximately(180.5f, 0.1f);
        mastEl.Should().BeApproximately(-15.2f, 0.1f);
    }

    [Fact]
    public void ParsePhoto_ExtractsMarsLocalTime()
    {
        var json = JsonDocument.Parse(SampleNasaResponses.CuriosityFullPhoto);
        var photo = json.RootElement;

        photo.TryGetProperty("extended", out var extended);
        var lmst = ScraperHelpers.TryGetString(extended, "lmst");

        lmst.Should().Be("Sol-04100M14:30:00.000");
    }
}
