using System.Text.Json;
using FluentAssertions;
using MarsVista.Scraper.Helpers;

namespace MarsVista.Scraper.Tests.Helpers;

public class ScraperHelpersTests
{
    // ============================================================================
    // ParseSubframeRect Tests
    // ============================================================================

    [Theory]
    [InlineData("(1,1,1024,1024)", 1024, 1024)]
    [InlineData("(0,0,1648,1200)", 1648, 1200)]
    [InlineData("(100, 200, 800, 600)", 800, 600)]
    [InlineData("(1, 1, 256, 256)", 256, 256)]
    public void ParseSubframeRect_ValidFormats_ExtractsDimensions(
        string rect, int expectedWidth, int expectedHeight)
    {
        var result = ScraperHelpers.ParseSubframeRect(rect);

        result.Should().NotBeNull();
        result!.Value.width.Should().Be(expectedWidth);
        result!.Value.height.Should().Be(expectedHeight);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("(1024,1024)")] // Only two values, not subframe format
    [InlineData("1024x1024")]
    public void ParseSubframeRect_InvalidFormats_ReturnsNull(string? rect)
    {
        var result = ScraperHelpers.ParseSubframeRect(rect);

        result.Should().BeNull();
    }

    // ============================================================================
    // ParseDimensionField Tests (Perseverance format)
    // ============================================================================

    [Theory]
    [InlineData("(1648,1200)", 1648, 1200)]
    [InlineData("(1024,1024)", 1024, 1024)]
    [InlineData("(256, 256)", 256, 256)]
    [InlineData("(1920,1080)", 1920, 1080)]
    public void ParseDimensionField_ValidFormats_ExtractsDimensions(
        string dimension, int expectedWidth, int expectedHeight)
    {
        var result = ScraperHelpers.ParseDimensionField(dimension);

        result.Should().NotBeNull();
        result!.Value.width.Should().Be(expectedWidth);
        result!.Value.height.Should().Be(expectedHeight);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("1648x1200")] // Wrong delimiter
    [InlineData("(1,1,1648,1200)")] // Subframe format, not dimension
    public void ParseDimensionField_InvalidFormats_ReturnsNull(string? dimension)
    {
        var result = ScraperHelpers.ParseDimensionField(dimension);

        result.Should().BeNull();
    }

    // ============================================================================
    // InferDimensionsFromSampleType Tests
    // ============================================================================

    [Theory]
    [InlineData("thumbnail", 160, 144)]
    [InlineData("THUMBNAIL", 160, 144)]
    [InlineData("full", 1024, 1024)]
    [InlineData("Full", 1024, 1024)]
    [InlineData("subframe", 1024, 1024)]
    [InlineData("downsampled", 800, 600)]
    [InlineData("mixed", 1024, 1024)]
    [InlineData("chemcam prc", 1024, 1024)]
    public void InferDimensionsFromSampleType_KnownTypes_ReturnsExpectedDimensions(
        string sampleType, int expectedWidth, int expectedHeight)
    {
        var result = ScraperHelpers.InferDimensionsFromSampleType(sampleType);

        result.width.Should().Be(expectedWidth);
        result.height.Should().Be(expectedHeight);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("unknown")]
    [InlineData("some_other_type")]
    public void InferDimensionsFromSampleType_UnknownTypes_ReturnsFallback(string? sampleType)
    {
        var result = ScraperHelpers.InferDimensionsFromSampleType(sampleType);

        // Fallback is 512x512
        result.width.Should().Be(512);
        result.height.Should().Be(512);
    }

    // ============================================================================
    // IsThumbnail Tests
    // ============================================================================

    [Theory]
    [InlineData("thumbnail", true)]
    [InlineData("THUMBNAIL", true)]
    [InlineData("Thumbnail", true)]
    [InlineData("full", false)]
    [InlineData("subframe", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void IsThumbnail_VariousSampleTypes_ReturnsCorrectResult(
        string? sampleType, bool expected)
    {
        var result = ScraperHelpers.IsThumbnail(sampleType);

        result.Should().Be(expected);
    }

    // ============================================================================
    // IsThumbnailByDimensions Tests
    // ============================================================================

    [Theory]
    [InlineData(160, 144, true)]
    [InlineData(200, 200, true)]
    [InlineData(64, 64, true)]
    [InlineData(201, 201, false)]
    [InlineData(1024, 1024, false)]
    [InlineData(1648, 1200, false)]
    public void IsThumbnailByDimensions_VariousSizes_ReturnsCorrectResult(
        int width, int height, bool expected)
    {
        var result = ScraperHelpers.IsThumbnailByDimensions(width, height);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, 144)]
    [InlineData(160, null)]
    [InlineData(null, null)]
    public void IsThumbnailByDimensions_NullDimensions_ReturnsFalse(
        int? width, int? height)
    {
        var result = ScraperHelpers.IsThumbnailByDimensions(width, height);

        result.Should().BeFalse();
    }

    // ============================================================================
    // JSON Helper Tests
    // ============================================================================

    [Fact]
    public void TryGetString_ValidProperty_ReturnsValue()
    {
        var json = JsonDocument.Parse("""{"name":"test"}""");

        var result = ScraperHelpers.TryGetString(json.RootElement, "name");

        result.Should().Be("test");
    }

    [Fact]
    public void TryGetString_MissingProperty_ReturnsNull()
    {
        var json = JsonDocument.Parse("""{"name":"test"}""");

        var result = ScraperHelpers.TryGetString(json.RootElement, "missing");

        result.Should().BeNull();
    }

    [Fact]
    public void TryGetString_NullProperty_ReturnsNull()
    {
        var json = JsonDocument.Parse("""{"name":null}""");

        var result = ScraperHelpers.TryGetString(json.RootElement, "name");

        result.Should().BeNull();
    }

    [Fact]
    public void TryGetString_NestedProperty_ReturnsValue()
    {
        var json = JsonDocument.Parse("""{"image_files":{"full_res":"https://example.com/full.jpg"}}""");

        var result = ScraperHelpers.TryGetString(json.RootElement, "image_files", "full_res");

        result.Should().Be("https://example.com/full.jpg");
    }

    [Fact]
    public void TryGetInt_ValidProperty_ReturnsValue()
    {
        var json = JsonDocument.Parse("""{"sol":1234}""");

        var result = ScraperHelpers.TryGetInt(json.RootElement, "sol");

        result.Should().Be(1234);
    }

    [Fact]
    public void TryGetInt_StringProperty_ReturnsNull()
    {
        var json = JsonDocument.Parse("""{"sol":"1234"}""");

        var result = ScraperHelpers.TryGetInt(json.RootElement, "sol");

        result.Should().BeNull();
    }

    [Fact]
    public void TryGetFloat_NumberProperty_ReturnsValue()
    {
        var json = JsonDocument.Parse("""{"value":123.45}""");

        var result = ScraperHelpers.TryGetFloat(json.RootElement, "value");

        result.Should().BeApproximately(123.45f, 0.01f);
    }

    [Fact]
    public void TryGetFloat_StringProperty_ReturnsValue()
    {
        var json = JsonDocument.Parse("""{"value":"123.45"}""");

        var result = ScraperHelpers.TryGetFloat(json.RootElement, "value");

        result.Should().BeApproximately(123.45f, 0.01f);
    }

    [Fact]
    public void TryGetDateTime_ValidProperty_ReturnsUtcDateTime()
    {
        var json = JsonDocument.Parse("""{"date":"2024-01-15T10:30:00"}""");

        var result = ScraperHelpers.TryGetDateTime(json.RootElement, "date");

        result.Should().NotBeNull();
        result!.Value.Kind.Should().Be(DateTimeKind.Utc);
        result.Value.Year.Should().Be(2024);
        result.Value.Month.Should().Be(1);
        result.Value.Day.Should().Be(15);
    }

    // ============================================================================
    // ExtractPerseveranceDimensions Tests
    // ============================================================================

    [Fact]
    public void ExtractPerseveranceDimensions_WithDimensionField_ExtractsDimensions()
    {
        // This is the CORRECT way to extract dimensions for Perseverance
        var json = JsonDocument.Parse("""{"dimension":"(1648,1200)"}""");

        var result = ScraperHelpers.ExtractPerseveranceDimensions(json.RootElement);

        result.width.Should().Be(1648);
        result.height.Should().Be(1200);
    }

    [Fact]
    public void ExtractPerseveranceDimensions_WithSubframeRect_FallsBackCorrectly()
    {
        var json = JsonDocument.Parse("""{"subframeRect":"(0,0,1024,1024)"}""");

        var result = ScraperHelpers.ExtractPerseveranceDimensions(json.RootElement);

        result.width.Should().Be(1024);
        result.height.Should().Be(1024);
    }

    [Fact]
    public void ExtractPerseveranceDimensions_DoesNotUseScaleFactor()
    {
        // This test verifies we don't use scaleFactor as width!
        // scaleFactor is typically 1, 2, or 4 - NOT image dimensions
        var json = JsonDocument.Parse("""
            {"scaleFactor":1,"subframeRect":"(1,1,1648,1200)","dimension":"(1648,1200)"}
        """);

        var result = ScraperHelpers.ExtractPerseveranceDimensions(json.RootElement);

        // Width should NOT be 1 (scaleFactor value)
        result.width.Should().NotBe(1);
        result.width.Should().Be(1648);
        result.height.Should().Be(1200);
    }

    [Fact]
    public void ExtractPerseveranceDimensions_UndefinedElement_ReturnsNulls()
    {
        var result = ScraperHelpers.ExtractPerseveranceDimensions(default);

        result.width.Should().BeNull();
        result.height.Should().BeNull();
    }

    // ============================================================================
    // ExtractCuriosityDimensions Tests
    // ============================================================================

    [Fact]
    public void ExtractCuriosityDimensions_WithSubframeRect_ExtractsDimensions()
    {
        var json = JsonDocument.Parse("""{"subframe_rect":"(1,1,1024,1024)"}""");

        var result = ScraperHelpers.ExtractCuriosityDimensions(json.RootElement, "full");

        result.width.Should().Be(1024);
        result.height.Should().Be(1024);
    }

    [Fact]
    public void ExtractCuriosityDimensions_WithoutSubframeRect_InfersFromSampleType()
    {
        var json = JsonDocument.Parse("""{}""");

        var result = ScraperHelpers.ExtractCuriosityDimensions(json.RootElement, "full");

        result.width.Should().Be(1024);
        result.height.Should().Be(1024);
    }

    [Fact]
    public void ExtractCuriosityDimensions_ThumbnailSampleType_ReturnsSmallDimensions()
    {
        var json = JsonDocument.Parse("""{}""");

        var result = ScraperHelpers.ExtractCuriosityDimensions(json.RootElement, "thumbnail");

        result.width.Should().Be(160);
        result.height.Should().Be(144);
    }
}
