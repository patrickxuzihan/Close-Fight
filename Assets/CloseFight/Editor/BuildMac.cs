using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CloseFight.EditorTools
{
    /// <summary>
    /// Builds a standalone macOS app for playtesting (lighter than play mode in
    /// the editor). Menu Close Fight → Build macOS App, or from the command line:
    ///   -executeMethod CloseFight.EditorTools.BuildMac.BuildFromCommandLine
    /// Output: Builds/macOS/Close Fight.app (ignored by git).
    /// </summary>
    public static class BuildMac
    {
        public const string OutputPath = "Builds/macOS/Close Fight.app";

        [MenuItem("Close Fight/Build macOS App")]
        public static void BuildFromMenu()
        {
            if (Build())
            {
                EditorUtility.RevealInFinder(OutputPath);
            }
        }

        public static void BuildFromCommandLine()
        {
            if (!Build())
            {
                EditorApplication.Exit(1);
            }
        }

        static bool Build()
        {
            // A resizable window rather than full screen, so a laptop renders fewer pixels.
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 1000;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.allowFullscreenSwitch = true;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { BattleSceneSetup.ScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"Close Fight build: {summary.result}, {summary.totalSize / (1024 * 1024)} MB, " +
                      $"{summary.totalTime.TotalSeconds:0} s, {summary.totalErrors} errors → {OutputPath}");
            return summary.result == BuildResult.Succeeded;
        }
    }
}
