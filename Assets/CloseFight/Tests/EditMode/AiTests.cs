using System.Collections.Generic;
using System.Linq;
using CloseFight.Core.AI;
using CloseFight.Core.Battle;
using CloseFight.Core.Map;
using CloseFight.Core.Path;
using CloseFight.Core.Units;
using NUnit.Framework;
using Unity.Mathematics;

namespace CloseFight.Tests
{
    public class AiTests
    {
        /// <summary>
        /// 160 x 160 m. Germans attack from the south over an open field with a rubble
        /// patch; the Soviets hold a row of brick houses at y = 96..110. The middle
        /// house (3 storeys) is the objective.
        /// </summary>
        static MapDefinition DefenseMap()
        {
            var map = TestMaps.Empty(160f);
            map.areas.Add(new AreaDefinition { terrain = TerrainType.Rubble, polygon = TestMaps.Rect(50, 55, 65, 62) });
            map.buildings.Add(new BuildingDefinition { id = 1, floors = 2, polygon = TestMaps.Rect(25, 96, 55, 110) });
            map.buildings.Add(new BuildingDefinition { id = 2, floors = 3, polygon = TestMaps.Rect(70, 96, 92, 110) });
            map.buildings.Add(new BuildingDefinition { id = 3, floors = 2, polygon = TestMaps.Rect(110, 96, 140, 110) });
            return map;
        }

        static BattleState DefenseBattle(MapDefinition map = null, ulong seed = 21)
        {
            var battle = new BattleState(MapBuilder.Build(map ?? DefenseMap()), seed);
            battle.SetRetreatEdge(Side.German, MapEdge.South);
            battle.SetRetreatEdge(Side.Soviet, MapEdge.North);
            battle.AddObjective(new ObjectiveDefinition { id = "house", position = new MapPoint(81, 103), radius = 12f, value = 3, owner = Side.Soviet });
            return battle;
        }

        static DefenseCommander SovietDefense(BattleState battle, bool withMortar = true)
        {
            battle.AddTeam("su_mg", Side.Soviet, TeamType.MachineGun, "", new float2(80, 140), 180f, 3, 50);
            battle.AddTeam("su_1", Side.Soviet, TeamType.Rifle, "", new float2(60, 145), 180f, 6, 50);
            battle.AddTeam("su_2", Side.Soviet, TeamType.Rifle, "", new float2(100, 145), 180f, 6, 50);
            if (withMortar)
            {
                battle.AddTeam("su_mortar", Side.Soviet, TeamType.Mortar, "", new float2(80, 150), 180f, 0, 50, "bm37");
            }

            var commander = new DefenseCommander(battle, Side.Soviet);
            battle.AddCommander(commander);
            return commander;
        }

        static void Run(BattleState battle, float seconds)
        {
            int ticks = (int)(seconds / 0.1f);
            for (int i = 0; i < ticks; i++)
            {
                battle.Step();
            }
        }

        static PositionAnalysis.Candidate Nearest(PositionAnalysis analysis, float2 point)
        {
            return analysis.Candidates.OrderBy(c => math.distancesq(c.Position, point)).First();
        }

        // ------------------------------------------------------------ analysis

        [Test]
        public void MachineGunPrefersAHighCoveredSpotOverlookingTheApproach()
        {
            var battle = DefenseBattle();
            var analysis = new PositionAnalysis(battle.Map, MapEdge.South, battle.Objectives);

            var upstairs = Nearest(analysis, new float2(81, 98));   // in the 3-storey house, south side
            var behind = Nearest(analysis, new float2(81, 140));    // in the open, north of the houses

            Assert.IsTrue(upstairs.InBuilding);
            Assert.Greater(upstairs.Coverage, behind.Coverage, "the house looks out over the field");
            Assert.Greater(DefenseCommander.RoleScore(DefenseRole.MachineGun, upstairs),
                DefenseCommander.RoleScore(DefenseRole.MachineGun, behind));
        }

        [Test]
        public void CrossfireRewardsCoveringTheSameGroundFromDifferentAngles()
        {
            var battle = DefenseBattle();
            var analysis = new PositionAnalysis(battle.Map, MapEdge.South, battle.Objectives);
            var west = Nearest(analysis, new float2(40, 98));
            var nextDoor = Nearest(analysis, new float2(46, 98));
            var east = Nearest(analysis, new float2(125, 98));

            var placed = new List<PositionAnalysis.Candidate> { west };
            Assert.Greater(analysis.Crossfire(east, placed), analysis.Crossfire(nextDoor, placed),
                "the far house crosses fire with the west house; the room next door does not");
        }

        // ------------------------------------------------------------ planning and execution

        [Test]
        public void DefendersSpreadOutIntoPositionsAndWaitInAmbush()
        {
            var battle = DefenseBattle();
            var commander = SovietDefense(battle);
            Run(battle, 90f);

            var plans = commander.Plans;
            Assert.AreEqual(4, plans.Count);
            Assert.AreEqual(1, plans.Count(p => p.Role == DefenseRole.Reserve), "one rifle team held back");

            for (int i = 0; i < plans.Count; i++)
            {
                for (int j = i + 1; j < plans.Count; j++)
                {
                    Assert.GreaterOrEqual(math.distance(plans[i].Primary, plans[j].Primary), 11.9f, "positions are spread out");
                }
            }

            foreach (var plan in plans)
            {
                Assert.AreEqual(PlanState.Holding, plan.State, $"{plan.Team.Key} reached its position");
                Assert.Less(math.distance(plan.Team.Centroid, plan.Primary), 8f, $"{plan.Team.Key} is where it was sent");
            }

            var mg = plans.First(p => p.Role == DefenseRole.MachineGun);
            Assert.AreEqual(OrderType.Ambush, mg.Team.Order.Type, "waiting in ambush while nobody is in sight");
            Assert.Less(mg.Primary.y, 115f, "the MG goes forward into the house row");
        }

        [Test]
        public void MortarIsPlacedOutOfSightOfTheApproach()
        {
            var battle = DefenseBattle();
            var commander = SovietDefense(battle);
            battle.Step();
            Run(battle, 2f);

            var mortar = commander.Plans.First(p => p.Role == DefenseRole.Mortar);
            var spot = Nearest(commander.Analysis, mortar.Primary);
            Assert.Less(spot.Exposure, 0.05f);
            Assert.GreaterOrEqual(mortar.Primary.y, 110f, "behind the houses (right behind the north wall is fine)");
        }

        [Test]
        public void EveryTeamGetsAFallbackFurtherBack()
        {
            var battle = DefenseBattle();
            var commander = SovietDefense(battle);
            Run(battle, 2.1f);

            foreach (var plan in commander.Plans.Where(p => p.HasFallback))
            {
                Assert.Greater(plan.Fallback.y, plan.Primary.y + 15f, $"{plan.Team.Key}: fallback lies behind the primary");
            }

            Assert.GreaterOrEqual(commander.Plans.Count(p => p.HasFallback), 3);
        }

        [Test]
        public void AiDoesNotCheat()
        {
            // Two identical battles; only a hidden German team differs in position.
            // Nothing the Soviets cannot see may change what they do (D17).
            float2[] hiddenAt = { new float2(20, 10), new float2(140, 15) };
            var results = new List<List<float2>>();

            foreach (var spot in hiddenAt)
            {
                var battle = DefenseBattle(seed: 77);
                battle.AddTeam("de_hidden", Side.German, TeamType.MachineGun, "", spot, 0f, 3, 50);
                battle.IssueOrder(battle.Teams[0], OrderType.Hide, float2.zero);
                var commander = SovietDefense(battle);
                Run(battle, 60f);

                Assert.AreEqual(0, battle.Visibility.VisibleEnemyCount(Side.Soviet), "precondition: the Germans stay hidden");
                var outcome = new List<float2>();
                foreach (var plan in commander.Plans)
                {
                    outcome.Add(plan.Primary);
                    outcome.Add(plan.Fallback);
                    outcome.AddRange(plan.Team.Members.Select(m => m.Position));
                }

                results.Add(outcome);
            }

            Assert.AreEqual(results[0].Count, results[1].Count);
            for (int i = 0; i < results[0].Count; i++)
            {
                Assert.AreEqual(results[0][i], results[1][i], $"decision {i} depended on something the AI could not see");
            }
        }

        [Test]
        public void ElasticTeamFallsBackWhenFlanked()
        {
            var battle = DefenseBattle();
            var commander = SovietDefense(battle, withMortar: false);
            Run(battle, 90f);

            var rifle = commander.Plans.First(p => p.Role == DefenseRole.Rifle);
            Assert.AreEqual(DefensePosture.Elastic, rifle.Posture);
            Assert.IsTrue(rifle.HasFallback);

            // Germans appear behind its flank (north-east of it, inside the Soviet rear).
            var flankers = battle.AddTeam("de_flank", Side.German, TeamType.Rifle, "", rifle.Team.Centroid + new float2(40, 50), 225f, 6, 50);
            foreach (var german in flankers.Members)
            {
                german.Ammo = 0;
            }

            for (int i = 0; i < 100 && rifle.State == PlanState.Holding; i++)
            {
                battle.Step();
            }

            Assert.That(rifle.State, Is.EqualTo(PlanState.Withdrawing).Or.EqualTo(PlanState.HoldingFallback));
        }

        [Test]
        public void MortarShellsGunfireItCanHearButNotSee()
        {
            var map = DefenseMap();
            // A long wall across the field hides the Germans from the houses.
            map.walls.Add(new WallDefinition { points = { new MapPoint(0, 50), new MapPoint(160, 50) }, height = 8f });
            var battle = DefenseBattle(map);
            var commander = SovietDefense(battle);
            Run(battle, 90f);
            var mortar = commander.Plans.First(p => p.Role == DefenseRole.Mortar);
            Assert.AreEqual(PlanState.Holding, mortar.State);

            var hidden = battle.AddTeam("de", Side.German, TeamType.MachineGun, "", new float2(80, 30), 0f, 3, 50);
            battle.Visibility.Recompute();
            Assert.AreEqual(0, battle.Visibility.VisibleEnemyCount(Side.Soviet));
            battle.Visibility.ReportSound(hidden.Members[0], hidden.Members[0].Position, 800f, 10f);

            Run(battle, 2.1f);
            Assert.AreEqual(OrderType.AreaFire, mortar.Team.Order.Type);
            Assert.Less(math.distance(mortar.Team.Order.Target, new float2(80, 30)), 15f);
        }

        [Test]
        public void DangerMapRoutesAroundGroundTheEnemyCovers()
        {
            var map = TestMaps.Empty(100f);
            // A wall gives a covered lane along the west side.
            map.walls.Add(new WallDefinition { points = { new MapPoint(20, 10), new MapPoint(20, 90) }, height = 2.5f });
            var tactical = MapBuilder.Build(map);
            var danger = new DangerMap(tactical);
            danger.Rebuild(new[] { (new float2(60f, 50f), 3f) }); // an enemy MG east of the wall

            var finder = new GridPathfinder(tactical);
            var plain = new List<GridCoord>();
            var careful = new List<GridCoord>();
            finder.FindPath(new GridCoord(25, 5), new GridCoord(25, 95), MovementClass.Infantry, plain);
            finder.ExtraCost = danger.PathCost(6f);
            finder.FindPath(new GridCoord(25, 5), new GridCoord(25, 95), MovementClass.Infantry, careful);

            float Exposure(List<GridCoord> path) => path.Sum(c => danger.At(tactical.Grid.CellCenter(c)));
            Assert.Less(Exposure(careful), Exposure(plain) * 0.5f, "the careful route hugs the far side of the wall");
        }

        [Test]
        public void ReserveCounterattacksAnObjectiveTheEnemyHoldsWeakly()
        {
            var battle = DefenseBattle();
            battle.AddObjective(new ObjectiveDefinition { id = "crossroads", position = new MapPoint(150, 70), radius = 8f, value = 1, owner = Side.Soviet });
            var commander = SovietDefense(battle, withMortar: false);
            Run(battle, 90f);
            var reserve = commander.Plans.First(p => p.Role == DefenseRole.Reserve);
            Assume.That(battle.Teams.Where(t => t.Side == Side.Soviet).All(t => math.distance(t.Centroid, new float2(150, 70)) > 8f),
                "precondition: nobody already sits on the crossroads");

            var lone = battle.AddTeam("de_lone", Side.German, TeamType.MachineGun, "", new float2(150, 70), 0f, 1, 50);
            lone.Members[0].Ammo = 0;
            for (int i = 0; i < 60 && reserve.State == PlanState.Holding; i++)
            {
                battle.Step();
            }

            Assert.AreEqual(PlanState.Counterattacking, reserve.State, "one visible enemy on an objective: the reserve goes in");
        }
    }
}
