using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using CloseFight.Core.Battle;
using CloseFight.Core.Map;
using CloseFight.Core.Sim;
using CloseFight.Core.Units;
using CloseFight.Game.Battle;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CloseFight.EditorTools
{
    /// <summary>
    /// Renders the battle scene to a PNG without entering play mode, so changes can
    /// be checked visually from the command line (batch mode, without -nographics):
    ///   -executeMethod CloseFight.EditorTools.BattleCapture.CaptureFromCommandLine
    ///   -captureOut path.png [-captureWidth 1600] [-captureHeight 1000]
    ///   [-captureOrthoSize 40] [-captureCenter 80,60]
    ///   [-captureViews "x,y,size;x,y,size"]  several close-ups: path_0.png, path_1.png, ...
    /// </summary>
    public static class BattleCapture
    {
        [MenuItem("Close Fight/Capture Battle Screenshot")]
        public static void CaptureFromMenu()
        {
            Capture("Temp/battle_capture.png", 1600, 1000, 0f, null);
            EditorUtility.RevealInFinder("Temp/battle_capture.png");
        }

        public static void CaptureFromCommandLine()
        {
            string path = Arg("-captureOut", "battle_capture.png");
            int width = int.Parse(Arg("-captureWidth", "1600"), CultureInfo.InvariantCulture);
            int height = int.Parse(Arg("-captureHeight", "1000"), CultureInfo.InvariantCulture);
            float orthoSize = float.Parse(Arg("-captureOrthoSize", "0"), CultureInfo.InvariantCulture);

            Vector2? center = null;
            string centerArg = Arg("-captureCenter", null);
            if (!string.IsNullOrEmpty(centerArg))
            {
                var parts = centerArg.Split(',');
                center = new Vector2(
                    float.Parse(parts[0], CultureInfo.InvariantCulture),
                    float.Parse(parts[1], CultureInfo.InvariantCulture));
            }

            float demoSeconds = float.Parse(Arg("-captureDemoSeconds", "0"), CultureInfo.InvariantCulture);
            Capture(path, width, height, orthoSize, center, demoSeconds);
        }

        static void Capture(string path, int width, int height, float orthoSize, Vector2? center, float demoSeconds = 0f)
        {
            EditorSceneManager.OpenScene(BattleSceneSetup.ScenePath, OpenSceneMode.Single);

            var camera = Camera.main;
            if (camera == null)
            {
                throw new InvalidOperationException("No main camera in the battle scene.");
            }

            camera.aspect = width / (float)height;

            var bootstrap = Object.FindAnyObjectByType<BattleBootstrap>();
            var battle = bootstrap != null ? bootstrap.Build() : null;
            if (battle == null)
            {
                throw new InvalidOperationException("Battle scene could not be built.");
            }

            if (demoSeconds > 0f)
            {
                RunDemo(bootstrap, battle, demoSeconds);
            }

            // Check the German flag: put a (harmless) German team on the first objective nobody holds.
            if (Arg("-captureGermanFlag", null) != null)
            {
                foreach (var objective in battle.Objectives)
                {
                    if (battle.Soldiers.Any(s => s.IsActive && objective.Contains(s.Position)))
                    {
                        continue;
                    }

                    battle.AddTeam("flag_test", Side.German, TeamType.Rifle, "", objective.Position, 0f, 2, 50);
                    battle.Victory.UpdateControl();
                    bootstrap.UnitsView.Sync(1f);
                    Debug.Log($"Close Fight: {objective.Id} now held by {objective.Owner}");
                    break;
                }
            }

            string views = Arg("-captureViews", null);
            if (!string.IsNullOrEmpty(views))
            {
                var list = views.Split(';');
                for (int i = 0; i < list.Length; i++)
                {
                    var v = list[i].Split(',');
                    float x = float.Parse(v[0], CultureInfo.InvariantCulture);
                    float y = float.Parse(v[1], CultureInfo.InvariantCulture);
                    float size = float.Parse(v[2], CultureInfo.InvariantCulture);
                    string viewPath = Path.Combine(Path.GetDirectoryName(path) ?? "", Path.GetFileNameWithoutExtension(path) + "_" + i + ".png");
                    RenderTo(camera, viewPath, width, height, size, new Vector2(x, y));
                }

                return;
            }

            RenderTo(camera, path, width, height, orthoSize, center);
        }

        static void RenderTo(Camera camera, string path, int width, int height, float orthoSize, Vector2? center)
        {
            if (orthoSize > 0f)
            {
                camera.orthographicSize = orthoSize;
            }

            if (center.HasValue)
            {
                var p = camera.transform.position;
                camera.transform.position = new Vector3(center.Value.x, p.y, center.Value.y);
            }

            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
            {
                antiAliasing = 4,
            };

            var previousActive = RenderTexture.active;
            camera.targetTexture = target;
            camera.Render();

            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();

            RenderTexture.active = previousActive;
            camera.targetTexture = null;

            string directory = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(path, image.EncodeToPNG());
            Object.DestroyImmediate(image);
            target.Release();
            Object.DestroyImmediate(target);

            Debug.Log("Close Fight: captured " + Path.GetFullPath(path));
        }

        /// <summary>Scripted German orders for the station or test map; returns the teams that got one.</summary>
        internal static List<Team> IssueDemoOrders(BattleState battle)
        {
            bool station = battle.Map.Definition.id == "stalingrad_station";
            var orders = station ? new (string key, OrderType order, float x, float y)[]
            {
                ("de_stug", OrderType.Move, 86f, 62f),        // along the southern avenue to the yard
                ("de_mortar", OrderType.AreaFire, 79f, 130f), // shell the signal box
                ("de_1", OrderType.Move, 86f, 40f),
                ("de_2", OrderType.Move, 66f, 100f),
                ("de_3", OrderType.Crawl, 60f, 160f),
                ("de_mg1", OrderType.Move, 56f, 118f),
                ("de_4", OrderType.Move, 80f, 232f),
            } : new (string key, OrderType order, float x, float y)[]
            {
                ("de_stug", OrderType.Move, 77f, 50f),        // the assault gun leads up the road
                ("de_mortar", OrderType.AreaFire, 59f, 82f),  // mortar the concrete block
                ("de_1", OrderType.Move, 56f, 48f),
                ("de_2", OrderType.MoveFast, 112f, 46f),
                ("de_mg", OrderType.Move, 86f, 40f),
                ("de_hq", OrderType.Move, 76f, 30f),
            };

            var selected = new List<Team>();
            foreach (var (key, order, x, y) in orders)
            {
                var team = battle.Teams.FirstOrDefault(t => t.Key == key);
                if (team == null)
                {
                    continue;
                }

                if (!battle.IssueOrder(team, order, new float2(x, y)))
                {
                    Debug.LogWarning($"Demo order {order} for {key} failed.");
                }

                if (!selected.Contains(team))
                {
                    selected.Add(team);
                }
            }

            return selected;
        }

        /// <summary>
        /// Scripted orders for the German teams of the test scenario, then a few
        /// seconds of simulation, so a screenshot shows movement, file order and routes.
        /// </summary>
        static void RunDemo(BattleBootstrap bootstrap, BattleState battle, float seconds)
        {
            bootstrap.UnitsView.TracerLifetime = 1f;
            var selected = IssueDemoOrders(battle);

            int ticks = Mathf.CeilToInt(seconds / SimClock.TickSeconds);
            var watch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < ticks && !battle.Victory.IsOver; i++)
            {
                battle.Step();
            }

            watch.Stop();
            Debug.Log($"Close Fight: simulated {battle.Clock.ElapsedSeconds:0} s of battle in {watch.ElapsedMilliseconds} ms " +
                      $"({battle.Clock.TickCount * 1000f / math.max(1, watch.ElapsedMilliseconds):0} ticks/s; real time needs 10). " +
                      $"Result {battle.Victory.Result} ({battle.Victory.EndReasonKey}), points DE {battle.Victory.Points(Side.German)} SU {battle.Victory.Points(Side.Soviet)}, " +
                      $"force morale DE {battle.Victory.ForceMorale(Side.German):0} SU {battle.Victory.ForceMorale(Side.Soviet):0}");

            // Optional: shell the concrete block to show the collapse decal.
            if (Arg("-captureBlast", null) != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    battle.Gunnery.Blast(new float2(55.5f, 75.5f), Equipment.StuK40, battle.Clock.ElapsedSeconds);
                    battle.Gunnery.Blast(new float2(62.5f, 80.5f), Equipment.StuK40, battle.Clock.ElapsedSeconds);
                }
            }

            // Fog of war as the German player sees it, plus one burst of unseen Soviet fire.
            battle.Visibility.Recompute();
            var soviet = battle.Teams.FirstOrDefault(t => t.Key == "su_mg");
            if (soviet != null && soviet.Leader != null)
            {
                battle.Visibility.ReportSound(soviet.Leader, soviet.Leader.Position, 400f, 12f);
            }

            bootstrap.UnitsView.SetViewSide(Side.German);
            bootstrap.UnitsView.ShowAiPlan = Arg("-captureAiPlan", null) != null;
            bootstrap.UnitsView.SetSelection(selected);
            bootstrap.UnitsView.Sync(1f);

            // Show the LOS tool from the first rifle team toward the station block.
            var first = battle.Teams.FirstOrDefault(t => t.Key == "de_1");
            if (first != null && first.Leader != null)
            {
                var from = first.Leader.Position;
                var to = new float2(118f, 100f);
                var sight = LineOfSight.Check(battle.Map, from, first.Leader.Posture, to, Posture.Standing, Visibility.MaxSightRange);
                bootstrap.UnitsView.ShowSightLine(from, to, sight.Visible ? 1f : sight.BlockedAt);
            }

            Debug.Log($"Close Fight: Germans see {battle.Visibility.VisibleEnemyCount(Side.German)} Soviet soldiers; sound markers {battle.Visibility.SoundMarkers.Count}.");
            Debug.Log($"Close Fight: demo ran {ticks} ticks.");
            foreach (var team in battle.Teams)
            {
                int active = team.ActiveCount;
                int wild = team.Members.Count(m => m.IsActive && m.IsOutOfControl);
                string kit = team.Vehicle != null
                    ? $" [{team.Vehicle.Definition.Id}: destroyed {team.Vehicle.Destroyed}, abandoned {team.Vehicle.Abandoned}, immobilized {team.Vehicle.Immobilized}, gun damaged {team.Vehicle.GunDamaged}, AP {team.Gun.AmmoAp} HE {team.Gun.AmmoHe}]"
                    : team.Gun != null ? $" [{team.Gun.Definition.Id}: AP {team.Gun.AmmoAp} HE {team.Gun.AmmoHe} smoke {team.Gun.AmmoSmoke}]" : "";
                Debug.Log($"Close Fight: team {team.Key}{kit}: {active}/{team.Members.Count} active, casualties {team.CasualtyFraction:P0}, " +
                          $"morale {team.AverageMorale:0}, suppression {team.AverageSuppression:0}, out of control {wild}, " +
                          $"shaken {team.IsShaken}, broken {team.IsBroken}, order {team.Order.Type}, ammo {team.TotalAmmo}/{team.StartingAmmo}");
            }
        }

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
