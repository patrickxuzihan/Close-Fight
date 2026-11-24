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
    /// <summary>Night battles and flares (M14, D35).</summary>
    public class NightTests
    {
        static BattleState Battle(bool night, ulong seed = 15)
        {
            var battle = new BattleState(MapBuilder.Build(TestMaps.Empty(400f)), seed) { IsNight = night };
            battle.Victory.TimeLimitSeconds = 1e6f;
            battle.Illumination.SetFlares(Side.German, 3);
            return battle;
        }

        static void Disarm(Team team)
        {
            foreach (var m in team.Members) m.Ammo = 0;
        }

        /// <summary>A German team at (200, 50) and a kneeling Soviet team 150 m north.</summary>
        static (Team watchers, Team target) Pair(BattleState battle)
        {
            var watchers = battle.AddTeam("de", Side.German, TeamType.Rifle, "", new float2(200, 50), 0f, 6, 50);
            var target = battle.AddTeam("su", Side.Soviet, TeamType.Rifle, "", new float2(200, 200), 180f, 6, 50);
            Disarm(watchers);
            Disarm(target);
            battle.Visibility.Recompute();
            return (watchers, target);
        }

        [Test]
        public void TheDarkHidesMenThatDaylightShows()
        {
            var day = Battle(false);
            Pair(day);
            Assert.Greater(day.Visibility.VisibleEnemyCount(Side.German), 0, "kneeling men at 150 m are seen by day");

            var night = Battle(true);
            Pair(night);
            Assert.AreEqual(0, night.Visibility.VisibleEnemyCount(Side.German), "...but not at night (D35: 50–100 m)");
        }

        [Test]
        public void AtNightMenAreSeenCloseUp()
        {
            var battle = Battle(true);
            var watchers = battle.AddTeam("de", Side.German, TeamType.Rifle, "", new float2(200, 50), 0f, 6, 50);
            var target = battle.AddTeam("su", Side.Soviet, TeamType.Rifle, "", new float2(200, 100), 180f, 6, 50);
            foreach (var m in target.Members) m.Posture = Posture.Standing;
            battle.Visibility.Recompute();
            Assert.Greater(battle.Visibility.VisibleEnemyCount(Side.German), 0, "standing men 50 m away are seen even at night");
        }

        [Test]
        public void AMuzzleFlashGivesAShooterAway()
        {
            var battle = Battle(true);
            var (_, target) = Pair(battle);
            target.Members[1].LastFiredAt = battle.Clock.ElapsedSeconds;
            battle.Visibility.Recompute();
            Assert.IsTrue(battle.Visibility.CanSee(Side.German, target.Members[1]), "the man who just fired is seen at 150 m");
            Assert.IsFalse(battle.Visibility.CanSee(Side.German, target.Members[2]), "his comrades stay in the dark");
        }

        [Test]
        public void AFlareLightsTheAreaUpUntilItBurnsOut()
        {
            var battle = Battle(true);
            var (_, target) = Pair(battle);
            Assert.AreEqual(0, battle.Visibility.VisibleEnemyCount(Side.German));

            var flare = battle.Illumination.Fire(Side.German, new float2(200, 190), out var refusal);
            Assert.IsNotNull(flare, refusal.ToString());
            Assert.AreEqual(2, battle.Illumination.FlaresLeft(Side.German));
            battle.Step();
            battle.Visibility.Recompute();
            Assert.AreEqual(target.ActiveCount, battle.Visibility.VisibleEnemyCount(Side.German), "everyone in the light is exposed");

            for (int i = 0; i < (Flare.BurnSeconds + 2f) * 10f; i++) battle.Step();
            battle.Visibility.Recompute();
            Assert.AreEqual(0, battle.Illumination.Flares.Count, "burnt out");
            Assert.AreEqual(0, battle.Visibility.VisibleEnemyCount(Side.German), "dark again");
        }

        [Test]
        public void AFlareDriftsAndSinksSoItsLightShrinks()
        {
            var battle = Battle(true);
            battle.AddTeam("de", Side.German, TeamType.Rifle, "", new float2(200, 50), 0f, 6, 50);
            var flare = battle.Illumination.Fire(Side.German, new float2(200, 150), out _);
            float2 start = flare.Position(battle.Clock.ElapsedSeconds);
            float startLight = battle.Illumination.LightAt(new float2(260, 150));
            for (int i = 0; i < 300; i++) battle.Step();

            float now = battle.Clock.ElapsedSeconds;
            Assert.Greater(math.distance(flare.Position(now), start), 10f, "the wind carries it");
            Assert.Less(flare.Height(now), Flare.StartHeight - 30f, "it sinks");
            Assert.Less(battle.Illumination.LightAt(flare.Position(now) + new float2(60, 0)), startLight, "the lit circle shrinks");
            Assert.Greater(battle.Illumination.LightAt(flare.Position(now)), Illumination.LitThreshold, "still bright right below it");
        }

        [Test]
        public void FlaresAreFewAndNeedOurMenNearby()
        {
            var battle = Battle(true);
            battle.AddTeam("de", Side.German, TeamType.Rifle, "", new float2(200, 50), 0f, 6, 50);
            Assert.IsNull(battle.Illumination.Fire(Side.German, new float2(200, 350), out var refusal), "300 m away");
            Assert.AreEqual(FlareRefusal.OutOfReach, refusal);

            for (int i = 0; i < 3; i++) Assert.IsNotNull(battle.Illumination.Fire(Side.German, new float2(200, 150), out _));
            Assert.IsNull(battle.Illumination.Fire(Side.German, new float2(200, 150), out refusal));
            Assert.AreEqual(FlareRefusal.NoFlaresLeft, refusal);

            var day = Battle(false);
            day.AddTeam("de", Side.German, TeamType.Rifle, "", new float2(200, 50), 0f, 6, 50);
            Assert.IsNull(day.Illumination.Fire(Side.German, new float2(200, 150), out refusal));
            Assert.AreEqual(FlareRefusal.Daylight, refusal);
        }

        [Test]
        public void DefendersPutUpAFlareOverWhatTheyHearInTheDark()
        {
            var battle = Battle(true);
            battle.SetRetreatEdge(Side.German, MapEdge.North);
            battle.SetRetreatEdge(Side.Soviet, MapEdge.South);
            battle.AddObjective(new ObjectiveDefinition { id = "o", position = new MapPoint(200, 300), radius = 12f, value = 2, owner = Side.German });
            battle.AddTeam("de", Side.German, TeamType.Rifle, "", new float2(200, 300), 180f, 6, 50);
            var attackers = battle.AddTeam("su", Side.Soviet, TeamType.Rifle, "", new float2(200, 160), 0f, 6, 50);
            battle.AddCommander(new DefenseCommander(battle, Side.German));
            for (int i = 0; i < 40; i++) battle.Step();

            // Unseen in the dark, the Soviets open fire from 140 m: the defenders hear it.
            battle.Visibility.ReportSound(attackers.Leader, attackers.Leader.Position, 500f, 10f);
            for (int i = 0; i < 60; i++) battle.Step();
            Assert.Less(battle.Illumination.FlaresLeft(Side.German), 3, "a flare goes up over the noise");
        }

        [Test]
        public void TheNightScenarioIsAGuardsCounterattack()
        {
            const string root = "Assets/CloseFight/Data";
            var scenario = ScenarioLoader.FromJson(File.ReadAllText(Path.Combine(root, "Scenarios", "stalingrad_station_sept15_night.json")));
            var map = MapBuilder.Build(MapSerializer.FromJson(File.ReadAllText(Path.Combine(root, "Maps", scenario.mapId + ".json"))));
            var battle = ScenarioLoader.CreateBattle(scenario, map, playerSide: Side.German, deployment: true);
            Assert.IsTrue(battle.IsNight);
            Assert.Greater(battle.Illumination.FlaresLeft(Side.German), 0);
            Assert.IsTrue(battle.Commanders.OfType<AttackCommander>().Any(c => c.Side == Side.Soviet), "the Soviets attack");
            Assert.IsTrue(battle.Objectives.All(o => o.Owner == Side.German), "the Germans hold the station area");
            Assert.IsFalse(battle.Support.Available(SupportAssets.Stuka), "no Stukas at night");
            Assert.IsTrue(battle.Support.Available(SupportAssets.Po2), "the night bombers fly");
        }
    }
}
