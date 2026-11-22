using System.Collections.Generic;
using CloseFight.Core.Map;
using Unity.Mathematics;

namespace CloseFight.Core.Path
{
    /// <summary>Turns grid paths into smooth polylines and samples points along them.</summary>
    public static class PathUtils
    {
        /// <summary>
        /// String-pulling: keeps only the waypoints needed so every straight leg
        /// crosses passable cells. Turns a staircase of cells into a few clean legs.
        /// </summary>
        public static void Smooth(GridPathfinder pathfinder, TacticalGrid grid, MovementClass movement,
            List<GridCoord> cells, float2 start, float2 end, List<float2> result)
        {
            result.Clear();
            var points = new List<float2>(cells.Count + 1) { start };
            for (int i = 1; i < cells.Count - 1; i++)
            {
                points.Add(grid.CellCenter(cells[i]));
            }

            points.Add(end);

            result.Add(points[0]);
            int anchor = 0;
            while (anchor < points.Count - 1)
            {
                int next = points.Count - 1;
                while (next > anchor + 1 && !IsWalkable(pathfinder, grid, movement, points[anchor], points[next]))
                {
                    next--;
                }

                result.Add(points[next]);
                anchor = next;
            }
        }

        /// <summary>True when every cell the straight segment touches is passable.</summary>
        public static bool IsWalkable(GridPathfinder pathfinder, TacticalGrid grid, MovementClass movement, float2 from, float2 to)
        {
            var visitor = new WalkableVisitor { Pathfinder = pathfinder, Grid = grid, Movement = movement, Walkable = true };
            GridTraversal.Traverse(grid, from, to, ref visitor);
            return visitor.Walkable;
        }

        struct WalkableVisitor : ICellVisitor
        {
            public GridPathfinder Pathfinder;
            public TacticalGrid Grid;
            public MovementClass Movement;
            public bool Walkable;

            public bool Visit(int x, int y, float tEnter, float tExit)
            {
                if (!Pathfinder.IsPassable(Grid.IndexOf(x, y), Movement))
                {
                    Walkable = false;
                    return false;
                }

                return true;
            }
        }

        /// <summary>Fills cumulative distances: cumulative[i] is the distance from points[0] to points[i].</summary>
        public static void Cumulate(List<float2> points, List<float> cumulative)
        {
            cumulative.Clear();
            float total = 0f;
            for (int i = 0; i < points.Count; i++)
            {
                if (i > 0)
                {
                    total += math.distance(points[i - 1], points[i]);
                }

                cumulative.Add(total);
            }
        }

        /// <summary>Point at the given distance along the polyline (clamped to its ends).</summary>
        public static float2 PointAt(List<float2> points, List<float> cumulative, float distance, out float2 direction)
        {
            direction = new float2(0f, 1f);
            if (points.Count == 0)
            {
                return float2.zero;
            }

            if (points.Count == 1 || distance <= 0f)
            {
                if (points.Count > 1)
                {
                    direction = math.normalizesafe(points[1] - points[0], direction);
                }

                return points[0];
            }

            for (int i = 1; i < points.Count; i++)
            {
                if (distance <= cumulative[i] || i == points.Count - 1)
                {
                    float segment = cumulative[i] - cumulative[i - 1];
                    float t = segment > 1e-5f ? math.saturate((distance - cumulative[i - 1]) / segment) : 1f;
                    direction = math.normalizesafe(points[i] - points[i - 1], direction);
                    return math.lerp(points[i - 1], points[i], t);
                }
            }

            return points[points.Count - 1];
        }
    }
}
