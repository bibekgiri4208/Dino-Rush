using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DinoRush.Editor
{
    /// <summary>Authors editable effects without rebuilding artwork or changing UI layout.</summary>
    public static class ForestUiMotionBuilder
    {
        [MenuItem("Dino Rush/Apply UI Motion")]
        public static void Build()
        {
            if (Application.isPlaying) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestRun.unity");
            ForestRunnerGame game = Object.FindAnyObjectByType<ForestRunnerGame>();
            Apply(game);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Saved hover / press / click feedback and unscaled panel transitions on the existing forest UI.");
        }

        internal static void Apply(ForestRunnerGame game)
        {
            RectTransform root = game != null
                ? game.transform.Find("Forest runner UI/16 by 9 safe content") as RectTransform : null;
            if (root == null) throw new System.InvalidOperationException("The authored forest UI is missing.");
            RunnerUiEffects.Install(root);
        }
    }
}
