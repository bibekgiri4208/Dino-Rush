using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DinoRush.Editor
{
    public static class ForestDirtEffectsBuilder
    {
        private const string AssetPath = "Assets/Generated/ForestDirtSprites.asset";

        [MenuItem("Dino Rush/Apply Dirt Effects")]
        public static void Build()
        {
            if (Application.isPlaying) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestRun.unity");
            ForestRunnerGame game = Object.FindAnyObjectByType<ForestRunnerGame>();
            if (game == null || !game.HasAuthoredScene) throw new System.InvalidOperationException("Bake the forest scene first.");
            Sprite[] sprites;
            if (AssetDatabase.LoadMainAssetAtPath(AssetPath) == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Generated")) AssetDatabase.CreateFolder("Assets", "Generated");
                var assets = new List<Object>();
                sprites = RunnerDirtArt.CreateSprites(assets);
                var container = ScriptableObject.CreateInstance<RunnerSceneAssets>();
                container.name = "Forest Dirt Sprites";
                container.SetAssets(assets);
                AssetDatabase.CreateAsset(container, AssetPath);
                foreach (Object asset in assets) AssetDatabase.AddObjectToAsset(asset, container);
                EditorUtility.SetDirty(container);
                AssetDatabase.SaveAssets();
            }
            else
            {
                // Keep any artist edits and existing asset identities on subsequent runs.
                sprites = new Sprite[3];
                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(AssetPath))
                {
                    if (!(asset is Sprite sprite)) continue;
                    if (sprite.name == "Dirt clod") sprites[0] = sprite;
                    if (sprite.name == "Dirt puff") sprites[1] = sprite;
                    if (sprite.name == "Dirt fleck") sprites[2] = sprite;
                }
                foreach (Sprite sprite in sprites)
                    if (sprite == null) throw new System.InvalidOperationException("Existing dirt art is incomplete; it was not overwritten.");
            }
            var data = new SerializedObject(game);
            SerializedProperty art = data.FindProperty("dirtSprites");
            art.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++) art.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            SerializedProperty pool = data.FindProperty("dust");
            if (pool.arraySize < 24) throw new System.InvalidOperationException("The saved dirt particle pool is too small.");
            for (int i = 0; i < pool.arraySize; i++)
            {
                SerializedProperty particle = pool.GetArrayElementAtIndex(i);
                var renderer = (SpriteRenderer)particle.FindPropertyRelative("renderer").objectReferenceValue;
                renderer.sprite = sprites[i % sprites.Length];
                renderer.color = new Color32(137, 105, 64, 255);
                renderer.sortingOrder = 25;
                renderer.transform.localScale = Vector3.one * 0.12f;
                renderer.gameObject.SetActive(false);
                particle.FindPropertyRelative("life").floatValue = 0;
                EditorUtility.SetDirty(renderer);
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(game);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Saved pixel dirt sprites on the existing 36-particle pool: running trail and stronger landing bursts.");
        }
    }
}
