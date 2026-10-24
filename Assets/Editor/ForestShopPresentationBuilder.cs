using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DinoRush.Editor
{
    /// <summary>Adds the visual shop in place; never changes the supplied egg importers or sprite slices.</summary>
    public static class ForestShopPresentationBuilder
    {
        public const string CatalogPath = "Assets/Generated/RunnerEggHatchCatalog.asset";
        private static readonly string[] Ids = { "red", "yellow", "green" };
        private static readonly string[] Folders = { "egg - 2", "egg - 3", "egg- 4" };

        [MenuItem("Dino Rush/Add Animated Mystery Egg Shop")]
        public static void Build()
        {
            if (Application.isPlaying) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestRun.unity");
            ForestMainMenu menu = Object.FindAnyObjectByType<ForestMainMenu>();
            if (menu == null) throw new System.InvalidOperationException("The authored forest menu is missing.");
            Apply(menu);
            ForestUiMotionBuilder.Apply(menu.GetComponentInParent<ForestRunnerGame>());
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Saved the visual mystery egg shop and move/crack/hatch reveal. Original egg sprites were preserved.");
        }

        internal static void Apply(ForestMainMenu menu)
        {
            menu.EnsureShopUi();
            RunnerEggHatchCatalog catalog = Catalog();
            RunnerShopPresentation presentation = menu.ShopPresentation;
            presentation.SetCatalog(catalog);
            presentation.SetPreview(Ids.Select(id =>
                AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(catalog.Find(id).move))
                    .OfType<Sprite>().OrderBy(sprite => sprite.name).First()).ToArray());
            EditorUtility.SetDirty(presentation);
            EditorUtility.SetDirty(menu);
        }

        private static RunnerEggHatchCatalog Catalog()
        {
            var existing = AssetDatabase.LoadAssetAtPath<RunnerEggHatchCatalog>(CatalogPath);
            if (existing != null) return existing;
            var catalog = ScriptableObject.CreateInstance<RunnerEggHatchCatalog>();
            var data = new SerializedObject(catalog);
            SerializedProperty eggs = data.FindProperty("eggs");
            eggs.arraySize = Ids.Length;
            Color[] accents = { new Color32(240, 127, 118, 255), new Color32(255, 217, 104, 255), new Color32(170, 221, 111, 255) };
            for (int i = 0; i < Ids.Length; i++)
            {
                SerializedProperty egg = eggs.GetArrayElementAtIndex(i);
                egg.FindPropertyRelative("dinoId").stringValue = Ids[i];
                egg.FindPropertyRelative("accent").colorValue = accents[i];
                foreach (string stage in new[] { "move", "crack", "hatch" })
                {
                    string path = "Assets/2D Sprites/dino/" + Folders[i] + "/" + stage + ".png";
                    Texture2D sheet = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (sheet == null || sheet.height != RunnerEggHatchCatalog.CellSize || sheet.width != 4 * RunnerEggHatchCatalog.CellSize)
                        throw new System.InvalidOperationException("Expected the original four-frame egg strip: " + path);
                    egg.FindPropertyRelative(stage).objectReferenceValue = sheet;
                }
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            return catalog;
        }
    }
}
