using System.IO;
using CloseFight.Core.Battle;
using CloseFight.Core.Map;
using NUnit.Framework;
using Unity.Mathematics;

namespace CloseFight.Tests
{
    /// <summary>Checks on the shipped scenario and map files, not on test fixtures.</summary>
    public class ScenarioDataTests
    {
        const string DataRoot = "Assets/CloseFight/Data";

        public static string[] ScenarioFiles() => Directory.GetFiles(Path.Combine(DataRoot, "Scenarios"), "*.json");

        [Test]
        public void TanksAndGunsDoNotStartUnderARoof([ValueSource(nameof(ScenarioFiles))] string file)
        {
            var scenario = ScenarioLoader.FromJson(File.ReadAllText(file));
            var map = MapBuilder.Build(MapSerializer.FromJson(File.ReadAllText(Path.Combine(DataRoot, "Maps", scenario.mapId + ".json"))));
            var battle = ScenarioLoader.CreateBattle(scenario, map, withAi: false);

            foreach (var team in battle.Teams)
            {
                if (team.Vehicle != null)
                {
                    AssertClear(map, team.Vehicle.Position, 3f, team.Key);
                }
                else if (team.Gun != null)
                {
                    // A towed gun's trails reach about 2.5 m behind the axle.
                    AssertClear(map, team.Gun.Position, 2.5f, team.Key);
                }
            }
        }

        static void AssertClear(TacticalMap map, float2 center, float radius, string key)
        {
            var grid = map.Grid;
            for (float dy = -radius; dy <= radius; dy += 0.5f)
            {
                for (float dx = -radius; dx <= radius; dx += 0.5f)
                {
                    if (dx * dx + dy * dy > radius * radius)
                    {
                        continue;
                    }

                    var cell = grid.WorldToCell(center + new float2(dx, dy));
                    if (!grid.InBounds(cell))
                    {
                        continue;
                    }

                    Assert.IsNull(map.BuildingAt(grid.IndexOf(cell)),
                        $"{key} at {center} has a building within {radius} m; its sprite would be hidden under the roof");
                }
            }
        }
    }
}
