using System.Linq;
using CloseFight.Core.Battle;
using CloseFight.Core.Map;
using CloseFight.Core.Units;
using NUnit.Framework;
using Unity.Mathematics;

namespace CloseFight.Tests
{
    public class VehicleTests
    {
        static BattleState Battle(MapDefinition map = null, ulong seed = 9) => new BattleState(MapBuilder.Build(map ?? TestMaps.Empty(600f)), seed);

        static void Run(BattleState battle, float seconds)
        {
            int ticks = (int)(seconds / 0.1f);
            for (int i = 0; i < ticks; i++)
            {
                battle.Step();
            }
        }

        static Team Tank(BattleState battle, string id, Side side, float2 at, float heading)
        {
            return battle.AddTeam(id, side, TeamType.Vehicle, id, at, heading, 0, 50, id);
        }

        static void Disarm(Team team)
        {
            foreach (var member in team.Members)
            {
                member.Ammo = 0;
            }

            if (team.Gun != null)
            {
                team.Gun.AmmoAp = 0;
                team.Gun.AmmoHe = 0;
                team.Gun.AmmoSmoke = 0;
            }
        }

        static bool Disabled(Vehicle v) => v.Destroyed || v.Abandoned || v.Immobilized || v.GunDamaged;

        // ------------------------------------------------------------ data

        [Test]
        public void ArmorFacingFollowsTheAngleOfTheHit()
        {
            var t34 = Equipment.T34;
            var at = new float2(100, 100);
            Assert.AreEqual(90f, Gunnery.EffectiveArmor(t34, 0f, at, new float2(100, 300), out var facing), 1e-3f);
            Assert.AreEqual(ArmorFacing.Front, facing);
            Assert.AreEqual(50f, Gunnery.EffectiveArmor(t34, 0f, at, new float2(300, 100), out facing), 1e-3f);
            Assert.AreEqual(ArmorFacing.Side, facing);
            Assert.AreEqual(45f, Gunnery.EffectiveArmor(t34, 0f, at, new float2(100, -100), out facing), 1e-3f);
            Assert.AreEqual(ArmorFacing.Rear, facing);

            // 30 degrees off the nose: the plate is effectively thicker.
            float2 angled = at + Soldier.DirectionOf(30f) * 200f;
            Assert.AreEqual(90f / math.cos(math.radians(30f)), Gunnery.EffectiveArmor(t34, 0f, at, angled, out facing), 0.1f);
        }

        [Test]
        public void PenetrationFallsWithRange()
        {
            var pak = Equipment.Pak38;
            Assert.AreEqual(72f, pak.PenetrationAt(50f), 1e-3f);
            Assert.AreEqual(66.5f, pak.PenetrationAt(300f), 1e-3f);
            Assert.AreEqual(61f, pak.PenetrationAt(500f), 1e-3f);
            Assert.AreEqual(50f, pak.PenetrationAt(1000f), 1e-3f);
            Assert.Less(pak.PenetrationAt(1500f), 50f);
        }

        [Test]
        public void Pak38BouncesOffTheT34FrontButPiercesItsSide()
        {
            float pen = Equipment.Pak38.PenetrationAt(500f);
            var at = new float2(0, 0);
            Assert.Less(pen, Gunnery.EffectiveArmor(Equipment.T34, 0f, at, new float2(0, 500), out _), "front");
            Assert.Greater(pen, Gunnery.EffectiveArmor(Equipment.T34, 0f, at, new float2(500, 0), out _), "side");
        }

        [Test]
        public void ScenarioEquipmentCreatesCrewAndGuns()
        {
            var battle = Battle();
            var stug = Tank(battle, "stug3f", Side.German, new float2(100, 100), 0f);
            var mortar = battle.AddTeam("m", Side.Soviet, TeamType.Mortar, "", new float2(300, 300), 180f, 0, 50, "bm37");

            Assert.IsNotNull(stug.Vehicle);
            Assert.AreEqual(4, stug.Members.Count);
            Assert.IsTrue(stug.Members.All(m => m.IsMounted));
            Assert.AreEqual(CrewRole.Driver, stug.Members[2].Role);
            Assert.IsTrue(stug.IsMechanized);
            Assert.AreEqual(3, mortar.Members.Count);
            Assert.IsNotNull(mortar.Gun);
            Assert.IsTrue(mortar.Gun.Definition.Indirect);
            Assert.IsNull(mortar.Members[1].Weapon, "the mortar gunner serves the tube, not a rifle");
        }

        // ------------------------------------------------------------ movement

        [Test]
        public void TanksNeedRoomButInfantryCanSqueezeThrough()
        {
            var map = TestMaps.Empty(100f);
            // Two blocks with a 2 m alley between them at x = 49..51.
            map.buildings.Add(new BuildingDefinition { id = 1, floors = 2, polygon = TestMaps.Rect(0, 40, 49, 60) });
            map.buildings.Add(new BuildingDefinition { id = 2, floors = 2, polygon = TestMaps.Rect(51, 40, 100, 60) });
            var battle = Battle(map);
            var tank = Tank(battle, "pz3j", Side.German, new float2(50, 20), 0f);

            Assert.IsFalse(battle.IssueOrder(tank, OrderType.Move, new float2(50, 80)) && tank.Vehicle.CurrentPath.Count > 0 && tank.Vehicle.CurrentPath.Last().y > 70f,
                "no way through a 2 m alley for a tank");

            var riflemen = battle.AddTeam("r", Side.German, TeamType.MachineGun, "", new float2(50, 20), 0f, 3, 50);
            Assert.IsTrue(battle.IssueOrder(riflemen, OrderType.Move, new float2(50, 80)));
        }

        [Test]
        public void TankDrivesToTargetTurningItsHull()
        {
            var battle = Battle();
            var tank = Tank(battle, "t34_76", Side.Soviet, new float2(100, 100), 0f);
            Assert.IsTrue(battle.IssueOrder(tank, OrderType.MoveFast, new float2(200, 100)));

            Run(battle, 2f);
            Assert.Greater(tank.Vehicle.HullHeading, 30f, "swinging round toward the east");

            for (int i = 0; i < 600 && tank.Activity != TeamActivity.Idle; i++)
            {
                battle.Step();
            }

            Assert.Less(math.distance(tank.Vehicle.Position, new float2(200, 100)), 1f);
            Assert.AreEqual(90f, tank.Vehicle.HullHeading, 15f);
            Assert.IsTrue(tank.Members.All(m => math.distance(m.Position, tank.Vehicle.Position) < 1e-3f), "crew ride along");
        }

        [Test]
        public void ReverseKeepsTheFrontTowardTheEnemy()
        {
            var battle = Battle();
            var tank = Tank(battle, "pz3j", Side.German, new float2(100, 200), 0f);
            Assert.IsTrue(battle.IssueOrder(tank, OrderType.Reverse, new float2(100, 150)));
            Run(battle, 15f);

            Assert.Less(tank.Vehicle.Position.y, 195f, "it backed away south");
            Assert.AreEqual(0f, math.abs(VehicleMovement.DeltaAngle(tank.Vehicle.HullHeading, 0f)), 10f, "still facing north");
        }

        [Test]
        public void TanksCrushWoodenShedsAndFencesButNotBrick()
        {
            var map = TestMaps.Empty(100f);
            // A long row of wooden sheds: going round would cost more than driving through.
            map.buildings.Add(new BuildingDefinition { id = 1, material = BuildingMaterial.Wood, floors = 1, polygon = TestMaps.Rect(10, 40, 90, 48) });
            map.walls.Add(new WallDefinition { points = { new MapPoint(30, 60), new MapPoint(70, 60) }, height = 1f, blocksMovement = false, crushable = true });
            var battle = Battle(map);
            var tank = Tank(battle, "t34_76", Side.Soviet, new float2(50, 20), 0f);
            var grid = battle.Map.Grid;

            Assert.IsTrue(battle.IssueOrder(tank, OrderType.MoveFast, new float2(50, 80)));
            for (int i = 0; i < 600 && tank.Activity != TeamActivity.Idle; i++)
            {
                battle.Step();
            }

            Assert.Greater(tank.Vehicle.Position.y, 75f, "drove straight through");
            Assert.IsTrue(grid.HasFlag(grid.IndexOf(50, 44), CellFlags.Collapsed), "shed flattened");
            Assert.IsFalse(grid.HasFlag(grid.IndexOf(50, 60), CellFlags.Wall), "fence flattened");

            var brick = TestMaps.Empty(100f);
            brick.buildings.Add(new BuildingDefinition { id = 1, material = BuildingMaterial.Brick, floors = 1, polygon = TestMaps.Rect(0, 40, 100, 48) });
            var blocked = Battle(brick);
            var other = Tank(blocked, "t34_76", Side.Soviet, new float2(50, 20), 0f);
            blocked.IssueOrder(other, OrderType.MoveFast, new float2(50, 80));
            Run(blocked, 30f);
            Assert.Less(other.Vehicle.Position.y, 40f, "a brick row stops it");
        }

        [Test]
        public void VehiclesRefuseInfantryOrders()
        {
            var battle = Battle();
            var tank = Tank(battle, "pz3j", Side.German, new float2(100, 100), 0f);
            Assert.IsFalse(battle.IssueOrder(tank, OrderType.Crawl, new float2(100, 150)));
            Assert.IsFalse(battle.IssueOrder(tank, OrderType.Smoke, new float2(100, 120)));

            var riflemen = battle.AddTeam("r", Side.German, TeamType.Rifle, "", new float2(200, 100), 0f, 6, 50);
            Assert.IsFalse(battle.IssueOrder(riflemen, OrderType.Reverse, new float2(200, 50)), "only vehicles reverse");
        }

        // ------------------------------------------------------------ fighting

        [Test]
        public void SmallArmsCannotHurtTankCrews()
        {
            var battle = Battle();
            battle.AddTeam("de", Side.German, TeamType.Rifle, "", new float2(100, 100), 0f, 6, 50);
            var t60 = Tank(battle, "t60", Side.Soviet, new float2(100, 180), 180f);
            Disarm(t60);
            battle.Visibility.Recompute();
            Assert.IsTrue(battle.Visibility.CanSee(Side.German, t60.Vehicle));

            Run(battle, 30f);
            Assert.IsTrue(t60.Members.All(m => m.Status == SoldierStatus.Healthy));
            Assert.IsTrue(t60.Vehicle.IsOperational);
        }

        [Test]
        public void AntiTankGunKnocksOutATankFromTheSide()
        {
            var battle = Battle();
            var pak = battle.AddTeam("pak", Side.German, TeamType.AntiTankGun, "", new float2(100, 300), 90f, 0, 60, "pak38");
            var t34 = Tank(battle, "t34_76", Side.Soviet, new float2(350, 300), 0f); // side-on to the gun
            Disarm(t34);
            battle.Visibility.Recompute();

            for (int i = 0; i < 1200 && !Disabled(t34.Vehicle); i++)
            {
                battle.Step();
            }

            Assert.IsTrue(Disabled(t34.Vehicle), "a PaK 38 at 250 m beats T-34 side armor");
            Assert.Less(pak.Gun.AmmoAp, Equipment.Pak38.StartingAp);
        }

        [Test]
        public void AntiTankGunCannotPierceTheT34Front()
        {
            var battle = Battle();
            battle.AddTeam("pak", Side.German, TeamType.AntiTankGun, "", new float2(100, 300), 90f, 0, 60, "pak38");
            var t34 = Tank(battle, "t34_76", Side.Soviet, new float2(580, 300), 270f); // nose-on at 480 m
            Disarm(t34);
            battle.Visibility.Recompute();

            Run(battle, 40f);
            Assert.IsFalse(t34.Vehicle.Destroyed);
            Assert.IsFalse(t34.Vehicle.Abandoned);
            Assert.IsTrue(t34.Members.All(m => m.Status == SoldierStatus.Healthy), "nothing got through");
        }

        [Test]
        public void HighExplosiveKillsInTheOpenAndBringsBuildingsDown()
        {
            var map = TestMaps.Empty(200f);
            map.buildings.Add(new BuildingDefinition { id = 1, floors = 2, polygon = TestMaps.Rect(150, 150, 170, 170) });
            var battle = Battle(map);
            var su = battle.AddTeam("su", Side.Soviet, TeamType.Rifle, "", new float2(50, 50), 0f, 6, 50);
            SetSteady(su);

            for (int i = 0; i < 4; i++)
            {
                battle.Gunnery.Blast(new float2(50, 50), Equipment.StuK40, 0f);
            }

            Assert.Greater(su.Members.Count(m => !m.IsActive || m.IsWounded), 0, "four 75 mm shells in the middle of a squad");
            Assert.Greater(su.Members.Where(m => m.IsActive).DefaultIfEmpty().Max(m => m == null ? 100f : m.Suppression), 40f);

            var grid = battle.Map.Grid;
            int cell = grid.IndexOf(160, 160);
            battle.Gunnery.Blast(new float2(160.5f, 160.5f), Equipment.StuK40, 0f);
            battle.Gunnery.Blast(new float2(160.5f, 160.5f), Equipment.StuK40, 0f);
            Assert.IsFalse(grid.HasFlag(cell, CellFlags.Collapsed));
            battle.Gunnery.Blast(new float2(160.5f, 160.5f), Equipment.StuK40, 0f);
            Assert.IsTrue(grid.HasFlag(cell, CellFlags.Collapsed), "three hits bring the wall down");
            Assert.IsTrue(grid.HasFlag(grid.IndexOf(161, 161), CellFlags.Collapsed), "and its neighbors");
        }

        static void SetSteady(Team team)
        {
            foreach (var m in team.Members)
            {
                m.Morale = m.BaseMorale = 80f;
            }
        }

        [Test]
        public void MortarShellsTakeTimeToLand()
        {
            var battle = Battle();
            var mortar = battle.AddTeam("m", Side.German, TeamType.Mortar, "", new float2(100, 100), 0f, 0, 50, "grw34");
            var su = battle.AddTeam("su", Side.Soviet, TeamType.Rifle, "", new float2(100, 400), 180f, 6, 50);
            Disarm(su);

            Assert.IsTrue(battle.IssueOrder(mortar, OrderType.AreaFire, new float2(100, 400)), "indirect fire needs no sight line");
            int ticks = 0;
            while (battle.Gunnery.ShellsInFlight == 0 && ticks++ < 100)
            {
                battle.Step();
            }

            Assert.Greater(battle.Gunnery.ShellsInFlight, 0, "the first round is on its way");
            Assert.AreEqual(0, battle.Gunnery.RecentExplosions.Count);
            Run(battle, 3f);
            Assert.AreEqual(0, battle.Gunnery.RecentExplosions.Count, "still in the air after 3 s at 300 m");
            Run(battle, 4f);
            Assert.Greater(battle.Gunnery.RecentExplosions.Count, 0, "and then it lands");
        }

        [Test]
        public void MortarFiresSmokeRounds()
        {
            var battle = Battle();
            var mortar = battle.AddTeam("m", Side.German, TeamType.Mortar, "", new float2(100, 100), 0f, 0, 50, "grw34");
            Assert.IsTrue(battle.IssueOrder(mortar, OrderType.Smoke, new float2(100, 300)));
            Run(battle, 12f);
            Assert.AreEqual(1, battle.Smoke.Clouds.Count);
            Assert.AreEqual(Equipment.GrW34.StartingSmoke - 1, mortar.Gun.AmmoSmoke);
        }

        [Test]
        public void CrewBailsOutWhenHalfAreDown()
        {
            var battle = Battle();
            var tank = Tank(battle, "pz3j", Side.German, new float2(100, 100), 0f);
            tank.Members[1].Status = SoldierStatus.Dead;
            tank.Members[3].Status = SoldierStatus.Dead;
            tank.Members[4].Status = SoldierStatus.Incapacitated;

            battle.Step();
            Assert.IsTrue(tank.Vehicle.Abandoned);
            Assert.IsFalse(tank.IsMechanized, "the survivors fight on foot");
            foreach (var survivor in tank.Members.Where(m => m.IsActive))
            {
                Assert.IsFalse(survivor.IsMounted);
                Assert.IsNotNull(survivor.Weapon);
                Assert.Greater(math.distance(survivor.Position, tank.Vehicle.Position), 1f);
            }

            Assert.IsTrue(battle.IssueOrder(tank, OrderType.Move, new float2(100, 60)), "and take orders as infantry");
        }

        [Test]
        public void InfantryCloseAssaultsATankWithinGrenadeRange()
        {
            var battle = Battle();
            var de = battle.AddTeam("de", Side.German, TeamType.Rifle, "", new float2(100, 100), 0f, 6, 50);
            SetSteady(de);
            var t60 = Tank(battle, "t60", Side.Soviet, new float2(100, 118), 180f);
            Disarm(t60);
            battle.Visibility.Recompute();

            for (int i = 0; i < 200 && de.AntiTankGrenades > 0; i++)
            {
                battle.Step();
            }

            Assert.AreEqual(0, de.AntiTankGrenades, "a man went in with a bundled charge");
        }

        [Test]
        public void TankUsesCoaxAndHighExplosiveOnInfantry()
        {
            var battle = Battle();
            var tank = Tank(battle, "pz3j", Side.German, new float2(100, 100), 0f);
            var su = battle.AddTeam("su", Side.Soviet, TeamType.Rifle, "", new float2(100, 250), 180f, 6, 50);
            Disarm(su);
            battle.Visibility.Recompute();

            var gunner = tank.Members.First(m => m.Role == CrewRole.Gunner);
            int mg = gunner.Ammo;
            int he = tank.Gun.AmmoHe;
            Run(battle, 15f);
            Assert.Less(gunner.Ammo, mg, "coaxial MG");
            Assert.Less(tank.Gun.AmmoHe, he, "main gun HE");
        }

        [Test]
        public void AssaultGunSwingsItsHullToBearOnATarget()
        {
            var battle = Battle();
            var stug = Tank(battle, "stug3f", Side.German, new float2(100, 100), 0f);
            var su = battle.AddTeam("su", Side.Soviet, TeamType.Rifle, "", new float2(300, 100), 270f, 6, 50);
            Disarm(su);
            battle.Visibility.Recompute();

            Run(battle, 5f);
            Assert.Greater(stug.Vehicle.HullHeading, 60f, "the casemate gun only traverses 10 degrees: the hull turns east");
        }

        [Test]
        public void EnemyTankCloseByShakesInfantry()
        {
            var battle = Battle();
            var de = battle.AddTeam("de", Side.German, TeamType.Rifle, "", new float2(100, 100), 0f, 6, 50);
            SetSteady(de);
            var t34 = Tank(battle, "t34_76", Side.Soviet, new float2(100, 140), 180f);
            Disarm(t34);
            battle.Visibility.Recompute();

            Run(battle, 10f);
            Assert.Less(de.Members.Where(m => m.IsActive).Average(m => m.Morale), 78f);
        }
    }
}
