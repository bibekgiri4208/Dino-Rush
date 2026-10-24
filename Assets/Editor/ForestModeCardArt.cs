using System.IO;
using UnityEditor;
using UnityEngine;

namespace DinoRush.Editor
{
    /// <summary>A story vignette and an angled, cover-cropped montage of the actual map previews.</summary>
    internal static class ForestModeCardArt
    {
        private static readonly Color32 Ink = PixelMenuArt.Hex(0x102f35);
        private static readonly Color32 Gold = PixelMenuArt.Hex(0xffd275);
        private static Vector2Int P(int x, int y) => new Vector2Int(x, y);

        internal static void Generate(RunnerMapCatalog maps)
        {
            Save("ModeStory", DrawStory());
            string path = PixelMenuArt.Directory + "/ModeEndless.png";
            if (!File.Exists(path)) RefreshEndless(maps);
        }

        internal static void RefreshEndless(RunnerMapCatalog maps)
        {
            if (maps == null || maps.Count == 0) throw new System.InvalidOperationException("Map previews are required for the Endless montage.");
            const int width = 320, height = 176, border = 2;
            var p = new PixelMenuArt.Pixels(width, height);
            p.Rect(0, 0, width, height, Ink);
            float stripWidth = (width - border * 2f) / maps.Count;
            // The slanted boundaries mask the photos; the landscapes themselves are never sheared or squeezed.
            for (int i = 0; i < maps.Count; i++)
            {
                Sprite preview = maps[i].preview;
                if (preview == null) throw new System.InvalidOperationException("Missing map preview: " + maps[i].id);
                var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    // Decode a temporary copy instead of changing the source texture's read/write importer.
                    if (!source.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(preview.texture))))
                        throw new System.InvalidOperationException("Cannot read map preview: " + maps[i].id);
                    Rect crop = preview.rect;
                    float scale = Mathf.Max((stripWidth + 24) / crop.width, (height - border * 2f) / crop.height);
                    float center = border + (i + 0.5f) * stripWidth;
                    for (int y = border; y < height - border; y++)
                    {
                        float slant = (0.5f - (float)(y - border) / (height - border * 2 - 1)) * 24;
                        float left = border + i * stripWidth + slant, right = left + stripWidth;
                        int sy = Mathf.Clamp(Mathf.FloorToInt(crop.yMax - 1 - (y - border) / scale), (int)crop.yMin, (int)crop.yMax - 1);
                        for (int x = border; x < width - border; x++)
                        {
                            if ((i > 0 && x < left) || (i < maps.Count - 1 && x >= right)) continue;
                            int sx = Mathf.Clamp(Mathf.FloorToInt(crop.center.x + (x + 0.5f - center) / scale), (int)crop.xMin, (int)crop.xMax - 1);
                            p.Set(x, y, source.GetPixel(sx, sy));
                        }
                    }
                }
                finally { Object.DestroyImmediate(source); }
            }
            for (int i = 1; i < maps.Count; i++)
            {
                int top = Mathf.RoundToInt(border + i * stripWidth + 12);
                int bottom = Mathf.RoundToInt(border + i * stripWidth - 12);
                p.Line(top, border, bottom, height - border - 1, 1, Ink);
                p.Line(top + 1, border, bottom + 1, height - border - 1, 0, PixelMenuArt.Hex(0xc8cfaa));
            }
            Save("ModeEndless", p, true);
        }

        private static PixelMenuArt.Pixels DrawStory()
        {
            var p = new PixelMenuArt.Pixels(160, 88);
            p.Rect(0, 0, 160, 88, Ink);
            p.Rect(2, 2, 156, 84, PixelMenuArt.Hex(0x315566));
            p.Disc(126, 17, 9, PixelMenuArt.Hex(0xd5bb7d));
            Cloud(p, 17, 13); Cloud(p, 82, 8);
            p.Poly(PixelMenuArt.Hex(0x466f70), P(2, 48), P(31, 19), P(65, 46), P(102, 21), P(157, 54), P(157, 86), P(2, 86));
            p.Poly(PixelMenuArt.Hex(0x89ada0), P(21, 29), P(31, 19), P(43, 29), P(33, 26), P(28, 31));
            p.Poly(PixelMenuArt.Hex(0x305a4c), P(2, 51), P(30, 44), P(69, 53), P(115, 37), P(157, 45), P(157, 86), P(2, 86));
            p.Poly(PixelMenuArt.Hex(0x3d6848), P(2, 73), P(35, 51), P(81, 67), P(124, 47), P(157, 55), P(157, 86), P(2, 86));
            for (int i = 0; i < 7; i++) Tree(p, 8 + i * 24, 45 + (i % 3) * 5);
            Vector2Int[] points = { P(12, 79), P(42, 59), P(72, 66), P(96, 52), P(129, 42) };
            for (int pass = 0; pass < 3; pass++)
                for (int i = 1; i < points.Length; i++)
                    p.Line(points[i - 1].x, points[i - 1].y, points[i].x, points[i].y,
                        4 - pass, pass == 0 ? Ink : pass == 1 ? PixelMenuArt.Hex(0xb29260) : PixelMenuArt.Hex(0xead19a));
            foreach (Vector2Int node in new[] { points[1], points[2], points[3] })
            {
                p.Disc(node.x, node.y, 4, Ink);
                p.Disc(node.x, node.y, 2, Gold);
            }
            // A little home and a destination flag make the campaign read as a journey, not a race.
            p.Rect(123, 32, 15, 12, PixelMenuArt.Hex(0xd6c69a));
            p.Poly(PixelMenuArt.Hex(0x9d6452), P(120, 33), P(130, 23), P(141, 33));
            p.Rect(128, 36, 4, 8, Ink);
            p.Rect(143, 19, 2, 25, PixelMenuArt.Hex(0xe6d6ac));
            p.Poly(Gold, P(145, 19), P(155, 22), P(145, 27));
            p.Rect(2, 84, 156, 2, PixelMenuArt.Hex(0xa38c5b));
            return p;
        }

        private static void Cloud(PixelMenuArt.Pixels p, int x, int y)
        {
            Color32 color = PixelMenuArt.Hex(0x779a97);
            p.Rect(x, y, 24, 3, color); p.Rect(x + 6, y - 3, 12, 3, color);
        }

        private static void Tree(PixelMenuArt.Pixels p, int x, int y)
        {
            p.Rect(x - 1, y, 3, 12, Ink);
            p.Poly(PixelMenuArt.Hex(0x254c40), P(x - 6, y + 6), P(x, y - 9), P(x + 7, y + 6));
            p.Poly(PixelMenuArt.Hex(0x54804f), P(x - 4, y), P(x, y - 9), P(x, y + 4));
        }

        private static void Save(string name, PixelMenuArt.Pixels pixels, bool replace = false)
        {
            string path = PixelMenuArt.Directory + "/" + name + ".png";
            if (File.Exists(path) && !replace) return; // Only the explicit montage refresh replaces existing art.
            var texture = new Texture2D(pixels.width, pixels.height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels.data);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }
}
