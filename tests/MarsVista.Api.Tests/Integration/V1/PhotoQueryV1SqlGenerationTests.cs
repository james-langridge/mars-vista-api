using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MarsVista.Api.Services;
using MarsVista.Core.Entities;

namespace MarsVista.Api.Tests.Integration.V1;

/// <summary>
/// Verifies that v1 PhotoQueryService.QueryPhotosAsync and GetLatestPhotosAsync
/// emit SQL that uses rover_id / camera_id (resolved via the cache) rather than
/// joining via p.Rover.Name. Parallel to PhotoQuerySqlGenerationTests for v2.
///
/// /api/v1/rovers/{rover}/photos was hit ~746 times/week in production at the
/// time of this fix; covering the v1 path with the same regression net as v2
/// is the only way to catch a future change that re-introduces a name-based
/// join on only one of the two services.
/// </summary>
public class PhotoQueryV1SqlGenerationTests : IntegrationTestBase
{
    private IPhotoQueryService _photoQueryService = null!;

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IPhotoQueryService, PhotoQueryService>();
    }

    protected override async Task SeedAdditionalDataAsync()
    {
        _photoQueryService = ServiceProvider.GetRequiredService<IPhotoQueryService>();

        // Same shape as the v2 SQL-generation seed: one photo on each (rover, camera)
        // combination relevant to the assertions, including a photo on the
        // duplicate-name FHAZ camera on rover 2.
        var now = DateTime.UtcNow;
        DbContext.Photos.AddRange(
            new Photo
            {
                NasaId = "V1-CUR-FHAZ", ImgSrcFull = "x", ImgSrcLarge = "x", ImgSrcMedium = "x", ImgSrcSmall = "x",
                Sol = 1, EarthDate = new DateTime(2013, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2013, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                SampleType = "Full",
                RoverId = 1, CameraId = 1,           // Curiosity FHAZ
                CreatedAt = now, UpdatedAt = now,
            },
            new Photo
            {
                NasaId = "V1-CUR-MAST", ImgSrcFull = "x", ImgSrcLarge = "x", ImgSrcMedium = "x", ImgSrcSmall = "x",
                Sol = 2, EarthDate = new DateTime(2013, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2013, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                SampleType = "Full",
                RoverId = 1, CameraId = 2,           // Curiosity MAST (unique name)
                CreatedAt = now, UpdatedAt = now,
            },
            new Photo
            {
                NasaId = "V1-PER-FHAZ", ImgSrcFull = "x", ImgSrcLarge = "x", ImgSrcMedium = "x", ImgSrcSmall = "x",
                Sol = 500, EarthDate = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                SampleType = "Full",
                RoverId = 2, CameraId = 4,           // Perseverance FHAZ (duplicate name)
                CreatedAt = now, UpdatedAt = now,
            }
        );
        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task QueryPhotos_RoverFilter_EmitsScalarRoverIdEquality()
    {
        SqlCapture.Clear();

        await _photoQueryService.QueryPhotosAsync("curiosity");

        var photoSqls = SqlCapture.ExecutedSql
            .Where(s => s.Contains("FROM photos", StringComparison.OrdinalIgnoreCase))
            .ToList();

        photoSqls.Should().NotBeEmpty();
        foreach (var sql in photoSqls)
        {
            sql.Should().Contain("rover_id");
            sql.Should().NotContain("= ANY (",
                "single-rover filter must use scalar equality so the planner uses ix_photos_rover_id_sol_covering");
            sql.Should().NotContain("LOWER(",
                "rover filter must be rover_id-based, not name-based via JOIN");
        }
    }

    [Fact]
    public async Task QueryPhotos_RoverAndCameraWithUniqueName_EmitsScalarCameraIdEquality()
    {
        SqlCapture.Clear();

        // MAST exists only on Curiosity (camera id 2), so the predicate must
        // collapse to scalar = camera_id rather than an ANY/IN expansion.
        // v1's DTO projection includes camera/rover names so the captured SQL
        // legitimately joins cameras and rovers for SELECT - we only assert
        // that the WHERE side uses camera_id (no LOWER() name filter).
        await _photoQueryService.QueryPhotosAsync("curiosity", camera: "MAST");

        var photoSqls = SqlCapture.ExecutedSql
            .Where(s => s.Contains("FROM photos", StringComparison.OrdinalIgnoreCase))
            .ToList();

        photoSqls.Should().NotBeEmpty();
        foreach (var sql in photoSqls)
        {
            sql.Should().Contain("camera_id");
            sql.Should().NotContain("= ANY (",
                "single-id camera filter must use scalar equality");
            sql.Should().NotContain("LOWER(",
                "filter must be camera_id-based, not LOWER(camera.name) via JOIN");
        }
    }

    [Fact]
    public async Task QueryPhotos_RoverAndCameraWithDuplicateName_EmitsAnyArrayOverAllMatchingIds()
    {
        SqlCapture.Clear();

        // FHAZ exists on multiple rovers (the seed has rover 1 camera_id=1 and
        // rover 2 camera_id=4). GetCameraIdsByName("FHAZ") returns both, so the
        // SQL must keep both - even though after the rover_id filter only one
        // actually matches any photo, dropping ids here would silently change
        // observable semantics on a different rover/camera combination.
        await _photoQueryService.QueryPhotosAsync("curiosity", camera: "FHAZ");

        var photoSqls = SqlCapture.ExecutedSql
            .Where(s => s.Contains("FROM photos", StringComparison.OrdinalIgnoreCase))
            .ToList();

        photoSqls.Should().NotBeEmpty();
        foreach (var sql in photoSqls)
        {
            sql.Should().Contain("camera_id");
            (sql.Contains("= ANY (") || sql.Contains("camera_id IN ("))
                .Should().BeTrue(
                    "FHAZ resolves to multiple camera_ids in production; v1 must preserve all of them");
        }
    }

    [Fact]
    public async Task QueryPhotos_UnknownRover_ReturnsEmptyWithoutHittingPhotosTable()
    {
        SqlCapture.Clear();

        var (photos, totalCount) = await _photoQueryService.QueryPhotosAsync("bogus_rover");

        photos.Should().BeEmpty();
        totalCount.Should().Be(0);

        SqlCapture.ExecutedSql
            .Where(s => s.Contains("FROM photos", StringComparison.OrdinalIgnoreCase))
            .Should().BeEmpty("unknown rover must short-circuit before issuing a photos query");
    }

    [Fact]
    public async Task QueryPhotos_UnknownCamera_ReturnsEmptyWithoutHittingPhotosTable()
    {
        SqlCapture.Clear();

        var (photos, totalCount) = await _photoQueryService.QueryPhotosAsync(
            "curiosity", camera: "no_such_camera");

        photos.Should().BeEmpty();
        totalCount.Should().Be(0);

        SqlCapture.ExecutedSql
            .Where(s => s.Contains("FROM photos", StringComparison.OrdinalIgnoreCase))
            .Should().BeEmpty("unknown camera must short-circuit before issuing a photos query");
    }

    [Fact]
    public async Task GetLatestPhotos_EmitsScalarRoverIdInMaxSolQuery()
    {
        SqlCapture.Clear();

        await _photoQueryService.GetLatestPhotosAsync("perseverance");

        var photoSqls = SqlCapture.ExecutedSql
            .Where(s => s.Contains("FROM photos", StringComparison.OrdinalIgnoreCase))
            .ToList();

        photoSqls.Should().NotBeEmpty(
            "GetLatestPhotos must issue a MAX(sol) probe followed by a photos query");
        foreach (var sql in photoSqls)
        {
            sql.Should().Contain("rover_id");
            sql.Should().NotContain("= ANY (",
                "MAX(sol) probe and follow-up query must use scalar rover_id =");
            sql.Should().NotContain("LOWER(",
                "neither query should join via rover.name");
        }
    }

    [Fact]
    public async Task GetLatestPhotos_UnknownRover_ReturnsEmptyWithoutHittingPhotosTable()
    {
        SqlCapture.Clear();

        var (photos, totalCount) = await _photoQueryService.GetLatestPhotosAsync("bogus_rover");

        photos.Should().BeEmpty();
        totalCount.Should().Be(0);

        SqlCapture.ExecutedSql
            .Where(s => s.Contains("FROM photos", StringComparison.OrdinalIgnoreCase))
            .Should().BeEmpty();
    }
}
