using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MarsVista.Api.Models.V2;
using MarsVista.Api.Services.V2;
using MarsVista.Core.Entities;

namespace MarsVista.Api.Tests.Integration.V2;

/// <summary>
/// Verifies that pagination total counts are served from the two-level cache
/// instead of running COUNT(*) on every request. In production each
/// rover-filtered count scans ~500K index entries (~184 MB of buffer reads),
/// so the second identical request must not touch the database for the count.
///
/// Uses SqlCapturingInterceptor to assert on the SQL the real service path
/// emits (see PhotoQuerySqlGenerationTests for the pattern).
/// </summary>
public class QueryCountCacheTests : IntegrationTestBase
{
    private IPhotoQueryServiceV2 _photoQueryService = null!;

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IPhotoQueryServiceV2, PhotoQueryServiceV2>();
    }

    protected override async Task SeedAdditionalDataAsync()
    {
        _photoQueryService = ServiceProvider.GetRequiredService<IPhotoQueryServiceV2>();

        var now = DateTime.UtcNow;
        DbContext.Photos.AddRange(
            new Photo
            {
                NasaId = "CC-CUR-1", ImgSrcFull = "x", ImgSrcLarge = "x", ImgSrcMedium = "x", ImgSrcSmall = "x",
                Sol = 100, EarthDate = new DateTime(2014, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2014, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                SampleType = "Full",
                RoverId = 1, CameraId = 1,
                CreatedAt = now, UpdatedAt = now,
            },
            new Photo
            {
                NasaId = "CC-CUR-2", ImgSrcFull = "x", ImgSrcLarge = "x", ImgSrcMedium = "x", ImgSrcSmall = "x",
                Sol = 101, EarthDate = new DateTime(2014, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2014, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                SampleType = "Full",
                RoverId = 1, CameraId = 1,
                CreatedAt = now, UpdatedAt = now,
            },
            new Photo
            {
                NasaId = "CC-PER-1", ImgSrcFull = "x", ImgSrcLarge = "x", ImgSrcMedium = "x", ImgSrcSmall = "x",
                Sol = 500, EarthDate = new DateTime(2021, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2021, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                SampleType = "Full",
                RoverId = 2, CameraId = 3,
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
        var parameters = new PhotoQueryParameters
        {
            Rovers = "curiosity",
            RoverList = new List<string> { "curiosity" },
            Page = 1, PerPage = 10,
        };

        SqlCapture.Clear();
        var first = await _photoQueryService.QueryPhotosAsync(parameters, default);
        CountSqls(SqlCapture.ExecutedSql).Should().NotBeEmpty(
            "the first request has a cold cache and must run the real COUNT");
        first.Meta!.TotalCount.Should().Be(2);

        SqlCapture.Clear();
        var second = await _photoQueryService.QueryPhotosAsync(parameters, default);
        CountSqls(SqlCapture.ExecutedSql).Should().BeEmpty(
            "an identical filter set within the cache TTL must not run COUNT(*) again");
        second.Meta!.TotalCount.Should().Be(2, "the cached count must equal the real count");
        second.Data.Should().HaveCount(2, "only the count is cached - photo rows are still fetched live");
    }

    [Fact]
    public async Task DifferentFilterValues_UseDistinctCacheEntries()
    {
        var curiosity = new PhotoQueryParameters
        {
            Rovers = "curiosity",
            RoverList = new List<string> { "curiosity" },
            Page = 1, PerPage = 10,
        };
        var perseverance = new PhotoQueryParameters
        {
            Rovers = "perseverance",
            RoverList = new List<string> { "perseverance" },
            Page = 1, PerPage = 10,
        };

        var curResponse = await _photoQueryService.QueryPhotosAsync(curiosity, default);

        SqlCapture.Clear();
        var perResponse = await _photoQueryService.QueryPhotosAsync(perseverance, default);

        CountSqls(SqlCapture.ExecutedSql).Should().NotBeEmpty(
            "a different rover filter is a different cache key, so its first request must run COUNT");
        curResponse.Meta!.TotalCount.Should().Be(2);
        perResponse.Meta!.TotalCount.Should().Be(1, "the perseverance count must not be served from the curiosity entry");
    }

    [Fact]
    public async Task PaginationParameters_DoNotFragmentTheCountCache()
    {
        var page1 = new PhotoQueryParameters
        {
            Rovers = "curiosity",
            RoverList = new List<string> { "curiosity" },
            Page = 1, PerPage = 1,
        };
        var page2 = new PhotoQueryParameters
        {
            Rovers = "curiosity",
            RoverList = new List<string> { "curiosity" },
            Page = 2, PerPage = 1,
        };

        await _photoQueryService.QueryPhotosAsync(page1, default);

        SqlCapture.Clear();
        var second = await _photoQueryService.QueryPhotosAsync(page2, default);

        CountSqls(SqlCapture.ExecutedSql).Should().BeEmpty(
            "page/per_page do not affect the total count, so page 2 must reuse page 1's cached count");
        second.Meta!.TotalCount.Should().Be(2);
        second.Data.Should().ContainSingle("per_page=1 still returns one live row");
    }

    [Fact]
    public async Task UnfilteredQuery_IsCachedToo()
    {
        var parameters = new PhotoQueryParameters { Page = 1, PerPage = 10 };

        await _photoQueryService.QueryPhotosAsync(parameters, default);

        SqlCapture.Clear();
        var second = await _photoQueryService.QueryPhotosAsync(parameters, default);

        CountSqls(SqlCapture.ExecutedSql).Should().BeEmpty(
            "the unfiltered (all-photos) count is the most expensive shape and must be cached");
        second.Meta!.TotalCount.Should().Be(3);
    }
}
