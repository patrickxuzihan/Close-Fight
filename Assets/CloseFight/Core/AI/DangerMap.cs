using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CloseFight.Core.Map;
using Unity.Mathematics;

namespace CloseFight.Core.AI
{
    /// <summary>
    /// Where the known enemy can shoot (D8: smarter pathfinding). A coarse grid
    /// (4 m cells) marks every cell a known threat has line of sight to, weighted
    /// by distance. Routes planned with <see cref="PathCost"/> prefer covered
    /// ground and dead angles. Built only from what the side knows.
    /// </summary>
    public sealed class DangerMap
    {
        /// <summary>Coarse cell size: 4 m on small maps, larger on big ones.</summary>
        public float CoarseCell { get; }
        const float ThreatRange = 350f;
        const float EyeHeight = 1.0f;

        readonly TacticalMap _map;
        readonly int _width;
        readonly int _height;
        readonly float[] _danger;

        public DangerMap(TacticalMap map)
        {
            _map = map;
            var grid = map.Grid;
            CoarseCell = math.max(4f, math.round(math.sqrt(grid.Width * grid.Height) * grid.CellSize / 50f));
            _width = (int)math.ceil(grid.Width * grid.CellSize / CoarseCell);
            _height = (int)math.ceil(grid.Height * grid.CellSize / CoarseCell);
            _danger = new float[_width * _height];
        }

        public bool IsEmpty { get; private set; } = true;

        /// <summary>Rebuilds from threats: (position, weight). Weight ~ firepower (an MG counts double).</summary>
        public void Rebuild(IEnumerable<(float2 position, float weight)> threats)
        {
            Array.Clear(_danger, 0, _danger.Length);
            var list = new List<(float2 position, float weight)>(threats);
            IsEmpty = list.Count == 0;
            if (IsEmpty)
            {
                return;
            }

            // Rows in parallel; each cell sums its threats in list order, so the result is deterministic.
            Parallel.For(0, _height, cy =>
            {
                for (int cx = 0; cx < _width; cx++)
                {
                    float2 center = new float2((cx + 0.5f) * CoarseCell, (cy + 0.5f) * CoarseCell);
                    float sum = 0f;
                    foreach (var (position, weight) in list)
                    {
                        float distance = math.distance(center, position);
                        if (distance > ThreatRange)
                        {
                            continue;
                        }

                        if (LineOfSight.Check(_map, position, EyeHeight, center, EyeHeight).Visible)
                        {
                            sum += weight * (1f - distance / ThreatRange * 0.7f);
                        }
                    }

                    _danger[cy * _width + cx] = sum;
                }
            });
        }

        public float At(float2 world)
        {
            int cx = math.clamp((int)(world.x / CoarseCell), 0, _width - 1);
            int cy = math.clamp((int)(world.y / CoarseCell), 0, _height - 1);
            return _danger[cy * _width + cx];
        }

        /// <summary>Extra path cost per 1 m cell: <paramref name="scale"/> × danger. Null when nothing is known.</summary>
        public Func<int, float> PathCost(float scale)
        {
            if (IsEmpty)
            {
                return null;
            }

            var grid = _map.Grid;
            return index => At(grid.CellCenter(grid.CoordOf(index))) * scale;
        }
    }
}
