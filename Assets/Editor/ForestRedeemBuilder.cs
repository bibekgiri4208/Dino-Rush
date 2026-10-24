using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DinoRush.Editor
{
    /// <summary>Adds the Options gift-code flow without rebuilding the Shop or existing menus.</summary>
    public static class ForestRedeemBuilder
    {
        [MenuItem("Dino Rush/Add Redeem Gift Code")]
        public static void Build()
        {
            if (Application.isPlaying) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestRun.unity");
            ForestMainMenu menu = Object.FindAnyObjectByType<ForestMainMenu>();
            if (menu == null) throw new System.InvalidOperationException("The authored forest menu is missing.");
            menu.EnsureRedeemUi();
            ForestUiMotionBuilder.Apply(menu.GetComponentInParent<ForestRunnerGame>());
            EditorUtility.SetDirty(menu);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
