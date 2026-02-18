using FluentAssertions;
using MarsVista.Core.Entities;
using MarsVista.Api.Services.V2;

namespace MarsVista.Api.Tests.Services.V2;

public class PanoramaStitchingServiceTests
{
    [Fact]
    public void SelectPhotosForStitching_PicksOnePhotoPerAzimuth()
    {
        var photos = new List<Photo>
        {
            CreatePhoto(1, mastAz: 45.0f, filterName: "RGB"),
            CreatePhoto(2, mastAz: 45.3f, filterName: "RGB"), // Same rounded azimuth
            CreatePhoto(3, mastAz: 55.0f, filterName: "RGB"),
            CreatePhoto(4, mastAz: 65.0f, filterName: "RGB"),
        };

        var selected = PanoramaStitchingService.SelectPhotosForStitching(photos);

        selected.Should().HaveCount(3); // 45, 55, 65
        selected.Select(p => Math.Round(p.MastAz!.Value)).Should().BeEquivalentTo(new[] { 45.0, 55.0, 65.0 });
    }

    [Fact]
    public void SelectPhotosForStitching_PrefersRgbFilter()
    {
        var photos = new List<Photo>
        {
            CreatePhoto(1, mastAz: 45.0f, filterName: "L0 (Visible)"),
            CreatePhoto(2, mastAz: 45.0f, filterName: "RGB Bayer"),
            CreatePhoto(3, mastAz: 55.0f, filterName: "L0 (Visible)"),
        };

        var selected = PanoramaStitchingService.SelectPhotosForStitching(photos);

        selected.Should().HaveCount(2);
        selected.First(p => Math.Round(p.MastAz!.Value) == 45).FilterName.Should().Be("RGB Bayer");
    }

    [Fact]
    public void SelectPhotosForStitching_PrefersBayerFilter()
    {
        var photos = new List<Photo>
        {
            CreatePhoto(1, mastAz: 45.0f, filterName: "IR"),
            CreatePhoto(2, mastAz: 45.0f, filterName: "Bayer"),
        };

        var selected = PanoramaStitchingService.SelectPhotosForStitching(photos);

        selected.Should().HaveCount(1);
        selected[0].FilterName.Should().Be("Bayer");
    }

    [Fact]
    public void SelectPhotosForStitching_PrefersLargerImages()
    {
        var photos = new List<Photo>
        {
            CreatePhoto(1, mastAz: 45.0f, width: 320),
            CreatePhoto(2, mastAz: 45.0f, width: 1200),
        };

        var selected = PanoramaStitchingService.SelectPhotosForStitching(photos);

        selected.Should().HaveCount(1);
        selected[0].Width.Should().Be(1200);
    }

    [Fact]
    public void SelectPhotosForStitching_OrdersByAzimuthLeftToRight()
    {
        var photos = new List<Photo>
        {
            CreatePhoto(1, mastAz: 90.0f),
            CreatePhoto(2, mastAz: 45.0f),
            CreatePhoto(3, mastAz: 135.0f),
            CreatePhoto(4, mastAz: 0.0f),
        };

        var selected = PanoramaStitchingService.SelectPhotosForStitching(photos);

        selected.Should().HaveCount(4);
        selected.Select(p => p.MastAz!.Value).Should().BeInAscendingOrder();
    }

    [Fact]
    public void SelectPhotosForStitching_HandlesNullAzimuth()
    {
        var photos = new List<Photo>
        {
            CreatePhoto(1, mastAz: null),
            CreatePhoto(2, mastAz: 45.0f),
            CreatePhoto(3, mastAz: 55.0f),
        };

        var selected = PanoramaStitchingService.SelectPhotosForStitching(photos);

        selected.Should().HaveCount(2); // Skips null azimuth
    }

    [Fact]
    public void SelectPhotosForStitching_HandlesSinglePhoto()
    {
        var photos = new List<Photo>
        {
            CreatePhoto(1, mastAz: 45.0f),
        };

        var selected = PanoramaStitchingService.SelectPhotosForStitching(photos);

        selected.Should().HaveCount(1);
    }

    [Fact]
    public void SelectPhotosForStitching_HandlesEmptyList()
    {
        var photos = new List<Photo>();

        var selected = PanoramaStitchingService.SelectPhotosForStitching(photos);

        selected.Should().BeEmpty();
    }

    [Fact]
    public void SelectPhotosForStitching_HandlesMissingFilterName()
    {
        var photos = new List<Photo>
        {
            CreatePhoto(1, mastAz: 45.0f, filterName: null),
            CreatePhoto(2, mastAz: 45.0f, filterName: null, width: 800),
        };

        var selected = PanoramaStitchingService.SelectPhotosForStitching(photos);

        selected.Should().HaveCount(1);
        // With no filter preference, should prefer larger width
        selected[0].Width.Should().Be(800);
    }

    private static Photo CreatePhoto(int id, float? mastAz, string? filterName = null, int? width = null)
    {
        return new Photo
        {
            Id = id,
            NasaId = $"TEST_{id}",
            Sol = 1000,
            DateTakenUtc = DateTime.UtcNow,
            ImgSrcFull = $"https://example.com/photo{id}.jpg",
            MastAz = mastAz,
            MastEl = -10.0f,
            FilterName = filterName,
            Width = width,
            RoverId = 1,
            CameraId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
