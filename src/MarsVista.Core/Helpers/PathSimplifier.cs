namespace MarsVista.Core.Helpers;

/// <summary>
/// Douglas-Peucker path simplification for reducing traverse point counts
/// while preserving the overall shape of the path.
/// </summary>
public static class PathSimplifier
{
    /// <summary>
    /// Simplify a 3D path using Douglas-Peucker algorithm.
    /// </summary>
    /// <param name="points">List of (x, y, z, index) tuples</param>
    /// <param name="tolerance">Maximum perpendicular distance tolerance in meters</param>
    /// <returns>Indices of points to keep</returns>
    public static List<int> Simplify(List<(float X, float Y, float Z, int Index)> points, float tolerance)
    {
        if (points.Count < 3)
        {
            return points.Select(p => p.Index).ToList();
        }

        var keep = new bool[points.Count];
        keep[0] = true;
        keep[points.Count - 1] = true;

        SimplifySection(points, 0, points.Count - 1, tolerance, keep);

        var result = new List<int>();
        for (int i = 0; i < keep.Length; i++)
        {
            if (keep[i])
            {
                result.Add(points[i].Index);
            }
        }

        return result;
    }

    private static void SimplifySection(
        List<(float X, float Y, float Z, int Index)> points,
        int start,
        int end,
        float tolerance,
        bool[] keep)
    {
        if (end - start < 2)
        {
            return;
        }

        float maxDist = 0;
        int maxIndex = start;

        var startPoint = points[start];
        var endPoint = points[end];

        for (int i = start + 1; i < end; i++)
        {
            float dist = PerpendicularDistance(points[i], startPoint, endPoint);
            if (dist > maxDist)
            {
                maxDist = dist;
                maxIndex = i;
            }
        }

        if (maxDist > tolerance)
        {
            keep[maxIndex] = true;
            SimplifySection(points, start, maxIndex, tolerance, keep);
            SimplifySection(points, maxIndex, end, tolerance, keep);
        }
    }

    /// <summary>
    /// Calculate perpendicular distance from point P to line segment AB in 3D.
    /// </summary>
    private static float PerpendicularDistance(
        (float X, float Y, float Z, int Index) p,
        (float X, float Y, float Z, int Index) a,
        (float X, float Y, float Z, int Index) b)
    {
        // Vector from A to B
        float abX = b.X - a.X;
        float abY = b.Y - a.Y;
        float abZ = b.Z - a.Z;

        // Vector from A to P
        float apX = p.X - a.X;
        float apY = p.Y - a.Y;
        float apZ = p.Z - a.Z;

        // Length squared of AB
        float abLengthSq = abX * abX + abY * abY + abZ * abZ;

        if (abLengthSq < 1e-10f)
        {
            // A and B are the same point, return distance to A
            return MathF.Sqrt(apX * apX + apY * apY + apZ * apZ);
        }

        // Project AP onto AB: t = (AP . AB) / |AB|^2
        float t = (apX * abX + apY * abY + apZ * abZ) / abLengthSq;

        // Clamp t to [0, 1] to get closest point on segment
        t = MathF.Max(0, MathF.Min(1, t));

        // Closest point on segment
        float closestX = a.X + t * abX;
        float closestY = a.Y + t * abY;
        float closestZ = a.Z + t * abZ;

        // Distance from P to closest point
        float dx = p.X - closestX;
        float dy = p.Y - closestY;
        float dz = p.Z - closestZ;

        return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    /// <summary>
    /// Calculate 3D Euclidean distance between two points.
    /// </summary>
    public static float Distance3D(float x1, float y1, float z1, float x2, float y2, float z2)
    {
        float dx = x2 - x1;
        float dy = y2 - y1;
        float dz = z2 - z1;
        return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    /// <summary>
    /// Calculate bearing (compass direction) from point A to B in degrees.
    /// Uses 2D (X, Y) coordinates. 0° = North (+Y), 90° = East (+X).
    /// </summary>
    public static float Bearing2D(float x1, float y1, float x2, float y2)
    {
        float dx = x2 - x1;
        float dy = y2 - y1;
        float bearing = MathF.Atan2(dx, dy) * 180f / MathF.PI;
        return (bearing + 360f) % 360f;
    }
}
