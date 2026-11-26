using CloseFight.Core.Battle;
using CloseFight.Core.Map;
using CloseFight.Core.Sim;
using CloseFight.Core.Text;
using CloseFight.Core.Units;
using NUnit.Framework;
using Unity.Mathematics;

namespace CloseFight.Tests
{
    public class BattleMovementTests
    {
        static BattleState NewBattle(MapDefinition map = null)
        {
            return new BattleState(MapBuilder.Build(map ?? TestMaps.HouseAndTower()), 7);
        }

        static void Run(BattleState battle, float seconds)
        {
            int ticks = (int)math.ceil(seconds / SimClock.TickSeconds);
            for (int i = 0; i < ticks; i++)
            {
                battle.Step();
            }
        }

        static void AssertNobodyInBlockedCell(BattleState battle)
        {
            var grid = battle.Map.Grid;
            foreach (var soldier in battle.Soldiers)
            {
                var cell = grid.WorldToCell(soldier.Position);
                Assert.IsTrue(grid.InBounds(cell), $"{soldier.FullName} left the map");
                Assert.IsFalse(grid.HasFlag(grid.IndexOf(cell), CellFlags.BlocksMovement), $"{soldier.FullName} stands in a blocked cell at {cell}");
            }
        }

        [Test]
        public void AddTeamCreatesNamedSoldiersWithOneLeader()
        {
            var battle = NewBattle();
            var team = battle.AddTeam("t", Side.German, TeamType.Rifle, "1. Gruppe", new float2(10, 10), 0f, 6, 50);

            Assert.AreEqual(6, team.Members.Count);
            Assert.AreEqual(1, team.Members.FindAll(s => s.IsLeader).Count);
            Assert.AreSame(team.Members[0], team.Leader);
            foreach (var soldier in team.Members)
            {
                Assert.IsFalse(string.IsNullOrEmpty(soldier.LastName));
                Assert.IsNotNull(soldier.RankKey);
            }

            Assert.AreEqual(0, team.Members[1].Attributes.Leadership, "only leaders have leadership");
        }

        [Test]
        public void SameSeedGivesSameSoldiers()
        {
            var a = NewBattle().AddTeam("t", Side.Soviet, TeamType.Rifle, "", new float2(10, 10), 0f, 6, 50);
            var b = NewBattle().AddTeam("t", Side.Soviet, TeamType.Rifle, "", new float2(10, 10), 0f, 6, 50);
            for (int i = 0; i < a.Members.Count; i++)
            {
                Assert.AreEqual(a.Members[i].FullName, b.Members[i].FullName);
                Assert.AreEqual(a.Members[i].Attributes.Morale, b.Members[i].Attributes.Morale);
            }
        }

        [Test]
        public void TeamWalksAroundHouseAndDeploysAtTarget()
        {
            var battle = NewBattle();
            var team = battle.AddTeam("t", Side.German, TeamType.Rifle, "", new float2(25, 10), 0f, 6, 50);
            var target = new float2(25, 42); // beyond the house; infantry may cut through it or go around

            Assert.IsTrue(battle.IssueOrder(team, OrderType.Move, target));
            Assert.AreEqual(TeamActivity.Moving, team.Activity);

            for (int i = 0; i < 1200 && team.Activity != TeamActivity.Idle; i++)
            {
                battle.Step();
                AssertNobodyInBlockedCell(battle);
            }

            Assert.AreEqual(TeamActivity.Idle, team.Activity, "team should have arrived within 120 s");
            Assert.Less(math.distance(team.Centroid, target), 6f);
            foreach (var soldier in team.Members)
            {
                Assert.AreEqual(Posture.Kneeling, soldier.Posture);
                Assert.IsFalse(soldier.IsMoving);
            }
        }

        [Test]
        public void WalledYardIsEnteredThroughTheGap()
        {
            // A yard walled on three sides, open to the north.
            var definition = TestMaps.Empty(40f);
            definition.walls.Add(new WallDefinition
            {
                points = { new MapPoint(10, 30), new MapPoint(10, 10), new MapPoint(30, 10), new MapPoint(30, 30) },
                height = 2.5f,
            });
            var battle = NewBattle(definition);
            var team = battle.AddTeam("t", Side.German, TeamType.MachineGun, "", new float2(20, 4), 0f, 3, 50);

            Assert.IsTrue(battle.IssueOrder(team, OrderType.MoveFast, new float2(20, 18)));
            for (int i = 0; i < 1200 && team.Activity != TeamActivity.Idle; i++)
            {
                battle.Step();
                AssertNobodyInBlockedCell(battle);
            }

            Assert.AreEqual(TeamActivity.Idle, team.Activity);
            foreach (var soldier in team.Members)
            {
                Assert.That(soldier.Position.x, Is.InRange(10.5f, 29.5f), "inside the yard");
                Assert.That(soldier.Position.y, Is.InRange(10.5f, 30f), "inside the yard");
            }
        }

        [Test]
        public void CrawlIsSlowerThanWalkAndKeepsSoldiersProne()
        {
            float TimeToArrive(OrderType order, out BattleState battle, out Team team)
            {
                battle = NewBattle(TestMaps.Empty(60f));
                team = battle.AddTeam("t", Side.German, TeamType.MachineGun, "", new float2(10, 10), 0f, 3, 50);
                battle.IssueOrder(team, order, new float2(10, 30));
                int ticks = 0;
                while (team.Activity != TeamActivity.Idle && ticks < 3000)
                {
                    battle.Step();
                    ticks++;
                }

                return ticks * SimClock.TickSeconds;
            }

            float walk = TimeToArrive(OrderType.Move, out _, out _);
            float crawl = TimeToArrive(OrderType.Crawl, out _, out var crawlers);
            float run = TimeToArrive(OrderType.MoveFast, out _, out _);

            Assert.Less(run, walk);
            Assert.Less(walk, crawl);
            foreach (var soldier in crawlers.Members)
            {
                Assert.AreEqual(Posture.Prone, soldier.Posture);
            }
        }

        [Test]
        public void DefendAmbushHideStopTheTeam()
        {
            var battle = NewBattle(TestMaps.Empty(60f));
            var team = battle.AddTeam("t", Side.German, TeamType.Rifle, "", new float2(10, 10), 0f, 6, 50);
            battle.IssueOrder(team, OrderType.Move, new float2(40, 40));
            Run(battle, 3f);

            Assert.IsTrue(battle.IssueOrder(team, OrderType.Hide, float2.zero));
            Assert.AreEqual(TeamActivity.Idle, team.Activity);
            Assert.AreEqual(OrderType.Hide, team.Order.Type);
            var before = team.Members[0].Position;
            Run(battle, 3f);
            Assert.AreEqual(before, team.Members[0].Position, "hidden team does not move");
            Assert.AreEqual(Posture.Prone, team.Members[0].Posture);

            Assert.IsTrue(battle.IssueOrder(team, OrderType.Defend, float2.zero));
            Assert.AreEqual(Posture.Kneeling, team.Members[0].Posture);
        }

        [Test]
        public void RetreatHeadsForOwnMapEdge()
        {
            var battle = NewBattle(TestMaps.Empty(60f));
            battle.SetRetreatEdge(Side.Soviet, MapEdge.North);
            var team = battle.AddTeam("t", Side.Soviet, TeamType.MachineGun, "", new float2(30, 20), 180f, 3, 50);

            Assert.IsTrue(battle.IssueOrder(team, OrderType.Retreat, float2.zero));
            Assert.Greater(team.Order.Target.y, 55f, "Soviets fall back north");
        }

        [Test]
        public void AssaultBuildingNeedsABuilding()
        {
            var battle = NewBattle();
            var team = battle.AddTeam("t", Side.German, TeamType.Rifle, "", new float2(25, 10), 0f, 6, 50);

            Assert.IsFalse(battle.IssueOrder(team, OrderType.AssaultBuilding, new float2(40, 10)), "open ground");
            Assert.IsTrue(battle.IssueOrder(team, OrderType.AssaultBuilding, new float2(25, 25)), "the house");
        }

        [Test]
        public void FireOrderNeedsAVisibleEnemy()
        {
            var battle = NewBattle();
            var team = battle.AddTeam("t", Side.German, TeamType.Rifle, "", new float2(25, 10), 0f, 6, 50);
            Assert.IsFalse(battle.IssueOrder(team, OrderType.Fire, new float2(25, 40)), "nobody there");
        }

        [Test]
        public void ClockPausesAndCountsTicks()
        {
            var clock = new SimClock();
            Assert.AreEqual(3, clock.Advance(0.35f));
            clock.Paused = true;
            Assert.AreEqual(0, clock.Advance(1f));
            clock.Paused = false;
            Assert.AreEqual(1, clock.Advance(0.06f), "0.05 left over + 0.06");

            var battle = NewBattle();
            battle.Update(0.35f);
            battle.Clock.Paused = true;
            battle.Update(1f);
            Assert.AreEqual(3, battle.Clock.TickCount, "only ticks that ran are counted");
            Assert.AreEqual(0.3f, battle.Clock.ElapsedSeconds, 1e-4f);
        }

        [Test]
        public void ScenarioJsonCreatesTeams()
        {
            const string json = @"{
                ""id"": ""t"", ""seed"": 5,
                ""sides"": [ { ""side"": ""Soviet"", ""retreatEdge"": ""North"" } ],
                ""teams"": [
                    { ""key"": ""a"", ""side"": ""German"", ""type"": ""Rifle"", ""displayName"": ""1. Gruppe"", ""position"": { ""x"": 10, ""y"": 10 } },
                    { ""key"": ""b"", ""side"": ""Soviet"", ""type"": ""MachineGun"", ""position"": { ""x"": 40, ""y"": 40 }, ""facing"": 180 }
                ]
            }";

            var scenario = ScenarioLoader.FromJson(json);
            var battle = ScenarioLoader.CreateBattle(scenario, MapBuilder.Build(TestMaps.Empty()));

            Assert.AreEqual(2, battle.Teams.Count);
            Assert.AreEqual(BattleState.DefaultSize(battle.Teams[0].Type, battle.Teams[0].Side), battle.Teams[0].Members.Count, "default (full) rifle squad size");
            Assert.AreEqual(BattleState.DefaultSize(TeamType.MachineGun), battle.Teams[1].Members.Count, "default MG team size");
            Assert.AreEqual(MapEdge.North, battle.RetreatEdgeOf(Side.Soviet));
        }

        [Test]
        public void LocalizationFallsBackToEnglishThenKey()
        {
            var table = LocalizationTable.FromJson(@"{ ""a"": { ""zh"": ""移动"", ""en"": ""Move"" }, ""b"": { ""en"": ""Only English"" } }");
            Assert.AreEqual("移动", table.Get("a", LocalizationTable.Chinese));
            Assert.AreEqual("Only English", table.Get("b", LocalizationTable.Chinese));
            Assert.AreEqual("missing.key", table.Get("missing.key", LocalizationTable.English));
        }
    }
}
