using System.Linq;
using CloseFight.Core.Battle;
using CloseFight.Core.Map;
using CloseFight.Core.Units;
using NUnit.Framework;
using Unity.Mathematics;

namespace CloseFight.Tests
{
    public class PsychologyTests
    {
        static BattleState Battle(float size = 200f) => new BattleState(MapBuilder.Build(TestMaps.Empty(size)), 5);

        static void Run(BattleState battle, float seconds)
        {
            int ticks = (int)(seconds / 0.1f);
            for (int i = 0; i < ticks; i++)
            {
                battle.Step();
            }
        }

        static void SetMorale(Team team, float morale)
        {
            foreach (var soldier in team.Members)
            {
                soldier.Morale = morale;
                soldier.BaseMorale = morale;
            }
        }

        [Test]
        public void SoldiersStartWithTheirMoraleAttribute()
        {
            var battle = Battle();
            var team = battle.AddTeam("t", Side.German, TeamType.Rifle, "", new float2(100, 100), 0f, 6, 50);
            foreach (var soldier in team.Members)
            {
                Assert.AreEqual(soldier.Attributes.Morale, soldier.Morale, 1e-3f);
                Assert.AreEqual(MoraleState.Steady, soldier.MoraleState);
            }
        }

        [Test]
        public void SuppressionDrivesStateFromSuppressedToCowering()
        {
            var battle = Battle();
            var team = battle.AddTeam("t", Side.German, TeamType.MachineGun, "", new float2(100, 100), 0f, 3, 50);
            SetMorale(team, 70f);
            var a = team.Members[0];
            var b = team.Members[1];
            var c = team.Members[2];
            a.Suppression = 40f;
            b.Suppression = 75f;
            c.Suppression = 95f;
            foreach (var s in team.Members)
            {
                s.LastUnderFireAt = 0f;
            }

            battle.Step();
            Assert.AreEqual(MoraleState.Suppressed, a.MoraleState);
            Assert.AreEqual(MoraleState.Pinned, b.MoraleState);
            Assert.AreEqual(MoraleState.Cowering, c.MoraleState);
            Assert.IsTrue(c.IsOutOfControl);
            Assert.AreEqual(Posture.Prone, c.Posture);
        }

        [Test]
        public void ComradeFallingCostsMoraleAndLeaderLossCostsMore()
        {
            var battle = Battle();
            var team = battle.AddTeam("t", Side.German, TeamType.Rifle, "", new float2(100, 100), 0f, 6, 50);
            SetMorale(team, 70f);

            battle.Psychology.OnCasualty(team.Members[3], SoldierStatus.Dead, 0f);
            float afterPrivate = team.Members[1].Morale;
            Assert.Less(afterPrivate, 70f);

            battle.Psychology.OnCasualty(team.Members[0], SoldierStatus.Dead, 1f);
            Assert.Less(team.Members[1].Morale, afterPrivate - 15f, "losing the leader hurts most");
            Assert.IsTrue(team.LeaderLost);
        }

        [Test]
        public void LowMoraleUnderFirePanicsAndBreaksAwayFromTheTeam()
        {
            var battle = Battle();
            var team = battle.AddTeam("t", Side.German, TeamType.MachineGun, "", new float2(100, 100), 0f, 3, 50);
            SetMorale(team, 20f);
            var victim = team.Members[1];
            victim.Suppression = 60f;
            victim.LastUnderFireAt = 0f;

            battle.Step();
            Assert.AreEqual(MoraleState.Panicked, victim.MoraleState);
            Assert.IsTrue(victim.IsOutOfControl);

            var start = victim.Position;
            Run(battle, 1.5f);
            Assert.Greater(math.distance(start, victim.Position), 1f, "he runs for cover");
        }

        [Test]
        public void CollapsedMoraleRoutsToOwnEdgeAndLeavesTheBattle()
        {
            var battle = Battle(100f);
            battle.SetRetreatEdge(Side.Soviet, MapEdge.North);
            var team = battle.AddTeam("t", Side.Soviet, TeamType.MachineGun, "", new float2(50, 80), 180f, 3, 50);
            var runner = team.Members[2];
            runner.Morale = 5f;
            runner.BaseMorale = 5f;

            battle.Step();
            Assert.AreEqual(MoraleState.Routed, runner.MoraleState);

            for (int i = 0; i < 300 && runner.Status != SoldierStatus.Fled; i++)
            {
                battle.Step();
            }

            Assert.AreEqual(SoldierStatus.Fled, runner.Status, "ran off the north edge");
            Assert.IsFalse(runner.IsActive);
            Assert.Greater(runner.Position.y, 95f);
        }

        [Test]
        public void TeamDoesNotWaitForPanickedMembers()
        {
            var battle = Battle();
            var team = battle.AddTeam("t", Side.German, TeamType.Rifle, "", new float2(100, 50), 0f, 6, 50);
            SetMorale(team, 70f);
            var panicked = team.Members[5];
            panicked.Morale = 20f;
            panicked.BaseMorale = 20f;
            panicked.Suppression = 60f;
            panicked.LastUnderFireAt = 0f;

            Assert.IsTrue(battle.IssueOrder(team, OrderType.Move, new float2(100, 90)));
            for (int i = 0; i < 900 && team.Activity != TeamActivity.Idle; i++)
            {
                battle.Step();
            }

            Assert.AreEqual(TeamActivity.Idle, team.Activity, "the team arrives without him");
        }

        [Test]
        public void HalfCasualtiesShakeTheTeamTwoThirdsBreakIt()
        {
            var battle = Battle();
            var team = battle.AddTeam("t", Side.German, TeamType.Rifle, "", new float2(100, 100), 0f, 6, 50);
            SetMorale(team, 80f);

            team.Members[3].Status = SoldierStatus.Dead;
            team.Members[4].Status = SoldierStatus.Dead;
            team.Members[5].Status = SoldierStatus.Incapacitated;
            battle.Step();
            Assert.IsTrue(team.IsShaken);
            Assert.IsFalse(team.IsBroken);

            team.Members[2].Status = SoldierStatus.Dead;
            battle.Step();
            Assert.IsTrue(team.IsBroken);
        }

        [Test]
        public void BrokenTeamFallsBackOnItsOwnAndFormsANewLine()
        {
            var battle = Battle();
            var team = battle.AddTeam("t", Side.German, TeamType.Rifle, "", new float2(100, 120), 0f, 6, 50);
            SetMorale(team, 80f);
            for (int i = 2; i < 6; i++)
            {
                team.Members[i].Status = SoldierStatus.Dead;
            }

            var start = team.Centroid;
            battle.Step();
            Assert.AreEqual(TeamActivity.Moving, team.Activity, "falls back by itself");

            for (int i = 0; i < 900 && team.Activity != TeamActivity.Idle; i++)
            {
                battle.Step();
            }

            battle.Step();
            Assert.Less(team.Centroid.y, start.y - 30f, "pulled back toward the German (south) edge");
            Assert.AreEqual(OrderType.Defend, team.Order.Type, "and dug in on a new line");
        }

        [Test]
        public void BrokenTeamRefusesEverythingButRetreatAndSaysWhy()
        {
            var battle = Battle();
            var team = battle.AddTeam("t", Side.German, TeamType.Rifle, "", new float2(100, 120), 0f, 6, 50);
            for (int i = 2; i < 6; i++)
            {
                team.Members[i].Status = SoldierStatus.Dead;
            }

            battle.Step();
            Assert.IsFalse(battle.TryIssueOrder(team, OrderType.Move, new float2(100, 160), out var refusal));
            Assert.AreEqual(OrderRefusal.Broken, refusal);
            Assert.AreEqual(OrderRefusal.Broken, team.LastRefusal);
            Assert.IsTrue(battle.TryIssueOrder(team, OrderType.Retreat, float2.zero, out refusal));
            Assert.AreEqual(OrderRefusal.None, refusal);
        }

        [Test]
        public void ShakenTeamMayRefuseToAdvanceButAlwaysAcceptsPullingBack()
        {
            var battle = Battle();
            var team = battle.AddTeam("t", Side.German, TeamType.Rifle, "", new float2(100, 60), 0f, 6, 50);
            battle.AddTeam("su", Side.Soviet, TeamType.Rifle, "", new float2(100, 160), 180f, 6, 50);
            SetMorale(team, 80f);
            team.Members[3].Status = SoldierStatus.Dead;
            team.Members[4].Status = SoldierStatus.Dead;
            team.Members[5].Status = SoldierStatus.Dead;
            battle.Visibility.Recompute();
            battle.Step();
            Assert.IsTrue(team.IsShaken);

            int refused = 0;
            for (int i = 0; i < 20; i++)
            {
                if (!battle.TryIssueOrder(team, OrderType.Move, new float2(100, 120), out var refusal))
                {
                    Assert.AreEqual(OrderRefusal.Shaken, refusal);
                    refused++;
                }
            }

            Assert.Greater(refused, 0, "a shaken team balks at advancing toward the enemy");
            Assert.Less(refused, 20, "but not every time");
            Assert.IsTrue(battle.TryIssueOrder(team, OrderType.Move, new float2(100, 20), out _), "moving away is fine");
        }

        [Test]
        public void MoraleRecoversWhenCalmNearALeader()
        {
            var battle = Battle();
            var team = battle.AddTeam("t", Side.German, TeamType.Rifle, "", new float2(100, 100), 0f, 6, 50);
            SetMorale(team, 70f);
            var soldier = team.Members[2];
            soldier.Morale = 40f;

            Run(battle, 20f);
            Assert.Greater(soldier.Morale, 45f);
            Assert.LessOrEqual(soldier.Morale, soldier.BaseMorale);
        }

        [Test]
        public void CoweringSoldiersDoNotFire()
        {
            var battle = Battle(300f);
            var de = battle.AddTeam("de", Side.German, TeamType.MachineGun, "", new float2(150, 100), 0f, 3, 50);
            battle.AddTeam("su", Side.Soviet, TeamType.MachineGun, "", new float2(150, 180), 180f, 3, 50);
            foreach (var soviet in battle.Teams[1].Members)
            {
                soviet.Ammo = 0;
            }

            SetMorale(de, 30f);
            foreach (var german in de.Members)
            {
                german.Suppression = 100f;
                german.LastUnderFireAt = 0f;
            }

            battle.Visibility.Recompute();
            int ammo = de.Members.Sum(m => m.Ammo);
            Run(battle, 1.5f);
            Assert.AreEqual(ammo, de.Members.Sum(m => m.Ammo));
            Assert.IsTrue(de.Members.All(m => m.MoraleState == MoraleState.Cowering));
        }
    }
}
