using System.IO;
using UnityEditor;
using UnityEngine;

namespace DinoRush.Editor
{
    /// <summary>Adapts named action strips to the runner's existing 24-frame pose contract, without editing sources.</summary>
    public static class ForestDinoAnimationBuilder
    {
        internal static readonly string[] Names = { "Cole", "Kira", "Kuro", "Loki", "Mono", "Sena" };
        internal const string Directory = "Assets/Generated/Dinos";

        [MenuItem("Dino Rush/Refresh Dino Animation Sheets")]
        public static void Refresh()
        {
            if (Application.isPlaying) return;
            foreach (string name in Names) Sheet(name.ToLowerInvariant(), true);
            Debug.Log("Refreshed the six derived dino sheets: move for running, kick for jumping, dash for ducking, and supplied hurt poses. Source art, importers, GUIDs and scenes were preserved.");
        }

        internal static Texture2D Sheet(string id, bool refresh = false)
        {
            if (!AssetDatabase.IsValidFolder(Directory)) AssetDatabase.CreateFolder("Assets/Generated", "Dinos");
            string path = Directory + "/" + id + "_runner.png";
            bool exists = File.Exists(path);
            if (!exists || refresh)
            {
                var result = new Texture2D(576, 24, TextureFormat.RGBA32, false);
                var pixels = new Color32[576 * 24];
                try
                {
                    Copy(pixels, id, "idle", 0, 4);
                    Copy(pixels, id, "move", 4, 6);
                    // Match the ORIGINAL four sheets, not the action filenames alone:
                    // RunnerPlayer uses 11/12 (kick 1/2) for ascent/descent. The jump strip
                    // bakes vertical displacement into the pixels and is not that pose pair.
                    Copy(pixels, id, "kick", 10, 3);
                    Copy(pixels, id, "hurt", 13, 4);
                    Copy(pixels, id, "dash", 17, 1, 1);
                    // 'avoid' stays upright; only 'dash' matches the legacy low duck cycle.
                    Copy(pixels, id, "dash", 18, 6);
                    result.SetPixels32(pixels);
                    result.Apply();
                    File.WriteAllBytes(path, result.EncodeToPNG());
                }
                finally { Object.DestroyImmediate(result); }
                AssetDatabase.ImportAsset(path);
                if (exists) return AssetDatabase.LoadAssetAtPath<Texture2D>(path); // Keep any authored sprite slices/import settings.
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 24;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void Copy(Color32[] output, string id, string action, int first, int count, int sourceStart = 0)
        {
            string path = "Assets/2D Sprites/dino/" + id + "/base/" + action + ".png";
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!source.LoadImage(File.ReadAllBytes(path)) || source.height != 24 || source.width % 24 != 0)
                    throw new System.InvalidOperationException("Expected a 24-pixel action strip: " + path);
                Color32[] pixels = source.GetPixels32();
                int available = source.width / 24;
                for (int frame = 0; frame < count; frame++)
                    for (int y = 0; y < 24; y++)
                        for (int x = 0; x < 24; x++)
                        {
                            Color32 color = pixels[y * source.width + ((sourceStart + frame) % available) * 24 + x];
                            output[y * 576 + (first + frame) * 24 + x] = color;
                        }
            }
            finally { Object.DestroyImmediate(source); }
        }
    }
}
