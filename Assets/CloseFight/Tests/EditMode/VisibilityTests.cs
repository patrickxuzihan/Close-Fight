using CloseFight.Core.Battle;
using CloseFight.Core.Map;
using CloseFight.Core.Units;
using NUnit.Framework;
using Unity.Mathematics;

namespace CloseFight.Tests
{
    public class VisibilityTests
    {
        static BattleState Battle(MapDefinition map) => new BattleState(MapBuilder.Build(map), 11);

        static MapDefinition Open(float size = 600f) => TestMaps.Empty(size);

        [Test]
        public void EnemyInOpenWithinRangeIsSeen()
        {
            var battle = Battle(Open());
            battle.AddTeam("de", Side.German, TeamType.MachineGun, "", new float2(100, 100), 0f, 3, 50);
            var su = battle.AddTeam("su", Side.Soviet, TeamType.MachineGun, "", new float2(100, 250), 180f, 3, 50);

            battle.Visibility.Recompute();
            // Kneeling (spawn posture) spotting range is 250 m; 150 m away in the open.
            Assert.IsTrue(battle.Visibility.CanSee(Side.German, su.Members[0]));
            Assert.IsTrue(battle.Visibility.CanSee(Side.Soviet, battle.Teams[0].Members[0]));
        }

        [Test]
        public void OwnSoldiersAreAlwaysVisible()
        {
            var battle = Battle(Open());
            var de = battle.AddTeam("de", Side.German, TeamType.MachineGun, "", new float2(100, 100), 0f, 3, 50);
            battle.Visibility.Recompute();
            Assert.IsTrue(battle.Visibility.CanSee(Side.German, de.Members[0]));
        }

        [Test]
        public void BuildingBlocksSight()
        {
            var map = TestMaps.Empty(100f);
            map.buildings.Add(new BuildingDefinition { id = 1, floors = 3, polygon = TestMaps.Rect(40, 30, 60, 70) });
            var battle = Battle(map);
            battle.AddTeam("de", Side.German, TeamType.MachineGun, "", new float2(20, 50), 90f, 3, 50);
            var su = battle.AddTeam("su", Side.Soviet, TeamType.MachineGun, "", new float2(80, 50), 270f, 3, 50);

            battle.Visibility.Recompute();
            Assert.IsFalse(battle.Visibility.CanSee(Side.German, su.Members[0]));
        }

        [Test]
        public void ProneFarTargetHiddenButStandingSeen()
        {
            var battle = Battle(Open());
            battle.AddTeam("de", Side.German, TeamType.MachineGun, "", new float2(100, 100), 0f, 3, 50);
            var su = battle.AddTeam("su", Side.Soviet, TeamType.MachineGun, "", new float2(100, 300), 180f, 3, 50);

            battle.IssueOrder(su, OrderType.Hide, float2.zero); // prone + hidden
            battle.Visibility.Recompute();
            Assert.IsFalse(battle.Visibility.CanSee(Side.German, su.Members[0]), "200 m, prone and hiding");

            // Standing and moving: 400 m * 1.5 range.
            battle.IssueOrder(su, OrderType.MoveFast, new float2(100, 320));
            battle.Step();
            battle.Visibility.Recompute();
            Assert.IsTrue(battle.Visibility.CanSee(Side.German, su.Members[0]), "running upright");
        }

        [Test]
        public void SightIsLostWhenTargetGoesBehindWall()
        {
            var map = TestMaps.Empty(100f);
            map.walls.Add(new WallDefinition { points = { new MapPoint(50, 0), new MapPoint(50, 60) }, height = 2.5f });
            var battle = Battle(map);
            battle.AddTeam("de", Side.German, TeamType.MachineGun, "", new float2(20, 20), 90f, 3, 50);
            var su = battle.AddTeam("su", Side.Soviet, TeamType.MachineGun, "", new float2(40, 80), 180f, 3, 50);

            battle.Visibility.Recompute();
            Assert.IsTrue(battle.Visibility.CanSee(Side.German, su.Members[0]), "north of the wall's end, in the open");

            // Walk behind the wall (east side, low down).
            battle.IssueOrder(su, OrderType.MoveFast, new float2(70, 20));
            for (int i = 0; i < 600 && su.Activity != TeamActivity.Idle; i++)
            {
                battle.Step();
            }

            battle.Visibility.Recompute();
            Assert.IsFalse(battle.Visibility.CanSee(Side.German, su.Members[0]), "hidden by the 2.5 m wall");
        }

        [Test]
        public void VisibilityRefreshesDuringSteps()
        {
            var battle = Battle(Open());
            battle.AddTeam("de", Side.German, TeamType.MachineGun, "", new float2(100, 100), 0f, 3, 50);
            var su = battle.AddTeam("su", Side.Soviet, TeamType.MachineGun, "", new float2(100, 200), 180f, 3, 50);
            Assert.IsFalse(battle.Visibility.CanSee(Side.German, su.Members[0]), "not computed yet");

            for (int i = 0; i < Visibility.UpdateIntervalTicks; i++)
            {
                battle.Step();
            }

            Assert.IsTrue(battle.Visibility.CanSee(Side.German, su.Members[0]));
        }

        [Test]
        public void UnseenGunfireLeavesFuzzySoundMarkerThatExpires()
        {
            var map = TestMaps.Empty(200f);
            map.buildings.Add(new BuildingDefinition { id = 1, floors = 4, polygon = TestMaps.Rect(80, 0, 120, 200) });
            var battle = Battle(map);
            battle.AddTeam("de", Side.German, TeamType.MachineGun, "", new float2(40, 100), 90f, 3, 50);
            var su = battle.AddTeam("su", Side.Soviet, TeamType.MachineGun, "", new float2(160, 100), 270f, 3, 50);
            battle.Visibility.Recompute();

            var shooter = su.Members[0];
            battle.Visibility.ReportSound(shooter, shooter.Position, 400f, 15f);

            Assert.AreEqual(1, battle.Visibility.SoundMarkers.Count, "only the Germans, who cannot see the shooter, hear him");
            var marker = battle.Visibility.SoundMarkers[0];
            Assert.AreEqual(Side.German, marker.Listener);
            Assert.LessOrEqual(math.distance(marker.Position, shooter.Position), 15.01f);

            for (int i = 0; i < (int)(Visibility.SoundMarkerLifetime / 0.1f) + 2; i++)
            {
                battle.Step();
            }

            Assert.AreEqual(0, battle.Visibility.SoundMarkers.Count);
        }

        [Test]
        public void SeenShooterLeavesNoSoundMarker()
        {
            var battle = Battle(Open());
            battle.AddTeam("de", Side.German, TeamType.MachineGun, "", new float2(100, 100), 0f, 3, 50);
            var su = battle.AddTeam("su", Side.Soviet, TeamType.MachineGun, "", new float2(100, 200), 180f, 3, 50);
            battle.Visibility.Recompute();

            battle.Visibility.ReportSound(su.Members[0], su.Members[0].Position, 400f, 15f);
            Assert.AreEqual(0, battle.Visibility.SoundMarkers.Count);
        }
    }
}
