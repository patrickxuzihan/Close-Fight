using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using CloseFight.Core.AI;
using CloseFight.Core.Battle;
using CloseFight.Core.Map;
using CloseFight.Core.Sim;
using CloseFight.Core.Units;
using Unity.Mathematics;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace CloseFight.EditorTools
{
    /// <summary>
    /// How the simulation copes with a battalion on each side (M18, for the M17 maps):
    /// about a thousand men on a 1200 × 800 m city of dense blocks, the German battalion
    /// attacking, the Soviet one defending, both under the AI. Simulation only, no graphics:
    ///   -executeMethod CloseFight.EditorTools.BattalionBenchmark.RunFromCommandLine [-benchSeconds 300]
    /// </summary>
    public static class BattalionBenchmark
    {
        [MenuItem("Close Fight/Run Battalion Benchmark")]
        public static void RunFromMenu() => Run(120f);

        public static void RunFromCommandLine()
        {
            Run(float.Parse(Arg("-benchSeconds", "300"), CultureInfo.InvariantCulture));
        }

        static void Run(float seconds)
        {
            var watch = Stopwatch.StartNew();
            var map = MapBuilder.Build(City(1200f, 800f, 7));
            Report($"map build: {watch.Elapsed.TotalMilliseconds:0} ms, {map.Buildings.Count} buildings");

            var battle = new BattleState(map, 11);
            battle.Victory.TimeLimitSeconds = 1e6f;
            battle.SetRetreatEdge(Side.German, MapEdge.West);
            battle.SetRetreatEdge(Side.Soviet, MapEdge.East);
            for (int i = 0; i < 4; i++)
            {
                battle.AddObjective(new ObjectiveDefinition
                {
                    id = "o" + i, position = new MapPoint(720f + (i % 2) * 220f, 160f + i * 160f), radius = 15f, value = 2, owner = Side.Soviet,
                });
            }

            GermanBattalion(battle);
            SovietBattalion(battle);
            battle.AddCommander(new AttackCommander(battle, Side.German));
            battle.AddCommander(new DefenseCommander(battle, Side.Soviet));
            Report($"{battle.Platoons.Count} platoons, {battle.Teams.Count} teams, {battle.Soldiers.Count} soldiers " +
                   $"(German {battle.Soldiers.Count(s => s.Team.Side == Side.German)}, Soviet {battle.Soldiers.Count(s => s.Team.Side == Side.Soviet)})");

            watch.Restart();
            battle.Step();
            Report($"first tick (AI planning): {watch.Elapsed.TotalMilliseconds:0} ms");

            int ticks = (int)math.ceil(seconds / SimClock.TickSeconds);
            var times = new List<double>(ticks);
            for (int i = 0; i < ticks && !battle.Victory.IsOver; i++)
            {
                watch.Restart();
                battle.Step();
                times.Add(watch.Elapsed.TotalMilliseconds);
            }

            Report($"simulation over {times.Count} ticks ({battle.Clock.ElapsedSeconds:0} s): {Stats(times)}; " +
                   $"ticks over 30 ms: {times.Count(t => t > 30.0)}; over 100 ms: {times.Count(t => t > 100.0)}; " +
                   $"men left DE {battle.Soldiers.Count(s => s.IsActive && s.Team.Side == Side.German)} SU {battle.Soldiers.Count(s => s.IsActive && s.Team.Side == Side.Soviet)}");

            var samples = new List<double>();
            for (int i = 0; i < 20; i++)
            {
                watch.Restart();
                battle.Visibility.Recompute();
                samples.Add(watch.Elapsed.TotalMilliseconds);
            }

            Report($"visibility pass (every {Visibility.UpdateIntervalTicks} ticks): {Stats(samples)}");

            samples.Clear();
            foreach (var commander in battle.Commanders)
            {
                for (int i = 0; i < 5; i++)
                {
                    watch.Restart();
                    commander.Think();
                    samples.Add(watch.Elapsed.TotalMilliseconds);
                }

                Report($"{commander.GetType().Name} think: {Stats(samples)}");
                samples.Clear();
            }
        }

        /// <summary>Dense city blocks: 4–6 houses per 44 m block, 14 m streets, a few open squares.</summary>
        static MapDefinition City(float width, float height, uint seed)
        {
            var random = new Unity.Mathematics.Random(seed);
            var map = new MapDefinition { id = "bench_city", width = width, height = height, cellSize = 1f };
            int id = 1;
            const float block = 44f, street = 14f;
            for (float by = street; by + block < height; by += block + street)
            {
                for (float bx = 160f; bx + block < width - 40f; bx += block + street)
                {
                    if (random.NextFloat() < 0.12f)
                    {
                        continue; // a square or a ruined block
                    }

                    // Two rows of houses along the block's edges, with yards between.
                    for (int row = 0; row < 2; row++)
                    {
                        float x = bx;
                        while (x < bx + block - 8f)
                        {
                            float w = random.NextFloat(9f, 16f);
                            float d = random.NextFloat(10f, 16f);
                            float x1 = math.min(x + w, bx + block);
                            float y0 = row == 0 ? by : by + block - d;
                            map.buildings.Add(new BuildingDefinition
                            {
                                id = id++,
                                floors = random.NextInt(1, 5),
                                material = random.NextFloat() < 0.6f ? BuildingMaterial.Brick : BuildingMaterial.Wood,
                                polygon = new List<MapPoint> { new MapPoint(x, y0), new MapPoint(x1, y0), new MapPoint(x1, y0 + d), new MapPoint(x, y0 + d) },
                            });
                            x = x1 + random.NextFloat(1.5f, 5f);
                        }
                    }
                }
            }

            return map;
        }

        static float2 Spot(BattleState battle, float x, float y) => new float2(x, y);

        static void GermanBattalion(BattleState battle)
        {
            const Side de = Side.German;
            var stab = battle.AddPlatoon("de_stab", de, PlatoonType.Headquarters, "Btl.-Stab", "I./IR 194");
            Add(battle, stab, "de_btl", TeamType.Headquarters, 40f, 400f, 90f);
            Add(battle, stab, "de_fo1", TeamType.Observer, 60f, 300f, 90f);
            Add(battle, stab, "de_fo2", TeamType.Observer, 60f, 500f, 90f);
            for (int c = 0; c < 3; c++)
            {
                string company = $"{c + 1}./IR 194";
                float y = 140f + c * 260f;
                var kp = battle.AddPlatoon($"de_kp{c}", de, PlatoonType.Headquarters, "Kompanietrupp", company);
                Add(battle, kp, $"de_kp{c}_hq", TeamType.Headquarters, 70f, y, 90f);
                Add(battle, kp, $"de_kp{c}_sn", TeamType.Sniper, 100f, y + 20f, 90f);
                for (int z = 0; z < 3; z++)
                {
                    var zug = battle.AddPlatoon($"de_c{c}z{z}", de, PlatoonType.Rifle, $"{z + 1}. Zug", company);
                    for (int g = 0; g < 4; g++)
                    {
                        Add(battle, zug, $"de_c{c}z{z}g{g}", TeamType.Rifle, 110f + g * 10f, y - 70f + z * 60f + g * 6f, 90f);
                    }
                }
            }

            var mg = battle.AddPlatoon("de_mgk", de, PlatoonType.MachineGun, "sMG-Züge", "4./IR 194");
            for (int i = 0; i < 12; i++) Add(battle, mg, $"de_smg{i}", TeamType.MachineGun, 90f, 60f + i * 58f, 90f);
            var grw = battle.AddPlatoon("de_grw", de, PlatoonType.Mortar, "Granatwerfer-Zug", "4./IR 194");
            for (int i = 0; i < 6; i++) Add(battle, grw, $"de_grw{i}", TeamType.Mortar, 30f, 150f + i * 100f, 90f, "grw34");
            var pzj = battle.AddPlatoon("de_pzj", de, PlatoonType.AntiTank, "Panzerjäger-Zug", "14./IR 194");
            for (int i = 0; i < 3; i++) Add(battle, pzj, $"de_pak{i}", TeamType.AntiTankGun, 130f, 200f + i * 200f, 90f, "pak38");
            var stug = battle.AddPlatoon("de_stug", de, PlatoonType.Armor, "Sturmgeschütz-Zug", "StuG-Abt. 244");
            for (int i = 0; i < 3; i++) Add(battle, stug, $"de_stug{i}", TeamType.Vehicle, 50f, 250f + i * 150f, 90f, "stug3f");
            var pi = battle.AddPlatoon("de_pi", de, PlatoonType.Pioneer, "Pionier-Zug", "Pi.-Btl. 171");
            for (int i = 0; i < 3; i++) Add(battle, pi, $"de_pi{i}", TeamType.Engineer, 120f, 300f + i * 100f, 90f);
        }

        static void SovietBattalion(BattleState battle)
        {
            const Side su = Side.Soviet;
            var shtab = battle.AddPlatoon("su_shtab", su, PlatoonType.Headquarters, "Shtab bataliona", "1-y bat., 42-y gv. sp");
            Add(battle, shtab, "su_btl", TeamType.Headquarters, 1120f, 400f, 270f);
            Add(battle, shtab, "su_fo1", TeamType.Observer, 1000f, 300f, 270f);
            Add(battle, shtab, "su_fo2", TeamType.Observer, 1000f, 500f, 270f);
            for (int c = 0; c < 3; c++)
            {
                string company = $"{c + 1}-ya rota";
                float y = 140f + c * 260f;
                var hq = battle.AddPlatoon($"su_r{c}", su, PlatoonType.Headquarters, "Upravleniye roty", company);
                Add(battle, hq, $"su_r{c}_hq", TeamType.Headquarters, 1060f, y, 270f);
                Add(battle, hq, $"su_r{c}_sn", TeamType.Sniper, 900f, y + 30f, 270f);
                for (int v = 0; v < 3; v++)
                {
                    var vzvod = battle.AddPlatoon($"su_r{c}v{v}", su, PlatoonType.Rifle, $"{v + 1}-y vzvod", company);
                    for (int o = 0; o < 4; o++)
                    {
                        Add(battle, vzvod, $"su_r{c}v{v}o{o}", TeamType.Rifle, 700f + v * 120f + o * 12f, y - 80f + o * 40f, 270f);
                    }
                }
            }

            var pv = battle.AddPlatoon("su_pr", su, PlatoonType.MachineGun, "Pulemyotnaya rota", "1-y bat., 42-y gv. sp");
            for (int i = 0; i < 9; i++) Add(battle, pv, $"su_max{i}", TeamType.MachineGun, 820f, 80f + i * 75f, 270f);
            var mr = battle.AddPlatoon("su_mr", su, PlatoonType.Mortar, "Minomyotnaya rota", "1-y bat., 42-y gv. sp");
            for (int i = 0; i < 9; i++) Add(battle, mr, $"su_mor{i}", TeamType.Mortar, 1150f, 80f + i * 75f, 270f, "bm37");
            var pto = battle.AddPlatoon("su_pto", su, PlatoonType.AntiTank, "Vzvod PTO", "1-y bat., 42-y gv. sp");
            for (int i = 0; i < 2; i++) Add(battle, pto, $"su_at{i}", TeamType.AntiTankGun, 760f, 300f + i * 200f, 270f, "m1937_45");
            var sap = battle.AddPlatoon("su_sap", su, PlatoonType.Pioneer, "Sapyorny vzvod", "42-y gv. sp");
            for (int i = 0; i < 2; i++) Add(battle, sap, $"su_sap{i}", TeamType.Engineer, 900f, 350f + i * 100f, 270f);
        }

        static void Add(BattleState battle, Platoon platoon, string key, TeamType type, float x, float y, float facing, string equipment = null)
        {
            var team = battle.AddTeam(key, platoon.Side, type, key, Spot(battle, x, y), facing, BattleState.DefaultSize(type, platoon.Side), 50, equipment);
            battle.AssignToPlatoon(team, platoon);
        }

        static string Stats(List<double> values)
        {
            if (values.Count == 0)
            {
                return "no samples";
            }

            var sorted = values.OrderBy(v => v).ToList();
            double P(double q) => sorted[Math.Min(sorted.Count - 1, (int)(q * sorted.Count))];
            return $"mean {values.Average():0.00} ms, p50 {P(0.5):0.00}, p95 {P(0.95):0.00}, p99 {P(0.99):0.00}, max {sorted[sorted.Count - 1]:0.00}";
        }

        static void Report(string line) => Debug.Log("Close Fight battalion bench: " + line);

        static string Arg(string name, string fallback)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }

            return fallback;
        }
    }
}
