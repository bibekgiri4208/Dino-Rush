using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DinoRush.Editor
{
    /// <summary>Seals the supplied art's edge gutters without overwriting the source images.</summary>
    public static class ForestParallaxRepair
    {
        private const string Directory = "Assets/Generated/ForestLoops";
        private static readonly string[] Sources =
            { "sky", "mountains", "distantForest", "middleForest", "tallTrees", "nearForest", "ground" };

        [MenuItem("Dino Rush/Repair Forest Scrolling Seams")]
        public static void RepairScene()
        {
            if (Application.isPlaying) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestRun.unity");
            Repair(Object.FindAnyObjectByType<ForestRunnerGame>());
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Repaired forest loops: sealed texture edges, uncompressed point sampling and one-texel tile overlap. Original artwork was preserved.");
        }

        internal static void Repair(ForestRunnerGame game)
        {
            if (game == null || !game.HasAuthoredScene)
                throw new System.InvalidOperationException("The authored forest scene is missing.");
            if (!AssetDatabase.IsValidFolder(Directory)) AssetDatabase.CreateFolder("Assets/Generated", "ForestLoops");
            var data = new SerializedObject(game);
            SerializedProperty layers = data.FindProperty("layers");
            if (layers.arraySize != Sources.Length)
                throw new System.InvalidOperationException("Unexpected forest layer layout; repair stopped without replacing scene objects.");
            for (int i = 0; i < Sources.Length; i++)
            {
                var source = (Texture2D)data.FindProperty(Sources[i]).objectReferenceValue;
                Sprite sprite = LoopSprite(source);
                SerializedProperty layer = layers.GetArrayElementAtIndex(i);
                SerializedProperty tiles = layer.FindPropertyRelative("tiles");
                var first = (Transform)tiles.GetArrayElementAtIndex(0).objectReferenceValue;
                // A shared repeat stride, not separately rounded positions, keeps the
                // tiles touching at every scroll offset and across the wrap itself.
                float stride = (sprite.rect.width - 1) / sprite.pixelsPerUnit * first.lossyScale.x;
                layer.FindPropertyRelative("width").floatValue = stride;
                layer.FindPropertyRelative("offset").floatValue = 0;
                for (int tileIndex = 0; tileIndex < tiles.arraySize; tileIndex++)
                {
                    var tile = (Transform)tiles.GetArrayElementAtIndex(tileIndex).objectReferenceValue;
                    SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();
                    renderer.sprite = sprite;
                    Vector3 position = tile.position;
                    position.x = (tileIndex - 1) * stride;
                    tile.position = position;
                    EditorUtility.SetDirty(renderer);
                    EditorUtility.SetDirty(tile);
                }
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(game);
        }

        private static Sprite LoopSprite(Texture2D source)
        {
            string path = $"{Directory}/{source.name}_loop.png";
            if (!File.Exists(path))
            {
                var original = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                var loop = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                try
                {
                    original.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(source)));
                    Color32[] raw = original.GetPixels32();
                    var pixels = new Color32[source.width * source.height];
                    // Keep the existing scene's texture dimensions and world framing,
                    // but sample the raw PNG rather than its compressed import.
                    for (int y = 0; y < source.height; y++)
                    for (int x = 0; x < source.width; x++)
                    {
                        int sx = Mathf.Min(original.width - 1, (int)((x + 0.5f) * original.width / source.width));
                        int sy = Mathf.Min(original.height - 1, (int)((y + 0.5f) * original.height / source.height));
                        pixels[y * source.width + x] = raw[sy * original.width + sx];
                    }
                    SealRightEdge(pixels, source.width, source.height);
                    loop.SetPixels32(pixels);
                    loop.Apply();
                    File.WriteAllBytes(path, loop.EncodeToPNG());
                }
                finally
                {
                    Object.DestroyImmediate(original);
                    Object.DestroyImmediate(loop);
                }
                AssetDatabase.ImportAsset(path);
            }
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = source.height;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            settings.spritePivot = new Vector2(0.5f, 0);
            importer.SetTextureSettings(settings);
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = Mathf.Max(2048, Mathf.NextPowerOfTwo(Mathf.Max(source.width, source.height)));
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void SealRightEdge(Color32[] pixels, int width, int height)
        {
            // The mountain has an almost empty terminal column, and the near forest
            // has a fully transparent gutter. Detect these relative to nearby art,
            // not the transparent sky above the layer's silhouette.
            int probe = Mathf.Max(2, width / 64), maxCoverage = 0;
            var coverage = new int[probe];
            for (int x = 0; x < probe; x++)
            {
                for (int y = 0; y < height; y++)
                    if (pixels[y * width + width - 1 - x].a > 0) coverage[x]++;
                maxCoverage = Mathf.Max(maxCoverage, coverage[x]);
            }
            int gutter = 0;
            while (gutter < probe - 1 && coverage[gutter] < maxCoverage * 0.25f) gutter++;
            int repairedColumns = Mathf.Max(1, gutter);
            for (int y = 0; y < height; y++)
            for (int x = 0; x < repairedColumns; x++)
                pixels[y * width + width - 1 - x] = pixels[y * width + x];
        }
    }
}
