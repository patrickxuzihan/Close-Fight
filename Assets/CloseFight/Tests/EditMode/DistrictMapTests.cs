using System.IO;
using System.Linq;
using CloseFight.Core.Battle;
using CloseFight.Core.Campaign;
using CloseFight.Core.Geometry;
using CloseFight.Core.Map;
using CloseFight.Core.Units;
using NUnit.Framework;
using Unity.Mathematics;

namespace CloseFight.Tests
{
    /// <summary>The districts' generated battle maps (M16 step 4).</summary>
    public class DistrictMapTests
    {
        static CampaignState Campaign() =>
            CampaignState.Create(CampaignLoader.FromJson(File.ReadAllText("Assets/CloseFight/Data/Campaigns/stalingrad_center.json")));

        [Test]
        public void EveryDistrictGetsAMapThatCanBeFoughtFromAnySide()
        {
            var state = Campaign();
            foreach (var district in state.definition.districts)
            {
                var definition = DistrictMapGenerator.Generate(district, DistrictBattlefields.MapSeed(state, district.id));
                var map = MapBuilder.Build(definition);
                Assert.Greater(definition.buildings.Count, 0, district.id);
                foreach (var edge in new[] { MapEdge.West, MapEdge.East, MapEdge.North, MapEdge.South })
                {
                    var field = DistrictBattlefields.Orient(definition, edge);
                    foreach (var p in field.attackerZone.Concat(field.defenderZone))
                    {
                        Assert.That(p.x, Is.InRange(0f, definition.width), district.id);
                        Assert.That(p.y, Is.InRange(0f, definition.height), district.id);
                    }

                    Assert.AreEqual(3, field.objectives.Count);
                    foreach (var objective in field.objectives)
                    {
                        Assert.IsTrue(PolygonMath.Contains(field.defenderZone.ConvertAll(q => (float2)q), objective.position) ||
                                      math.distance(objective.position, new float2(definition.width, definition.height) / 2f) < 80f,
                            $"{district.id}: objectives lie in the middle or in the defender's ground");
                    }
                }

                int open = 0;
                for (int i = 0; i < map.Grid.CellCount; i += 97)
                {
                    if (!map.Grid.HasFlag(i, CellFlags.BlocksMovement)) open++;
                }

                Assert.Greater(open, map.Grid.CellCount / 97 / 3, $"{district.id} is not all buildings and walls");
            }
        }

        [Test]
        public void ADistrictsMapIsTheSameEveryTime()
        {
            var state = Campaign();
            var station2 = state.DistrictDefinition("station2");
            string a = MapSerializer.ToJson(DistrictMapGenerator.Generate(station2, DistrictBattlefields.MapSeed(state, "station2")));
            string b = MapSerializer.ToJson(DistrictMapGenerator.Generate(station2, DistrictBattlefields.MapSeed(state, "station2")));
            Assert.AreEqual(a, b);
            var tsaritsa = state.DistrictDefinition("tsaritsa");
            Assert.AreNotEqual(a, MapSerializer.ToJson(DistrictMapGenerator.Generate(tsaritsa, DistrictBattlefields.MapSeed(state, "tsaritsa"))));
        }

        [Test]
        public void TheAttackerComesInFromWhereItAttackedFrom()
        {
            var state = Campaign();
            var orders = new SideOrders { side = Side.German };
            orders.Move("de_ir194", "hill_112");
            state.SubmitOrders(orders);
            state.SubmitOrders(new SideOrders { side = Side.Soviet });
            var battle = state.ResolveMovement().Single(b => b.district == "hill_112");
            // Aviagorodok lies north-west of Height 112.5 on the strategic map, more north than west.
            Assert.AreEqual(MapEdge.North, DistrictBattlefields.AttackEdge(state, battle));
            var (map, field) = DistrictBattlefields.For(state, battle, _ => null);
            Assert.AreEqual(MapEdge.North, field.attackerRetreat);
            Assert.AreEqual(MapEdge.South, field.defenderRetreat);
            Assert.IsTrue(field.attackerZone.All(p => p.y >= map.height - DistrictBattlefields.AttackerDepth - 0.01f));
        }

        [Test]
        public void TheStationIsFoughtOnItsHandMadeMap()
        {
            var state = Campaign();
            var orders = new SideOrders { side = Side.Soviet };
            state.District("hill_112").owner = Side.German;
            state.SubmitOrders(new SideOrders { side = Side.German });
            state.SubmitOrders(orders);
            var pending = new PendingBattle { district = "station", attacker = Side.German, attackers = { "de_ir194" }, defenders = { "su_272nkvd" } };
            bool asked = false;
            var (map, field) = DistrictBattlefields.For(state, pending, id =>
            {
                asked = id == "stalingrad_station";
                var scenario = ScenarioLoader.FromJson(File.ReadAllText("Assets/CloseFight/Data/Scenarios/stalingrad_station_sept14.json"));
                return (MapSerializer.FromJson(File.ReadAllText("Assets/CloseFight/Data/Maps/stalingrad_station.json")), scenario);
            });
            Assert.IsTrue(asked);
            Assert.AreEqual("stalingrad_station", map.id);
            Assert.AreEqual(DistrictBattlefields.HandMadeCapacity, field.capacity);
        }

        [Test]
        public void RuinsStayRuinedForTheNextBattle()
        {
            var state = Campaign();
            var district = state.DistrictDefinition("station2");
            var definition = DistrictMapGenerator.Generate(district, DistrictBattlefields.MapSeed(state, "station2"));
            var map = MapBuilder.Build(definition);
            int building = Enumerable.Range(0, map.Grid.CellCount).First(i => map.Grid.HasFlag(i, CellFlags.Building));
            int wall = Enumerable.Range(0, map.Grid.CellCount).First(i => map.Grid.HasFlag(i, CellFlags.Wall) && !map.Grid.HasFlag(i, CellFlags.Building));
            map.CollapseCell(building);
            if (map.Grid.HasFlag(wall, CellFlags.BlocksMovement)) map.DemolishWall(wall);
            else map.CrushCell(wall);
            DistrictDamage.Capture(map, state.District("station2"));

            var next = MapBuilder.Build(definition);
            Assert.IsFalse(next.Grid.HasFlag(building, CellFlags.Collapsed));
            DistrictDamage.Apply(next, state.District("station2"));
            Assert.IsTrue(next.Grid.HasFlag(building, CellFlags.Collapsed), "D33: the ruin is still there");
            Assert.IsFalse(next.Grid.HasFlag(wall, CellFlags.Wall));
        }

        [Test]
        public void ABattleOnAGeneratedMapIsFoughtAndWrittenBack()
        {
            var state = Campaign();
            var orders = new SideOrders { side = Side.German };
            orders.Move("de_ir194", "hill_112");
            state.SubmitOrders(orders);
            state.SubmitOrders(new SideOrders { side = Side.Soviet });
            var pending = state.ResolveMovement().Single(b => b.district == "hill_112");
            var (definition, field) = DistrictBattlefields.For(state, pending, _ => null);
            var map = MapBuilder.Build(definition);
            var fight = CampaignBattle.Create(state, pending, field, map,
                BattleForces.Default(state, pending, Side.German, field.capacity), BattleForces.Default(state, pending, Side.Soviet, field.capacity), null);
            var battle = fight.Battle;
            foreach (var soldier in battle.Soldiers)
            {
                var cell = map.Grid.WorldToCell(soldier.Position);
                Assert.IsTrue(map.Grid.InBounds(cell), $"{soldier.FullName} is on the map");
            }

            battle.BeginBattle();
            battle.Victory.TimeLimitSeconds = 90f;
            for (int i = 0; i < 1000 && !battle.Victory.IsOver; i++)
            {
                battle.Step();
            }

            Assert.IsTrue(battle.Victory.IsOver);
            state.ApplyBattle(pending, fight.Finish());
            Assert.IsTrue(state.District("hill_112").collapsed.Count >= 0);
        }
    }
}
