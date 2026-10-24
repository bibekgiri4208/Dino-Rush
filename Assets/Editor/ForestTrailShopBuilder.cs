using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DinoRush.Editor
{
    public static class ForestTrailShopBuilder
    {
        [MenuItem("Dino Rush/Add Trail Effects Shop")]
        public static void Build()
        {
            if (Application.isPlaying) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestRun.unity");
            ForestMainMenu menu = Object.FindAnyObjectByType<ForestMainMenu>();
            if (menu == null) throw new System.InvalidOperationException("The forest menu is missing.");
            menu.EnsureTrailUi();
            ForestUiMotionBuilder.Apply(menu.GetComponentInParent<ForestRunnerGame>());
            EditorUtility.SetDirty(menu);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
