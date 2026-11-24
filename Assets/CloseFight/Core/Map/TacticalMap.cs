using System;
using System.Collections.Generic;

namespace CloseFight.Core.Map
{
    /// <summary>Runtime state of one building: its definition plus damage.</summary>
    public sealed class BuildingState
    {
        public short Index { get; }
        public BuildingDefinition Definition { get; }
        public int Floors => Definition.floors;
        public BuildingMaterial Material => Definition.material;
        /// <summary>Roof height in meters.</summary>
        public float RoofHeight => Definition.floors * TerrainInfo.FloorHeight;

        /// <summary>Grid indices of every cell in the footprint.</summary>
        public List<int> Cells { get; } = new List<int>();
        public int CollapsedCellCount { get; internal set; }

        public BuildingState(short index, BuildingDefinition definition)
        {
            Index = index;
            Definition = definition;
        }
    }

    /// <summary>
    /// A loaded battle map: the gameplay grid plus building state.
    /// Raised events let the presentation layer refresh only what changed.
    /// </summary>
    public sealed class TacticalMap
    {
        public MapDefinition Definition { get; }
        public TacticalGrid Grid { get; }
        public IReadOnlyList<BuildingState> Buildings => _buildings;

        /// <summary>Raised with the grid index of every cell whose gameplay data changed.</summary>
        public event Action<int> CellChanged;

        readonly List<BuildingState> _buildings = new List<BuildingState>();

        public TacticalMap(MapDefinition definition, TacticalGrid grid)
        {
            Definition = definition;
            Grid = grid;
        }

        internal BuildingState AddBuilding(BuildingDefinition definition)
        {
            if (_buildings.Count >= short.MaxValue)
            {
                throw new InvalidOperationException("Too many buildings on one map.");
            }

            var state = new BuildingState((short)_buildings.Count, definition);
            _buildings.Add(state);
            return state;
        }

        /// <summary>Building occupying the cell, or null.</summary>
        public BuildingState BuildingAt(int cellIndex)
        {
            short index = Grid.GetBuildingIndex(cellIndex);
            return index == TacticalGrid.NoBuilding ? null : _buildings[index];
        }

        float[] _damage;

        /// <summary>
        /// Blast damage to a building cell (HE shells). At 100 the cell and its
        /// building neighbours collapse into rubble (D38). Returns true if anything collapsed.
        /// </summary>
        public bool DamageCell(int cellIndex, float amount)
        {
            var grid = Grid;
            if (!grid.HasFlag(cellIndex, CellFlags.Building) || grid.HasFlag(cellIndex, CellFlags.Collapsed))
            {
                return false;
            }

            if (_damage == null)
            {
                _damage = new float[grid.CellCount];
            }

            _damage[cellIndex] += amount;
            if (_damage[cellIndex] < 100f)
            {
                return false;
            }

            var coord = grid.CoordOf(cellIndex);
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (grid.InBounds(coord.X + dx, coord.Y + dy))
                    {
                        CollapseCell(grid.IndexOf(coord.X + dx, coord.Y + dy));
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// A demolition charge blows a wall away (D38, D39: engineers open a new way):
        /// the cell becomes passable rubble. Building cells are left to <see cref="DamageCell"/>.
        /// Returns true if a wall was removed.
        /// </summary>
        public bool DemolishWall(int cellIndex)
        {
            var grid = Grid;
            if (!grid.HasFlag(cellIndex, CellFlags.Wall) || grid.HasFlag(cellIndex, CellFlags.Building))
            {
                return false;
            }

            grid.RemoveFlags(cellIndex, CellFlags.Wall | CellFlags.BlocksMovement | CellFlags.BlocksSight);
            grid.SetTerrain(cellIndex, TerrainType.Rubble);
            grid.SetObstacleHeight(cellIndex, TerrainInfo.TerrainObstacleHeight(TerrainType.Rubble));
            grid.SetCover(cellIndex, TerrainInfo.TerrainCover(TerrainType.Rubble));
            CellChanged?.Invoke(cellIndex);
            return true;
        }

        /// <summary>A tank drives over a fence or a wooden shed: flatten it (D37).</summary>
        public void CrushCell(int cellIndex)
        {
            var grid = Grid;
            if (grid.HasFlag(cellIndex, CellFlags.Building))
            {
                var building = BuildingAt(cellIndex);
                if (building != null && building.Material == BuildingMaterial.Wood && building.Floors <= 1)
                {
                    CollapseCell(cellIndex);
                }

                return;
            }

            // Crushable fences do not block movement; solid walls do (and tanks never enter them).
            if (grid.HasFlag(cellIndex, CellFlags.Wall) && !grid.HasFlag(cellIndex, CellFlags.BlocksMovement))
            {
                grid.RemoveFlags(cellIndex, CellFlags.Wall);
                var terrain = grid.GetTerrain(cellIndex);
                grid.SetObstacleHeight(cellIndex, TerrainInfo.TerrainObstacleHeight(terrain));
                grid.SetCover(cellIndex, TerrainInfo.TerrainCover(terrain));
                CellChanged?.Invoke(cellIndex);
            }
        }

        /// <summary>
        /// Collapses one building cell (D38): no drawn collapse animation, the view
        /// overlays a collapse decal. The cell becomes a rubble heap: low, see-through
        /// above the heap, and it no longer gives building cover.
        /// </summary>
        public bool CollapseCell(int cellIndex)
        {
            var grid = Grid;
            if (!grid.HasFlag(cellIndex, CellFlags.Building) || grid.HasFlag(cellIndex, CellFlags.Collapsed))
            {
                return false;
            }

            grid.AddFlags(cellIndex, CellFlags.Collapsed);
            grid.RemoveFlags(cellIndex, CellFlags.BlocksSight);
            grid.SetTerrain(cellIndex, TerrainType.Rubble);
            grid.SetObstacleHeight(cellIndex, TerrainInfo.RubbleHeight);
            grid.SetCover(cellIndex, TerrainInfo.TerrainCover(TerrainType.Rubble));

            var building = BuildingAt(cellIndex);
            if (building != null)
            {
                building.CollapsedCellCount++;
            }

            CellChanged?.Invoke(cellIndex);
            return true;
        }
    }
}
