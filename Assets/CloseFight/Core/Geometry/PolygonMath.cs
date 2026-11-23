using System.Collections.Generic;
using Unity.Mathematics;

namespace CloseFight.Core.Geometry
{
    /// <summary>Small polygon helpers in map space (x east, y north).</summary>
    public static class PolygonMath
    {
        /// <summary>Point-in-polygon by the even-odd rule (either winding).</summary>
        public static bool Contains(IReadOnlyList<float2> polygon, float2 point)
        {
            bool inside = false;
            int n = polygon.Count;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float2 a = polygon[i];
                float2 b = polygon[j];
                if ((a.y > point.y) != (b.y > point.y)
                    && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        /// <summary>The point of the polygon's boundary (or inside) closest to <paramref name="point"/>.</summary>
        public static float2 Nearest(IReadOnlyList<float2> polygon, float2 point)
        {
            if (Contains(polygon, point))
            {
                return point;
            }

            float2 best = polygon[0];
            float bestDistance = float.MaxValue;
            int n = polygon.Count;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float2 a = polygon[j];
                float2 b = polygon[i];
                float2 ab = b - a;
                float t = math.saturate(math.dot(point - a, ab) / math.max(math.lengthsq(ab), 1e-6f));
                float2 q = a + ab * t;
                float d = math.distancesq(q, point);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = q;
                }
            }

            return best;
        }
    }
}
