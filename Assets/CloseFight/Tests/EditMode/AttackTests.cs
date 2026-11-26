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
    public class AttackTests
    {
        const string DataRoot = "Assets/CloseFight/Data";

        /// <summary>
        /// 300 × 300 m. A brick house on the objective at (150, 200); the Germans
        /// start spread out along the south edge and attack north.
        /// </summary>
        static BattleState Battle(float2 sovietAt, bool houseOnObjective = true, ulong seed = 7)
        {
            var map = TestMaps.Empty(300f);
            if (houseOnObjective)
            {
                map.buildings.Add(new BuildingDefinition { id = 1, material = BuildingMaterial.Brick, floors = 2, polygon = TestMaps.Rect(142f, 194f, 158f, 206f) });
            }

            var battle = new BattleState(MapBuilder.Build(map), seed);
            battle.SetRetreatEdge(Side.German, MapEdge.South);
            battle.SetRetreatEdge(Side.Soviet, MapEdge.North);
            battle.AddObjective(new ObjectiveDefinition { id = "house", position = new MapPoint(150, 200), radius = 12f, value = 2, owner = Side.Soviet });
            battle.AddTeam("de_1", Side.German, TeamType.Rifle, "", new float2(60, 20), 0f, 6, 60);
            battle.AddTeam("de_2", Side.German, TeamType.Rifle, "", new float2(150, 20), 0f, 6, 60);
            battle.AddTeam("de_3", Side.German, TeamType.Rifle, "", new float2(240, 20), 0f, 6, 60);
            battle.AddTeam("de_mg", Side.German, TeamType.MachineGun, "", new float2(150, 30), 0f, 3, 60);
            var soviet = battle.AddTeam("su", Side.Soviet, TeamType.Rifle, "", sovietAt, 180f, 6, 50);
            battle.Victory.TimeLimitSeconds = 1e6f;
            battle.IssueOrder(soviet, OrderType.Hide, soviet.Centroid);

            // The rest of the Soviet company lies hidden and unarmed far off, so one team's fate does not end the battle.
            for (int i = 0; i < 3; i++)
            {
                var far = battle.AddTeam("su_far" + i, Side.Soviet, TeamType.Rifle, "", new float2(20 + i * 12, 296), 180f, 6, 50);
                Disarm(far);
                battle.IssueOrder(far, OrderType.Hide, far.Centroid);
            }

            return battle;
        }

        static AttackCommander Attack(BattleState battle)
        {
            var attack = new AttackCommander(battle, Side.German);
            battle.AddCommander(attack);
            return attack;
        }

        static void Disarm(Team team)
        {
            foreach (var m in team.Members) m.Ammo = 0;
        }

        [Test]
        public void TakesAnUndefendedObjective()
        {
            var battle = Battle(new float2(290, 295), houseOnObjective: false);
            Disarm(battle.Teams.Single(t => t.Key == "su"));
            var attack = Attack(battle);

            for (int i = 0; i < 9000 && battle.Objectives[0].Owner != Side.German; i++)
            {
                battle.Step();
            }

            for (int i = 0; i < 40; i++) battle.Step(); // the commander notices on its next think

            Assert.AreEqual(Side.German, battle.Objectives[0].Owner, "the attack walks into an empty objective");
            Assert.IsTrue(attack.Events.Any(e => e.Phase == AttackPhase.Consolidate));
        }

        [Test]
        public void AssaultTeamsGatherBeforeAdvancing()
        {
            var battle = Battle(new float2(290, 295), houseOnObjective: false);
            var attack = Attack(battle);
            battle.Step();
            Assert.AreEqual(AttackPhase.Assemble, attack.Phase, "spread 180 m apart, the rifle teams first gather");

            for (int i = 0; i < 3000 && attack.Phase == AttackPhase.Assemble; i++)
            {
                battle.Step();
            }

            Assert.AreEqual(AttackPhase.Advance, attack.Phase);
            var assault = attack.Plans.Where(p => p.Role == AttackRole.Assault).Select(p => p.Team.Centroid).ToArray();
            float spread = assault.Max(a => assault.Max(b => math.distance(a, b)));
            Assert.Less(spread, 60f, "the group moves off together (D16: mass on one point)");
        }

        [Test]
        public void ItDoesNotKnowWhatItHasNotSeen()
        {
            // The hidden defender stands in different places; the attack must not react to that (D17).
            float2[] Run(float2 sovietAt)
            {
                var battle = Battle(sovietAt, houseOnObjective: false);
                var attack = Attack(battle);
                for (int i = 0; i < 600; i++) battle.Step();
                Assert.AreEqual(0, attack.Contacts.Count, "precondition: the defender stayed hidden");
                return battle.Teams.Where(t => t.Side == Side.German).Select(t => t.Centroid).ToArray();
            }

            var a = Run(new float2(290, 295));
            var b = Run(new float2(10, 295));
            for (int i = 0; i < a.Length; i++)
            {
                Assert.Less(math.distance(a[i], b[i]), 1e-3f);
            }
        }

        [Test]
        public void JumpOffComesInFromTheFlank()
        {
            var battle = Battle(new float2(290, 295));
            var attack = Attack(battle);
            battle.Step();
            Assert.IsTrue(attack.HasGeometry);

            float2 o = battle.Objectives[0].Position;
            float cos = math.dot(math.normalizesafe(attack.Overwatch - o), math.normalizesafe(attack.JumpOff - o));
            Assert.Less(cos, math.cos(math.radians(30f)), "the assault does not come in along the base of fire's line");
        }

        [Test]
        public void SuppressesKnownEnemyBeforeTheAssault()
        {
            // A Soviet team in the house on the objective, in plain sight but with no ammunition.
            var battle = Battle(new float2(150, 200));
            var soviet = battle.Teams.Single(t => t.Key == "su");
            Disarm(soviet);
            battle.IssueOrder(soviet, OrderType.Defend, soviet.Centroid);
            var attack = Attack(battle);

            float firedAt = float.NaN;
            for (int i = 0; i < 9000 && !attack.Events.Any(e => e.Phase == AttackPhase.Assault); i++)
            {
                battle.Step();
                var mg = battle.Teams.Single(t => t.Key == "de_mg");
                if (float.IsNaN(firedAt) && mg.Members.Any(m => m.LastFiredAt > 0f && m.LastFiredAt <= battle.Clock.ElapsedSeconds))
                {
                    firedAt = battle.Clock.ElapsedSeconds;
                }
            }

            UnityEngine.Debug.Log("attack events: " + string.Join(" | ", attack.Events.Select(e => $"{e.Time:0} {e.Phase} {e.Note}"))
                + $" ; now {battle.Clock.ElapsedSeconds:0} phase {attack.Phase} jump-off {attack.JumpOff} assembly {attack.Assembly} teams "
                + string.Join(" ", attack.Plans.Select(p => $"{p.Team.Key}:{p.Role}:{p.Team.Centroid}:{p.Team.Activity}:{p.Team.Order.Type}:{p.Team.ActiveCount}")));
            var suppress = attack.Events.FirstOrDefault(e => e.Phase == AttackPhase.Suppress);
            var assault = attack.Events.FirstOrDefault(e => e.Phase == AttackPhase.Assault);
            Assert.AreEqual(AttackPhase.Suppress, suppress.Phase, "contact puts the attack into suppression");
            Assert.AreEqual(AttackPhase.Assault, assault.Phase, "precondition: the assault went in");
            Assert.IsFalse(float.IsNaN(firedAt), "the machine gun fired");
            Assert.Less(firedAt, assault.Time, "fire came before the assault");
            Assert.GreaterOrEqual(assault.Time - suppress.Time, 20f, "the enemy was kept under fire for a while first");
        }

        [Test]
        public void SetsUpForTheFirstObjectiveDuringDeployment()
        {
            var scenario = ScenarioLoader.FromJson(File.ReadAllText(Path.Combine(DataRoot, "Scenarios", "stalingrad_station_sept14.json")));
            var map = MapBuilder.Build(MapSerializer.FromJson(File.ReadAllText(Path.Combine(DataRoot, "Maps", scenario.mapId + ".json"))));
            var battle = ScenarioLoader.CreateBattle(scenario, map, playerSide: Side.Soviet, deployment: true);
            var attack = battle.Commanders.OfType<AttackCommander>().Single();
            Assert.IsNotNull(attack.Target, "a first objective is chosen before the battle");

            var assault = attack.Plans.Where(p => p.Role == AttackRole.Assault).Select(p => p.Team.Centroid).ToArray();
            float spread = assault.Max(a => assault.Max(b => math.distance(a, b)));
            // The assault teams set up in a line across the front, 12 m apart, each free to
            // pick cover nearby: gathered means that line plus 18 m at either end (60 m for
            // three teams; the M18 scenario has two platoons, seven teams).
            float gathered = (assault.Length - 1) * 12f + 36f;
            Assert.Less(spread, gathered, $"the {assault.Length} assault teams start gathered");
            foreach (var team in battle.Teams.Where(t => t.Side == Side.German))
            {
                Assert.IsTrue(battle.InDeployZone(Side.German, team.Vehicle != null ? team.Vehicle.Position : team.Centroid), $"{team.Key} stays in the zone");
            }
        }
    }
}
