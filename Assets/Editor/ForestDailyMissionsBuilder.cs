using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DinoRush.Editor
{
    public static class ForestDailyMissionsBuilder
    {
        [MenuItem("Dino Rush/Add Daily Missions")]
        public static void Build()
        {
            if (Application.isPlaying) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestRun.unity");
            var game = Object.FindAnyObjectByType<ForestRunnerGame>();
            var menu = game.GetComponentInChildren<ForestMainMenu>(true);
            menu.EnsureDailyMissionsUi();
            EditorUtility.SetDirty(menu); EditorUtility.SetDirty(menu.MissionsPage);
            ForestUiMotionBuilder.Apply(game);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
    }
}
