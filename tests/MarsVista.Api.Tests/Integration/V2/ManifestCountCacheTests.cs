using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MarsVista.Api.Services;
using MarsVista.Api.Services.V2;
using MarsVista.Core.Entities;

namespace MarsVista.Api.Tests.Integration.V2;

/// <summary>
/// The manifest endpoints embed the rover's photo count in their cache key
/// ("auto-invalidation via key mutation"), which means the COUNT(*) itself ran
/// on every manifest request even when the manifest body was cached. These
/// tests pin the fix: the count comes from the shared count cache, so a second
/// manifest request within the TTL executes no SQL against photos at all.
/// </summary>
public class ManifestCountCacheTests : IntegrationTestBase
{
    private IRoverQueryServiceV2 _roverServiceV2 = null!;
    private IRoverQueryService _roverServiceV1 = null!;

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IRoverQueryServiceV2, RoverQueryServiceV2>();
        services.AddScoped<IRoverQueryService, RoverQueryService>();
    }

    protected override async Task SeedAdditionalDataAsync()
    {
        _roverServiceV2 = ServiceProvider.GetRequiredService<IRoverQueryServiceV2>();
        _roverServiceV1 = ServiceProvider.GetRequiredService<IRoverQueryService>();

        var now = DateTime.UtcNow;
        DbContext.Photos.AddRange(
            new Photo
            {
                NasaId = "MCC-1", ImgSrcFull = "x", ImgSrcLarge = "x", ImgSrcMedium = "x", ImgSrcSmall = "x",
                Sol = 1, EarthDate = new DateTime(2013, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2013, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                SampleType = "Full",
                RoverId = 1, CameraId = 1,
                CreatedAt = now, UpdatedAt = now,
            },
            new Photo
            {
                NasaId = "MCC-2", ImgSrcFull = "x", ImgSrcLarge = "x", ImgSrcMedium = "x", ImgSrcSmall = "x",
                Sol = 2, EarthDate = new DateTime(2013, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                DateTakenUtc = new DateTime(2013, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                SampleType = "Full",
                RoverId = 1, CameraId = 2,
                CreatedAt = now, UpdatedAt = now,
            }
        );
        await DbContext.SaveChangesAsync();
    }

    // Match SQL against the photos TABLE. The rovers lookup that legitimately runs
    // on every request selects a total_photos column, so a bare "photos" substring
    // would false-positive on it.
    private static List<string> PhotosSqls(IReadOnlyList<string> executed) =>
        executed.Where(s => s.Contains("FROM photos", StringComparison.OrdinalIgnoreCase)).ToList();

    [Fact]
    public async Task V2Manifest_SecondRequest_ExecutesNoPhotosSql()
    {
        var first = await _roverServiceV2.GetRoverManifestAsync("curiosity", default);
        first!.Attributes!.TotalPhotos.Should().Be(2);

        SqlCapture.Clear();
        var second = await _roverServiceV2.GetRoverManifestAsync("curiosity", default);

        PhotosSqls(SqlCapture.ExecutedSql).Should().BeEmpty(
            "the second manifest request must serve both the photo count and the manifest body from cache");
        second!.Attributes!.TotalPhotos.Should().Be(2);
        second.Attributes.Photos.Should().HaveCount(2, "one manifest entry per seeded sol");
    }

    [Fact]
    public async Task V1Manifest_SecondRequest_ExecutesNoPhotosSql()
    {
        var first = await _roverServiceV1.GetManifestAsync("curiosity", default);
        first!.TotalPhotos.Should().Be(2);

        SqlCapture.Clear();
        var second = await _roverServiceV1.GetManifestAsync("curiosity", default);

        PhotosSqls(SqlCapture.ExecutedSql).Should().BeEmpty(
            "the second v1 manifest request must serve both the photo count and the manifest body from cache");
        second!.TotalPhotos.Should().Be(2);
    }
}
