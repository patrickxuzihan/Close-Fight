using System.Linq;
using CloseFight.Core.Battle;
using CloseFight.Core.Map;
using CloseFight.Core.Units;
using NUnit.Framework;
using Unity.Mathematics;

namespace CloseFight.Tests
{
    public class VictoryTests
    {
        static BattleState Battle()
        {
            var battle = new BattleState(MapBuilder.Build(TestMaps.Empty(300f)), 13);
            battle.AddObjective(new ObjectiveDefinition { id = "a", position = new MapPoint(150, 150), radius = 12f, value = 2, owner = Side.Soviet });
            battle.AddObjective(new ObjectiveDefinition { id = "b", position = new MapPoint(250, 250), radius = 10f, value = 1, owner = Side.Soviet });
            return battle;
        }

        static void Disarm(Team team)
        {
            foreach (var m in team.Members) m.Ammo = 0;
        }

        [Test]
        public void ObjectiveChangesHandsOnlyWhenOneSideHoldsIt()
        {
            var battle = Battle();
            var de = battle.AddTeam("de", Side.German, TeamType.MachineGun, "", new float2(150, 150), 0f, 3, 50);
            Disarm(de);
            battle.Victory.UpdateControl();
            Assert.AreEqual(Side.German, battle.Objectives[0].Owner);
            Assert.AreEqual(3, battle.Victory.Points(Side.German) + battle.Victory.Points(Side.Soviet));

            var su = battle.AddTeam("su", Side.Soviet, TeamType.MachineGun, "", new float2(150, 155), 180f, 3, 50);
            Disarm(su);
            battle.Victory.UpdateControl();
            Assert.IsTrue(battle.Objectives[0].Contested);
            Assert.AreEqual(Side.German, battle.Objectives[0].Owner, "contested: it stays with the holder");
        }

        [Test]
        public void ForceMoraleFallsWithLossesAndBreaksTheSide()
        {
            var battle = Battle();
            var de = battle.AddTeam("de", Side.German, TeamType.Rifle, "", new float2(50, 50), 0f, 6, 50);
            var su = battle.AddTeam("su", Side.Soviet, TeamType.Rifle, "", new float2(150, 150), 180f, 6, 50);
            Disarm(de);
            Disarm(su);
            battle.Step();
            Assert.AreEqual(100f, battle.Victory.ForceMorale(Side.German), 1e-3f);

            for (int i = 0; i < 4; i++) su.Members[i].Status = SoldierStatus.Dead;
            Assert.Less(battle.Victory.ForceMorale(Side.Soviet), 40f);
            Assert.IsFalse(battle.Victory.IsOver);

            su.Members[4].Status = SoldierStatus.Fled;
            battle.Step();
            Assert.IsTrue(battle.Victory.IsOver);
            Assert.AreEqual(BattleResult.GermanVictory, battle.Victory.Result);
            Assert.AreEqual("result.reason.soviet_morale", battle.Victory.EndReasonKey);
        }

        [Test]
        public void LosingObjectivesCostsForceMorale()
        {
            var battle = Battle();
            battle.AddTeam("de", Side.German, TeamType.Rifle, "", new float2(50, 50), 0f, 6, 50);
            battle.AddTeam("su", Side.Soviet, TeamType.Rifle, "", new float2(20, 280), 180f, 6, 50);
            battle.Step();
            float before = battle.Victory.ForceMorale(Side.Soviet);

            battle.Teams[0].Members.ForEach(m => m.Ammo = 0);
            var raiders = battle.AddTeam("raid", Side.German, TeamType.MachineGun, "", new float2(150, 150), 0f, 3, 50);
            Disarm(raiders);
            battle.Victory.UpdateControl();
            Assert.Less(battle.Victory.ForceMorale(Side.Soviet), before - 9f, "losing a 2-point objective costs 10");
        }

        [Test]
        public void TimeLimitEndsTheBattleOnPoints()
        {
            var battle = Battle();
            battle.Victory.TimeLimitSeconds = 3f;
            var de = battle.AddTeam("de", Side.German, TeamType.MachineGun, "", new float2(50, 50), 0f, 3, 50);
            var su = battle.AddTeam("su", Side.Soviet, TeamType.MachineGun, "", new float2(250, 250), 180f, 3, 50);
            Disarm(de);
            Disarm(su);

            for (int i = 0; i < 40; i++) battle.Step();
            Assert.IsTrue(battle.Victory.IsOver);
            Assert.AreEqual(BattleResult.SovietVictory, battle.Victory.Result, "the Soviets still hold both objectives");
            Assert.AreEqual("result.reason.time", battle.Victory.EndReasonKey);
        }

        [Test]
        public void EqualPointsLeaveTheSectorContested()
        {
            var battle = new BattleState(MapBuilder.Build(TestMaps.Empty(300f)), 13);
            battle.AddObjective(new ObjectiveDefinition { id = "a", position = new MapPoint(50, 50), radius = 10f, value = 1, owner = Side.German });
            battle.AddObjective(new ObjectiveDefinition { id = "b", position = new MapPoint(250, 250), radius = 10f, value = 1, owner = Side.Soviet });
            battle.Victory.RequestCeasefire(Side.German);
            Assert.AreEqual(BattleResult.Contested, battle.Victory.Result);
        }

        [Test]
        public void CeasefireEndsTheBattleAndFreezesIt()
        {
            var battle = Battle();
            var de = battle.AddTeam("de", Side.German, TeamType.Rifle, "", new float2(50, 50), 0f, 6, 50);
            battle.IssueOrder(de, OrderType.Move, new float2(80, 50));
            battle.Step();
            battle.Victory.RequestCeasefire(Side.German);

            Assert.IsTrue(battle.Victory.IsOver);
            Assert.IsTrue(battle.Clock.Paused);
            var at = de.Members[0].Position;
            long ticks = battle.Clock.TickCount;
            battle.Step();
            Assert.AreEqual(at, de.Members[0].Position, "nothing moves after the end");
            Assert.AreEqual(ticks, battle.Clock.TickCount);
        }

        [Test]
        public void KillsAreCreditedToTheShooter()
        {
            var battle = Battle();
            battle.Victory.TimeLimitSeconds = 1e6f;
            var de = battle.AddTeam("de", Side.German, TeamType.MachineGun, "", new float2(150, 60), 0f, 3, 70);
            var su = battle.AddTeam("su", Side.Soviet, TeamType.Rifle, "", new float2(150, 160), 180f, 6, 30);
            Disarm(su);
            battle.Visibility.Recompute();

            for (int i = 0; i < 1200 && !battle.Victory.IsOver && su.Members.All(m => m.Status != SoldierStatus.Dead && m.Status != SoldierStatus.Incapacitated); i++)
            {
                battle.Step();
            }

            int downed = su.Members.Count(m => m.Status == SoldierStatus.Dead || m.Status == SoldierStatus.Incapacitated);
            Assert.Greater(downed, 0, "precondition: the MG team hit someone");
            Assert.AreEqual(downed, de.Members.Sum(m => m.Kills), "every man down is somebody's kill");
        }
    }
}
