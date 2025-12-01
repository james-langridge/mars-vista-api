using FluentAssertions;
using MarsVista.Core.Helpers;

namespace MarsVista.Scraper.Tests.Helpers;

public class PathSimplifierTests
{
    // ============================================================================
    // Distance3D Tests
    // ============================================================================

    [Fact]
    public void Distance3D_SamePoint_ReturnsZero()
    {
        var result = PathSimplifier.Distance3D(0, 0, 0, 0, 0, 0);

        result.Should().Be(0);
    }

    [Fact]
    public void Distance3D_UnitDistanceOnXAxis_ReturnsOne()
    {
        var result = PathSimplifier.Distance3D(0, 0, 0, 1, 0, 0);

        result.Should().BeApproximately(1.0f, 0.0001f);
    }

    [Fact]
    public void Distance3D_UnitDistanceOnYAxis_ReturnsOne()
    {
        var result = PathSimplifier.Distance3D(0, 0, 0, 0, 1, 0);

        result.Should().BeApproximately(1.0f, 0.0001f);
    }

    [Fact]
    public void Distance3D_UnitDistanceOnZAxis_ReturnsOne()
    {
        var result = PathSimplifier.Distance3D(0, 0, 0, 0, 0, 1);

        result.Should().BeApproximately(1.0f, 0.0001f);
    }

    [Fact]
    public void Distance3D_DiagonalIn2D_ReturnsCorrectDistance()
    {
        // 3-4-5 right triangle
        var result = PathSimplifier.Distance3D(0, 0, 0, 3, 4, 0);

        result.Should().BeApproximately(5.0f, 0.0001f);
    }

    [Fact]
    public void Distance3D_DiagonalIn3D_ReturnsCorrectDistance()
    {
        // sqrt(1^2 + 2^2 + 2^2) = sqrt(1 + 4 + 4) = sqrt(9) = 3
        var result = PathSimplifier.Distance3D(0, 0, 0, 1, 2, 2);

        result.Should().BeApproximately(3.0f, 0.0001f);
    }

    [Fact]
    public void Distance3D_NegativeCoordinates_ReturnsCorrectDistance()
    {
        // Distance from (-1, -1, -1) to (1, 1, 1)
        // sqrt(2^2 + 2^2 + 2^2) = sqrt(12) ≈ 3.464
        var result = PathSimplifier.Distance3D(-1, -1, -1, 1, 1, 1);

        result.Should().BeApproximately(MathF.Sqrt(12), 0.0001f);
    }

    [Fact]
    public void Distance3D_LargeCoordinates_ReturnsCorrectDistance()
    {
        // Typical Mars rover coordinates (meters from landing site)
        var result = PathSimplifier.Distance3D(-492.711f, -259.95f, -39.23f, -543.348f, -405.131f, -14.15f);

        // Calculate expected: sqrt((−543.348+492.711)² + (−405.131+259.95)² + (−14.15+39.23)²)
        var dx = -543.348f - (-492.711f);
        var dy = -405.131f - (-259.95f);
        var dz = -14.15f - (-39.23f);
        var expected = MathF.Sqrt(dx * dx + dy * dy + dz * dz);

        result.Should().BeApproximately(expected, 0.001f);
    }

    [Theory]
    [InlineData(0, 0, 0, 10, 0, 0, 10.0f)]
    [InlineData(0, 0, 0, 0, 10, 0, 10.0f)]
    [InlineData(0, 0, 0, 0, 0, 10, 10.0f)]
    [InlineData(5, 5, 5, 5, 5, 5, 0.0f)]
    [InlineData(0, 0, 0, 3, 4, 0, 5.0f)]
    [InlineData(1, 2, 3, 4, 6, 3, 5.0f)] // sqrt(3² + 4² + 0²) = 5
    public void Distance3D_VariousInputs_ReturnsCorrectDistance(
        float x1, float y1, float z1,
        float x2, float y2, float z2,
        float expected)
    {
        var result = PathSimplifier.Distance3D(x1, y1, z1, x2, y2, z2);

        result.Should().BeApproximately(expected, 0.0001f);
    }

    // ============================================================================
    // Bearing2D Tests
    // ============================================================================

    [Fact]
    public void Bearing2D_North_ReturnsZero()
    {
        // Moving from (0,0) to (0,10) - pure +Y direction is North (0°)
        var result = PathSimplifier.Bearing2D(0, 0, 0, 10);

        result.Should().BeApproximately(0.0f, 0.01f);
    }

    [Fact]
    public void Bearing2D_East_Returns90()
    {
        // Moving from (0,0) to (10,0) - pure +X direction is East (90°)
        var result = PathSimplifier.Bearing2D(0, 0, 10, 0);

        result.Should().BeApproximately(90.0f, 0.01f);
    }

    [Fact]
    public void Bearing2D_South_Returns180()
    {
        // Moving from (0,0) to (0,-10) - pure -Y direction is South (180°)
        var result = PathSimplifier.Bearing2D(0, 0, 0, -10);

        result.Should().BeApproximately(180.0f, 0.01f);
    }

    [Fact]
    public void Bearing2D_West_Returns270()
    {
        // Moving from (0,0) to (-10,0) - pure -X direction is West (270°)
        var result = PathSimplifier.Bearing2D(0, 0, -10, 0);

        result.Should().BeApproximately(270.0f, 0.01f);
    }

    [Fact]
    public void Bearing2D_Northeast_Returns45()
    {
        // Equal movement in +X and +Y
        var result = PathSimplifier.Bearing2D(0, 0, 10, 10);

        result.Should().BeApproximately(45.0f, 0.01f);
    }

    [Fact]
    public void Bearing2D_Southeast_Returns135()
    {
        // +X and -Y
        var result = PathSimplifier.Bearing2D(0, 0, 10, -10);

        result.Should().BeApproximately(135.0f, 0.01f);
    }

    [Fact]
    public void Bearing2D_Southwest_Returns225()
    {
        // -X and -Y
        var result = PathSimplifier.Bearing2D(0, 0, -10, -10);

        result.Should().BeApproximately(225.0f, 0.01f);
    }

    [Fact]
    public void Bearing2D_Northwest_Returns315()
    {
        // -X and +Y
        var result = PathSimplifier.Bearing2D(0, 0, -10, 10);

        result.Should().BeApproximately(315.0f, 0.01f);
    }

    [Fact]
    public void Bearing2D_SamePoint_ReturnsZero()
    {
        // No movement - atan2(0,0) = 0
        var result = PathSimplifier.Bearing2D(5, 5, 5, 5);

        result.Should().BeApproximately(0.0f, 0.01f);
    }

    [Fact]
    public void Bearing2D_AlwaysReturnsNormalizedValue()
    {
        // Test that bearing is always in [0, 360) range
        var bearings = new[]
        {
            PathSimplifier.Bearing2D(0, 0, 1, 0),    // 90°
            PathSimplifier.Bearing2D(0, 0, 0, 1),    // 0°
            PathSimplifier.Bearing2D(0, 0, -1, 0),   // 270°
            PathSimplifier.Bearing2D(0, 0, 0, -1),   // 180°
            PathSimplifier.Bearing2D(0, 0, 1, 1),    // 45°
            PathSimplifier.Bearing2D(0, 0, -1, -1),  // 225°
        };

        foreach (var bearing in bearings)
        {
            bearing.Should().BeGreaterThanOrEqualTo(0.0f);
            bearing.Should().BeLessThan(360.0f);
        }
    }

    [Theory]
    [InlineData(0, 0, 0, 1, 0.0f)]     // North
    [InlineData(0, 0, 1, 0, 90.0f)]    // East
    [InlineData(0, 0, 0, -1, 180.0f)]  // South
    [InlineData(0, 0, -1, 0, 270.0f)]  // West
    [InlineData(0, 0, 1, 1, 45.0f)]    // NE
    [InlineData(0, 0, 1, -1, 135.0f)]  // SE
    [InlineData(0, 0, -1, -1, 225.0f)] // SW
    [InlineData(0, 0, -1, 1, 315.0f)]  // NW
    public void Bearing2D_CardinalAndIntercardinal_ReturnsCorrectBearing(
        float x1, float y1, float x2, float y2, float expected)
    {
        var result = PathSimplifier.Bearing2D(x1, y1, x2, y2);

        result.Should().BeApproximately(expected, 0.01f);
    }

    // ============================================================================
    // Simplify Tests
    // ============================================================================

    [Fact]
    public void Simplify_SinglePoint_ReturnsSamePoint()
    {
        var points = new List<(float X, float Y, float Z, int Index)>
        {
            (0, 0, 0, 0)
        };

        var result = PathSimplifier.Simplify(points, 1.0f);

        result.Should().HaveCount(1);
        result.Should().Contain(0);
    }

    [Fact]
    public void Simplify_TwoPoints_ReturnsBothPoints()
    {
        var points = new List<(float X, float Y, float Z, int Index)>
        {
            (0, 0, 0, 0),
            (10, 0, 0, 1)
        };

        var result = PathSimplifier.Simplify(points, 1.0f);

        result.Should().HaveCount(2);
        result.Should().Contain(0);
        result.Should().Contain(1);
    }

    [Fact]
    public void Simplify_CollinearPoints_KeepsEndpoints()
    {
        // Three points on a straight line - middle point should be removed
        var points = new List<(float X, float Y, float Z, int Index)>
        {
            (0, 0, 0, 0),
            (5, 0, 0, 1),  // On the line between 0 and 2
            (10, 0, 0, 2)
        };

        var result = PathSimplifier.Simplify(points, 1.0f);

        result.Should().HaveCount(2);
        result.Should().Contain(0);
        result.Should().Contain(2);
        result.Should().NotContain(1);
    }

    [Fact]
    public void Simplify_PointOffLine_KeepsPoint()
    {
        // Three points where middle point is off the line by more than tolerance
        var points = new List<(float X, float Y, float Z, int Index)>
        {
            (0, 0, 0, 0),
            (5, 5, 0, 1),  // 5 meters off the x-axis line
            (10, 0, 0, 2)
        };

        // With tolerance of 1m, the point 5m off line should be kept
        var result = PathSimplifier.Simplify(points, 1.0f);

        result.Should().HaveCount(3);
        result.Should().Contain(0);
        result.Should().Contain(1);
        result.Should().Contain(2);
    }

    [Fact]
    public void Simplify_LargeTolerance_KeepsOnlyEndpoints()
    {
        var points = new List<(float X, float Y, float Z, int Index)>
        {
            (0, 0, 0, 0),
            (5, 5, 0, 1),
            (10, 0, 0, 2)
        };

        // With very large tolerance, middle point should be removed
        var result = PathSimplifier.Simplify(points, 100.0f);

        result.Should().HaveCount(2);
        result.Should().Contain(0);
        result.Should().Contain(2);
    }

    [Fact]
    public void Simplify_ZeroTolerance_KeepsAllPoints()
    {
        var points = new List<(float X, float Y, float Z, int Index)>
        {
            (0, 0, 0, 0),
            (5, 0.001f, 0, 1),  // Tiny deviation
            (10, 0, 0, 2)
        };

        // With tolerance of 0, even tiny deviation should keep the point
        var result = PathSimplifier.Simplify(points, 0.0f);

        result.Should().HaveCount(3);
    }

    [Fact]
    public void Simplify_ComplexPath_ReducesPoints()
    {
        // Simulate a rover path with many points, some on straight segments
        var points = new List<(float X, float Y, float Z, int Index)>
        {
            (0, 0, 0, 0),
            (1, 0, 0, 1),   // On line
            (2, 0, 0, 2),   // On line
            (3, 5, 0, 3),   // Off line (turn)
            (4, 5, 0, 4),   // On line
            (5, 5, 0, 5),   // On line
            (10, 10, 0, 6)  // End
        };

        var result = PathSimplifier.Simplify(points, 0.5f);

        // Should keep: 0 (start), 3 (turn point), 6 (end)
        // Points 1,2,4,5 should be removed as they're on straight segments
        result.Should().Contain(0);  // Start
        result.Should().Contain(3);  // Turn point
        result.Should().Contain(6);  // End
    }

    [Fact]
    public void Simplify_PreservesOriginalIndices()
    {
        var points = new List<(float X, float Y, float Z, int Index)>
        {
            (0, 0, 0, 100),
            (5, 5, 0, 200),
            (10, 0, 0, 300)
        };

        var result = PathSimplifier.Simplify(points, 1.0f);

        // Should return original indices, not array positions
        result.Should().Contain(100);
        result.Should().Contain(200);
        result.Should().Contain(300);
    }
}
