using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CloseFight.EditorTools
{
    /// <summary>
    /// When the project opens on an empty, untitled scene (first launch on a new
    /// machine, fresh clone), open the battle scene instead, so there is
    /// something to look at right away. Never replaces a scene the user opened.
    /// </summary>
    [InitializeOnLoad]
    static class OpenBattleSceneOnStartup
    {
        const string SessionKey = "CloseFight.OpenBattleSceneOnStartup.Done";

        static OpenBattleSceneOnStartup()
        {
            if (Application.isBatchMode || SessionState.GetBool(SessionKey, false))
            {
                return;
            }

            SessionState.SetBool(SessionKey, true);
            EditorApplication.delayCall += OpenIfUntitled;
        }

        static void OpenIfUntitled()
        {
            var active = EditorSceneManager.GetActiveScene();
            bool untitled = string.IsNullOrEmpty(active.path);
            if (untitled && !active.isDirty && File.Exists(BattleSceneSetup.ScenePath))
            {
                EditorSceneManager.OpenScene(BattleSceneSetup.ScenePath, OpenSceneMode.Single);
            }
        }
    }
}
