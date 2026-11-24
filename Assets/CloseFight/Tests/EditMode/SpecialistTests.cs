using System.Collections.Generic;
using System.Linq;
using CloseFight.Core.Battle;
using CloseFight.Core.Map;
using CloseFight.Core.Path;
using CloseFight.Core.Units;
using NUnit.Framework;
using Unity.Mathematics;

namespace CloseFight.Tests
{
    /// <summary>Snipers and engineers (M12, D38/D39).</summary>
    public class SpecialistTests
    {
        static BattleState Battle(MapDefinition map = null, ulong seed = 21)
        {
            var battle = new BattleState(MapBuilder.Build(map ?? TestMaps.Empty(300f)), seed);
            battle.Victory.TimeLimitSeconds = 1e6f;
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

        // ------------------------------------------------------------ snipers

        [Test]
        public void SnipersAreIssuedScopedRiflesAndEngineersAFlamethrower()
        {
            var battle = Battle();
            var sniper = battle.AddTeam("s", Side.German, TeamType.Sniper, "", new float2(50, 50), 0f, BattleState.DefaultSize(TeamType.Sniper), 60);
            var engineers = battle.AddTeam("e", Side.Soviet, TeamType.Engineer, "", new float2(150, 150), 0f, BattleState.DefaultSize(TeamType.Engineer), 60);
            Assert.AreEqual(2, sniper.Members.Count);
            Assert.AreEqual(WeaponClass.SniperRifle, sniper.Members[0].Weapon.Class);
            Assert.IsTrue(engineers.Members.Any(m => m.Weapon.Class == WeaponClass.Flamethrower));
            Assert.AreEqual(2, engineers.DemolitionCharges);
            Assert.AreEqual(2, engineers.MineLoads);
        }

        [Test]
        public void SniperShootsTheOfficerBeforeCloserRiflemen()
        {
            var battle = Battle();
            var sniper = battle.AddTeam("s", Side.German, TeamType.Sniper, "", new float2(150, 20), 0f, 2, 80);
            sniper.Members[1].Ammo = 0; // only the sniper shoots
            var enemy = battle.AddTeam("su", Side.Soviet, TeamType.Rifle, "", new float2(150, 200), 180f, 6, 50);
            Disarm(enemy);
            // The officer stands well behind his men.
            enemy.Leader.SetPosition(new float2(150, 260));
            foreach (var m in enemy.Members) m.Posture = Posture.Standing;
            battle.Visibility.Recompute();

            int officerShots = 0, otherShots = 0;
            float seen = -1f;
            var officer = enemy.Leader;
            for (int i = 0; i < 1200 && officer.IsActive; i++)
            {
                battle.Step();
                foreach (var shot in battle.Combat.RecentShots.Where(s => s.ShooterId == sniper.Members[0].Id && s.Time > seen))
                {
                    if (math.distance(shot.Impact, officer.Position) < 4f) officerShots++;
                    else otherShots++;
                    seen = shot.Time;
                }
            }

            Assert.Greater(officerShots, 0, "precondition: the sniper fired");
            Assert.AreEqual(0, otherShots, "every shot went at the officer, 60 m further off than his men");
        }

        [Test]
        public void ASniperKillShakesTheTeamMoreThanARifleKill()
        {
            float MateMorale(HitCause cause)
            {
                var battle = Battle();
                var team = battle.AddTeam("su", Side.Soviet, TeamType.Rifle, "", new float2(150, 150), 0f, 6, 50);
                foreach (var m in team.Members) m.Morale = 80f;
                battle.Combat.InflictHit(team.Members[3], 1f, 0f, null, cause);
                return team.Members.Where((m, i) => i != 3 && m.IsActive).Average(m => m.Morale);
            }

            Assert.Less(MateMorale(HitCause.Sniper), MateMorale(HitCause.Gunfire) - 3f);
        }

        [Test]
        public void HiddenSniperIsHarderToSpotThanARifleman()
        {
            var battle = Battle();
            var sniper = battle.AddTeam("s", Side.German, TeamType.Sniper, "", new float2(50, 50), 0f, 2, 60);
            var rifles = battle.AddTeam("r", Side.German, TeamType.Rifle, "", new float2(80, 50), 0f, 6, 60);
            float s = Visibility.SpottingRange(sniper.Members[0], battle.Map, 0f);
            float r = Visibility.SpottingRange(rifles.Members[1], battle.Map, 0f);
            Assert.Less(s, r * 0.7f);
        }

        // ------------------------------------------------------------ flamethrower

        [Test]
        public void FlamethrowerBurnsOutDefendersInAHouseOnlyAtCloseRange()
        {
            var battle = Battle(TestMaps.HouseAndTower());
            var engineers = battle.AddTeam("e", Side.German, TeamType.Engineer, "", new float2(25, 6), 0f, 5, 60);
            foreach (var m in engineers.Members) if (m.Weapon.Class != WeaponClass.Flamethrower) m.Ammo = 0;
            var flamer = engineers.Members.First(m => m.Weapon.Class == WeaponClass.Flamethrower);
            var defenders = battle.AddTeam("su", Side.Soviet, TeamType.Rifle, "", new float2(25, 23), 180f, 6, 50);
            Disarm(defenders);
            battle.IssueOrder(defenders, OrderType.Defend, defenders.Centroid);

            // First out of reach (38 m from the men inside), then close in (15 m).
            foreach (var m in engineers.Members) m.SetPosition(new float2(25, -15) + new float2(m.Id % 3, 0));
            battle.Visibility.Recompute();
            Run(battle, 10f);
            Assert.AreEqual(8, flamer.Ammo, "no flame at 38 m");

            foreach (var m in engineers.Members) m.SetPosition(new float2(25, 8) + new float2(m.Id % 3, 0));
            battle.Visibility.Recompute();
            Run(battle, 20f);
            Assert.Less(flamer.Ammo, 8, "the flamethrower fires within its reach");
            int down = defenders.Members.Count(m => !m.IsActive);
            Assert.Greater(down, 1, "flame poured into the house takes several men despite the walls");
        }

        // ------------------------------------------------------------ demolition

        /// <summary>200 × 100 m; a solid wall from (0,50) to (200,50) splits the map, with a gap at the far east.</summary>
        static MapDefinition WallMap()
        {
            var map = TestMaps.Empty(200f);
            map.height = 100f;
            map.walls.Add(new WallDefinition
            {
                points = new List<MapPoint> { new MapPoint(0, 50), new MapPoint(190, 50) },
                height = 2.5f,
                blocksMovement = true,
                crushable = false,
            });
            return map;
        }

        [Test]
        public void EngineersBlowAHoleInAWallAndOpenANewWay()
        {
            var battle = Battle(WallMap());
            var engineers = battle.AddTeam("e", Side.German, TeamType.Engineer, "", new float2(40, 40), 0f, 5, 60);
            var pathfinder = new GridPathfinder(battle.Map);
            var grid = battle.Map.Grid;
            var cells = new List<GridCoord>();
            pathfinder.FindPath(grid.WorldToCell(new float2(40, 40)), grid.WorldToCell(new float2(40, 60)), MovementClass.Infantry, cells);
            int before = cells.Count;
            Assert.Greater(before, 200, "precondition: the way round goes via the gap 150 m east");

            Assert.IsTrue(battle.IssueOrder(engineers, OrderType.Demolish, new float2(40, 50)));
            Run(battle, Engineering.DemolishSeconds + Engineering.FuseSeconds + 15f);

            Assert.AreEqual(1, engineers.DemolitionCharges, "one charge used");
            pathfinder.FindPath(grid.WorldToCell(new float2(40, 40)), grid.WorldToCell(new float2(40, 60)), MovementClass.Infantry, cells);
            Assert.Less(cells.Count, 40, "straight through the breach now");
            Assert.AreEqual(5, engineers.ActiveCount, "the team pulled back before the charge went off");
        }

        [Test]
        public void AChargeOnAHouseBringsItDownOnTheDefenders()
        {
            var battle = Battle(TestMaps.HouseAndTower());
            var engineers = battle.AddTeam("e", Side.German, TeamType.Engineer, "", new float2(25, 8), 0f, 5, 60);
            Disarm(engineers);
            var defenders = battle.AddTeam("su", Side.Soviet, TeamType.Rifle, "", new float2(25, 22), 180f, 6, 50);
            Disarm(defenders);

            Assert.IsTrue(battle.IssueOrder(engineers, OrderType.Demolish, new float2(25, 21)));
            for (int i = 0; i < 600 && engineers.DemolitionCharges == 2; i++) battle.Step();
            Assert.AreEqual(1, engineers.DemolitionCharges, "precondition: the charge was planted");
            Run(battle, Engineering.FuseSeconds + 0.5f);

            var grid = battle.Map.Grid;
            Assert.IsTrue(grid.HasFlag(grid.IndexOf(grid.WorldToCell(new float2(25, 21))), CellFlags.Collapsed), "the charge collapses that part of the house");
            Assert.Greater(defenders.Members.Count(m => !m.IsActive || m.Suppression > 30f), 2, "the men inside are hurt or stunned");
        }

        [Test]
        public void WorkStopsWhileTheTeamIsPinned()
        {
            var battle = Battle(WallMap());
            var engineers = battle.AddTeam("e", Side.German, TeamType.Engineer, "", new float2(40, 47), 0f, 5, 60);
            Assert.IsTrue(battle.IssueOrder(engineers, OrderType.LayMines, engineers.Centroid));
            for (int i = 0; i < 300; i++)
            {
                foreach (var m in engineers.Members) { m.Suppression = 90f; m.LastUnderFireAt = battle.Clock.ElapsedSeconds; }
                battle.Step();
            }

            Assert.AreEqual(0, battle.Engineering.MineFields.Count, "30 s under heavy fire: no mines laid");
            Assert.IsNotNull(engineers.Task);
        }

        // ------------------------------------------------------------ mines

        [Test]
        public void EngineersLayMinesThatOnlyTheirSideKnowsAbout()
        {
            var battle = Battle();
            var engineers = battle.AddTeam("e", Side.Soviet, TeamType.Engineer, "", new float2(100, 100), 0f, 5, 60);
            Assert.IsTrue(battle.IssueOrder(engineers, OrderType.LayMines, new float2(110, 100)));
            Run(battle, Engineering.LayMinesSeconds + 15f);

            Assert.AreEqual(1, battle.Engineering.MineFields.Count);
            var field = battle.Engineering.MineFields[0];
            Assert.IsTrue(field.KnownTo(Side.Soviet));
            Assert.IsFalse(field.KnownTo(Side.German));
            Assert.AreEqual(1, engineers.MineLoads);
        }

        [Test]
        public void SoldiersCrossingAMinefieldSetMinesOffAndRevealIt()
        {
            var battle = Battle(seed: 5);
            var field = battle.Engineering.AddMineField(Side.Soviet, new float2(150, 100));
            var rifles = battle.AddTeam("de", Side.German, TeamType.Rifle, "", new float2(150, 60), 0f, 6, 50);
            Assert.IsTrue(battle.IssueOrder(rifles, OrderType.Move, new float2(150, 140)));
            Run(battle, 60f);

            Assert.IsTrue(field.KnownTo(Side.German) || !battle.Engineering.MineFields.Contains(field), "the field announced itself");
            Assert.Less(rifles.ActiveCount, 6, "walking straight through a minefield costs men");
        }

        [Test]
        public void TanksLoseATrackOnMines()
        {
            var battle = Battle(seed: 3);
            battle.Engineering.AddMineField(Side.Soviet, new float2(150, 100));
            var tank = battle.AddTeam("stug", Side.German, TeamType.Vehicle, "", new float2(150, 60), 0f, 4, 50, "stug3f");
            Assert.IsTrue(battle.IssueOrder(tank, OrderType.Move, new float2(150, 150)));
            Run(battle, 60f);
            Assert.IsTrue(tank.Vehicle.Immobilized);
        }

        [Test]
        public void KnownMinefieldsAreGoneRoundAndCanBeCleared()
        {
            var battle = Battle();
            var field = battle.Engineering.AddMineField(Side.Soviet, new float2(150, 100));
            field.Reveal(Side.German);
            var rifles = battle.AddTeam("de", Side.German, TeamType.Rifle, "", new float2(150, 60), 0f, 6, 50);
            Assert.IsTrue(battle.IssueOrder(rifles, OrderType.Move, new float2(150, 140)));
            Assert.IsFalse(rifles.CurrentPath.Any(p => field.Contains(p)), "the route goes round the known field");

            var engineers = battle.AddTeam("e", Side.German, TeamType.Engineer, "", new float2(140, 80), 0f, 5, 60);
            Assert.IsTrue(battle.IssueOrder(engineers, OrderType.ClearMines, field.Center));
            Run(battle, 70f);
            Assert.IsFalse(battle.Engineering.MineFields.Contains(field), "cleared");
        }

        [Test]
        public void OnlyEngineersTakeEngineeringOrders()
        {
            var battle = Battle(WallMap());
            var rifles = battle.AddTeam("r", Side.German, TeamType.Rifle, "", new float2(40, 40), 0f, 6, 50);
            Assert.IsFalse(battle.IssueOrder(rifles, OrderType.Demolish, new float2(40, 50)));
            Assert.IsFalse(battle.IssueOrder(rifles, OrderType.LayMines, new float2(40, 30)));
        }
    }
}
