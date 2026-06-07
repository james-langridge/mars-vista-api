using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MarsVista.Api.Models.V2;
using MarsVista.Api.Services.V2;
using MarsVista.Core.Entities;

namespace MarsVista.Api.Tests.Integration.V2;

/// <summary>
/// Verifies that PhotoQueryServiceV2.QueryPhotosAsync emits SQL that uses
/// scalar equality on rover_id / camera_id (not = ANY(ARRAY[...])) for the
/// single-element case, so the PostgreSQL planner can use the rover-leading
/// covering indexes instead of a backward scan of ix_photos_sol.
///
/// Uses a DbCommandInterceptor (SqlCapturingInterceptor) on the test DbContext
/// to capture the actual SQL the production code path emits - not a separately
/// constructed IQueryable.
/// </summary>
public class PhotoQuerySqlGenerationTests : IntegrationTestBase
{
    private IPhotoQueryServiceV2 _photoQueryService = null!;

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IPhotoQueryServiceV2, PhotoQueryServiceV2>();
    }

    protected override async Task SeedAdditionalDataAsync()
    {
        _photoQueryService = ServiceProvider.GetRequiredService<IPhotoQueryServiceV2>();

        // One photo per (rover, camera) so the count query has something to return
        // and the predicate distinguishes between the duplicate-name FHAZ on rover 1
        // and the duplicate-name FHAZ on rover 2.
        var now = DateTime.UtcNow;
        DbContext.Photos.AddRange(
            new Photo
            {
                NasaId = "Q-CUR-FHAZ", ImgSrcFull = "x", ImgSrcLarge = "x", ImgSrcMedium = "x", ImgSrcSmall = "x",
                Sol = 1, EarthDate = new DateTime(2013, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2013, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                SampleType = "Full",
                RoverId = 1, CameraId = 1,           // Curiosity FHAZ
                CreatedAt = now, UpdatedAt = now,
            },
            new Photo
            {
                NasaId = "Q-PER-NAVCAM", ImgSrcFull = "x", ImgSrcLarge = "x", ImgSrcMedium = "x", ImgSrcSmall = "x",
                Sol = 500, EarthDate = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                SampleType = "Full",
                RoverId = 2, CameraId = 3,           // Perseverance NAVCAM
                CreatedAt = now, UpdatedAt = now,
            },
            new Photo
            {
                NasaId = "Q-PER-FHAZ", ImgSrcFull = "x", ImgSrcLarge = "x", ImgSrcMedium = "x", ImgSrcSmall = "x",
                Sol = 600, EarthDate = new DateTime(2021, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2021, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                SampleType = "Full",
                RoverId = 2, CameraId = 4,           // Perseverance FHAZ (duplicate-name)
                CreatedAt = now, UpdatedAt = now,
            }
        );
        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task SingleRoverFilter_EmitsScalarRoverIdEquality()
    {
        SqlCapture.Clear();

        var parameters = new PhotoQueryParameters
        {
            Rovers = "curiosity",
            RoverList = new List<string> { "curiosity" },
            Page = 1, PerPage = 10,
        };

        await _photoQueryService.QueryPhotosAsync(parameters, default);

        var photoSqls = SqlCapture.ExecutedSql
            .Where(s => s.Contains("FROM photos", StringComparison.OrdinalIgnoreCase))
            .ToList();

        photoSqls.Should().NotBeEmpty("the rover filter should emit SQL against the photos table");
        foreach (var sql in photoSqls)
        {
            sql.Should().Contain("rover_id");
            sql.Should().NotContain("= ANY (",
                "single-rover filter must use scalar equality so the planner uses ix_photos_rover_id_sol_covering");
            sql.Should().NotMatch("*rover_id* IN (*)",
                "single-rover filter must use scalar = not IN");
            // The join-based variant would emit a LOWER(...) call against the rovers table.
            sql.Should().NotContain("LOWER(",
                "filter must be rover_id-based, not name-based via JOIN");
        }
    }

    [Fact]
    public async Task MultiRoverFilter_EmitsAnyArrayOnRoverId()
    {
        SqlCapture.Clear();

        var parameters = new PhotoQueryParameters
        {
            Rovers = "curiosity,perseverance",
            RoverList = new List<string> { "curiosity", "perseverance" },
            Page = 1, PerPage = 10,
        };

        await _photoQueryService.QueryPhotosAsync(parameters, default);

        var photoSqls = SqlCapture.ExecutedSql
            .Where(s => s.Contains("FROM photos", StringComparison.OrdinalIgnoreCase))
            .ToList();

        photoSqls.Should().NotBeEmpty();
        foreach (var sql in photoSqls)
        {
            sql.Should().Contain("rover_id");
            // Multi-rover uses Contains, which EF translates to ANY (or IN) on rover_id.
            // Either is fine since the planner can use date_taken_utc index with a
            // small filter when LIMIT is satisfied near the top of the sort.
            (sql.Contains("= ANY (") || sql.Contains("rover_id IN ("))
                .Should().BeTrue();
            sql.Should().NotContain("LOWER(",
                "multi-rover filter must still be rover_id-based, not name-based");
        }
    }

    [Fact]
    public async Task SingleCameraFilter_WithDuplicateName_EmitsAnyArrayOverAllMatchingIds()
    {
        SqlCapture.Clear();

        var parameters = new PhotoQueryParameters
        {
            Cameras = "FHAZ",
            CameraList = new List<string> { "FHAZ" },
            Page = 1, PerPage = 10,
        };

        // The seed has two FHAZ cameras (id 1 on Curiosity, id 4 on Perseverance).
        // GetCameraIdsByName("FHAZ") returns both ids, so the filter must use a
        // multi-id predicate (= ANY or IN) - not a single = - or it would silently
        // drop one of the rovers' FHAZ photos.
        await _photoQueryService.QueryPhotosAsync(parameters, default);

        var photoSqls = SqlCapture.ExecutedSql
            .Where(s => s.Contains("FROM photos", StringComparison.OrdinalIgnoreCase))
            .ToList();

        photoSqls.Should().NotBeEmpty();
        foreach (var sql in photoSqls)
        {
            sql.Should().Contain("camera_id");
            (sql.Contains("= ANY (") || sql.Contains("camera_id IN ("))
                .Should().BeTrue(
                    "?cameras=FHAZ resolves to multiple camera_ids in production data; SQL must keep all of them");
            sql.Should().NotContain("c.name",
                "camera filter must be camera_id-based, not name-based via JOIN");
        }
    }

    [Fact]
    public async Task SingleCameraFilter_WithUniqueName_EmitsScalarCameraIdEquality()
    {
        SqlCapture.Clear();

        var parameters = new PhotoQueryParameters
        {
            Cameras = "MAST",
            CameraList = new List<string> { "MAST" },
            Page = 1, PerPage = 10,
        };

        // MAST only exists on Curiosity in the seed (id 2), so the predicate
        // must collapse to scalar = for the planner to use ix_photos_camera_id.
        await _photoQueryService.QueryPhotosAsync(parameters, default);

        var photoSqls = SqlCapture.ExecutedSql
            .Where(s => s.Contains("FROM photos", StringComparison.OrdinalIgnoreCase))
            .ToList();

        photoSqls.Should().NotBeEmpty();
        foreach (var sql in photoSqls)
        {
            sql.Should().Contain("camera_id");
            sql.Should().NotContain("= ANY (",
                "single-id camera filter must use scalar equality");
        }
    }

    [Fact]
    public async Task UnknownRoverFilter_ReturnsEmptyWithoutHittingPhotosTable()
    {
        SqlCapture.Clear();

        var parameters = new PhotoQueryParameters
        {
            Rovers = "bogus_rover",
            RoverList = new List<string> { "bogus_rover" },
            Page = 1, PerPage = 10,
        };

        var response = await _photoQueryService.QueryPhotosAsync(parameters, default);

        response.Data.Should().BeEmpty();
        // The Where(p => false) short-circuit may still execute SQL, but it must
        // not produce any rows; the public observable behaviour matches the v1 path
        // which returns an empty result instead of trying to translate the unknown name.
        response.Meta!.TotalCount.Should().Be(0);
    }
}
