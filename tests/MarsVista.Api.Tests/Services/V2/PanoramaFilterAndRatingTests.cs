using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MarsVista.Core.Entities;
using MarsVista.Api.Services.V2;
using MarsVista.Api.Tests.Integration;

namespace MarsVista.Api.Tests.Services.V2;

public class PanoramaFilterAndRatingTests : IntegrationTestBase
{
    private Mock<ILogger<PanoramaService>> _mockLogger = null!;
    private Mock<IPhotoQueryServiceV2> _mockPhotoService = null!;
    private PanoramaService _service = null!;

    protected override void ConfigureServices(IServiceCollection services)
    {
        _mockLogger = new Mock<ILogger<PanoramaService>>();
        _mockPhotoService = new Mock<IPhotoQueryServiceV2>();

        services.AddSingleton(_mockLogger.Object);
        services.AddSingleton(_mockPhotoService.Object);
        services.AddScoped<MarsVista.Core.Services.PanoramaDetector>();
        services.AddScoped<PanoramaService>();
    }

    protected override async Task SeedAdditionalDataAsync()
    {
        _service = ServiceProvider.GetRequiredService<PanoramaService>();

        var now = DateTime.UtcNow;

        // Panorama 1: Sol 1900, 5 photos, ~40° coverage (quality: "partial", single_row)
        // Using sol 1900 (not 1000) to stay within DefaultSolRangeLimit=500 of sol 2000
        for (int i = 0; i < 5; i++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"NRF_1900_{i:D4}",
                Sol = 1900,
                EarthDate = new DateTime(2015, 5, 30, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2015, 5, 30, 10, i, 0, DateTimeKind.Utc),
                ImgSrcFull = $"https://mars.nasa.gov/photo{i}_f.jpg",
                Site = 79,
                Drive = 1204,
                MastAz = 45.0f + (i * 10.0f), // 45-85°, 40° range
                MastEl = -10.0f,
                SpacecraftClock = 813073000.0f + (i * 100.0f),
                RoverId = 1,
                CameraId = 2, // MAST
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        // Panorama 2: Sol 2000, 12 photos, ~220° coverage (quality: "wide", single_row)
        for (int i = 0; i < 12; i++)
        {
            DbContext.Photos.Add(new Photo
            {
                NasaId = $"NRF_2000_{i:D4}",
                Sol = 2000,
                EarthDate = new DateTime(2016, 3, 15, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2016, 3, 15, 10, i, 0, DateTimeKind.Utc),
                ImgSrcFull = $"https://mars.nasa.gov/photo2k{i}_f.jpg",
                Site = 80,
                Drive = 1300,
                MastAz = 10.0f + (i * 20.0f), // 10-230°, 220° range
                MastEl = -5.0f,
                SpacecraftClock = 913073000.0f + (i * 100.0f),
                RoverId = 1,
                CameraId = 2,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await DbContext.SaveChangesAsync();

        // Add stitch records for panorama 2 (completed with feature_match)
        DbContext.StitchedPanoramas.Add(new StitchedPanorama
        {
            PanoramaId = "pano_curiosity_2000_0",
            Status = "completed",
            StitchMethod = "feature_match",
            ImageWidth = 8519,
            ImageHeight = 2322,
            ImageSizeBytes = 1234567,
            SourcePhotoCount = 12,
            CreatedAt = now,
            CompletedAt = now
        });

        // Add stitch record for panorama 1 (failed)
        DbContext.StitchedPanoramas.Add(new StitchedPanorama
        {
            PanoramaId = "pano_curiosity_1900_0",
            Status = "failed",
            ErrorMessage = "Not enough overlap",
            CreatedAt = now,
            CompletedAt = now
        });

        // Add ratings for panorama 2
        DbContext.PanoramaRatings.Add(new PanoramaRating
        {
            PanoramaId = "pano_curiosity_2000_0",
            Rating = 5,
            ClientId = "client_a",
            CreatedAt = now
        });
        DbContext.PanoramaRatings.Add(new PanoramaRating
        {
            PanoramaId = "pano_curiosity_2000_0",
            Rating = 3,
            ClientId = "client_b",
            CreatedAt = now
        });

        await DbContext.SaveChangesAsync();
    }

    // -- Stitch status filter tests --

    [Fact]
    public async Task GetPanoramasAsync_StitchStatusCompleted_FiltersToStitchedOnly()
    {
        var result = await _service.GetPanoramasAsync(
            stitchStatus: "completed",
            pageNumber: 1,
            pageSize: 25);

        result.Data.Should().HaveCount(1);
        result.Data[0].Attributes.Sol.Should().Be(2000);
        result.Data[0].Attributes.Stitch.Should().NotBeNull();
        result.Data[0].Attributes.Stitch!.Status.Should().Be("completed");
    }

    [Fact]
    public async Task GetPanoramasAsync_StitchStatusFailed_FiltersToFailedOnly()
    {
        var result = await _service.GetPanoramasAsync(
            stitchStatus: "failed",
            pageNumber: 1,
            pageSize: 25);

        result.Data.Should().HaveCount(1);
        result.Data[0].Attributes.Sol.Should().Be(1900);
        result.Data[0].Attributes.Stitch.Should().NotBeNull();
        result.Data[0].Attributes.Stitch!.Status.Should().Be("failed");
    }

    [Fact]
    public async Task GetPanoramasAsync_StitchStatusNotStarted_ExcludesStitched()
    {
        // Both panoramas have stitch records, so not_started should return empty
        var result = await _service.GetPanoramasAsync(
            stitchStatus: "not_started",
            pageNumber: 1,
            pageSize: 25);

        result.Data.Should().BeEmpty();
    }

    // -- Stitch method filter tests --

    [Fact]
    public async Task GetPanoramasAsync_StitchMethodFeatureMatch_FiltersCorrectly()
    {
        var result = await _service.GetPanoramasAsync(
            stitchMethod: "feature_match",
            pageNumber: 1,
            pageSize: 25);

        result.Data.Should().HaveCount(1);
        result.Data[0].Attributes.Sol.Should().Be(2000);
        result.Data[0].Attributes.Stitch!.Method.Should().Be("feature_match");
    }

    [Fact]
    public async Task GetPanoramasAsync_StitchMethodTelemetryProjection_ReturnsEmpty()
    {
        var result = await _service.GetPanoramasAsync(
            stitchMethod: "telemetry_projection",
            pageNumber: 1,
            pageSize: 25);

        result.Data.Should().BeEmpty();
    }

    // -- Quality filter tests --

    [Fact]
    public async Task GetPanoramasAsync_QualityWide_FiltersCorrectly()
    {
        var result = await _service.GetPanoramasAsync(
            quality: "wide",
            pageNumber: 1,
            pageSize: 25);

        result.Data.Should().HaveCount(1);
        result.Data[0].Attributes.Sol.Should().Be(2000);
        result.Data[0].Attributes.Quality.Should().Be("wide");
    }

    [Fact]
    public async Task GetPanoramasAsync_QualityPartial_FiltersCorrectly()
    {
        var result = await _service.GetPanoramasAsync(
            quality: "partial",
            pageNumber: 1,
            pageSize: 25);

        result.Data.Should().HaveCount(1);
        result.Data[0].Attributes.Sol.Should().Be(1900);
        result.Data[0].Attributes.Quality.Should().Be("partial");
    }

    // -- Mosaic type filter tests --

    [Fact]
    public async Task GetPanoramasAsync_MosaicTypeSingleRow_FiltersCorrectly()
    {
        var result = await _service.GetPanoramasAsync(
            mosaicType: "single_row",
            pageNumber: 1,
            pageSize: 25);

        result.Data.Should().HaveCount(2);
        result.Data.Should().OnlyContain(p => p.Attributes.MosaicType == "single_row");
    }

    [Fact]
    public async Task GetPanoramasAsync_MosaicTypeMultiRow_ReturnsEmpty()
    {
        var result = await _service.GetPanoramasAsync(
            mosaicType: "multi_row",
            pageNumber: 1,
            pageSize: 25);

        result.Data.Should().BeEmpty();
    }

    // -- Min rating filter tests --

    [Fact]
    public async Task GetPanoramasAsync_MinRating4_FiltersToHighRatedOnly()
    {
        // Panorama 2 has ratings 5+3=8/2=4.0 avg
        var result = await _service.GetPanoramasAsync(
            minRating: 4.0,
            pageNumber: 1,
            pageSize: 25);

        result.Data.Should().HaveCount(1);
        result.Data[0].Attributes.Sol.Should().Be(2000);
    }

    [Fact]
    public async Task GetPanoramasAsync_MinRating5_ReturnsEmpty()
    {
        // Average is 4.0, so min_rating=5 excludes everything
        var result = await _service.GetPanoramasAsync(
            minRating: 5.0,
            pageNumber: 1,
            pageSize: 25);

        result.Data.Should().BeEmpty();
    }

    // -- Sort tests --

    [Fact]
    public async Task GetPanoramasAsync_SortByPhotosDesc_HighestFirst()
    {
        var result = await _service.GetPanoramasAsync(
            sort: "photos",
            order: "desc",
            pageNumber: 1,
            pageSize: 25);

        result.Data.Should().HaveCountGreaterThanOrEqualTo(2);
        result.Data[0].Attributes.TotalPhotos.Should()
            .BeGreaterThanOrEqualTo(result.Data[1].Attributes.TotalPhotos);
    }

    [Fact]
    public async Task GetPanoramasAsync_SortByPhotosAsc_LowestFirst()
    {
        var result = await _service.GetPanoramasAsync(
            sort: "photos",
            order: "asc",
            pageNumber: 1,
            pageSize: 25);

        result.Data.Should().HaveCountGreaterThanOrEqualTo(2);
        result.Data[0].Attributes.TotalPhotos.Should()
            .BeLessThanOrEqualTo(result.Data[1].Attributes.TotalPhotos);
    }

    // -- StitchInfo in response tests --

    [Fact]
    public async Task GetPanoramasAsync_CompletedStitch_IncludesStitchInfo()
    {
        var result = await _service.GetPanoramasAsync(
            stitchStatus: "completed",
            pageNumber: 1,
            pageSize: 25);

        var panorama = result.Data.First();
        panorama.Attributes.Stitch.Should().NotBeNull();
        panorama.Attributes.Stitch!.Status.Should().Be("completed");
        panorama.Attributes.Stitch.Method.Should().Be("feature_match");
        panorama.Attributes.Stitch.Width.Should().Be(8519);
        panorama.Attributes.Stitch.Height.Should().Be(2322);
        panorama.Attributes.Stitch.AverageRating.Should().Be(4.0);
        panorama.Attributes.Stitch.RatingCount.Should().Be(2);
    }

    // -- Rating endpoint tests --

    [Fact]
    public async Task UpsertRatingAsync_CreatesNewRating()
    {
        var result = await _service.UpsertRatingAsync("pano_curiosity_1900_0", "new_client", 4);

        result.AverageRating.Should().Be(4.0);
        result.RatingCount.Should().Be(1);
        result.UserRating.Should().Be(4);
    }

    [Fact]
    public async Task UpsertRatingAsync_UpdatesExistingRating()
    {
        // First rating
        await _service.UpsertRatingAsync("pano_test_999_0", "test_client", 2);

        // Update rating
        var result = await _service.UpsertRatingAsync("pano_test_999_0", "test_client", 5);

        result.AverageRating.Should().Be(5.0); // Updated, not averaged with old
        result.RatingCount.Should().Be(1); // Still one rating
        result.UserRating.Should().Be(5);
    }

    [Fact]
    public async Task UpsertRatingAsync_MultipleClients_AveragesCorrectly()
    {
        await _service.UpsertRatingAsync("pano_test_888_0", "client_1", 5);
        var result = await _service.UpsertRatingAsync("pano_test_888_0", "client_2", 3);

        result.AverageRating.Should().Be(4.0);
        result.RatingCount.Should().Be(2);
        result.UserRating.Should().Be(3); // client_2's rating
    }

    [Fact]
    public async Task GetRatingAsync_WithClientId_IncludesUserRating()
    {
        // Panorama 2 was seeded with ratings from client_a (5) and client_b (3)
        var result = await _service.GetRatingAsync("pano_curiosity_2000_0", "client_a");

        result.AverageRating.Should().Be(4.0);
        result.RatingCount.Should().Be(2);
        result.UserRating.Should().Be(5);
    }

    [Fact]
    public async Task GetRatingAsync_WithoutClientId_OmitsUserRating()
    {
        var result = await _service.GetRatingAsync("pano_curiosity_2000_0");

        result.AverageRating.Should().Be(4.0);
        result.RatingCount.Should().Be(2);
        result.UserRating.Should().BeNull();
    }

    [Fact]
    public async Task GetRatingAsync_NoRatings_ReturnsZeros()
    {
        var result = await _service.GetRatingAsync("pano_no_ratings_0");

        result.AverageRating.Should().Be(0);
        result.RatingCount.Should().Be(0);
        result.UserRating.Should().BeNull();
    }
}
