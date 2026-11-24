using System.Collections.Generic;
using System.Linq;
using CloseFight.Core.AI;
using CloseFight.Core.Battle;
using CloseFight.Core.Map;
using CloseFight.Core.Units;
using NUnit.Framework;
using Unity.Mathematics;

namespace CloseFight.Tests
{
    /// <summary>Off-map support (M13, D36).</summary>
    public class SupportTests
    {
        /// <summary>300 × 300 m with a 3 m wall along y = 150 from x = 0 to 200.</summary>
        static BattleState Battle(ulong seed = 9)
        {
            var map = TestMaps.Empty(300f);
            map.walls.Add(new WallDefinition { points = new List<MapPoint> { new MapPoint(0, 150), new MapPoint(200, 150) }, height = 3f, blocksMovement = true });
            var battle = new BattleState(MapBuilder.Build(map), seed);
            battle.Victory.TimeLimitSeconds = 1e6f;
            battle.Support.Configure(Side.German, 12, new[] { "lefh18", "sfh18", "stuka" });
            battle.Support.Configure(Side.Soviet, 12, new[] { "ml20", "katyusha", "po2" });
            return battle;
        }

        static void Disarm(Team team)
        {
            foreach (var m in team.Members) m.Ammo = 0;
        }

        static void Run(BattleState battle, float seconds)
        {
            for (int i = 0; i < seconds * 10f; i++) battle.Step();
        }

        [Test]
        public void AnOfficerMustSeeTheTarget()
        {
            var battle = Battle();
            battle.AddTeam("hq", Side.German, TeamType.Headquarters, "", new float2(230, 100), 0f, 3, 50);

            Assert.IsNull(battle.Support.Request(Side.German, "lefh18", new float2(100, 200), out var refusal), "behind the wall");
            Assert.AreEqual(SupportRefusal.NoObserver, refusal);
            Assert.AreEqual(12, battle.Support.Points(Side.German));

            var mission = battle.Support.Request(Side.German, "lefh18", new float2(250, 220), out refusal);
            Assert.IsNotNull(mission, "past the end of the wall, in sight");
            Assert.AreEqual(9, battle.Support.Points(Side.German));
            Assert.AreEqual(battle.Teams[0], mission.Observer.Team, "an officer of the headquarters directs it");
        }

        [Test]
        public void RiflemenCannotCallItOnlyOfficers()
        {
            var battle = Battle();
            battle.AddTeam("r", Side.German, TeamType.Rifle, "", new float2(250, 100), 0f, 6, 50);
            Assert.IsNull(battle.Support.Request(Side.German, "lefh18", new float2(250, 220), out var refusal));
            Assert.AreEqual(SupportRefusal.NoObserver, refusal);
        }

        [Test]
        public void PointsRunOut()
        {
            var battle = Battle();
            battle.AddTeam("hq", Side.German, TeamType.Headquarters, "", new float2(250, 100), 0f, 3, 50);
            Assert.IsNotNull(battle.Support.Request(Side.German, "sfh18", new float2(250, 220), out _));
            Assert.IsNotNull(battle.Support.Request(Side.German, "sfh18", new float2(260, 220), out _));
            Assert.IsNull(battle.Support.Request(Side.German, "sfh18", new float2(270, 220), out var refusal));
            Assert.AreEqual(SupportRefusal.NotEnoughPoints, refusal);
            Assert.AreEqual(2, battle.Support.Points(Side.German));
        }

        [Test]
        public void RoundsComeAfterTheDelayAndScatter()
        {
            var battle = Battle();
            battle.AddTeam("hq", Side.German, TeamType.Headquarters, "", new float2(250, 100), 0f, 3, 50);
            var mission = battle.Support.Request(Side.German, "lefh18", new float2(250, 220), out _);
            float arrives = mission.ArrivesAt;
            Assert.GreaterOrEqual(arrives, SupportAssets.Lefh18.DelayMin);
            Assert.LessOrEqual(arrives, SupportAssets.Lefh18.DelayMax);

            var bursts = new List<float2>();
            while (battle.Clock.ElapsedSeconds < arrives + 40f)
            {
                battle.Step();
                foreach (var e in battle.Gunnery.RecentExplosions)
                {
                    if (!bursts.Contains(e.Position)) bursts.Add(e.Position);
                }

                if (battle.Clock.ElapsedSeconds < arrives - 0.2f)
                {
                    Assert.AreEqual(0, bursts.Count, "nothing lands before the delay is up");
                }
            }

            Assert.AreEqual(SupportAssets.Lefh18.Rounds, bursts.Count);
            Assert.AreEqual(0, battle.Support.Missions.Count);
            Assert.Greater(bursts.Max(b => math.distance(b, mission.AimPoint)), 3f, "the rounds scatter");
            Assert.IsTrue(bursts.All(b => math.distance(b, mission.AimPoint) <= SupportAssets.Lefh18.Spread + 0.01f));
        }

        [Test]
        public void ShellsHitOurOwnMenTooIfTheyAreTooClose()
        {
            var battle = Battle(seed: 4);
            battle.AddTeam("hq", Side.German, TeamType.Headquarters, "", new float2(250, 60), 0f, 3, 50);
            var ours = battle.AddTeam("de", Side.German, TeamType.Rifle, "", new float2(250, 220), 0f, 6, 50);
            Disarm(ours);
            var mission = battle.Support.Request(Side.German, "sfh18", new float2(250, 220), out _);
            Assert.IsNotNull(mission);
            float worst = 0f;
            for (int i = 0; i < (mission.ArrivesAt + 30f) * 10f; i++)
            {
                battle.Step();
                worst = math.max(worst, ours.AverageSuppression);
            }


            // The rounds scatter over ~30 m: men on the spot are at least pinned down, and often hit.
            Assert.IsTrue(ours.Members.Any(m => !m.IsActive || m.IsWounded) || worst > 10f,
                "German shells on German heads (D36: calling it close to one's own troops is dangerous)");
        }

        [Test]
        public void StukasFlyByDayAndNightBombersByNight()
        {
            var battle = Battle();
            battle.AddTeam("hq", Side.German, TeamType.Headquarters, "", new float2(250, 100), 0f, 3, 50);
            battle.AddTeam("shq", Side.Soviet, TeamType.Headquarters, "", new float2(250, 280), 180f, 3, 50);

            Assert.IsNull(battle.Support.Request(Side.Soviet, "po2", new float2(250, 200), out var refusal));
            Assert.AreEqual(SupportRefusal.WrongTimeOfDay, refusal);
            Assert.IsNotNull(battle.Support.Request(Side.German, "stuka", new float2(250, 200), out _));

            battle.IsNight = true;
            Assert.IsNull(battle.Support.Request(Side.German, "stuka", new float2(250, 210), out refusal));
            Assert.AreEqual(SupportRefusal.WrongTimeOfDay, refusal);
            Assert.IsNotNull(battle.Support.Request(Side.Soviet, "po2", new float2(250, 200), out _));
        }

        [Test]
        public void AttackingAiCallsArtilleryOnTheDefenders()
        {
            var battle = Battle();
            battle.SetRetreatEdge(Side.German, MapEdge.South);
            battle.SetRetreatEdge(Side.Soviet, MapEdge.North);
            battle.AddObjective(new ObjectiveDefinition { id = "o", position = new MapPoint(250, 250), radius = 12f, value = 2, owner = Side.Soviet });
            battle.AddTeam("de_hq", Side.German, TeamType.Headquarters, "", new float2(250, 40), 0f, 3, 50);
            battle.AddTeam("de_1", Side.German, TeamType.Rifle, "", new float2(240, 30), 0f, 6, 50);
            battle.AddTeam("de_2", Side.German, TeamType.Rifle, "", new float2(260, 30), 0f, 6, 50);
            battle.AddTeam("de_mg", Side.German, TeamType.MachineGun, "", new float2(250, 25), 0f, 3, 50);
            var defenders = battle.AddTeam("su", Side.Soviet, TeamType.Rifle, "", new float2(250, 250), 180f, 6, 50);
            Disarm(defenders);
            battle.IssueOrder(defenders, OrderType.Defend, defenders.Centroid);
            for (int i = 0; i < 3; i++)
            {
                var far = battle.AddTeam("su_far" + i, Side.Soviet, TeamType.Rifle, "", new float2(20 + i * 12, 296), 180f, 6, 50);
                Disarm(far);
                battle.IssueOrder(far, OrderType.Hide, far.Centroid);
            }

            var attack = new AttackCommander(battle, Side.German);
            battle.AddCommander(attack);
            for (int i = 0; i < 3000 && battle.Support.Points(Side.German) == 12; i++) battle.Step();
            Assert.Less(battle.Support.Points(Side.German), 12, "the attack calls in fire on the defenders it has found");
        }

        [Test]
        public void DefendingAiShellsTheEnemyWhereHeBunchesUp()
        {
            var battle = Battle();
            battle.SetRetreatEdge(Side.German, MapEdge.South);
            battle.SetRetreatEdge(Side.Soviet, MapEdge.North);
            battle.AddObjective(new ObjectiveDefinition { id = "o", position = new MapPoint(250, 280), radius = 12f, value = 2, owner = Side.Soviet });
            battle.AddTeam("su_hq", Side.Soviet, TeamType.Headquarters, "", new float2(250, 285), 180f, 3, 50);
            battle.AddTeam("su_1", Side.Soviet, TeamType.Rifle, "", new float2(240, 280), 180f, 6, 50);
            for (int i = 0; i < 2; i++)
            {
                var crowd = battle.AddTeam("de_" + i, Side.German, TeamType.Rifle, "", new float2(245 + i * 10, 160), 0f, 6, 50);
                Disarm(crowd);
                foreach (var m in crowd.Members) m.Posture = Posture.Standing;
            }

            battle.AddCommander(new DefenseCommander(battle, Side.Soviet));
            battle.Visibility.Recompute();
            Run(battle, 20f);
            Assert.Less(battle.Support.Points(Side.Soviet), 12, "twelve men bunched up in the open draw the guns across the Volga");
        }
    }
}
