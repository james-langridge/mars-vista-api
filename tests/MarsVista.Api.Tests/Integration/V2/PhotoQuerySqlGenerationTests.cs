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
    public async Task SingleRoverDefaultSort_OrdersBySolThenDate()
    {
        SqlCapture.Clear();

        var parameters = new PhotoQueryParameters
        {
            Rovers = "curiosity",
            RoverList = new List<string> { "curiosity" },
            SolMin = 1, SolMax = 1000,
            Page = 1, PerPage = 10,
        };

        await _photoQueryService.QueryPhotosAsync(parameters, default);

        // The default "most recent first" sort must be expressed sol-first for a
        // single-rover query so the planner serves it from
        // ix_photos_rover_id_camera_id_sol / ix_photos_rover_id_sol_covering with
        // an incremental sort, instead of backward-scanning ix_photos_date_taken_utc
        // past every newer photo of every rover (~1.2M rows discarded, 4-5s per
        // request in production for wide sol ranges).
        var dataSql = SqlCapture.ExecutedSql
            .Where(s => s.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase))
            .ToList();

        dataSql.Should().NotBeEmpty("the paginated data query must be ordered");
        foreach (var sql in dataSql)
        {
            sql.Should().MatchRegex(
                @"ORDER BY\s+\S*\.?""?sol""?\s+DESC\s*,\s*\S*\.?""?date_taken_utc""?\s+DESC",
                "single-rover default sort must be sol DESC, date_taken_utc DESC");
        }
    }

    [Fact]
    public async Task MultiRoverDefaultSort_OrdersByDateAlone()
    {
        SqlCapture.Clear();

        var parameters = new PhotoQueryParameters
        {
            Rovers = "curiosity,perseverance",
            RoverList = new List<string> { "curiosity", "perseverance" },
            Page = 1, PerPage = 10,
        };

        await _photoQueryService.QueryPhotosAsync(parameters, default);

        // Across rovers, sol numbers are not comparable (Spirit sol 1 is 2004,
        // Curiosity sol 1 is 2012), so the multi-rover default sort must stay
        // date-only.
        var dataSql = SqlCapture.ExecutedSql
            .Where(s => s.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase))
            .ToList();

        dataSql.Should().NotBeEmpty();
        foreach (var sql in dataSql)
        {
            sql.Should().NotMatchRegex(
                @"ORDER BY\s+\S*\.?""?sol""?",
                "multi-rover queries must not sort by sol - sols are not comparable across rovers");
        }
    }

    // Explicit date-family sorts on single-rover queries get the same sol
    // prefix as the default sort, in the requested direction - the prefix only
    // refines ties the caller left unspecified (earth_date is verified
    // perfectly sol-monotone in production; date_taken_utc modulo the known
    // early-Perseverance anomalies). Sorts where the prefix could reorder an
    // explicit tiebreak (earth_date with a tail) are left exactly as given.

    [Fact]
    public async Task SingleRoverExplicitDateTakenSort_GetsSolPrefix()
    {
        var dataSql = await CaptureOrderedSql("curiosity", new List<SortField>
        {
            new() { Field = "date_taken_utc", Direction = SortDirection.Descending },
        });

        foreach (var sql in dataSql)
        {
            sql.Should().MatchRegex(
                @"ORDER BY\s+\S*\.?""?sol""?\s+DESC\s*,\s*\S*\.?""?date_taken_utc""?\s+DESC",
                "explicit -date_taken_utc on a single rover must be served sol-first");
        }
    }

    [Fact]
    public async Task SingleRoverExplicitEarthDateSort_GetsSolPrefix()
    {
        var dataSql = await CaptureOrderedSql("curiosity", new List<SortField>
        {
            new() { Field = "earth_date", Direction = SortDirection.Descending },
        });

        foreach (var sql in dataSql)
        {
            sql.Should().MatchRegex(
                @"ORDER BY\s+\S*\.?""?sol""?\s+DESC\s*,\s*\S*\.?""?earth_date""?\s+DESC",
                "explicit -earth_date on a single rover must be served sol-first");
        }
    }

    [Fact]
    public async Task SingleRoverExplicitEarthDateAscending_GetsAscendingSolPrefix()
    {
        var dataSql = await CaptureOrderedSql("curiosity", new List<SortField>
        {
            new() { Field = "earth_date", Direction = SortDirection.Ascending },
        });

        foreach (var sql in dataSql)
        {
            // EF emits no keyword for ascending, so both keys must be followed
            // directly by a comma / end - not DESC.
            sql.Should().MatchRegex(
                @"ORDER BY\s+\S*\.?""?sol""?\s*,\s*\S*\.?""?earth_date""?(?!\s+DESC)",
                "ascending earth_date must get an ascending sol prefix, both ascending");
        }
    }

    [Fact]
    public async Task SingleRoverExplicitDateTakenAscending_GetsAscendingSolPrefix()
    {
        var dataSql = await CaptureOrderedSql("curiosity", new List<SortField>
        {
            new() { Field = "date_taken_utc", Direction = SortDirection.Ascending },
        });

        foreach (var sql in dataSql)
        {
            sql.Should().MatchRegex(
                @"ORDER BY\s+\S*\.?""?sol""?\s*,\s*\S*\.?""?date_taken_utc""?(?!\s+DESC)",
                "ascending date_taken_utc must get an ascending sol prefix, both ascending");
        }
    }

    [Fact]
    public async Task SingleRoverDateTakenSortWithNavigationTail_KeepsJoinedTail()
    {
        var dataSql = await CaptureOrderedSql("curiosity", new List<SortField>
        {
            new() { Field = "date_taken_utc", Direction = SortDirection.Descending },
            new() { Field = "camera", Direction = SortDirection.Ascending },
        });

        foreach (var sql in dataSql)
        {
            // The camera tail sorts on the joined cameras.name; the sol seed
            // must not disturb how the navigation tail is composed.
            sql.Should().MatchRegex(
                @"ORDER BY\s+\S*\.?""?sol""?\s+DESC\s*,\s*\S*\.?""?date_taken_utc""?\s+DESC\s*,\s*\S*\.?""?name""?",
                "a navigation-property tail must follow the sol prefix intact");
        }
    }

    [Fact]
    public async Task SingleRoverExplicitDateTakenSort_SolWinsOverDiscordantDate()
    {
        // Pins the accepted trade-off consciously for the EXPLICIT sort, not
        // just the default: where sol and timestamp disagree (the 17
        // early-Perseverance boundary anomalies in production), sol-attributed
        // order wins even though the caller literally asked for timestamp
        // order. Under a pure -date_taken_utc sort these would come back
        // reversed.
        var now = DateTime.UtcNow;
        DbContext.Photos.AddRange(
            new Photo
            {
                NasaId = "XDIS-SOL-WINS", ImgSrcFull = "x", ImgSrcLarge = "x", ImgSrcMedium = "x", ImgSrcSmall = "x",
                Sol = 3100, EarthDate = new DateTime(2013, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2013, 7, 1, 10, 0, 0, DateTimeKind.Utc),
                SampleType = "Full", RoverId = 1, CameraId = 1,
                CreatedAt = now, UpdatedAt = now,
            },
            new Photo
            {
                NasaId = "XDIS-DATE-LOSES", ImgSrcFull = "x", ImgSrcLarge = "x", ImgSrcMedium = "x", ImgSrcSmall = "x",
                Sol = 3000, EarthDate = new DateTime(2015, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2015, 7, 1, 10, 0, 0, DateTimeKind.Utc),
                SampleType = "Full", RoverId = 1, CameraId = 1,
                CreatedAt = now, UpdatedAt = now,
            });
        await DbContext.SaveChangesAsync();

        var parameters = new PhotoQueryParameters
        {
            Rovers = "curiosity",
            RoverList = new List<string> { "curiosity" },
            SolMin = 3000, SolMax = 3200,
            SortFields = new List<SortField>
            {
                new() { Field = "date_taken_utc", Direction = SortDirection.Descending },
            },
            Page = 1, PerPage = 10,
        };

        var response = await _photoQueryService.QueryPhotosAsync(parameters, default);

        response.Data.Select(p => p.Attributes!.NasaId).Should().Equal(
            "XDIS-SOL-WINS", "XDIS-DATE-LOSES");
    }

    [Fact]
    public async Task SingleRoverDateTakenSortWithTail_KeepsTailAfterSolPrefix()
    {
        var dataSql = await CaptureOrderedSql("curiosity", new List<SortField>
        {
            new() { Field = "date_taken_utc", Direction = SortDirection.Descending },
            new() { Field = "id", Direction = SortDirection.Descending },
        });

        foreach (var sql in dataSql)
        {
            sql.Should().MatchRegex(
                @"ORDER BY\s+\S*\.?""?sol""?\s+DESC\s*,\s*\S*\.?""?date_taken_utc""?\s+DESC\s*,\s*\S*\.?""?id""?\s+DESC",
                "the caller's tiebreak tail must follow the sol prefix intact");
        }
    }

    [Fact]
    public async Task SingleRoverEarthDateSortWithTail_StaysAsGiven()
    {
        var dataSql = await CaptureOrderedSql("curiosity", new List<SortField>
        {
            new() { Field = "earth_date", Direction = SortDirection.Descending },
            new() { Field = "camera", Direction = SortDirection.Ascending },
        });

        foreach (var sql in dataSql)
        {
            // Adjacent sols share earth_dates, so a sol prefix would override
            // the caller's camera tiebreak for same-date photos.
            sql.Should().NotMatchRegex(
                @"ORDER BY\s+\S*\.?""?sol""?",
                "earth_date with an explicit tiebreak must be honoured exactly as given");
        }
    }

    [Fact]
    public async Task MultiRoverExplicitEarthDateSort_StaysAsGiven()
    {
        var dataSql = await CaptureOrderedSql("curiosity,perseverance", new List<SortField>
        {
            new() { Field = "earth_date", Direction = SortDirection.Descending },
        });

        foreach (var sql in dataSql)
        {
            sql.Should().NotMatchRegex(
                @"ORDER BY\s+\S*\.?""?sol""?",
                "sols are not comparable across rovers");
        }
    }

    [Fact]
    public async Task SingleRoverNonDateSort_StaysAsGiven()
    {
        var dataSql = await CaptureOrderedSql("curiosity", new List<SortField>
        {
            new() { Field = "id", Direction = SortDirection.Descending },
        });

        foreach (var sql in dataSql)
        {
            sql.Should().NotMatchRegex(
                @"ORDER BY\s+\S*\.?""?sol""?",
                "only date-family sorts benefit from a sol prefix");
        }
    }

    /// <summary>
    /// Runs a query with the given rovers and explicit sort, returning the
    /// captured ORDER BY SQL. The validator normally parses Sort into
    /// SortFields before the service runs; tests set SortFields directly.
    /// </summary>
    private async Task<List<string>> CaptureOrderedSql(string rovers, List<SortField> sortFields)
    {
        SqlCapture.Clear();

        var parameters = new PhotoQueryParameters
        {
            Rovers = rovers,
            RoverList = rovers.Split(',').ToList(),
            SortFields = sortFields,
            Page = 1, PerPage = 10,
        };

        await _photoQueryService.QueryPhotosAsync(parameters, default);

        var dataSql = SqlCapture.ExecutedSql
            .Where(s => s.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase))
            .ToList();

        dataSql.Should().NotBeEmpty();
        return dataSql;
    }

    [Fact]
    public async Task SingleRoverDefaultSort_ReturnsPhotosMostRecentFirst()
    {
        // Three curiosity photos out of sol order in the table; the default sort
        // must return them newest-first regardless of how it is expressed in SQL.
        var now = DateTime.UtcNow;
        DbContext.Photos.AddRange(
            new Photo
            {
                NasaId = "ORD-MID", ImgSrcFull = "x", ImgSrcLarge = "x", ImgSrcMedium = "x", ImgSrcSmall = "x",
                Sol = 500, EarthDate = new DateTime(2014, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2014, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                SampleType = "Full", RoverId = 1, CameraId = 1,
                CreatedAt = now, UpdatedAt = now,
            },
            new Photo
            {
                NasaId = "ORD-NEW", ImgSrcFull = "x", ImgSrcLarge = "x", ImgSrcMedium = "x", ImgSrcSmall = "x",
                Sol = 900, EarthDate = new DateTime(2015, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2015, 2, 1, 8, 0, 0, DateTimeKind.Utc),
                SampleType = "Full", RoverId = 1, CameraId = 1,
                CreatedAt = now, UpdatedAt = now,
            },
            new Photo
            {
                NasaId = "ORD-OLD", ImgSrcFull = "x", ImgSrcLarge = "x", ImgSrcMedium = "x", ImgSrcSmall = "x",
                Sol = 100, EarthDate = new DateTime(2013, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2013, 3, 1, 9, 0, 0, DateTimeKind.Utc),
                SampleType = "Full", RoverId = 1, CameraId = 1,
                CreatedAt = now, UpdatedAt = now,
            });
        await DbContext.SaveChangesAsync();

        var parameters = new PhotoQueryParameters
        {
            Rovers = "curiosity",
            RoverList = new List<string> { "curiosity" },
            SolMin = 50, SolMax = 950,
            Page = 1, PerPage = 10,
        };

        var response = await _photoQueryService.QueryPhotosAsync(parameters, default);

        response.Data.Select(p => p.Attributes!.NasaId).Should().ContainInOrder(
            "ORD-NEW", "ORD-MID", "ORD-OLD");
    }

    [Fact]
    public async Task SingleRoverDefaultSort_SolWinsOverDiscordantDate()
    {
        // Pins the one deliberate semantic choice of the sol-first rewrite:
        // when sol and date disagree (17 early-Perseverance sol boundaries in
        // production have date_taken_utc overlapping the neighbouring sol by up
        // to 17 days - NASA data quirks), the default order follows sol, i.e.
        // NASA's mission attribution, not the unreliable timestamp. Under the
        // old date-only sort these two photos would come back reversed.
        var now = DateTime.UtcNow;
        DbContext.Photos.AddRange(
            new Photo
            {
                NasaId = "DIS-SOL-WINS", ImgSrcFull = "x", ImgSrcLarge = "x", ImgSrcMedium = "x", ImgSrcSmall = "x",
                Sol = 2100, EarthDate = new DateTime(2013, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2013, 6, 1, 10, 0, 0, DateTimeKind.Utc),
                SampleType = "Full", RoverId = 1, CameraId = 1,
                CreatedAt = now, UpdatedAt = now,
            },
            new Photo
            {
                NasaId = "DIS-DATE-LOSES", ImgSrcFull = "x", ImgSrcLarge = "x", ImgSrcMedium = "x", ImgSrcSmall = "x",
                Sol = 2000, EarthDate = new DateTime(2015, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2015, 6, 1, 10, 0, 0, DateTimeKind.Utc),
                SampleType = "Full", RoverId = 1, CameraId = 1,
                CreatedAt = now, UpdatedAt = now,
            });
        await DbContext.SaveChangesAsync();

        var parameters = new PhotoQueryParameters
        {
            Rovers = "curiosity",
            RoverList = new List<string> { "curiosity" },
            SolMin = 2000, SolMax = 2200,
            Page = 1, PerPage = 10,
        };

        var response = await _photoQueryService.QueryPhotosAsync(parameters, default);

        response.Data.Select(p => p.Attributes!.NasaId).Should().Equal(
            "DIS-SOL-WINS", "DIS-DATE-LOSES");
    }

    [Fact]
    public async Task SingleRoverMultiCameraFilter_KeepsSolFirstDefaultSort()
    {
        SqlCapture.Clear();

        // FHAZ resolves to two camera ids (duplicate name across rovers) and
        // MAST to one, so the camera predicate is a multi-id ANY/IN - the sort
        // decision must still be sol-first because it depends only on the
        // rover filter resolving to a single rover.
        var parameters = new PhotoQueryParameters
        {
            Rovers = "curiosity",
            RoverList = new List<string> { "curiosity" },
            Cameras = "FHAZ,MAST",
            CameraList = new List<string> { "FHAZ", "MAST" },
            Page = 1, PerPage = 10,
        };

        await _photoQueryService.QueryPhotosAsync(parameters, default);

        var dataSql = SqlCapture.ExecutedSql
            .Where(s => s.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase))
            .ToList();

        dataSql.Should().NotBeEmpty();
        foreach (var sql in dataSql)
        {
            sql.Should().Contain("camera_id");
            (sql.Contains("= ANY (") || sql.Contains("camera_id IN ("))
                .Should().BeTrue("multi-id camera filter must keep all camera ids");
            sql.Should().MatchRegex(
                @"ORDER BY\s+\S*\.?""?sol""?\s+DESC\s*,\s*\S*\.?""?date_taken_utc""?\s+DESC",
                "a multi-camera filter must not flip the single-rover sol-first sort");
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

    [Fact]
    public async Task MinRatingCountFilter_EmitsAggregatedSubquery_NotCorrelated()
    {
        // Seed two photos and two ratings on the second so a min_rating_count=2
        // filter can return exactly one. The same data lets us verify that the
        // generated SQL uses GROUP BY photo_id once, not a per-row correlated
        // subquery (which previously caused a 5.5 s / 23 GB scan on production).
        var now = DateTime.UtcNow;
        var p1 = new Photo
        {
            NasaId = "R-NO-RATINGS", ImgSrcFull = "x", ImgSrcLarge = "x", ImgSrcMedium = "x", ImgSrcSmall = "x",
            Sol = 10, EarthDate = new DateTime(2013, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            DateTakenUtc = new DateTime(2013, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            SampleType = "Full", RoverId = 1, CameraId = 1,
            CreatedAt = now, UpdatedAt = now,
        };
        var p2 = new Photo
        {
            NasaId = "R-TWO-RATINGS", ImgSrcFull = "x", ImgSrcLarge = "x", ImgSrcMedium = "x", ImgSrcSmall = "x",
            Sol = 11, EarthDate = new DateTime(2013, 5, 2, 0, 0, 0, DateTimeKind.Utc),
            DateTakenUtc = new DateTime(2013, 5, 2, 0, 0, 0, DateTimeKind.Utc),
            SampleType = "Full", RoverId = 1, CameraId = 1,
            CreatedAt = now, UpdatedAt = now,
        };
        DbContext.Photos.AddRange(p1, p2);
        await DbContext.SaveChangesAsync();
        DbContext.PhotoRatings.AddRange(
            new PhotoRating { PhotoId = p2.Id, Rating = 4, ClientId = "client-a", CreatedAt = now, UpdatedAt = now },
            new PhotoRating { PhotoId = p2.Id, Rating = 5, ClientId = "client-b", CreatedAt = now, UpdatedAt = now });
        await DbContext.SaveChangesAsync();

        SqlCapture.Clear();

        var parameters = new PhotoQueryParameters
        {
            MinRatingCount = 2,
            Page = 1, PerPage = 10,
        };

        var response = await _photoQueryService.QueryPhotosAsync(parameters, default);

        // Correctness: only p2 (2 ratings) qualifies. p1 (0 ratings) and the
        // three base-seed photos (Q-CUR-FHAZ, Q-PER-NAVCAM, Q-PER-FHAZ - see
        // SeedAdditionalDataAsync above) have no photo_ratings rows so they
        // do not appear in the GroupBy result the subquery filters on.
        response.Data.Should().ContainSingle();
        response.Data.Single().Attributes!.NasaId.Should().Be("R-TWO-RATINGS");

        // SQL shape: the predicate must aggregate once, not per outer row.
        var ratingSqls = SqlCapture.ExecutedSql
            .Where(s => s.Contains("photo_ratings", StringComparison.OrdinalIgnoreCase))
            .ToList();
        ratingSqls.Should().NotBeEmpty();
        foreach (var sql in ratingSqls)
        {
            sql.Should().Contain("GROUP BY",
                "MinRatingCount must compile to an aggregated subquery, not a correlated per-row COUNT");
        }
    }

    [Fact]
    public async Task MinRatingFilter_EmitsAggregatedSubquery_NotCorrelated()
    {
        var now = DateTime.UtcNow;
        var pLow = new Photo
        {
            NasaId = "AVG-LOW", ImgSrcFull = "x", ImgSrcLarge = "x", ImgSrcMedium = "x", ImgSrcSmall = "x",
            Sol = 20, EarthDate = new DateTime(2013, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            DateTakenUtc = new DateTime(2013, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            SampleType = "Full", RoverId = 1, CameraId = 1,
            CreatedAt = now, UpdatedAt = now,
        };
        var pHigh = new Photo
        {
            NasaId = "AVG-HIGH", ImgSrcFull = "x", ImgSrcLarge = "x", ImgSrcMedium = "x", ImgSrcSmall = "x",
            Sol = 21, EarthDate = new DateTime(2013, 6, 2, 0, 0, 0, DateTimeKind.Utc),
            DateTakenUtc = new DateTime(2013, 6, 2, 0, 0, 0, DateTimeKind.Utc),
            SampleType = "Full", RoverId = 1, CameraId = 1,
            CreatedAt = now, UpdatedAt = now,
        };
        DbContext.Photos.AddRange(pLow, pHigh);
        await DbContext.SaveChangesAsync();
        DbContext.PhotoRatings.AddRange(
            new PhotoRating { PhotoId = pLow.Id,  Rating = 2, ClientId = "c1", CreatedAt = now, UpdatedAt = now },
            new PhotoRating { PhotoId = pHigh.Id, Rating = 5, ClientId = "c2", CreatedAt = now, UpdatedAt = now });
        await DbContext.SaveChangesAsync();

        SqlCapture.Clear();

        var parameters = new PhotoQueryParameters
        {
            MinRating = 4.0,
            Page = 1, PerPage = 10,
        };

        var response = await _photoQueryService.QueryPhotosAsync(parameters, default);

        // Correctness: only pHigh (avg 5) qualifies; pLow (avg 2) does not.
        // The three base-seed photos (Q-CUR-FHAZ, Q-PER-NAVCAM, Q-PER-FHAZ -
        // see SeedAdditionalDataAsync above) have no photo_ratings rows so
        // they are excluded by the GroupBy subquery.
        response.Data.Should().ContainSingle();
        response.Data.Single().Attributes!.NasaId.Should().Be("AVG-HIGH");

        var ratingSqls = SqlCapture.ExecutedSql
            .Where(s => s.Contains("photo_ratings", StringComparison.OrdinalIgnoreCase))
            .ToList();
        ratingSqls.Should().NotBeEmpty();
        foreach (var sql in ratingSqls)
        {
            sql.Should().Contain("GROUP BY",
                "MinRating must compile to an aggregated subquery, not a correlated per-row AVG");
        }
    }
}
