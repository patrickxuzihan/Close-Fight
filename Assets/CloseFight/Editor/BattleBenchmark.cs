using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using CloseFight.Core.Battle;
using CloseFight.Core.Sim;
using CloseFight.Core.Units;
using CloseFight.Game.Battle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace CloseFight.EditorTools
{
    /// <summary>
    /// Measures where a frame's time goes on the station battle, from the
    /// command line (batch mode, without -nographics so the GPU renders):
    ///   -executeMethod CloseFight.EditorTools.BattleBenchmark.RunFromCommandLine
    ///   [-benchSeconds 300] [-benchWidth 1920] [-benchHeight 1200]
    /// Reports simulation ticks (mean and spikes), AI and visibility passes,
    /// the per-frame view sync, and GPU render time per frame.
    /// </summary>
    public static class BattleBenchmark
    {
        [MenuItem("Close Fight/Run Benchmark")]
        public static void RunFromMenu() => Run(120f, 1920, 1200);

        public static void RunFromCommandLine()
        {
            float seconds = float.Parse(Arg("-benchSeconds", "300"), CultureInfo.InvariantCulture);
            int width = int.Parse(Arg("-benchWidth", "1920"), CultureInfo.InvariantCulture);
            int height = int.Parse(Arg("-benchHeight", "1200"), CultureInfo.InvariantCulture);
            Run(seconds, width, height);
        }

        static void Run(float seconds, int width, int height)
        {
            EditorSceneManager.OpenScene(BattleSceneSetup.ScenePath, OpenSceneMode.Single);
            var bootstrap = Object.FindAnyObjectByType<BattleBootstrap>();
            var watch = Stopwatch.StartNew();
            var battle = bootstrap.Build();
            Report($"build (map, views, scenario): {watch.Elapsed.TotalMilliseconds:0} ms; " +
                   $"{battle.Teams.Count} teams, {battle.Soldiers.Count} soldiers");

            // The first tick plans the AI (position analysis); time it on its own.
            watch.Restart();
            battle.Step();
            Report($"first tick (AI planning): {watch.Elapsed.TotalMilliseconds:0} ms");

            BattleCapture.IssueDemoOrders(battle);
            int ticks = Mathf.CeilToInt(seconds / SimClock.TickSeconds);
            var times = new List<double>(ticks);
            for (int i = 0; i < ticks && !battle.Victory.IsOver; i++)
            {
                watch.Restart();
                battle.Step();
                times.Add(watch.Elapsed.TotalMilliseconds);
            }

            Report($"simulation over {times.Count} ticks ({battle.Clock.ElapsedSeconds:0} s of battle): {Stats(times)}; " +
                   $"ticks over 30 ms: {times.Count(t => t > 30.0)}; result {battle.Victory.Result}");

            // Passes that run on a cadence, timed in isolation.
            var samples = new List<double>();
            for (int i = 0; i < 20; i++)
            {
                watch.Restart();
                battle.Visibility.Recompute();
                samples.Add(watch.Elapsed.TotalMilliseconds);
            }

            Report($"visibility pass (every {Visibility.UpdateIntervalTicks} ticks): {Stats(samples)}");

            samples.Clear();
            for (int i = 0; i < 10; i++)
            {
                watch.Restart();
                foreach (var commander in battle.Commanders)
                {
                    commander.Think();
                }

                samples.Add(watch.Elapsed.TotalMilliseconds);
            }

            Report($"AI think (every {BattleState.AiIntervalTicks} ticks): {Stats(samples)}");

            // View sync, as done once per rendered frame.
            var units = bootstrap.UnitsView;
            units.SetViewSide(Side.German);
            samples.Clear();
            for (int i = 0; i < 200; i++)
            {
                watch.Restart();
                units.Sync(0.5f);
                samples.Add(watch.Elapsed.TotalMilliseconds);
            }

            Report($"view sync per frame: {Stats(samples)}");

            // GPU: render the default framing; reading one pixel back waits for the frame to finish.
            var camera = Camera.main;
            camera.aspect = width / (float)height;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var probe = new Texture2D(1, 1, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            camera.targetTexture = target;
            samples.Clear();
            for (int i = 0; i < 70; i++)
            {
                watch.Restart();
                camera.Render();
                RenderTexture.active = target;
                probe.ReadPixels(new Rect(0, 0, 1, 1), 0, 0);
                probe.Apply();
                if (i >= 10)
                {
                    samples.Add(watch.Elapsed.TotalMilliseconds);
                }
            }

            RenderTexture.active = previous;
            camera.targetTexture = null;
            target.Release();
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(probe);
            Report($"render {width}x{height} (camera + GPU): {Stats(samples)}");
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

        static void Report(string line) => Debug.Log("Close Fight bench: " + line);

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
