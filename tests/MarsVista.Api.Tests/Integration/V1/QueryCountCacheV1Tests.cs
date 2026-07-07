using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MarsVista.Api.Services;
using MarsVista.Core.Entities;

namespace MarsVista.Api.Tests.Integration.V1;

/// <summary>
/// Verifies the v1 photo query service serves pagination total counts from the
/// count cache instead of running COUNT(*) per request - same contract as the
/// v2 tests in QueryCountCacheTests.
/// </summary>
public class QueryCountCacheV1Tests : IntegrationTestBase
{
    private IPhotoQueryService _photoQueryService = null!;

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IPhotoQueryService, PhotoQueryService>();
    }

    protected override async Task SeedAdditionalDataAsync()
    {
        _photoQueryService = ServiceProvider.GetRequiredService<IPhotoQueryService>();

        var now = DateTime.UtcNow;
        DbContext.Photos.AddRange(
            new Photo
            {
                NasaId = "V1CC-1", ImgSrcFull = "x", ImgSrcLarge = "x", ImgSrcMedium = "x", ImgSrcSmall = "x",
                Sol = 100, EarthDate = new DateTime(2014, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2014, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                SampleType = "Full",
                RoverId = 1, CameraId = 1,
                CreatedAt = now, UpdatedAt = now,
            },
            new Photo
            {
                NasaId = "V1CC-2", ImgSrcFull = "x", ImgSrcLarge = "x", ImgSrcMedium = "x", ImgSrcSmall = "x",
                Sol = 100, EarthDate = new DateTime(2014, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2014, 1, 1, 6, 0, 0, DateTimeKind.Utc),
                SampleType = "Full",
                RoverId = 1, CameraId = 2,
                CreatedAt = now, UpdatedAt = now,
            }
        );
        await DbContext.SaveChangesAsync();
    }

    private static List<string> CountSqls(IReadOnlyList<string> executed) =>
        executed.Where(s => s.Contains("count(*)", StringComparison.OrdinalIgnoreCase)).ToList();

    [Fact]
    public async Task SecondIdenticalQuery_ServesCountFromCache_WithoutCountSql()
    {
        SqlCapture.Clear();
        var (firstPhotos, firstTotal) = await _photoQueryService.QueryPhotosAsync(
            "curiosity", sol: 100, page: 1, perPage: 10);
        CountSqls(SqlCapture.ExecutedSql).Should().NotBeEmpty(
            "the first request has a cold cache and must run the real COUNT");
        firstTotal.Should().Be(2);
        firstPhotos.Should().HaveCount(2);

        SqlCapture.Clear();
        var (secondPhotos, secondTotal) = await _photoQueryService.QueryPhotosAsync(
            "curiosity", sol: 100, page: 1, perPage: 10);
        CountSqls(SqlCapture.ExecutedSql).Should().BeEmpty(
            "an identical v1 filter set within the cache TTL must not run COUNT(*) again");
        secondTotal.Should().Be(2, "the cached count must equal the real count");
        secondPhotos.Should().HaveCount(2, "photo rows are still fetched live");
    }

    [Fact]
    public async Task PaginationParameters_DoNotFragmentTheCountCache()
    {
        await _photoQueryService.QueryPhotosAsync("curiosity", sol: 100, page: 1, perPage: 1);

        SqlCapture.Clear();
        var (photos, total) = await _photoQueryService.QueryPhotosAsync(
            "curiosity", sol: 100, page: 2, perPage: 1);

        CountSqls(SqlCapture.ExecutedSql).Should().BeEmpty(
            "page/per_page do not affect the total, so page 2 must reuse page 1's cached count");
        total.Should().Be(2);
        photos.Should().ContainSingle();
    }

    [Fact]
    public async Task DifferentCameraFilter_UsesDistinctCacheEntry()
    {
        var (_, allTotal) = await _photoQueryService.QueryPhotosAsync("curiosity", sol: 100);

        SqlCapture.Clear();
        var (_, fhazTotal) = await _photoQueryService.QueryPhotosAsync("curiosity", sol: 100, camera: "FHAZ");

        CountSqls(SqlCapture.ExecutedSql).Should().NotBeEmpty(
            "a camera-filtered count is a different cache key, so its first request must run COUNT");
        allTotal.Should().Be(2);
        fhazTotal.Should().Be(1, "the FHAZ count must not be served from the unfiltered entry");
    }
}
