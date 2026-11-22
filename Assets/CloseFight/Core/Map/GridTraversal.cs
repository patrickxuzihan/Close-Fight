using Unity.Mathematics;

namespace CloseFight.Core.Map
{
    /// <summary>Callback for <see cref="GridTraversal.Traverse{TVisitor}"/>.</summary>
    public interface ICellVisitor
    {
        /// <summary>
        /// Called for each cell the segment passes through, in order.
        /// tEnter/tExit are the segment parameters (0..1) where the segment enters and
        /// leaves the cell. Return false to stop the traversal.
        /// </summary>
        bool Visit(int x, int y, float tEnter, float tExit);
    }

    /// <summary>
    /// Walks every grid cell a line segment touches (Amanatides–Woo).
    /// When the segment passes exactly through a cell corner, both side cells are
    /// visited too, so a diagonal line cannot slip between two wall cells.
    /// Visitors are structs passed by ref, so traversal never allocates.
    /// </summary>
    public static class GridTraversal
    {
        public static void Traverse<TVisitor>(TacticalGrid grid, float2 from, float2 to, ref TVisitor visitor)
            where TVisitor : struct, ICellVisitor
        {
            float invCell = 1f / grid.CellSize;
            float2 a = from * invCell;
            float2 b = to * invCell;
            float2 d = b - a;

            int x = (int)math.floor(a.x);
            int y = (int)math.floor(a.y);
            int endX = (int)math.floor(b.x);
            int endY = (int)math.floor(b.y);

            int stepX = d.x > 0f ? 1 : (d.x < 0f ? -1 : 0);
            int stepY = d.y > 0f ? 1 : (d.y < 0f ? -1 : 0);

            float tDeltaX = stepX != 0 ? math.abs(1f / d.x) : float.PositiveInfinity;
            float tDeltaY = stepY != 0 ? math.abs(1f / d.y) : float.PositiveInfinity;

            float tMaxX = stepX > 0 ? (x + 1 - a.x) / d.x
                : stepX < 0 ? (a.x - x) / -d.x
                : float.PositiveInfinity;
            float tMaxY = stepY > 0 ? (y + 1 - a.y) / d.y
                : stepY < 0 ? (a.y - y) / -d.y
                : float.PositiveInfinity;

            float t = 0f;
            int remaining = math.abs(endX - x) + math.abs(endY - y);

            while (true)
            {
                bool lastCell = remaining <= 0;
                float tNext = lastCell ? 1f : math.min(math.min(tMaxX, tMaxY), 1f);

                if (grid.InBounds(x, y) && !visitor.Visit(x, y, t, tNext))
                {
                    return;
                }

                if (lastCell)
                {
                    return;
                }

                // Once one axis has reached its end cell, only step along the other.
                // This keeps float error from overshooting the end cell.
                bool stepAlongX;
                bool corner = false;
                if (x == endX)
                {
                    stepAlongX = false;
                }
                else if (y == endY)
                {
                    stepAlongX = true;
                }
                else if (math.abs(tMaxX - tMaxY) < 1e-6f)
                {
                    stepAlongX = true;
                    corner = true;
                }
                else
                {
                    stepAlongX = tMaxX < tMaxY;
                }

                if (corner)
                {
                    // Exactly through a corner: visit both side cells, then move diagonally.
                    float tCorner = tMaxX;
                    if (grid.InBounds(x + stepX, y) && !visitor.Visit(x + stepX, y, tCorner, tCorner))
                    {
                        return;
                    }

                    if (grid.InBounds(x, y + stepY) && !visitor.Visit(x, y + stepY, tCorner, tCorner))
                    {
                        return;
                    }

                    x += stepX;
                    y += stepY;
                    t = tCorner;
                    tMaxX += tDeltaX;
                    tMaxY += tDeltaY;
                    remaining -= 2;
                }
                else if (stepAlongX)
                {
                    x += stepX;
                    t = tMaxX;
                    tMaxX += tDeltaX;
                    remaining -= 1;
                }
                else
                {
                    y += stepY;
                    t = tMaxY;
                    tMaxY += tDeltaY;
                    remaining -= 1;
                }

                t = math.min(t, 1f);
            }
        }
    }
}
