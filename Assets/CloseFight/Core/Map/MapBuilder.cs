using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace CloseFight.Core.Map
{
    /// <summary>
    /// Rasterizes a <see cref="MapDefinition"/> into a <see cref="TacticalMap"/>.
    /// Order: base terrain, then areas, then buildings, then walls.
    /// A cell belongs to a polygon when the cell's center lies inside it.
    /// </summary>
    public static class MapBuilder
    {
        public static TacticalMap Build(MapDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (definition.cellSize <= 0f || definition.width <= 0f || definition.height <= 0f)
            {
                throw new ArgumentException("Map width, height and cellSize must be positive.");
            }

            int width = (int)math.ceil(definition.width / definition.cellSize);
            int height = (int)math.ceil(definition.height / definition.cellSize);
            var grid = new TacticalGrid(width, height, definition.cellSize);
            var map = new TacticalMap(definition, grid);

            for (int i = 0; i < grid.CellCount; i++)
            {
                ApplyTerrain(grid, i, definition.baseTerrain);
            }

            var scratch = new List<float>();

            foreach (var area in definition.areas)
            {
                var terrain = area.terrain;
                FillPolygon(grid, area.polygon, scratch, index => ApplyTerrain(grid, index, terrain));
            }

            foreach (var buildingDefinition in definition.buildings)
            {
                var building = map.AddBuilding(buildingDefinition);
                FillPolygon(grid, buildingDefinition.polygon, scratch, index => ApplyBuilding(grid, index, building));
            }

            foreach (var wall in definition.walls)
            {
                ApplyWall(grid, wall);
            }

            foreach (var prop in definition.props)
            {
                var p = prop;
                FillPolygon(grid, p.polygon, scratch, index => ApplyProp(grid, index, p));
            }

            return map;
        }

        static void ApplyTerrain(TacticalGrid grid, int index, TerrainType terrain)
        {
            grid.SetTerrain(index, terrain);
            grid.SetObstacleHeight(index, TerrainInfo.TerrainObstacleHeight(terrain));
            grid.SetCover(index, TerrainInfo.TerrainCover(terrain));

            var flags = CellFlags.None;
            if (float.IsPositiveInfinity(TerrainInfo.MoveCost(terrain, MovementClass.Infantry)))
            {
                flags |= CellFlags.BlocksMovement;
            }

            grid.SetFlags(index, flags);
        }

        static void ApplyBuilding(TacticalGrid grid, int index, BuildingState building)
        {
            // A cell already claimed by an earlier building stays with it.
            if (grid.GetBuildingIndex(index) != TacticalGrid.NoBuilding)
            {
                return;
            }

            grid.SetTerrain(index, TerrainType.BuildingFloor);
            grid.SetObstacleHeight(index, building.RoofHeight);
            grid.SetCover(index, TerrainInfo.BuildingCover(building.Material));
            grid.SetFlags(index, CellFlags.Building | CellFlags.BlocksSight);
            grid.SetBuildingIndex(index, building.Index);
            building.Cells.Add(index);
        }

        static void ApplyProp(TacticalGrid grid, int index, PropDefinition prop)
        {
            if (grid.HasFlag(index, CellFlags.Building))
            {
                return;
            }

            grid.AddFlags(index, CellFlags.BlocksMovement);
            grid.SetObstacleHeight(index, math.max(grid.GetObstacleHeight(index), prop.height));
            grid.SetCover(index, (byte)math.clamp(prop.cover, 0f, 100f));
        }

        static void ApplyWall(TacticalGrid grid, WallDefinition wall)
        {
            if (wall.points == null || wall.points.Count < 2)
            {
                return;
            }

            var visitor = new WallVisitor(grid, wall);
            for (int i = 0; i < wall.points.Count - 1; i++)
            {
                GridTraversal.Traverse(grid, wall.points[i], wall.points[i + 1], ref visitor);
            }
        }

        struct WallVisitor : ICellVisitor
        {
            readonly TacticalGrid _grid;
            readonly WallDefinition _wall;

            public WallVisitor(TacticalGrid grid, WallDefinition wall)
            {
                _grid = grid;
                _wall = wall;
            }

            public bool Visit(int x, int y, float tEnter, float tExit)
            {
                int index = _grid.IndexOf(x, y);

                // Walls do not overwrite buildings.
                if (_grid.HasFlag(index, CellFlags.Building))
                {
                    return true;
                }

                _grid.AddFlags(index, CellFlags.Wall);
                if (_wall.blocksMovement)
                {
                    _grid.AddFlags(index, CellFlags.BlocksMovement);
                }

                _grid.SetObstacleHeight(index, math.max(_grid.GetObstacleHeight(index), _wall.height));
                byte cover = TerrainInfo.WallCover(_wall.height, _wall.crushable);
                if (cover > _grid.GetCover(index))
                {
                    _grid.SetCover(index, cover);
                }

                return true;
            }
        }

        /// <summary>
        /// Scanline fill: calls <paramref name="fill"/> with the index of every cell
        /// whose center lies inside the polygon (even-odd rule).
        /// </summary>
        public static void FillPolygon(TacticalGrid grid, IReadOnlyList<MapPoint> polygon, List<float> scratch, Action<int> fill)
        {
            if (polygon == null || polygon.Count < 3)
            {
                return;
            }

            float cell = grid.CellSize;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            for (int i = 0; i < polygon.Count; i++)
            {
                minY = math.min(minY, polygon[i].y);
                maxY = math.max(maxY, polygon[i].y);
            }

            // Rows whose center y lies in [minY, maxY].
            int rowStart = math.max(0, (int)math.ceil(minY / cell - 0.5f));
            int rowEnd = math.min(grid.Height - 1, (int)math.floor(maxY / cell - 0.5f));

            for (int row = rowStart; row <= rowEnd; row++)
            {
                float cy = (row + 0.5f) * cell;
                scratch.Clear();

                for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
                {
                    var pi = polygon[i];
                    var pj = polygon[j];
                    bool crosses = (pi.y <= cy && cy < pj.y) || (pj.y <= cy && cy < pi.y);
                    if (crosses)
                    {
                        scratch.Add(pi.x + (cy - pi.y) * (pj.x - pi.x) / (pj.y - pi.y));
                    }
                }

                scratch.Sort();
                for (int k = 0; k + 1 < scratch.Count; k += 2)
                {
                    // Columns whose center x lies in [x0, x1).
                    int colStart = math.max(0, (int)math.ceil(scratch[k] / cell - 0.5f));
                    int colEnd = math.min(grid.Width - 1, (int)math.ceil(scratch[k + 1] / cell - 0.5f) - 1);
                    for (int col = colStart; col <= colEnd; col++)
                    {
                        fill(grid.IndexOf(col, row));
                    }
                }
            }
        }
    }
}
