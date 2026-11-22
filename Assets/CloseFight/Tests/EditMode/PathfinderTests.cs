using System.Collections.Generic;
using CloseFight.Core.Map;
using CloseFight.Core.Path;
using CloseFight.Core.Sim;
using NUnit.Framework;

namespace CloseFight.Tests
{
    public class PathfinderTests
    {
        [Test]
        public void StraightLineOnOpenGround()
        {
            var map = MapBuilder.Build(TestMaps.Empty());
            var finder = new GridPathfinder(map);
            var path = new List<GridCoord>();

            Assert.IsTrue(finder.FindPath(new GridCoord(5, 5), new GridCoord(15, 5), MovementClass.Infantry, path));
            Assert.AreEqual(11, path.Count);
            Assert.AreEqual(new GridCoord(5, 5), path[0]);
            Assert.AreEqual(new GridCoord(15, 5), path[path.Count - 1]);
        }

        [Test]
        public void VehicleDrivesAroundBuilding()
        {
            var map = MapBuilder.Build(TestMaps.HouseAndTower());
            var finder = new GridPathfinder(map);
            var path = new List<GridCoord>();

            Assert.IsTrue(finder.FindPath(new GridCoord(15, 25), new GridCoord(40, 25), MovementClass.Vehicle, path));
            foreach (var cell in path)
            {
                Assert.IsFalse(map.Grid.HasFlag(map.Grid.IndexOf(cell), CellFlags.Building), $"vehicle entered building at {cell}");
            }
        }

        [Test]
        public void InfantryMayCrossBuildingButVehicleMayNot()
        {
            var map = MapBuilder.Build(TestMaps.HouseAndTower());
            var finder = new GridPathfinder(map);
            int houseCell = map.Grid.IndexOf(25, 25);

            Assert.IsTrue(finder.IsPassable(houseCell, MovementClass.Infantry));
            Assert.IsFalse(finder.IsPassable(houseCell, MovementClass.Vehicle));
        }

        [Test]
        public void NoCornerCuttingThroughWallJoint()
        {
            var definition = TestMaps.Empty(20f);
            // Two wall pieces touching only at a corner: cells (10,10) and (11,11) are blocked.
            // The horizontal piece ends at x = 15, so there is a legal way around.
            definition.walls.Add(new WallDefinition { points = { new MapPoint(10.5f, 0.5f), new MapPoint(10.5f, 10.5f) } });
            definition.walls.Add(new WallDefinition { points = { new MapPoint(11.5f, 11.5f), new MapPoint(15.5f, 11.5f) } });
            var map = MapBuilder.Build(definition);
            var finder = new GridPathfinder(map);
            var path = new List<GridCoord>();

            Assert.IsTrue(finder.FindPath(new GridCoord(12, 10), new GridCoord(10, 12), MovementClass.Infantry, path));
            var jointA = new GridCoord(11, 10);
            var jointB = new GridCoord(10, 11);
            for (int i = 1; i < path.Count; i++)
            {
                var a = path[i - 1];
                var b = path[i];
                bool squeezesThroughJoint = (a == jointA && b == jointB) || (a == jointB && b == jointA);
                Assert.IsFalse(squeezesThroughJoint, "path cut a blocked corner");
            }

            Assert.Greater(path.Count, 6, "the legal route detours around the wall's end");
        }

        [Test]
        public void UnreachableGoalReturnsClosestPartialPath()
        {
            var definition = TestMaps.Empty(30f);
            definition.areas.Add(new AreaDefinition { terrain = TerrainType.Water, polygon = TestMaps.Rect(18f, 0f, 30f, 30f) });
            var map = MapBuilder.Build(definition);
            var finder = new GridPathfinder(map);
            var path = new List<GridCoord>();

            Assert.IsFalse(finder.FindPath(new GridCoord(2, 15), new GridCoord(25, 15), MovementClass.Infantry, path));
            Assert.IsNotEmpty(path);
            Assert.AreEqual(17, path[path.Count - 1].X, "stops at the water's edge");
        }

        [Test]
        public void ExtraCostSteersAwayFromDanger()
        {
            var map = MapBuilder.Build(TestMaps.Empty(30f));
            var finder = new GridPathfinder(map);
            var path = new List<GridCoord>();
            var grid = map.Grid;

            // A "kill zone" across the middle of the direct route.
            finder.ExtraCost = index =>
            {
                var c = grid.CoordOf(index);
                return c.X >= 10 && c.X <= 20 && c.Y >= 10 && c.Y <= 20 ? 20f : 0f;
            };

            Assert.IsTrue(finder.FindPath(new GridCoord(5, 15), new GridCoord(25, 15), MovementClass.Infantry, path));
            foreach (var cell in path)
            {
                bool inZone = cell.X >= 10 && cell.X <= 20 && cell.Y >= 10 && cell.Y <= 20;
                Assert.IsFalse(inZone, $"path entered kill zone at {cell}");
            }
        }

        [Test]
        public void SimRandomIsReproducible()
        {
            var a = new SimRandom(12345);
            var b = new SimRandom(12345);
            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(a.NextUInt(), b.NextUInt());
            }

            var c = new SimRandom(1);
            float f = c.NextFloat();
            Assert.That(f, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
        }
    }
}
