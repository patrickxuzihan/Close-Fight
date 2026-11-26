using System;
using System.Collections;
using CloseFight.Core.Units;
using CloseFight.Game.Controls;
using UnityEngine;

namespace CloseFight.Game.Battle
{
    /// <summary>
    /// Checking the play-mode screens (briefing, deployment, battle UI) from the
    /// command line: the built game started with
    ///   -cfShot path.png [-cfStage briefing|deployment|battle] [-cfSeconds 3]
    ///   [-cfSide german|soviet] [-cfAiPlan] [-cfZoom 60 -cfCenter 120,130] [-cfRosterOpen 1 [-cfDelegate]]
    /// opens at that stage, saves one screenshot and quits. Does nothing otherwise.
    /// </summary>
    sealed class AutoScreenshot : MonoBehaviour
    {
        string _path;
        string _stage;
        float _seconds;
        Side _side;
        BattleInput _input;

        public static void AttachIfRequested(GameObject host, BattleInput input)
        {
            string path = Arg("-cfShot");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            // Started from a script the window may not have focus; keep running anyway.
            Application.runInBackground = true;
            var shot = host.AddComponent<AutoScreenshot>();
            shot._path = path;
            shot._stage = Arg("-cfStage") ?? "briefing";
            shot._seconds = float.TryParse(Arg("-cfSeconds"), out var s) ? s : 3f;
            shot._input = input;
            shot._side = (Arg("-cfSide") ?? "german").ToLowerInvariant() == "soviet" ? Side.Soviet : Side.German;
        }

        IEnumerator Start()
        {
            yield return null;
            if (int.TryParse(Arg("-cfScenario"), out int scenario))
            {
                for (int i = 0; i < scenario; i++) _input.DebugCycleScenario(1);
                yield return null;
            }

            if (_stage != "briefing")
            {
                _input.DebugChooseSide(_side);
                yield return null;
            }

            if (HasArg("-cfMines"))
            {
                _input.DebugLayMines();
            }

            if (_stage == "battle")
            {
                _input.DebugStartBattle();
            }

            _input.DebugView(HasArg("-cfAiPlan"), Arg("-cfZoom"), Arg("-cfCenter"));
            if (HasArg("-cfSupport"))
            {
                _input.DebugSupport();
            }

            if (int.TryParse(Arg("-cfRosterOpen"), out int platoon))
            {
                _input.DebugOpenPlatoon(platoon);
            }

            _input.DebugHideHud = HasArg("-cfNoHud");
            var menuAt = Arg("-cfMenu");
            if (!string.IsNullOrEmpty(menuAt))
            {
                var inv = System.Globalization.CultureInfo.InvariantCulture;
                var parts = menuAt.Split(',');
                _input.DebugMenu(new Unity.Mathematics.float2(float.Parse(parts[0], inv), float.Parse(parts[1], inv)));
            }

            if (HasArg("-cfDelegate"))
            {
                _input.DebugDelegate(); // M15: the open platoon's squads handed to the AI, the first one asking to pull back
            }

            var flareAt = Arg("-cfFlare");
            if (!string.IsNullOrEmpty(flareAt))
            {
                // One or more points: -cfFlare 250,140 or -cfFlare "250,140;180,120"
                var inv = System.Globalization.CultureInfo.InvariantCulture;
                foreach (var point in flareAt.Split(';'))
                {
                    var parts = point.Split(',');
                    _input.DebugFlare(new Unity.Mathematics.float2(float.Parse(parts[0], inv), float.Parse(parts[1], inv)));
                }
            }

            yield return new WaitForSecondsRealtime(_seconds);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(_path);
            yield return new WaitForSecondsRealtime(1f);
            Application.Quit();
        }

        static bool HasArg(string name) => Array.IndexOf(Environment.GetCommandLineArgs(), name) >= 0;

        static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}
