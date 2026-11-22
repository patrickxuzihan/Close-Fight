using CloseFight.Core.Map;
using NUnit.Framework;
using Unity.Mathematics;

namespace CloseFight.Tests
{
    public class MapBuilderTests
    {
        [Test]
        public void GridSizeMatchesMapSize()
        {
            var map = MapBuilder.Build(TestMaps.Empty(50f));
            Assert.AreEqual(50, map.Grid.Width);
            Assert.AreEqual(50, map.Grid.Height);
        }

        [Test]
        public void RectangleBuildingFillsExactlyItsCells()
        {
            var map = MapBuilder.Build(TestMaps.HouseAndTower());
            var house = map.Buildings[0];

            Assert.AreEqual(100, house.Cells.Count, "10 x 10 m footprint on a 1 m grid");

            var grid = map.Grid;
            Assert.AreEqual(house.Index, grid.GetBuildingIndex(grid.IndexOf(20, 20)));
            Assert.AreEqual(house.Index, grid.GetBuildingIndex(grid.IndexOf(29, 29)));
            Assert.AreEqual(TacticalGrid.NoBuilding, grid.GetBuildingIndex(grid.IndexOf(19, 20)));
            Assert.AreEqual(TacticalGrid.NoBuilding, grid.GetBuildingIndex(grid.IndexOf(30, 29)));
        }

        [Test]
        public void BuildingCellsCarryRoofHeightAndCover()
        {
            var map = MapBuilder.Build(TestMaps.HouseAndTower());
            var grid = map.Grid;
            int index = grid.IndexOf(25, 25);

            Assert.AreEqual(2 * TerrainInfo.FloorHeight, grid.GetObstacleHeight(index), 1e-4f);
            Assert.AreEqual(TerrainInfo.BuildingCover(BuildingMaterial.Brick), grid.GetCover(index));
            Assert.IsTrue(grid.HasFlag(index, CellFlags.Building));
        }

        [Test]
        public void TriangleAreaPaintsTerrain()
        {
            var definition = TestMaps.Empty(20f);
            definition.areas.Add(new AreaDefinition
            {
                terrain = TerrainType.Rubble,
                polygon = { new MapPoint(0f, 0f), new MapPoint(10f, 0f), new MapPoint(0f, 10f) },
            });

            var map = MapBuilder.Build(definition);
            var grid = map.Grid;
            Assert.AreEqual(TerrainType.Rubble, grid.GetTerrain(grid.IndexOf(1, 1)));
            Assert.AreEqual(TerrainType.Open, grid.GetTerrain(grid.IndexOf(8, 8)));
        }

        [Test]
        public void DiagonalWallHasNoGaps()
        {
            var definition = TestMaps.Empty(20f);
            definition.walls.Add(new WallDefinition
            {
                points = { new MapPoint(2f, 2f), new MapPoint(18f, 18f) },
                height = 2f,
            });

            var map = MapBuilder.Build(definition);
            var grid = map.Grid;

            // Walk the wall's cells: each step must be 4-connected to the previous
            // one, otherwise a unit could slip through diagonally.
            for (int i = 2; i < 18; i++)
            {
                Assert.IsTrue(grid.HasFlag(grid.IndexOf(i, i), CellFlags.Wall), $"cell {i},{i}");
                bool linked = grid.HasFlag(grid.IndexOf(i + 1, i), CellFlags.Wall)
                    || grid.HasFlag(grid.IndexOf(i, i + 1), CellFlags.Wall);
                Assert.IsTrue(linked, $"gap after {i},{i}");
            }
        }

        [Test]
        public void CollapseTurnsCellIntoRubble()
        {
            var map = MapBuilder.Build(TestMaps.HouseAndTower());
            var grid = map.Grid;
            int index = grid.IndexOf(25, 25);

            int changed = -1;
            map.CellChanged += i => changed = i;

            Assert.IsTrue(map.CollapseCell(index));
            Assert.AreEqual(index, changed);
            Assert.IsTrue(grid.HasFlag(index, CellFlags.Collapsed));
            Assert.AreEqual(TerrainInfo.RubbleHeight, grid.GetObstacleHeight(index), 1e-4f);
            Assert.Less(grid.GetCover(index), TerrainInfo.BuildingCover(BuildingMaterial.Brick));
            Assert.AreEqual(1, map.Buildings[0].CollapsedCellCount);
            Assert.IsFalse(map.CollapseCell(index), "collapsing twice does nothing");
        }

        [Test]
        public void JsonRoundTripKeepsData()
        {
            var original = TestMaps.HouseAndTower();
            string json = MapSerializer.ToJson(original);
            StringAssert.Contains("\"Concrete\"", json, "enums are written as names");

            var copy = MapSerializer.FromJson(json);
            Assert.AreEqual(original.buildings.Count, copy.buildings.Count);
            Assert.AreEqual(5, copy.buildings[1].floors);
            Assert.AreEqual(BuildingMaterial.Concrete, copy.buildings[1].material);
            Assert.AreEqual(new float2(8f, 30f), (float2)copy.buildings[1].polygon[2]);
        }
    }
}
