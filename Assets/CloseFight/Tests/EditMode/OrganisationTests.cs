using System.IO;
using System.Linq;
using CloseFight.Core.Battle;
using CloseFight.Core.Map;
using CloseFight.Core.Units;
using NUnit.Framework;
using Unity.Mathematics;

namespace CloseFight.Tests
{
    /// <summary>Platoons and the 1942 tables of organisation (M18).</summary>
    public class OrganisationTests
    {
        static BattleState Battle() => new BattleState(MapBuilder.Build(TestMaps.Empty(200f)), 3) { Victory = { TimeLimitSeconds = 1e6f } };

        [Test]
        public void AGermanRifleSquadIsTenMenWithAnMg34()
        {
            var battle = Battle();
            var squad = battle.AddTeam("g", Side.German, TeamType.Rifle, "1. Gruppe", new float2(100, 100), 0f, BattleState.DefaultSize(TeamType.Rifle, Side.German), 50);
            Assert.AreEqual(10, squad.Members.Count, "full strength (campaigns start full)");
            Assert.AreEqual(Weapons.Mp40, squad.Members[0].Weapon, "the Gruppenführer carries the MP 40");
            Assert.AreEqual(Weapons.Mg34, squad.Members[1].Weapon, "MG-Schütze 1");
            Assert.AreEqual(Weapons.P08, squad.Members[2].Weapon, "MG-Schütze 2 has a pistol");
            Assert.IsTrue(squad.Members.Skip(3).All(m => m.Weapon == Weapons.Kar98k), "the rest carry the Kar 98k");
        }

        [Test]
        public void ASovietRifleSectionIsElevenMenWithADp()
        {
            var battle = Battle();
            var squad = battle.AddTeam("s", Side.Soviet, TeamType.Rifle, "1-ye otdeleniye", new float2(100, 100), 0f, BattleState.DefaultSize(TeamType.Rifle, Side.Soviet), 50);
            Assert.AreEqual(11, squad.Members.Count);
            Assert.AreEqual(Weapons.Ppsh41, squad.Members[0].Weapon);
            Assert.AreEqual(Weapons.Dp28, squad.Members[1].Weapon);
            Assert.AreEqual(2, squad.Members.Count(m => m.Weapon == Weapons.Ppsh41));
            Assert.AreEqual(8, squad.Members.Count(m => m.Weapon == Weapons.Mosin));
        }

        [Test]
        public void HeavyMachineGunTeamsHaveTripodOrWheeledGuns()
        {
            var battle = Battle();
            var de = battle.AddTeam("d", Side.German, TeamType.MachineGun, "", new float2(50, 50), 0f, BattleState.DefaultSize(TeamType.MachineGun), 50);
            var su = battle.AddTeam("s", Side.Soviet, TeamType.MachineGun, "", new float2(150, 150), 0f, BattleState.DefaultSize(TeamType.MachineGun), 50);
            Assert.AreEqual(4, de.Members.Count);
            Assert.AreEqual(Weapons.Mg34Heavy, de.Members[0].Weapon);
            Assert.AreEqual(Weapons.Maxim, su.Members[0].Weapon);
            Assert.Greater(Weapons.Mg34Heavy.EffectiveRange, Weapons.Mg34.EffectiveRange, "the tripod and sight reach further than the bipod");
        }

        [Test]
        public void WhenTheGunnerFallsTheNumberTwoTakesTheGun()
        {
            var battle = Battle();
            var squad = battle.AddTeam("g", Side.German, TeamType.Rifle, "", new float2(100, 100), 0f, 10, 50);
            var gunner = squad.Members[1];
            int belts = gunner.Ammo;
            gunner.Status = SoldierStatus.Dead;
            for (int i = 0; i < 20; i++) battle.Step();

            var taker = squad.Members.Single(m => m.IsActive && m.Weapon == Weapons.Mg34);
            Assert.AreEqual(squad.Members[2], taker, "the No. 2 picks it up first");
            Assert.AreEqual(belts, taker.Ammo, "with the belts");
            Assert.AreEqual(Weapons.P08, gunner.Weapon, "his pistol stays by the fallen gunner");
        }

        [Test]
        public void TeamsBelongToOnePlatoonAtATime()
        {
            var battle = Battle();
            var first = battle.AddPlatoon("z1", Side.German, PlatoonType.Rifle, "1. Zug", "1./IR 194");
            var second = battle.AddPlatoon("z2", Side.German, PlatoonType.Rifle, "2. Zug", "1./IR 194");
            var squad = battle.AddTeam("g", Side.German, TeamType.Rifle, "", new float2(100, 100), 0f, 10, 50);
            battle.AssignToPlatoon(squad, first);
            battle.AssignToPlatoon(squad, second);
            Assert.AreSame(second, squad.Platoon);
            Assert.IsEmpty(first.Teams);
            Assert.AreEqual(1, second.Teams.Count);
            Assert.AreEqual(10, second.ActiveCount);
        }

        [TestCase("stalingrad_station_sept14.json")]
        [TestCase("stalingrad_station_sept15_night.json")]
        public void StationScenariosGroupEveryTeamInAPlatoonOfItsSide(string file)
        {
            const string root = "Assets/CloseFight/Data";
            var scenario = ScenarioLoader.FromJson(File.ReadAllText(Path.Combine(root, "Scenarios", file)));
            var map = MapBuilder.Build(MapSerializer.FromJson(File.ReadAllText(Path.Combine(root, "Maps", scenario.mapId + ".json"))));
            var battle = ScenarioLoader.CreateBattle(scenario, map, withAi: false);

            foreach (var team in battle.Teams)
            {
                Assert.IsNotNull(team.Platoon, team.Key + " has no platoon");
                Assert.AreEqual(team.Side, team.Platoon.Side, team.Key);
            }

            Assert.IsTrue(battle.Platoons.All(p => p.Teams.Count > 0), "no empty platoons");
            var rifle = battle.Teams.Where(t => t.Type == TeamType.Rifle).ToList();
            Assert.IsTrue(rifle.Where(t => t.Side == Side.German).All(t => t.Members.Count == 10), "German squads start at full strength");
            Assert.IsTrue(rifle.Where(t => t.Side == Side.Soviet).All(t => t.Members.Count == 11), "Soviet sections start at full strength");
        }
    }
}
