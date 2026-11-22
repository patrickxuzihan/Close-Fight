using System;
using System.Collections.Generic;
using CloseFight.Core.Map;
using Unity.Mathematics;

namespace CloseFight.Core.Path
{
    /// <summary>
    /// A* over the tactical grid, 8 directions, terrain-weighted.
    /// Diagonal moves may not cut corners past a blocked cell.
    /// One instance per thread; it reuses its buffers between searches
    /// (generation stamps avoid clearing 250k-entry arrays every call).
    /// Later: flow fields for regiment-scale movement (see prototype-plan.md).
    /// </summary>
    public sealed class GridPathfinder
    {
        const float DiagonalCost = 1.41421356f;

        static readonly int[] DirX = { 1, -1, 0, 0, 1, 1, -1, -1 };
        static readonly int[] DirY = { 0, 0, 1, -1, 1, -1, 1, -1 };

        readonly TacticalMap _map;
        readonly float[] _g;
        readonly int[] _parent;
        readonly int[] _seenStamp;
        readonly int[] _closedStamp;
        readonly MinHeap _open;
        int _stamp;

        /// <summary>
        /// Optional extra cost per cell, added on top of terrain cost (for example,
        /// cells under known enemy fire). Return 0 for no penalty.
        /// </summary>
        public Func<int, float> ExtraCost { get; set; }

        /// <summary>Safety cap on expanded nodes per search.</summary>
        public int MaxExpandedNodes { get; set; } = 200000;

        public GridPathfinder(TacticalMap map)
        {
            _map = map;
            int count = map.Grid.CellCount;
            _g = new float[count];
            _parent = new int[count];
            _seenStamp = new int[count];
            _closedStamp = new int[count];
            _open = new MinHeap(1024);
        }

        /// <summary>Cost to enter a cell, or +infinity when the cell is impassable.</summary>
        public float CellCost(int index, MovementClass movement)
        {
            float cost = BaseCost(index, movement);
            if (float.IsPositiveInfinity(cost))
            {
                return cost;
            }

            // A tank needs room: every cell around its center must take it too (about 3 m clearance).
            if (movement == MovementClass.Vehicle)
            {
                var grid = _map.Grid;
                int x = index % grid.Width;
                int y = index / grid.Width;
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if ((dx != 0 || dy != 0) && (!grid.InBounds(x + dx, y + dy)
                            || float.IsPositiveInfinity(BaseCost(grid.IndexOf(x + dx, y + dy), movement))))
                        {
                            return float.PositiveInfinity;
                        }
                    }
                }
            }

            if (ExtraCost != null)
            {
                cost += math.max(0f, ExtraCost(index));
            }

            return cost;
        }

        /// <summary>Cost of the cell itself, ignoring clearance and danger.</summary>
        float BaseCost(int index, MovementClass movement)
        {
            var grid = _map.Grid;
            if (grid.HasFlag(index, CellFlags.BlocksMovement))
            {
                return float.PositiveInfinity;
            }

            // Tanks crush one-storey wooden sheds (D37); brick and concrete stop them.
            if (movement == MovementClass.Vehicle && grid.HasFlag(index, CellFlags.Building) && !grid.HasFlag(index, CellFlags.Collapsed))
            {
                var building = _map.BuildingAt(index);
                return building != null && building.Material == BuildingMaterial.Wood && building.Floors <= 1 ? 3f : float.PositiveInfinity;
            }

            float cost = TerrainInfo.MoveCost(grid.GetTerrain(index), movement);
            return cost;
        }

        public bool IsPassable(int index, MovementClass movement)
        {
            return !float.IsPositiveInfinity(CellCost(index, movement));
        }

        /// <summary>
        /// Finds a path from start to goal. On success <paramref name="result"/> holds the
        /// cells from start to goal inclusive. When the goal is unreachable, returns false
        /// and fills the path to the reachable cell closest to the goal, so units still
        /// move as near as they can.
        /// </summary>
        public bool FindPath(GridCoord start, GridCoord goal, MovementClass movement, List<GridCoord> result)
        {
            result.Clear();
            var grid = _map.Grid;
            if (!grid.InBounds(start) || !grid.InBounds(goal))
            {
                return false;
            }

            NextStamp();
            _open.Clear();

            int startIndex = grid.IndexOf(start);
            int goalIndex = grid.IndexOf(goal);

            _g[startIndex] = 0f;
            _parent[startIndex] = -1;
            _seenStamp[startIndex] = _stamp;
            _open.Push(startIndex, Heuristic(start, goal));

            int bestIndex = startIndex;
            float bestH = Heuristic(start, goal);
            int expanded = 0;

            while (_open.Count > 0)
            {
                int current = _open.Pop();
                if (_closedStamp[current] == _stamp)
                {
                    continue; // stale duplicate entry
                }

                _closedStamp[current] = _stamp;

                if (current == goalIndex)
                {
                    BuildPath(goalIndex, result);
                    return true;
                }

                if (++expanded > MaxExpandedNodes)
                {
                    break;
                }

                int cx = current % grid.Width;
                int cy = current / grid.Width;

                float h = Heuristic(new GridCoord(cx, cy), goal);
                if (h < bestH)
                {
                    bestH = h;
                    bestIndex = current;
                }

                for (int dir = 0; dir < 8; dir++)
                {
                    int nx = cx + DirX[dir];
                    int ny = cy + DirY[dir];
                    if (!grid.InBounds(nx, ny))
                    {
                        continue;
                    }

                    int neighbor = grid.IndexOf(nx, ny);
                    if (_closedStamp[neighbor] == _stamp)
                    {
                        continue;
                    }

                    float enterCost = CellCost(neighbor, movement);
                    if (float.IsPositiveInfinity(enterCost))
                    {
                        continue;
                    }

                    bool diagonal = dir >= 4;
                    if (diagonal)
                    {
                        // No corner cutting: both orthogonal neighbors must be passable.
                        if (!IsPassable(grid.IndexOf(nx, cy), movement) || !IsPassable(grid.IndexOf(cx, ny), movement))
                        {
                            continue;
                        }
                    }

                    float step = (diagonal ? DiagonalCost : 1f) * grid.CellSize * enterCost;
                    float tentative = _g[current] + step;

                    if (_seenStamp[neighbor] != _stamp || tentative < _g[neighbor])
                    {
                        _seenStamp[neighbor] = _stamp;
                        _g[neighbor] = tentative;
                        _parent[neighbor] = current;
                        _open.Push(neighbor, tentative + Heuristic(new GridCoord(nx, ny), goal));
                    }
                }
            }

            if (bestIndex != startIndex)
            {
                BuildPath(bestIndex, result);
            }

            return false;
        }

        /// <summary>Octile distance in meters, scaled by the cheapest terrain cost.</summary>
        float Heuristic(GridCoord a, GridCoord b)
        {
            int dx = math.abs(a.X - b.X);
            int dy = math.abs(a.Y - b.Y);
            int straight = math.abs(dx - dy);
            int diagonal = math.min(dx, dy);
            return (straight + diagonal * DiagonalCost) * _map.Grid.CellSize * TerrainInfo.MinMoveCost;
        }

        void BuildPath(int endIndex, List<GridCoord> result)
        {
            var grid = _map.Grid;
            for (int index = endIndex; index != -1; index = _parent[index])
            {
                result.Add(grid.CoordOf(index));
            }

            result.Reverse();
        }

        void NextStamp()
        {
            _stamp++;
            if (_stamp == int.MaxValue)
            {
                Array.Clear(_seenStamp, 0, _seenStamp.Length);
                Array.Clear(_closedStamp, 0, _closedStamp.Length);
                _stamp = 1;
            }
        }
    }
}
