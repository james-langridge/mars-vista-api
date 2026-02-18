using FluentAssertions;
using MarsVista.Api.Models.V2;
using MarsVista.Api.Validators.V2;
using Xunit;

namespace MarsVista.Api.Tests.Validators;

public class QueryParameterValidatorTests
{
    private const string RequestPath = "/api/v2/photos";

    [Fact]
    public void ReversedSolRange_ShouldAutoSwap()
    {
        var parameters = new PhotoQueryParameters { SolMin = 200, SolMax = 100 };

        var error = QueryParameterValidator.ValidatePhotoQuery(parameters, RequestPath);

        error.Should().BeNull();
        parameters.SolMin.Should().Be(100);
        parameters.SolMax.Should().Be(200);
    }

    [Fact]
    public void ReversedDateRange_ShouldAutoSwapBothRawAndParsed()
    {
        var parameters = new PhotoQueryParameters { DateMin = "2024-12-31", DateMax = "2024-01-01" };

        var error = QueryParameterValidator.ValidatePhotoQuery(parameters, RequestPath);

        error.Should().BeNull();
        parameters.DateMin.Should().Be("2024-01-01");
        parameters.DateMax.Should().Be("2024-12-31");
        parameters.DateMinParsed.Should().Be(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        parameters.DateMaxParsed.Should().Be(new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ReversedMarsTimeRange_ShouldAutoSwap()
    {
        var parameters = new PhotoQueryParameters { MarsTimeMin = "M18:00:00", MarsTimeMax = "M06:00:00" };

        var error = QueryParameterValidator.ValidatePhotoQuery(parameters, RequestPath);

        error.Should().BeNull();
        parameters.MarsTimeMin.Should().Be("M06:00:00");
        parameters.MarsTimeMax.Should().Be("M18:00:00");
        parameters.MarsTimeMinParsed.Should().Be(TimeSpan.FromHours(6));
        parameters.MarsTimeMaxParsed.Should().Be(TimeSpan.FromHours(18));
    }

    [Fact]
    public void ReversedSiteRange_ShouldAutoSwap()
    {
        var parameters = new PhotoQueryParameters { SiteMin = 80, SiteMax = 70 };

        var error = QueryParameterValidator.ValidatePhotoQuery(parameters, RequestPath);

        error.Should().BeNull();
        parameters.SiteMin.Should().Be(70);
        parameters.SiteMax.Should().Be(80);
    }

    [Fact]
    public void ReversedDriveRange_ShouldAutoSwap()
    {
        var parameters = new PhotoQueryParameters { DriveMin = 1200, DriveMax = 1000 };

        var error = QueryParameterValidator.ValidatePhotoQuery(parameters, RequestPath);

        error.Should().BeNull();
        parameters.DriveMin.Should().Be(1000);
        parameters.DriveMax.Should().Be(1200);
    }

    [Fact]
    public void ReversedWidthRange_ShouldAutoSwap()
    {
        var parameters = new PhotoQueryParameters { MinWidth = 1920, MaxWidth = 1024 };

        var error = QueryParameterValidator.ValidatePhotoQuery(parameters, RequestPath);

        error.Should().BeNull();
        parameters.MinWidth.Should().Be(1024);
        parameters.MaxWidth.Should().Be(1920);
    }

    [Fact]
    public void ReversedHeightRange_ShouldAutoSwap()
    {
        var parameters = new PhotoQueryParameters { MinHeight = 1080, MaxHeight = 768 };

        var error = QueryParameterValidator.ValidatePhotoQuery(parameters, RequestPath);

        error.Should().BeNull();
        parameters.MinHeight.Should().Be(768);
        parameters.MaxHeight.Should().Be(1080);
    }

    [Fact]
    public void ReversedElevationRange_ShouldAutoSwap()
    {
        var parameters = new PhotoQueryParameters { MastElevationMin = 30, MastElevationMax = -30 };

        var error = QueryParameterValidator.ValidatePhotoQuery(parameters, RequestPath);

        error.Should().BeNull();
        parameters.MastElevationMin.Should().Be(-30);
        parameters.MastElevationMax.Should().Be(30);
    }

    [Fact]
    public void ReversedAzimuthRange_ShouldReturnError_BecauseCircularWrapAround()
    {
        var parameters = new PhotoQueryParameters { MastAzimuthMin = 350, MastAzimuthMax = 10 };

        var error = QueryParameterValidator.ValidatePhotoQuery(parameters, RequestPath);

        error.Should().NotBeNull();
        error!.Errors.Should().ContainSingle(e => e.Field == "mast_azimuth_min");
    }

    [Fact]
    public void CorrectRanges_ShouldNotSwap()
    {
        var parameters = new PhotoQueryParameters
        {
            SolMin = 100,
            SolMax = 200,
            DateMin = "2024-01-01",
            DateMax = "2024-12-31",
            SiteMin = 70,
            SiteMax = 80
        };

        var error = QueryParameterValidator.ValidatePhotoQuery(parameters, RequestPath);

        error.Should().BeNull();
        parameters.SolMin.Should().Be(100);
        parameters.SolMax.Should().Be(200);
        parameters.DateMin.Should().Be("2024-01-01");
        parameters.DateMax.Should().Be("2024-12-31");
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(-100, 1)]
    [InlineData(1, 1)]
    [InlineData(5, 5)]
    [InlineData(null, 1)]
    public void PageNumber_ShouldClampToMinimumOf1(int? page, int expected)
    {
        var parameters = new PhotoQueryParameters { Page = page };

        parameters.PageNumber.Should().Be(expected);
    }
}
