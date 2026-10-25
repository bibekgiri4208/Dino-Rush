using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DinoRush.Editor
{
    public static class ForestSlashShopBuilder
    {
        [MenuItem("Dino Rush/Update Slash Counts UI")]
        public static void UpdateCountsUi()
        {
            if (Application.isPlaying) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestRun.unity");
            var game = Object.FindAnyObjectByType<ForestRunnerGame>();
            var hud = game.GetComponentInChildren<RunnerSlashHud>(true);
            var button = hud.transform.Find("Slash indicator").GetComponent<Button>();
            hud.EnsureUi(button.GetComponentInChildren<Text>(true).font, button.GetComponent<Image>().sprite);
            EditorUtility.SetDirty(hud);
            var shop = game.GetComponentInChildren<RunnerSlashShop>(true);
            shop.RefreshCountLabels(); shop.ApplyLayout(false);
            shop.transform.Find("Slashes section/Slash loadout").GetComponent<Text>().text = "STOCK 0\nNONE";
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }

        [MenuItem("Dino Rush/Add Slash Attacks and Shop")]
        public static void Build()
        {
            if (Application.isPlaying) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestRun.unity");
            const string path = "Assets/RunnerSlashCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<RunnerSlashCatalog>(path);
            if (catalog == null) { catalog = ScriptableObject.CreateInstance<RunnerSlashCatalog>(); AssetDatabase.CreateAsset(catalog, path); }
            var data = new SerializedObject(catalog);
            // Imported frames are auto-trimmed differently. Restore their common PNG canvas so
            // the attack does not balloon or jitter between frames; never change source importers.
            foreach (var old in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
                if (old.name.StartsWith("Slash canvas ")) Object.DestroyImmediate(old, true);
            string[] folders = { "Fire slash/Fire", "Lightning slash/Lightning", "Water slash/Water" };
            string[] fields = { "fire", "lightning", "water" };
            for (int k = 0; k < 3; k++)
            {
                var frames = data.FindProperty(fields[k]); frames.arraySize = 12;
                for (int i = 0; i < 12; i++)
                {
                    string file = "Assets/2D Sprites/Slash attack/" + folders[k] + " Slash_Frame_" + (i + 1).ToString("00") + ".png";
                    var sprite = AssetDatabase.LoadAllAssetsAtPath(file).OfType<Sprite>().FirstOrDefault();
                    if (sprite == null) throw new System.InvalidOperationException("Missing slash sprite: " + file);
                    var canvas = Sprite.Create(sprite.texture, new Rect(0, 0, sprite.texture.width, sprite.texture.height),
                        Vector2.one * 0.5f, 100, 0, SpriteMeshType.FullRect);
                    canvas.name = "Slash canvas " + fields[k] + " " + (i + 1).ToString("00");
                    AssetDatabase.AddObjectToAsset(canvas, catalog);
                    frames.GetArrayElementAtIndex(i).objectReferenceValue = canvas;
                }
            }
            data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(catalog);
            var game = Object.FindAnyObjectByType<ForestRunnerGame>();
            var attack = game.GetComponent<RunnerSlashAttack>(); if (attack == null) attack = game.gameObject.AddComponent<RunnerSlashAttack>();
            attack.Configure(catalog); EditorUtility.SetDirty(attack);
            var menu = game.GetComponentInChildren<ForestMainMenu>(true);
            var trails = menu.TrailShop;
            var tab = trails.transform.Find("Dinos category");
            var font = tab.GetComponentInChildren<Text>(true).font;
            var button = tab.GetComponent<Image>().sprite;
            trails.EnsureSlashUi(font, trails.transform.parent.GetComponent<Image>().sprite, button, catalog);
            EditorUtility.SetDirty(trails); EditorUtility.SetDirty(trails.GetComponent<RunnerSlashShop>());
            Transform safe = game.transform.Find("Forest runner UI/16 by 9 safe content");
            var hud = safe.GetComponent<RunnerSlashHud>(); if (hud == null) hud = safe.gameObject.AddComponent<RunnerSlashHud>();
            hud.EnsureUi(font, button); EditorUtility.SetDirty(hud);
            var gameData = new SerializedObject(game);
            var buttons = gameData.FindProperty("hud").FindPropertyRelative("buttons");
            var slashButton = safe.Find("Slash indicator").GetComponent<Button>();
            bool found = false;
            for (int i = 0; i < buttons.arraySize; i++) if (buttons.GetArrayElementAtIndex(i).objectReferenceValue == slashButton) found = true;
            if (!found) { buttons.InsertArrayElementAtIndex(buttons.arraySize); buttons.GetArrayElementAtIndex(buttons.arraySize - 1).objectReferenceValue = slashButton; }
            gameData.ApplyModifiedPropertiesWithoutUndo();
            ForestUiMotionBuilder.Apply(game);
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
    }
}
