using System.Linq;
using CloseFight.Core.Battle;
using CloseFight.Core.Map;
using CloseFight.Core.Sim;
using CloseFight.Core.Units;
using NUnit.Framework;
using Unity.Mathematics;

namespace CloseFight.Tests
{
    /// <summary>Teams handed to the AI (M15: D14, D22, D53) and firing first (D54).</summary>
    public class DelegationTests
    {
        /// <summary>400 x 400 m, open ground; optionally a 2-storey house at (108..118, 95..105).</summary>
        static BattleState Battle(bool house = false, ulong seed = 15)
        {
            var map = TestMaps.Empty(400f);
            if (house)
            {
                map.buildings.Add(new BuildingDefinition
                {
                    id = 1,
                    material = BuildingMaterial.Brick,
                    floors = 2,
                    polygon = TestMaps.Rect(108f, 95f, 118f, 105f),
                });
            }

            var battle = new BattleState(MapBuilder.Build(map), seed);
            battle.Victory.TimeLimitSeconds = 1e6f;
            return battle;
        }

        static void Disarm(Team team)
        {
            foreach (var m in team.Members) m.Ammo = 0;
        }

        static void Steps(BattleState battle, float seconds)
        {
            int ticks = (int)(seconds / SimClock.TickSeconds);
            for (int i = 0; i < ticks; i++) battle.Step();
        }

        /// <summary>Half the team out of action: shaken, not broken.</summary>
        static void LoseHalf(Team team)
        {
            for (int i = team.Members.Count / 2; i < team.Members.Count; i++)
            {
                team.Members[i].Status = SoldierStatus.Dead;
            }
        }

        static bool InHouse(BattleState battle, float2 p)
        {
            var grid = battle.Map.Grid;
            return battle.Map.BuildingAt(grid.IndexOf(grid.WorldToCell(p))) != null;
        }

        [Test]
        public void AHandedOverTeamTakesTheCoverNearbyAndWaitsInAmbush()
        {
            var battle = Battle(house: true);
            var team = battle.AddTeam("de", Side.German, TeamType.Rifle, "", new float2(100, 100), 0f, 6, 50);
            Assert.IsNotNull(battle.Delegate(team, Stance.Elastic));
            Steps(battle, 40f);

            Assert.AreEqual(DelegationState.Holding, team.Delegation.State);
            Assert.IsTrue(InHouse(battle, team.Centroid), $"it moved into the house 10 m away (at {team.Centroid})");
            Assert.AreEqual(OrderType.Ambush, team.Order.Type, "and waits in ambush");
            Assert.IsTrue(team.Delegation.HasFallback, "with a fallback position ready");
            Assert.Less(team.Delegation.Fallback.y, team.Centroid.y - 30f, "behind it, away from the enemy's side (north)");
        }

        [Test]
        public void ASecondHandOverOnlyChangesTheStance()
        {
            var battle = Battle();
            var team = battle.AddTeam("de", Side.German, TeamType.Rifle, "", new float2(100, 100), 0f, 6, 50);
            var first = battle.Delegate(team, Stance.Elastic);
            Steps(battle, 10f);
            var second = battle.Delegate(team, Stance.Hold);
            Assert.AreSame(first, second);
            Assert.AreEqual(Stance.Hold, team.Delegation.Stance);

            battle.Undelegate(team);
            Assert.IsNull(team.Delegation, "taken back under direct command");
        }

        [Test]
        public void AShakenElasticTeamAsksToPullBackAndGoesUnlessRefused()
        {
            var battle = Battle();
            var team = battle.AddTeam("de", Side.German, TeamType.Rifle, "", new float2(200, 200), 0f, 8, 50);
            battle.Delegate(team, Stance.Elastic);
            Steps(battle, 10f);
            float2 start = team.Centroid;

            LoseHalf(team);
            Steps(battle, 3f);
            Assert.IsTrue(team.IsShaken && !team.IsBroken, "precondition: shaken, not broken");
            Assert.AreEqual(DelegationState.RequestingWithdrawal, team.Delegation.State, "it asks to pull back");
            Assert.That(team.Delegation.SecondsToWithdraw(battle.Clock.ElapsedSeconds), Is.InRange(15f, 20f));

            Steps(battle, 10f);
            Assert.AreEqual(DelegationState.RequestingWithdrawal, team.Delegation.State, "it waits for an answer...");

            Steps(battle, 12f);
            Assert.AreNotEqual(DelegationState.RequestingWithdrawal, team.Delegation.State, "...and goes after 20 s (D53)");
            Steps(battle, 40f);
            Assert.Less(team.Centroid.y, start.y - 40f, "back toward its own side");
            Assert.AreEqual(1, team.Delegation.Withdrawals, "and forms a new line there");
        }

        [Test]
        public void ARefusedRequestHoldsOn()
        {
            var battle = Battle();
            var team = battle.AddTeam("de", Side.German, TeamType.Rifle, "", new float2(200, 200), 0f, 8, 50);
            battle.Delegate(team, Stance.Elastic);
            Steps(battle, 10f);
            float2 start = team.Centroid;
            LoseHalf(team);
            Steps(battle, 3f);
            Assert.IsTrue(team.Delegation.IsRequesting);

            Assert.IsTrue(battle.AnswerWithdrawal(team, false));
            Steps(battle, 40f);
            Assert.AreEqual(DelegationState.Holding, team.Delegation.State, "refused: it holds on, and does not ask again at once");
            Assert.Less(math.distance(team.Centroid, start), 5f);
        }

        [Test]
        public void AnApprovedRequestGoesAtOnce()
        {
            var battle = Battle();
            var team = battle.AddTeam("de", Side.German, TeamType.Rifle, "", new float2(200, 200), 0f, 8, 50);
            battle.Delegate(team, Stance.Elastic);
            Steps(battle, 10f);
            LoseHalf(team);
            Steps(battle, 3f);

            Assert.IsTrue(battle.AnswerWithdrawal(team, true));
            Assert.AreEqual(DelegationState.Withdrawing, team.Delegation.State);
            Assert.AreNotEqual(TeamActivity.Idle, team.Activity, "on its way back");
        }

        [Test]
        public void AHoldingTeamNeverAsks()
        {
            var battle = Battle();
            var team = battle.AddTeam("de", Side.German, TeamType.Rifle, "", new float2(200, 200), 0f, 8, 50);
            battle.Delegate(team, Stance.Hold);
            Steps(battle, 10f);
            LoseHalf(team);
            Steps(battle, 30f);
            Assert.AreEqual(DelegationState.Holding, team.Delegation.State);
        }

        [Test]
        public void ADelayingTeamFallsBackWhenTheEnemyComesClose()
        {
            var battle = Battle();
            var team = battle.AddTeam("de", Side.German, TeamType.Rifle, "", new float2(200, 150), 0f, 6, 50);
            battle.Delegate(team, Stance.Delay);
            Steps(battle, 10f);
            Assert.AreEqual(DelegationState.Holding, team.Delegation.State);
            Assert.AreEqual(OrderType.Defend, team.Order.Type, "a delaying team fires at will rather than waiting in ambush");

            var far = battle.AddTeam("su", Side.Soviet, TeamType.Rifle, "", new float2(200, 300), 180f, 12, 50);
            Disarm(far);
            foreach (var m in far.Members) m.Posture = Posture.Standing;
            Steps(battle, 5f);
            Assert.AreEqual(DelegationState.Holding, team.Delegation.State, "the enemy 150 m away: hold and shoot");

            // Its fire keeps an enemy walking up pinned, so let one appear close by.
            var near = battle.AddTeam("su2", Side.Soviet, TeamType.Rifle, "", new float2(230, 215), 180f, 6, 50);
            Disarm(near);
            foreach (var m in near.Members) m.Posture = Posture.Standing;
            Steps(battle, 3f);
            Assert.AreEqual(DelegationState.Withdrawing, team.Delegation.State, "the enemy within 80 m: fall back");
            Steps(battle, 40f);
            Assert.Less(team.Centroid.y, 120f, "to the prepared position behind");
            Assert.AreEqual(1, team.Delegation.Withdrawals, "where it forms a new line");
        }

        [Test]
        public void ACorneredTeamChargesTheEnemyThatBrokeIn()
        {
            var battle = Battle();
            var team = battle.AddTeam("de", Side.German, TeamType.Rifle, "", new float2(200, 200), 0f, 8, 50);
            battle.Delegate(team, Stance.Hold);
            Steps(battle, 10f);

            var enemy = battle.AddTeam("su", Side.Soviet, TeamType.Rifle, "", team.Centroid + new float2(0, 18), 180f, 4, 50);
            Disarm(enemy);
            foreach (var m in enemy.Members) m.Posture = Posture.Standing;
            Steps(battle, 3f);
            Assert.AreEqual(DelegationState.Counterattacking, team.Delegation.State, "holding at all costs, it charges the enemy 18 m away");
        }

        [Test]
        public void BrokenItFallsBackToThePreparedPosition()
        {
            var battle = Battle();
            var team = battle.AddTeam("de", Side.German, TeamType.Rifle, "", new float2(200, 200), 0f, 9, 50);
            battle.Delegate(team, Stance.Hold);
            Steps(battle, 10f);
            float2 fallback = team.Delegation.Fallback;
            Assert.AreEqual(fallback, battle.FallbackPoint(team, Psychology.FallbackDistance), "Psychology uses the prepared position");

            for (int i = 3; i < team.Members.Count; i++) team.Members[i].Status = SoldierStatus.Dead;
            Steps(battle, 60f);
            Assert.IsTrue(team.IsBroken);
            Assert.Less(math.distance(team.Centroid, fallback), 12f, "it fell back to where it prepared to");
        }

        // ------------------------------------------------------------ D54

        /// <summary>A kneeling German team in ambush at (200, 50); a Soviet team standing <paramref name="distance"/> m north.</summary>
        static (BattleState battle, Team ambush) Ambush(float distance)
        {
            var battle = Battle();
            var ambush = battle.AddTeam("de", Side.German, TeamType.Rifle, "", new float2(200, 50), 0f, 6, 50);
            battle.IssueOrder(ambush, OrderType.Ambush, ambush.Centroid);
            foreach (var m in ambush.Members) m.Posture = Posture.Kneeling;
            var enemy = battle.AddTeam("su", Side.Soviet, TeamType.Rifle, "", new float2(200, 50 + distance), 180f, 6, 50);
            Disarm(enemy);
            foreach (var m in enemy.Members) m.Posture = Posture.Standing;
            battle.Visibility.Recompute();
            Steps(battle, 3f);
            return (battle, ambush);
        }

        [Test]
        public void AnAmbushFiresFirstWhenTheEnemyIsAboutToFindIt()
        {
            var (_, ambush) = Ambush(120f);
            Assert.IsTrue(ambush.Members.Any(m => m.LastFiredAt > 0f), "the enemy 120 m away would spot the ambush soon: fire first");
        }

        [Test]
        public void AnAmbushStillWaitsForAFarEnemy()
        {
            var (_, ambush) = Ambush(220f);
            Assert.IsFalse(ambush.Members.Any(m => m.LastFiredAt > 0f), "the enemy 220 m away cannot see the ambush yet: wait");
        }
    }
}
