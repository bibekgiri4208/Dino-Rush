using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DinoRush.Editor
{
    public static class ForestRunnerMenu
    {
        private const string ScenePath = "Assets/Scenes/ForestRun.unity";
        private const string SpriteAssetPath = "Assets/Generated/ForestSceneSprites.asset";

        [MenuItem("Dino Rush/Open Forest Run")]
        public static void OpenForest()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Dino Rush/Bake Scene Objects (Once)")]
        public static void BakeSceneObjects()
        {
            if (Application.isPlaying) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Scene scene = EditorSceneManager.OpenScene(ScenePath);
            ForestRunnerGame game = Object.FindAnyObjectByType<ForestRunnerGame>();
            if (game.HasAuthoredScene)
            {
                Debug.Log("ForestRun already has saved scene objects. Edit them directly in the Hierarchy.");
                return;
            }
            if (AssetDatabase.LoadMainAssetAtPath(SpriteAssetPath) != null)
                throw new System.InvalidOperationException("Forest sprite assets already exist; refusing to overwrite them.");

            if (!AssetDatabase.IsValidFolder("Assets/Generated")) AssetDatabase.CreateFolder("Assets", "Generated");
            var assets = game.CreateEditorScene();
            var container = ScriptableObject.CreateInstance<RunnerSceneAssets>();
            container.name = "Forest Scene Sprites";
            container.SetAssets(assets);
            AssetDatabase.CreateAsset(container, SpriteAssetPath);
            foreach (Object asset in assets) AssetDatabase.AddObjectToAsset(asset, container);
            EditorUtility.SetDirty(container);
            EditorUtility.SetDirty(game);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"ForestRun saved with {game.GetComponentsInChildren<Transform>(true).Length} editable scene objects.");
            if (!Application.isBatchMode && SceneView.lastActiveSceneView != null)
            {
                SceneView view = SceneView.lastActiveSceneView;
                view.in2DMode = true;
                view.LookAt(Vector3.zero, Quaternion.identity, 11f, true);
            }
        }
    }
}
