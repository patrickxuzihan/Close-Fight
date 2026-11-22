using CloseFight.Core.Map;
using NUnit.Framework;
using Unity.Mathematics;

namespace CloseFight.Tests
{
    public class LineOfSightTests
    {
        const float Range = 500f;

        [Test]
        public void OpenGroundIsVisible()
        {
            var map = MapBuilder.Build(TestMaps.Empty());
            var result = LineOfSight.Check(map, new float2(5f, 5f), Posture.Standing, new float2(45f, 45f), Posture.Standing, Range);
            Assert.IsTrue(result.Visible);
        }

        [Test]
        public void BuildingBlocksGroundLevelSight()
        {
            var map = MapBuilder.Build(TestMaps.HouseAndTower());
            // Straight through the house at y = 25.
            var result = LineOfSight.Check(map, new float2(15f, 25f), Posture.Standing, new float2(40f, 25f), Posture.Standing, Range);
            Assert.IsFalse(result.Visible);
            Assert.That(result.BlockedAt, Is.GreaterThan(0f).And.LessThan(1f));
        }

        [Test]
        public void TowerSeesOverLowerHouse()
        {
            // D5: a unit in the 5-storey tower is on its top floor and can see over the 2-storey house.
            var map = MapBuilder.Build(TestMaps.HouseAndTower());
            var observer = new float2(5f, 25f);
            var target = new float2(48f, 25f);

            Assert.IsTrue(LineOfSight.Check(map, observer, Posture.Standing, target, Posture.Standing, Range).Visible);
        }

        [Test]
        public void ProneTargetRightBehindHouseStaysHidden()
        {
            var map = MapBuilder.Build(TestMaps.HouseAndTower());
            var observer = new float2(5f, 25f);
            var target = new float2(31f, 25f); // hugging the house's east wall

            Assert.IsFalse(LineOfSight.Check(map, observer, Posture.Standing, target, Posture.Prone, Range).Visible);
        }

        [Test]
        public void UnitInsideBuildingSeesOutOfItsOwnWindows()
        {
            var map = MapBuilder.Build(TestMaps.HouseAndTower());
            var inside = new float2(25f, 25f);
            var outside = new float2(25f, 45f);

            Assert.IsTrue(LineOfSight.Check(map, inside, Posture.Standing, outside, Posture.Standing, Range).Visible);
            Assert.IsTrue(LineOfSight.Check(map, outside, Posture.Standing, inside, Posture.Standing, Range).Visible);
        }

        [Test]
        public void EyeHeightInsideBuildingIsTopFloor()
        {
            var map = MapBuilder.Build(TestMaps.HouseAndTower());
            float eye = LineOfSight.EyeHeightAt(map, new float2(5f, 25f), Posture.Prone);
            Assert.AreEqual(4 * TerrainInfo.FloorHeight + LineOfSight.EyeStanding, eye, 1e-4f);
        }

        [Test]
        public void LowWallHidesProneButNotStanding()
        {
            var definition = TestMaps.Empty();
            definition.walls.Add(new WallDefinition
            {
                points = { new MapPoint(25f, 0f), new MapPoint(25f, 50f) },
                height = 1.0f,
            });
            var map = MapBuilder.Build(definition);

            var a = new float2(10f, 25f);
            var b = new float2(40f, 25f);
            Assert.IsTrue(LineOfSight.Check(map, a, Posture.Standing, b, Posture.Standing, Range).Visible);
            Assert.IsFalse(LineOfSight.Check(map, a, Posture.Prone, b, Posture.Prone, Range).Visible);
        }

        [Test]
        public void CollapsedWallSectionOpensSight()
        {
            var map = MapBuilder.Build(TestMaps.HouseAndTower());
            var grid = map.Grid;
            // Knock a 1 m wide corridor through the house along y = 25.
            for (int x = 20; x < 30; x++)
            {
                map.CollapseCell(grid.IndexOf(x, 25));
            }

            var result = LineOfSight.Check(map, new float2(15f, 25.5f), Posture.Standing, new float2(40f, 25.5f), Posture.Standing, Range);
            Assert.IsTrue(result.Visible, "standing eyes (1.6 m) see over the rubble heap");
        }

        [Test]
        public void OutOfRangeIsNotVisible()
        {
            var map = MapBuilder.Build(TestMaps.Empty());
            var result = LineOfSight.Check(map, new float2(0f, 0f), Posture.Standing, new float2(40f, 0f), Posture.Standing, 30f);
            Assert.IsFalse(result.Visible);
        }
    }
}
