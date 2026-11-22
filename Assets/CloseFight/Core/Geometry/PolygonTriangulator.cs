using System.Collections.Generic;
using Unity.Mathematics;

namespace CloseFight.Core.Geometry
{
    /// <summary>
    /// Ear-clipping triangulation for simple polygons (building footprints, areas).
    /// Works with either winding; output triangles are always counter-clockwise in
    /// map space (x east, y north). Convex and concave (L, U shaped) footprints are
    /// fine; self-intersecting polygons are not supported.
    /// </summary>
    public static class PolygonTriangulator
    {
        const float Epsilon = 1e-6f;

        /// <summary>
        /// Fills <paramref name="triangles"/> with index triples into
        /// <paramref name="polygon"/>. Returns false if the polygon is degenerate.
        /// </summary>
        public static bool Triangulate(IReadOnlyList<float2> polygon, List<int> triangles)
        {
            triangles.Clear();
            int n = polygon.Count;
            if (n < 3)
            {
                return false;
            }

            bool counterClockwise = SignedArea(polygon) > 0f;
            var remaining = new List<int>(n);
            for (int i = 0; i < n; i++)
            {
                remaining.Add(counterClockwise ? i : n - 1 - i);
            }

            int guard = n * n + 8;
            while (remaining.Count > 3 && guard-- > 0)
            {
                bool clipped = false;
                int count = remaining.Count;

                for (int i = 0; i < count; i++)
                {
                    int prev = remaining[(i + count - 1) % count];
                    int cur = remaining[i];
                    int next = remaining[(i + 1) % count];
                    float2 a = polygon[prev];
                    float2 b = polygon[cur];
                    float2 c = polygon[next];

                    float turn = Cross(b - a, c - b);
                    if (math.abs(turn) <= Epsilon)
                    {
                        // Collinear vertex: drop it without emitting a triangle.
                        remaining.RemoveAt(i);
                        clipped = true;
                        break;
                    }

                    if (turn < 0f)
                    {
                        continue; // reflex corner, not an ear
                    }

                    if (ContainsOtherVertex(polygon, remaining, prev, cur, next, a, b, c))
                    {
                        continue;
                    }

                    triangles.Add(prev);
                    triangles.Add(cur);
                    triangles.Add(next);
                    remaining.RemoveAt(i);
                    clipped = true;
                    break;
                }

                if (!clipped)
                {
                    return false;
                }
            }

            if (remaining.Count == 3)
            {
                triangles.Add(remaining[0]);
                triangles.Add(remaining[1]);
                triangles.Add(remaining[2]);
            }

            return triangles.Count > 0;
        }

        /// <summary>Shoelace area; positive for counter-clockwise polygons.</summary>
        public static float SignedArea(IReadOnlyList<float2> polygon)
        {
            float area = 0f;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                area += polygon[j].x * polygon[i].y - polygon[i].x * polygon[j].y;
            }

            return area * 0.5f;
        }

        static bool ContainsOtherVertex(IReadOnlyList<float2> polygon, List<int> remaining, int prev, int cur, int next, float2 a, float2 b, float2 c)
        {
            for (int j = 0; j < remaining.Count; j++)
            {
                int k = remaining[j];
                if (k == prev || k == cur || k == next)
                {
                    continue;
                }

                if (PointInTriangle(polygon[k], a, b, c))
                {
                    return true;
                }
            }

            return false;
        }

        static float Cross(float2 u, float2 v) => u.x * v.y - u.y * v.x;

        static bool PointInTriangle(float2 p, float2 a, float2 b, float2 c)
        {
            return Cross(b - a, p - a) >= 0f && Cross(c - b, p - b) >= 0f && Cross(a - c, p - c) >= 0f;
        }
    }
}
