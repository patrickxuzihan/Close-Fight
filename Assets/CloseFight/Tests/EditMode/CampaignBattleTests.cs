using System.IO;
using System.Linq;
using CloseFight.Core.Battle;
using CloseFight.Core.Campaign;
using CloseFight.Core.Map;
using CloseFight.Core.Units;
using NUnit.Framework;
using Unity.Mathematics;

namespace CloseFight.Tests
{
    /// <summary>Campaign battles fought with the campaign's own men (M16 step 3), on the station map.</summary>
    public class CampaignBattleTests
    {
        const string DataRoot = "Assets/CloseFight/Data";

        static CampaignState Campaign() =>
            CampaignState.Create(CampaignLoader.FromJson(File.ReadAllText(Path.Combine(DataRoot, "Campaigns", "stalingrad_center.json"))));

        static TacticalMap StationMap() =>
            MapBuilder.Build(MapSerializer.FromJson(File.ReadAllText(Path.Combine(DataRoot, "Maps", "stalingrad_station.json"))));

        static Battlefield StationField() =>
            Battlefield.FromScenario(ScenarioLoader.FromJson(File.ReadAllText(Path.Combine(DataRoot, "Scenarios", "stalingrad_station_sept14.json"))), 3);

        /// <summary>IR 194 attacks Height 112.5, held by the 42nd Rifle Brigade.</summary>
        static (CampaignState state, PendingBattle battle) Attack()
        {
            var state = Campaign();
            var de = new SideOrders { side = Side.German };
            de.Move("de_ir194", "hill_112");
            state.SubmitOrders(de);
            state.SubmitOrders(new SideOrders { side = Side.Soviet });
            var battle = state.ResolveMovement().Single(b => b.district == "hill_112");
            return (state, battle);
        }

        [Test]
        public void EachSideBringsWhatTheMapHoldsAndAtMostTwoThirdsOfARegiment()
        {
            var (state, battle) = Attack();
            var forces = BattleForces.Default(state, battle, Side.German, 3);
            var picked = forces.Resolve(state).ToList();
            Assert.AreEqual(3, picked.Count, "the station map holds three companies a side");
            Assert.IsTrue(picked.Any(x => x.company.template == "de_rifle"), "a rifle company goes first");
            foreach (var group in picked.GroupBy(x => x.regiment))
            {
                Assert.LessOrEqual(group.Count(), BattleForces.MaxFrom(group.Key), "D21");
            }

            var big = BattleForces.Default(state, battle, Side.German, 99).Resolve(state).Count();
            Assert.AreEqual(BattleForces.MaxFrom(state.Regiment("de_ir194")), big, "with room to spare, still only two thirds of the regiment");
        }

        [Test]
        public void TheRollsMenFightWithTheirOwnNamesAndTheRegimentsAmmunition()
        {
            var (state, pending) = Attack();
            state.Regiment("de_ir194").ammo = 0.5f;
            var field = StationField();
            var fight = CampaignBattle.Create(state, pending, field, StationMap(),
                BattleForces.Default(state, pending, Side.German, 3), BattleForces.Default(state, pending, Side.Soviet, 3), Side.German);
            var battle = fight.Battle;

            var names = state.regiments.SelectMany(r => r.Soldiers).Select(s => s.FullName).ToHashSet();
            Assert.Greater(battle.Soldiers.Count, 100);
            Assert.IsTrue(battle.Soldiers.All(s => names.Contains(s.FullName)), "no generated strangers");

            var riflemen = battle.Soldiers.Where(s => s.Team.Side == Side.German && s.Weapon == Weapons.Kar98k).ToList();
            Assert.IsNotEmpty(riflemen);
            Assert.IsTrue(riflemen.All(s => s.Ammo == (int)math.ceil(Weapons.Kar98k.StartingAmmo * 0.5f)), "half a basic load (D40)");

            Assert.IsTrue(battle.IsDeploying);
            foreach (var team in battle.Teams.Where(t => t.Side == Side.German))
            {
                Assert.IsTrue(battle.InDeployZone(Side.German, team.Vehicle != null ? team.Vehicle.Position : team.Centroid) ||
                              battle.InDeployZone(Side.German, team.Members[0].Position), $"{team.DisplayName} starts in its zone");
            }

            Assert.AreEqual(field.objectives.Count, battle.Objectives.Count);
            Assert.IsTrue(battle.Objectives.All(o => o.Owner == Side.Soviet), "the defender holds the objectives at the start");
        }

        [Test]
        public void AFoughtBattleIsWrittenBackIntoTheRolls()
        {
            var (state, pending) = Attack();
            var fight = CampaignBattle.Create(state, pending, StationField(), StationMap(),
                BattleForces.Default(state, pending, Side.German, 3), BattleForces.Default(state, pending, Side.Soviet, 3), playerSide: null);
            var battle = fight.Battle;
            battle.BeginBattle();
            battle.Victory.TimeLimitSeconds = 240f;
            for (int i = 0; i < 2600 && !battle.Victory.IsOver; i++)
            {
                battle.Step();
            }

            Assert.IsTrue(battle.Victory.IsOver);
            int dead = battle.Soldiers.Count(s => s.Status == SoldierStatus.Dead);
            int kills = battle.Soldiers.Sum(s => s.Kills);
            var before = state.Regiment("de_ir194").Soldiers.ToDictionary(s => s.id, s => s.attributes.Experience);

            var outcome = fight.Finish();
            var records = state.regiments.SelectMany(r => r.Soldiers).ToList();
            Assert.AreEqual(dead, records.Count(s => s.status == SoldierStatus.Dead), "every death is on the rolls");
            Assert.AreEqual(kills, records.Sum(s => s.kills));
            Assert.Greater(dead, 0, "four minutes of fighting costs lives");
            var veterans = state.Regiment("de_ir194").Soldiers.Where(s => s.battles == 1 && (s.status == SoldierStatus.Healthy || s.status == SoldierStatus.Wounded)).ToList();
            Assert.IsNotEmpty(veterans);
            Assert.IsTrue(veterans.All(s => s.attributes.Experience > before[s.id] || before[s.id] >= 98), "D30: experience only grows");
            Assert.Greater(outcome.ammoUsed["de_ir194"], 0f);
            Assert.Greater(outcome.moraleLost["de_ir194"], 0f);

            state.ApplyBattle(pending, outcome);
            Assert.AreEqual(0, state.regiments.SelectMany(r => r.Soldiers).Count(s => s.status == SoldierStatus.Dead), "the dead are struck off");
            Assert.Greater(state.Regiment("de_ir194")?.killed ?? 1, 0);
            Assert.Throws<System.InvalidOperationException>(() => fight.Finish(), "written back once only");
        }
    }
}
