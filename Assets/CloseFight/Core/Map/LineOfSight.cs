using Unity.Mathematics;

namespace CloseFight.Core.Map
{
    public enum Posture : byte
    {
        Standing = 0,
        Kneeling = 1,
        Prone = 2,
    }

    /// <summary>Result of one line-of-sight check.</summary>
    public readonly struct SightResult
    {
        public readonly bool Visible;
        /// <summary>Segment parameter (0..1) where sight was blocked; 1 when visible.</summary>
        public readonly float BlockedAt;
        public readonly float Distance;

        public SightResult(bool visible, float blockedAt, float distance)
        {
            Visible = visible;
            BlockedAt = blockedAt;
            Distance = distance;
        }
    }

    /// <summary>
    /// Line of sight over the height grid.
    /// Rule D5: a unit inside a building counts as being on its top floor. It looks
    /// out from (and is seen at) roof height minus one storey plus eye height.
    /// Cells of the observer's and the target's own building never block their view,
    /// which models firing from windows.
    /// </summary>
    public static class LineOfSight
    {
        public const float EyeStanding = 1.6f;
        public const float EyeKneeling = 1.0f;
        public const float EyeProne = 0.4f;

        public static float PostureEyeHeight(Posture posture)
        {
            switch (posture)
            {
                case Posture.Kneeling: return EyeKneeling;
                case Posture.Prone: return EyeProne;
                default: return EyeStanding;
            }
        }

        /// <summary>Eye height in meters above ground for a unit at this position.</summary>
        public static float EyeHeightAt(TacticalMap map, float2 position, Posture posture)
        {
            var grid = map.Grid;
            var cell = grid.WorldToCell(position);
            if (grid.InBounds(cell))
            {
                int index = grid.IndexOf(cell);
                var building = map.BuildingAt(index);
                if (building != null && !grid.HasFlag(index, CellFlags.Collapsed))
                {
                    return (building.Floors - 1) * TerrainInfo.FloorHeight + EyeStanding;
                }
            }

            return PostureEyeHeight(posture);
        }

        public static SightResult Check(TacticalMap map, float2 from, Posture fromPosture, float2 to, Posture toPosture, float maxRange)
        {
            float distance = math.distance(from, to);
            if (distance > maxRange)
            {
                return new SightResult(false, maxRange / math.max(distance, 1e-4f), distance);
            }

            float fromHeight = EyeHeightAt(map, from, fromPosture);
            float toHeight = EyeHeightAt(map, to, toPosture);
            return Check(map, from, fromHeight, to, toHeight);
        }

        /// <summary>Checks sight between two explicit eye heights (meters above ground).</summary>
        public static SightResult Check(TacticalMap map, float2 from, float fromHeight, float2 to, float toHeight)
        {
            var grid = map.Grid;
            var fromCell = grid.WorldToCell(from);
            var toCell = grid.WorldToCell(to);

            var visitor = new SightVisitor
            {
                Grid = grid,
                FromIndex = grid.InBounds(fromCell) ? grid.IndexOf(fromCell) : -1,
                ToIndex = grid.InBounds(toCell) ? grid.IndexOf(toCell) : -1,
                FromBuilding = grid.InBounds(fromCell) ? grid.GetBuildingIndex(grid.IndexOf(fromCell)) : TacticalGrid.NoBuilding,
                ToBuilding = grid.InBounds(toCell) ? grid.GetBuildingIndex(grid.IndexOf(toCell)) : TacticalGrid.NoBuilding,
                FromHeight = fromHeight,
                ToHeight = toHeight,
                BlockedAt = 1f,
                Blocked = false,
            };

            GridTraversal.Traverse(grid, from, to, ref visitor);
            return new SightResult(!visitor.Blocked, visitor.BlockedAt, math.distance(from, to));
        }

        struct SightVisitor : ICellVisitor
        {
            public TacticalGrid Grid;
            public int FromIndex;
            public int ToIndex;
            public short FromBuilding;
            public short ToBuilding;
            public float FromHeight;
            public float ToHeight;
            public bool Blocked;
            public float BlockedAt;

            public bool Visit(int x, int y, float tEnter, float tExit)
            {
                int index = Grid.IndexOf(x, y);
                if (index == FromIndex || index == ToIndex)
                {
                    return true;
                }

                short building = Grid.GetBuildingIndex(index);
                bool ownBuilding = building != TacticalGrid.NoBuilding
                    && (building == FromBuilding || building == ToBuilding)
                    && !Grid.HasFlag(index, CellFlags.Collapsed);
                if (ownBuilding)
                {
                    return true;
                }

                float obstacle = Grid.GetObstacleHeight(index);
                if (obstacle <= 0f)
                {
                    return true;
                }

                // Lowest point of the sight line while it crosses this cell.
                float rayHeight = math.min(
                    math.lerp(FromHeight, ToHeight, tEnter),
                    math.lerp(FromHeight, ToHeight, tExit));

                if (obstacle >= rayHeight)
                {
                    Blocked = true;
                    BlockedAt = tEnter;
                    return false;
                }

                return true;
            }
        }
    }
}
