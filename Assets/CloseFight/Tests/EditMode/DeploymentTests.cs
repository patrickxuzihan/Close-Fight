using System.IO;
using System.Linq;
using CloseFight.Core.AI;
using CloseFight.Core.Battle;
using CloseFight.Core.Map;
using CloseFight.Core.Units;
using NUnit.Framework;
using Unity.Mathematics;

namespace CloseFight.Tests
{
    public class DeploymentTests
    {
        const string DataRoot = "Assets/CloseFight/Data";

        /// <summary>House and tower map; Germans may deploy in the south strip, Soviets in the north.</summary>
        static BattleState Battle()
        {
            var battle = new BattleState(MapBuilder.Build(TestMaps.HouseAndTower()), 5);
            battle.AddDeployZone(Side.German, new[] { new float2(0, 0), new float2(50, 0), new float2(50, 32), new float2(0, 32) });
            battle.AddDeployZone(Side.Soviet, new[] { new float2(0, 40), new float2(50, 40), new float2(50, 50), new float2(0, 50) });
            return battle;
        }

        [Test]
        public void TeamsGoOnlyWhereTheirZoneAllowsAndOnlyBeforeTheBattle()
        {
            var battle = Battle();
            var team = battle.AddTeam("de", Side.German, TeamType.Rifle, "", new float2(10, 10), 0f, 6, 50);
            battle.BeginDeployment();

            Assert.AreEqual(DeployRefusal.OutsideZone, battle.CheckDeploy(team, new float2(10, 45)));
            Assert.IsFalse(battle.Redeploy(team, new float2(10, 45), 0f));
            Assert.AreEqual(10f, team.Centroid.y, 1.5f, "a refused placement leaves the team where it was");

            Assert.IsTrue(battle.Redeploy(team, new float2(40, 12), 90f));
            Assert.Less(math.distance(team.Centroid, new float2(40, 12)), 3f);
            Assert.AreEqual(90f, team.Members[0].Heading, 1e-3f);

            battle.BeginBattle();
            Assert.AreEqual(DeployRefusal.NotDeploying, battle.CheckDeploy(team, new float2(40, 20)));
        }

        [Test]
        public void TanksNeedRoomAndGunsStayOutOfBuildings()
        {
            var battle = Battle();
            var tank = battle.AddTeam("stug", Side.German, TeamType.Vehicle, "", new float2(40, 8), 0f, 4, 50, "stug3f");
            var gun = battle.AddTeam("pak", Side.German, TeamType.AntiTankGun, "", new float2(14, 8), 0f, 4, 50, "pak38");
            var rifles = battle.AddTeam("de", Side.German, TeamType.Rifle, "", new float2(10, 10), 0f, 6, 50);
            battle.BeginDeployment();

            var inHouse = new float2(25, 25);
            Assert.AreEqual(DeployRefusal.NoRoomForVehicle, battle.CheckDeploy(tank, inHouse));
            Assert.AreEqual(DeployRefusal.GunInBuilding, battle.CheckDeploy(gun, inHouse));
            Assert.AreEqual(DeployRefusal.None, battle.CheckDeploy(rifles, inHouse), "infantry may hold a house");

            Assert.IsTrue(battle.Redeploy(tank, new float2(40, 20), 0f));
            Assert.AreEqual(new float2(40, 20), tank.Vehicle.Position);
            Assert.IsTrue(battle.Redeploy(gun, new float2(12, 12), 0f));
            Assert.Less(math.distance(gun.Gun.Position, new float2(12, 12)), 3f, "the gun moves with its crew");
        }

        [Test]
        public void TheClockWaitsUntilDeploymentEnds()
        {
            var battle = Battle();
            battle.AddTeam("de", Side.German, TeamType.Rifle, "", new float2(10, 10), 0f, 6, 50);
            battle.BeginDeployment();

            battle.Update(5f);
            battle.Step();
            Assert.AreEqual(0, battle.Clock.TickCount);

            battle.BeginBattle();
            battle.Update(1f);
            Assert.Greater(battle.Clock.TickCount, 0);
        }

        static (ScenarioDefinition scenario, TacticalMap map) Load(string scenarioFile)
        {
            var scenario = ScenarioLoader.FromJson(File.ReadAllText(Path.Combine(DataRoot, "Scenarios", scenarioFile)));
            var map = MapBuilder.Build(MapSerializer.FromJson(File.ReadAllText(Path.Combine(DataRoot, "Maps", scenario.mapId + ".json"))));
            return (scenario, map);
        }

        [Test]
        public void DefendersAreInTheirPositionsWhenTheBattleOpens()
        {
            var (scenario, map) = Load("test_block_skirmish.json");
            var battle = ScenarioLoader.CreateBattle(scenario, map, deployment: true);
            Assert.IsTrue(battle.IsDeploying);

            var ai = battle.Commanders.OfType<DefenseCommander>().Single();
            int placed = 0;
            foreach (var plan in ai.Plans)
            {
                if (!battle.InDeployZone(Side.Soviet, plan.Primary))
                {
                    continue;
                }

                float2 at = plan.Team.Vehicle != null ? plan.Team.Vehicle.Position : plan.Team.Centroid;
                Assert.Less(math.distance(at, plan.Primary), 4f, $"{plan.Team.Key} should start in its planned position");
                placed++;
            }

            Assert.Greater(placed, 0, "precondition: some planned positions lie in the Soviet zone");
        }

        [Test]
        public void AiDeploymentDoesNotDependOnWhereTheEnemyStands()
        {
            float2[] Deployed(float germanShift)
            {
                var (scenario, map) = Load("test_block_skirmish.json");
                foreach (var team in scenario.teams.Where(t => t.side == Side.German))
                {
                    team.position = new MapPoint(team.position.x + germanShift, team.position.y);
                }

                var battle = ScenarioLoader.CreateBattle(scenario, map, deployment: true);
                return battle.Teams.Where(t => t.Side == Side.Soviet).Select(t => t.Centroid).ToArray();
            }

            var a = Deployed(0f);
            var b = Deployed(-25f);
            Assert.AreEqual(a.Length, b.Length);
            for (int i = 0; i < a.Length; i++)
            {
                Assert.Less(math.distance(a[i], b[i]), 1e-3f, "the AI deploys from the map alone (D17)");
            }
        }

        [Test]
        public void AttackersWaitingInTheirZoneAreNotShotToPieces()
        {
            var (scenario, map) = Load("stalingrad_station_sept14.json");
            var battle = ScenarioLoader.CreateBattle(scenario, map, deployment: true);
            battle.BeginBattle();
            Assert.IsTrue(battle.Teams.Where(t => t.Side == Side.German).All(t => t.Order.Type == OrderType.Ambush),
                "teams without orders start in ambush");

            int start = battle.Soldiers.Count(s => s.Team.Side == Side.German && s.IsActive);
            for (int i = 0; i < 600; i++) battle.Step();
            int lost = start - battle.Soldiers.Count(s => s.Team.Side == Side.German && s.IsActive);
            UnityEngine.Debug.Log($"German soldiers lost in the first minute without orders: {lost} of {start}");
            Assert.LessOrEqual(lost, 2, "the default deployment should not be in the defenders' sights");
        }

        [Test]
        public void ScenarioTeamsStartInsideTheirZones([ValueSource(typeof(ScenarioDataTests), nameof(ScenarioDataTests.ScenarioFiles))] string file)
        {
            var scenario = ScenarioLoader.FromJson(File.ReadAllText(file));
            foreach (var side in scenario.sides)
            {
                Assert.GreaterOrEqual(side.deployZone.Count, 3, $"{side.side} has a deployment zone");
                var zone = side.deployZone.Select(p => (float2)p).ToArray();
                foreach (var team in scenario.teams.Where(t => t.side == side.side))
                {
                    Assert.IsTrue(Core.Geometry.PolygonMath.Contains(zone, team.position), $"{team.key} starts outside the {side.side} zone");
                }
            }
        }
    }
}
