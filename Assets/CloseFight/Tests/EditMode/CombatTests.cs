using System.Linq;
using CloseFight.Core.Battle;
using CloseFight.Core.Map;
using CloseFight.Core.Units;
using NUnit.Framework;
using Unity.Mathematics;

namespace CloseFight.Tests
{
    public class CombatTests
    {
        static BattleState Battle(MapDefinition map = null, ulong seed = 3) => new BattleState(MapBuilder.Build(map ?? TestMaps.Empty(300f)), seed);

        static void Run(BattleState battle, float seconds)
        {
            int ticks = (int)(seconds / 0.1f);
            for (int i = 0; i < ticks; i++)
            {
                battle.Step();
            }
        }

        static int Casualties(Team team) => team.Members.Count(m => !m.IsActive || m.IsWounded);

        /// <summary>Rounds left across the whole team, fallen included (TotalAmmo drops when men fall).</summary>
        static int AmmoLeft(Team team) => team.Members.Sum(m => m.Ammo);

        [Test]
        public void TeamsAreIssuedPeriodWeapons()
        {
            var battle = Battle();
            var de = battle.AddTeam("de", Side.German, TeamType.Rifle, "", new float2(50, 50), 0f, 6, 50);
            var mg = battle.AddTeam("mg", Side.Soviet, TeamType.MachineGun, "", new float2(150, 150), 0f, 3, 50);

            Assert.AreSame(Weapons.Mp40, de.Members[0].Weapon, "German squad leader carries the MP40");
            Assert.AreSame(Weapons.Kar98k, de.Members[1].Weapon);
            Assert.AreSame(Weapons.Dp28, mg.Members[0].Weapon);
            Assert.AreEqual(Weapons.Kar98k.StartingAmmo, de.Members[1].Ammo);
            Assert.AreEqual(2, de.SmokeGrenades);
        }

        [Test]
        public void OpposingTeamsInTheOpenTradeFireAndTakeCasualties()
        {
            var battle = Battle();
            var de = battle.AddTeam("de", Side.German, TeamType.Rifle, "", new float2(150, 100), 0f, 6, 50);
            var su = battle.AddTeam("su", Side.Soviet, TeamType.Rifle, "", new float2(150, 200), 180f, 6, 50);
            battle.Visibility.Recompute();

            int startAmmo = AmmoLeft(de);
            Run(battle, 60f);

            Assert.Less(AmmoLeft(de), startAmmo, "Germans fired");
            Assert.Greater(Casualties(de) + Casualties(su), 0, "a minute of fire at 100 m in the open hurts someone");
        }

        [Test]
        public void NoOneFiresWithoutSight()
        {
            var map = TestMaps.Empty(200f);
            map.buildings.Add(new BuildingDefinition { id = 1, floors = 4, polygon = TestMaps.Rect(80, 0, 120, 200) });
            var battle = Battle(map);
            var de = battle.AddTeam("de", Side.German, TeamType.Rifle, "", new float2(50, 100), 90f, 6, 50);
            battle.AddTeam("su", Side.Soviet, TeamType.Rifle, "", new float2(150, 100), 270f, 6, 50);

            int startAmmo = AmmoLeft(de);
            Run(battle, 20f);
            Assert.AreEqual(startAmmo, AmmoLeft(de));
            Assert.AreEqual(0, battle.Combat.RecentShots.Count);
        }

        [Test]
        public void CoverAndPostureLowerHitChance()
        {
            var battle = Battle();
            var shooterTeam = battle.AddTeam("de", Side.German, TeamType.Rifle, "", new float2(10, 10), 0f, 1, 50);
            var targetTeam = battle.AddTeam("su", Side.Soviet, TeamType.Rifle, "", new float2(10, 110), 180f, 1, 50);
            var shooter = shooterTeam.Members[0];
            var target = targetTeam.Members[0];

            target.Posture = Posture.Standing;
            float open = Combat.HitChance(Weapons.Kar98k, 100f, shooter, target, 0f, false);
            float behindWall = Combat.HitChance(Weapons.Kar98k, 100f, shooter, target, 65f, false);
            target.Posture = Posture.Prone;
            float prone = Combat.HitChance(Weapons.Kar98k, 100f, shooter, target, 0f, false);
            target.Posture = Posture.Standing;
            float onTheMove = Combat.HitChance(Weapons.Kar98k, 100f, shooter, target, 0f, true);

            Assert.Less(behindWall, open * 0.5f);
            Assert.Less(prone, open);
            Assert.Less(onTheMove, open);
            Assert.Less(Combat.HitChance(Weapons.Mp40, 150f, shooter, target, 0f, false),
                Combat.HitChance(Weapons.Kar98k, 150f, shooter, target, 0f, false), "SMGs are poor at range");
        }

        [Test]
        public void WallInFrontGivesCover()
        {
            var map = TestMaps.Empty(100f);
            map.walls.Add(new WallDefinition { points = { new MapPoint(40, 49), new MapPoint(60, 49) }, height = 1.0f });
            var battle = Battle(map);
            var su = battle.AddTeam("su", Side.Soviet, TeamType.Rifle, "", new float2(50, 50.5f), 180f, 1, 50);
            var target = su.Members[0];

            Assert.Greater(battle.Combat.CoverOf(target, new float2(50, 10)), 50f, "wall between him and a shooter to the south");
            Assert.AreEqual(0f, battle.Combat.CoverOf(target, new float2(50, 90)), "no cover from the north");
        }

        [Test]
        public void IncomingFireSuppressesAndPinsThenDecays()
        {
            // Soviets in a brick house 120 m away, out of ammunition, under MG fire.
            var map = TestMaps.Empty(300f);
            map.buildings.Add(new BuildingDefinition { id = 1, floors = 1, polygon = TestMaps.Rect(140, 210, 160, 230) });
            var battle = Battle(map);
            battle.AddTeam("de", Side.German, TeamType.MachineGun, "", new float2(150, 100), 0f, 3, 30);
            var su = battle.AddTeam("su", Side.Soviet, TeamType.Rifle, "", new float2(150, 220), 180f, 6, 30);
            foreach (var soviet in su.Members)
            {
                soviet.Ammo = 0;
            }

            battle.Visibility.Recompute();
            Run(battle, 6f);
            float peak = su.Members.Where(m => m.IsActive).Max(m => m.Suppression);
            Assert.Greater(peak, 20f, "MG fire suppresses");

            // Take the Germans' ammunition away; suppression must fade.
            foreach (var german in battle.Teams[0].Members)
            {
                german.Ammo = 0;
            }

            Run(battle, 20f);
            Assert.Less(su.Members.Where(m => m.IsActive).Max(m => m.Suppression), peak * 0.5f);
        }

        [Test]
        public void PinnedSoldiersDoNotMove()
        {
            var battle = Battle();
            var su = battle.AddTeam("su", Side.Soviet, TeamType.MachineGun, "", new float2(100, 100), 0f, 3, 50);
            foreach (var soldier in su.Members)
            {
                soldier.Suppression = 100f;
                soldier.LastUnderFireAt = 0f;
            }

            var before = su.Members[0].Position;
            battle.IssueOrder(su, OrderType.Move, new float2(100, 150));
            Run(battle, 1f); // within the decay delay
            Assert.AreEqual(before, su.Members[0].Position);
            Assert.AreEqual(Posture.Prone, su.Members[0].Posture);
        }

        [Test]
        public void RunningTeamsHoldFire()
        {
            var battle = Battle();
            var de = battle.AddTeam("de", Side.German, TeamType.Rifle, "", new float2(150, 100), 0f, 6, 50);
            battle.AddTeam("su", Side.Soviet, TeamType.Rifle, "", new float2(150, 220), 180f, 6, 50);
            battle.Visibility.Recompute();

            battle.IssueOrder(de, OrderType.MoveFast, new float2(250, 100));
            int startAmmo = AmmoLeft(de);
            Run(battle, 3f);
            Assert.AreEqual(startAmmo, AmmoLeft(de));
        }

        [Test]
        public void AmbushHoldsFireUntilEnemyIsClose()
        {
            var battle = Battle();
            var su = battle.AddTeam("su", Side.Soviet, TeamType.Rifle, "", new float2(150, 200), 180f, 6, 50);
            var de = battle.AddTeam("de", Side.German, TeamType.Rifle, "", new float2(150, 100), 0f, 6, 50);
            battle.IssueOrder(su, OrderType.Ambush, float2.zero);
            battle.IssueOrder(de, OrderType.Hide, float2.zero); // keep the Germans quiet too
            battle.Visibility.Recompute();

            int startAmmo = AmmoLeft(su);
            Run(battle, 5f);
            Assert.AreEqual(startAmmo, AmmoLeft(su), "100 m away: the ambush waits");

            battle.IssueOrder(de, OrderType.Move, new float2(150, 165));
            for (int i = 0; i < 600 && AmmoLeft(su) == startAmmo; i++)
            {
                battle.Step();
            }

            Assert.Less(AmmoLeft(su), startAmmo, "the ambush is sprung");
            Assert.AreEqual(OrderType.Defend, su.Order.Type, "a sprung ambush fights normally");
            Assert.LessOrEqual(math.distance(de.Centroid, su.Centroid), Combat.AmbushRange + 10f);
        }

        [Test]
        public void FireOrderTargetsTheNamedTeam()
        {
            var battle = Battle();
            var de = battle.AddTeam("de", Side.German, TeamType.Rifle, "", new float2(150, 100), 0f, 6, 50);
            var near = battle.AddTeam("near", Side.Soviet, TeamType.Rifle, "", new float2(130, 150), 180f, 6, 50);
            var far = battle.AddTeam("far", Side.Soviet, TeamType.Rifle, "", new float2(180, 190), 180f, 6, 50);
            battle.Visibility.Recompute();
            Assert.IsTrue(battle.Visibility.CanSee(Side.German, far.Members[0]),
                $"far leader at {far.Members[0].Position} visible? German leader at {de.Members[0].Position}, posture {far.Members[0].Posture}, status {far.Members[0].Status}, " +
                $"range {Visibility.SpottingRange(far.Members[0], battle.Map, 0f)}, visible enemies {battle.Visibility.VisibleEnemyCount(Side.German)}");

            Assert.IsTrue(battle.IssueOrder(de, OrderType.Fire, far.Members[0].Position));
            Assert.AreSame(far, de.FireTarget);

            Run(battle, 4f);
            var germanIds = de.Members.Select(m => m.Id).ToList();
            foreach (var shot in battle.Combat.RecentShots.Where(s => germanIds.Contains(s.ShooterId)))
            {
                Assert.Less(math.distance(shot.Impact, far.Centroid), math.distance(shot.Impact, near.Centroid) + 5f,
                    "German rounds land around the named team");
            }

            Assert.IsTrue(battle.IssueOrder(de, OrderType.Defend, float2.zero));
            Assert.IsNull(de.FireTarget, "a new order cancels the fire order");
        }

        [Test]
        public void AreaFireNeedsSightOfThePointAndSuppressesUnseenEnemies()
        {
            var map = TestMaps.Empty(200f);
            map.walls.Add(new WallDefinition { points = { new MapPoint(80, 140), new MapPoint(120, 140) }, height = 1.0f });
            var battle = Battle(map);
            var de = battle.AddTeam("de", Side.German, TeamType.MachineGun, "", new float2(100, 60), 0f, 3, 50);
            var su = battle.AddTeam("su", Side.Soviet, TeamType.Rifle, "", new float2(100, 142), 180f, 6, 50);
            battle.IssueOrder(su, OrderType.Hide, float2.zero);
            battle.Visibility.Recompute();
            Assert.IsFalse(battle.Visibility.CanSee(Side.German, su.Members[0]), "hidden behind the wall");

            Assert.IsTrue(battle.IssueOrder(de, OrderType.AreaFire, new float2(100, 142)));
            Run(battle, 10f);
            Assert.Greater(su.Members.Max(m => m.Suppression), 10f);
        }

        [Test]
        public void SmokeIsThrownWithinRangeAndBlocksSight()
        {
            var battle = Battle();
            // Three-man teams, so every sight line passes through the cloud.
            var de = battle.AddTeam("de", Side.German, TeamType.MachineGun, "", new float2(150, 100), 0f, 3, 50);
            var su = battle.AddTeam("su", Side.Soviet, TeamType.MachineGun, "", new float2(150, 180), 180f, 3, 50);
            battle.Visibility.Recompute();
            Assert.IsTrue(battle.Visibility.CanSee(Side.German, su.Members[0]));

            Assert.IsFalse(battle.IssueOrder(de, OrderType.Smoke, new float2(150, 160)), "60 m is too far to throw");
            Assert.IsTrue(battle.IssueOrder(de, OrderType.Smoke, new float2(150, 125)));
            Assert.AreEqual(0, de.SmokeGrenades);

            Run(battle, 5f); // let the cloud grow
            battle.Visibility.Recompute();
            Assert.IsFalse(battle.Visibility.CanSee(Side.German, su.Members[0]), "smoke screens the Soviets");

            Run(battle, SmokeCloud.HoldSeconds + SmokeCloud.FadeSeconds + 1f);
            Assert.AreEqual(0, battle.Smoke.Clouds.Count, "the cloud clears");
        }

        [Test]
        public void SustainedFireKeepsOneSoundMarker()
        {
            var map = TestMaps.Empty(200f);
            map.buildings.Add(new BuildingDefinition { id = 1, floors = 4, polygon = TestMaps.Rect(80, 0, 120, 200) });
            var battle = Battle(map);
            battle.AddTeam("de", Side.German, TeamType.MachineGun, "", new float2(40, 100), 90f, 3, 50);
            var su = battle.AddTeam("su", Side.Soviet, TeamType.MachineGun, "", new float2(160, 100), 270f, 3, 50);
            battle.Visibility.Recompute();

            for (int i = 0; i < 10; i++)
            {
                battle.Visibility.ReportSound(su.Members[0], su.Members[0].Position, 700f, 15f);
            }

            Assert.AreEqual(1, battle.Visibility.SoundMarkers.Count);
        }
    }
}
