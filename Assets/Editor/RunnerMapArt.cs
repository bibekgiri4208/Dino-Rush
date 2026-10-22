using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DinoRush.Editor
{
    /// <summary>Source-resolution map variants and layered previews; original artwork is never overwritten.</summary>
    internal static class RunnerMapArt
    {
        internal const string Directory = "Assets/Generated/RunnerMaps";

        internal static Sprite Loop(string sourcePath, string name, bool sky = false,
            FilterMode filter = FilterMode.Bilinear, bool replace = false)
        {
            string path = $"{Directory}/{name}_loop.png";
            if (!File.Exists(path) || replace)
            {
                Texture2D source = Read(sourcePath);
                try
                {
                    int height = source.height;
                    int halfWidth = source.width;
                    var pixels = new Color32[halfWidth * 2 * height];
                    Color32[] raw = source.GetPixels32();
                    for (int x = 0; x < halfWidth; x++)
                    {
                        Color32 below = Color.clear;
                        for (int y = height - 1; y >= 0; y--)
                        {
                            Color32 color = raw[y * source.width + x];
                            if (sky && color.a == 0) color = below; // Sky fills below its supplied transparent horizon.
                            if (color.a > 0) below = color;
                            pixels[y * halfWidth * 2 + x] = color;
                            pixels[y * halfWidth * 2 + halfWidth * 2 - 1 - x] = color;
                        }
                    }
                    // A reflected repeat preserves silhouettes at both joins, instead of
                    // blending a mountain or canopy into a vertical seam. Source art is untouched.
                    Write(path, halfWidth * 2, height, pixels);
                }
                finally { Object.DestroyImmediate(source); }
            }
            return Import(path, true, filter);
        }

        internal static Sprite Ground(string id, Color dark, Color light)
        {
            string path = $"{Directory}/{id}_ground_loop.png";
            if (!File.Exists(path))
            {
                Texture2D source = Read("Assets/Generated/ForestLoops/ground_loop.png");
                try
                {
                    Color32[] pixels = source.GetPixels32();
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        Color color = pixels[i];
                        float alpha = color.a;
                        color = Color.Lerp(dark, light, Mathf.Clamp01(color.grayscale * 1.55f));
                        color.a = alpha;
                        pixels[i] = color;
                    }
                    Write(path, source.width, source.height, pixels);
                }
                finally { Object.DestroyImmediate(source); }
            }
            return Import(path, true);
        }

        internal static Sprite Moon(string sourcePath, string id, bool replace = false)
        {
            string path = $"{Directory}/{id}_moon.png";
            if (!File.Exists(path) || replace)
            {
                Texture2D source = Read(sourcePath);
                try
                {
                    Color32[] raw = source.GetPixels32();
                    int minX = source.width, maxX = 0, minY = source.height, maxY = 0;
                    for (int y = 0; y < source.height; y++)
                    for (int x = 0; x < source.width; x++)
                        if (raw[y * source.width + x].a > 0)
                        {
                            minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                            minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
                        }
                    int height = maxY - minY + 5, width = maxX - minX + 5;
                    var pixels = new Color32[width * height];
                    for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                    {
                        int sx = minX - 2 + x;
                        int sy = minY - 2 + y;
                        if (sx >= 0 && sx < source.width && sy >= 0 && sy < source.height)
                            pixels[y * width + x] = raw[sy * source.width + sx];
                    }
                    Write(path, width, height, pixels);
                }
                finally { Object.DestroyImmediate(source); }
            }
            return Import(path, true, FilterMode.Bilinear);
        }

        internal static Sprite Preview(RunnerMapCatalog.Map map, bool replace = false)
        {
            string path = $"{Directory}/{map.id}_preview.png";
            if (!File.Exists(path) || replace)
            {
                const int width = 256, height = 144;
                var pixels = new Color32[width * height];
                var textures = new Dictionary<Sprite, Texture2D>();
                var colors = new Dictionary<Sprite, Color32[]>();
                try
                {
                    foreach (RunnerMapCatalog.MapLayer layer in map.layers)
                    {
                        if (textures.ContainsKey(layer.sprite)) continue;
                        Texture2D texture = Read(AssetDatabase.GetAssetPath(layer.sprite.texture));
                        textures[layer.sprite] = texture;
                        colors[layer.sprite] = texture.GetPixels32();
                    }
                    for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                    {
                        float wx = (x + 0.5f) / width * 20f - 10f;
                        float wy = (y + 0.5f) / height * 11.25f - 5.625f;
                        Color color = wy < -3.4f ? map.soilColor : map.skyColor;
                        foreach (RunnerMapCatalog.MapLayer layer in map.layers)
                        {
                            if (layer.order >= 0 && wy < -3.5f) color = map.soilColor;
                            float v = (wy - layer.bottom) / layer.height;
                            if (v < 0f || v >= 1f) continue;
                            Texture2D texture = textures[layer.sprite];
                            float worldWidth = texture.width / layer.sprite.pixelsPerUnit * layer.height;
                            float stride = (texture.width - 1f) / layer.sprite.pixelsPerUnit * layer.height;
                            float local = layer.stationary ? wx - layer.x + worldWidth * 0.5f
                                : Mathf.Repeat(wx + worldWidth * 0.5f + (map.id == "forest" ? 0f : stride * 0.25f), stride);
                            if (local < 0f || local >= worldWidth) continue;
                            int sx = Mathf.Clamp((int)(local / worldWidth * texture.width), 0, texture.width - 1);
                            int sy = Mathf.Clamp((int)(v * texture.height), 0, texture.height - 1);
                            Color sample = colors[layer.sprite][sy * texture.width + sx];
                            color = Color.Lerp(color, sample, sample.a);
                            color.a = 1f;
                        }
                        pixels[y * width + x] = color;
                    }
                    Write(path, width, height, pixels);
                }
                finally { foreach (Texture2D texture in textures.Values) Object.DestroyImmediate(texture); }
            }
            return Import(path, false, map.id == "desert" || map.id == "rural" ? FilterMode.Bilinear : FilterMode.Point);
        }

        private static Texture2D Read(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(path))) throw new System.InvalidOperationException("Cannot read map art: " + path);
            return texture;
        }

        private static void Write(string path, int width, int height, Color32[] pixels)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                texture.SetPixels32(pixels);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally { Object.DestroyImmediate(texture); }
            AssetDatabase.ImportAsset(path);
        }

        private static Sprite Import(string path, bool bottomPivot, FilterMode filter = FilterMode.Point)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            byte[] png = File.ReadAllBytes(path);
            int width = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
            int height = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];
            importer.spritePixelsPerUnit = bottomPivot ? height : 100f;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = bottomPivot ? (int)SpriteAlignment.BottomCenter : (int)SpriteAlignment.Center;
            settings.spritePivot = bottomPivot ? new Vector2(0.5f, 0f) : Vector2.one * 0.5f;
            importer.SetTextureSettings(settings);
            importer.npotScale = TextureImporterNPOTScale.None;
            // Reflected 3800px source layers are 7600px wide. A 4096 cap would
            // silently downsample the supposedly full-resolution PNG on import.
            importer.maxTextureSize = Mathf.Max(4096, Mathf.NextPowerOfTwo(Mathf.Max(width, height)));
            importer.filterMode = filter;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
