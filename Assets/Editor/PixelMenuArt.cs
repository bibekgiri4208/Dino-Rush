using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DinoRush.Editor
{
    /// <summary>Original, deterministic pixel artwork for the reference-inspired forest menu.</summary>
    internal static class PixelMenuArt
    {
        internal const string Directory = "Assets/UI/ForestMenu";
        private static readonly Color32 Ink = Hex(0x092a35);
        private static readonly Color32[] Greens = { Hex(0x244d38), Hex(0x3c6c35), Hex(0x64933c), Hex(0x9cbe46), Hex(0xc8dd66) };

        internal sealed class Pixels
        {
            internal readonly int width, height;
            internal readonly Color32[] data;
            internal Pixels(int width, int height)
            {
                this.width = width;
                this.height = height;
                data = new Color32[width * height];
            }
            internal void Set(int x, int y, Color32 color)
            {
                if (x >= 0 && y >= 0 && x < width && y < height) data[(height - 1 - y) * width + x] = color;
            }
            internal void Rect(int x, int y, int w, int h, Color32 color)
            {
                for (int yy = y; yy < y + h; yy++) for (int xx = x; xx < x + w; xx++) Set(xx, yy, color);
            }
            internal void Disc(int cx, int cy, int radius, Color32 color)
            {
                for (int y = -radius; y <= radius; y++) for (int x = -radius; x <= radius; x++)
                    if (x * x + y * y <= radius * radius) Set(cx + x, cy + y, color);
            }
            internal void Line(int x0, int y0, int x1, int y1, int thickness, Color32 color)
            {
                int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
                for (int i = 0; i <= steps; i++)
                {
                    float t = steps == 0 ? 0 : (float)i / steps;
                    Disc(Mathf.RoundToInt(Mathf.Lerp(x0, x1, t)), Mathf.RoundToInt(Mathf.Lerp(y0, y1, t)), thickness, color);
                }
            }
            internal void Poly(Color32 color, params Vector2Int[] points)
            {
                int minX = width, minY = height, maxX = 0, maxY = 0;
                foreach (Vector2Int p in points)
                {
                    minX = Mathf.Min(minX, p.x); minY = Mathf.Min(minY, p.y);
                    maxX = Mathf.Max(maxX, p.x); maxY = Mathf.Max(maxY, p.y);
                }
                for (int y = minY; y <= maxY; y++) for (int x = minX; x <= maxX; x++)
                {
                    bool inside = false;
                    for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
                    {
                        Vector2Int a = points[i], b = points[j];
                        if ((a.y > y) != (b.y > y) && x < (float)(b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x) inside = !inside;
                    }
                    if (inside) Set(x, y, color);
                }
            }
        }

        internal static Color32 Hex(uint value) => new Color32((byte)(value >> 16), (byte)(value >> 8), (byte)value, 255);
        private static Vector2Int P(int x, int y) => new Vector2Int(x, y);
        private static int Noise(int x, int y, int seed = 0)
        {
            unchecked { uint n = (uint)(x * 374761393 + y * 668265263 + seed * 144269); n = (n ^ (n >> 13)) * 1274126177; return (int)((n ^ (n >> 16)) & 0x7fffffff); }
        }

        internal static void Generate()
        {
            if (!AssetDatabase.IsValidFolder(Directory)) AssetDatabase.CreateFolder("Assets/UI", "ForestMenu");
            Save("DinoRushLogo", Logo());
            Save("ButtonPlay", ButtonFrame(true));
            Save("ButtonForest", ButtonFrame(false));
            Save("SoundFrame", Frame(38, 38, false, false));
            Save("DialogFrame", Frame(260, 188, false));
            Save("ForestFrame", ForestFrame());
            foreach (string icon in new[] { "Play", "Dino", "Gear", "Quit", "Speaker", "Muted", "Pause", "Restart", "Jump" }) Save("Icon" + icon, Icon(icon));
            AssetDatabase.Refresh();
        }

        internal static Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Directory}/{name}.png");

        internal static void RebuildButtonFrames()
        {
            // This explicit repair replaces only the two requested button textures.
            // Normal menu generation continues to preserve existing artwork.
            Save("ButtonPlay", ButtonFrame(true), true);
            Save("ButtonForest", ButtonFrame(false), true);
        }

        private static void Save(string name, Pixels pixels, bool replace = false)
        {
            string path = $"{Directory}/{name}.png";
            if (File.Exists(path) && !replace) return; // Never replace hand-edited menu art during normal generation.
            var image = new Texture2D(pixels.width, pixels.height, TextureFormat.RGBA32, false);
            image.SetPixels32(pixels.data);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            Object.DestroyImmediate(image);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            if (name == "ButtonPlay" || name == "ButtonForest")
                importer.spriteBorder = new Vector4(24, 10, 24, 10);
            importer.SaveAndReimport();
        }

        private static Pixels ButtonFrame(bool primary)
        {
            const int w = 180, h = 34;
            var art = new Pixels(w, h);
            // Concentric, equally inset rectangles with identical pixel-cut corners.
            art.Rect(2, 0, w - 4, h, Ink);
            art.Rect(0, 2, w, h - 4, Ink);
            art.Rect(1, 1, w - 2, h - 2, Ink);
            art.Rect(3, 2, w - 6, h - 4, Hex(0x82a66a));
            art.Rect(2, 3, w - 4, h - 6, Hex(0x82a66a));
            art.Rect(4, 3, w - 8, h - 6, Hex(0x3c6044));
            art.Rect(3, 4, w - 6, h - 8, Hex(0x3c6044));
            for (int y = 5; y < h - 5; y++)
            for (int x = 5; x < w - 5; x++)
            {
                float t = (y - 5f) / (h - 11);
                Color color = primary ? Color.Lerp(Hex(0x567f36), Hex(0x315c2e), t)
                    : Color.Lerp(Hex(0x1c3d35), Hex(0x102d2d), t);
                if (Noise(Mathf.Min(x, w - 1 - x), y) % 200 < 7) color *= 0.9f;
                color.a = 1f;
                art.Set(x, y, color);
            }
            art.Rect(6, 4, w - 12, 1, primary ? Hex(0xd1df8e) : Hex(0x688d56));
            art.Rect(6, h - 5, w - 12, 1, Hex(0x416c4d));
            for (int i = 0; i < 16; i++)
                art.Rect(5 + Noise(i, 4) % 17, 2 + Noise(i, 9) % 7, 2, 2, Greens[2 + Noise(i, 10) % 3]);
            art.Rect(6, 8, 2, 4, Greens[2]);
            Rock(art, 10, h - 5, 5);
            // Mirror the corner decorations too; neither edge has a heavier silhouette.
            for (int y = 0; y < h; y++)
            for (int x = 0; x < 24; x++)
                art.Set(w - 1 - x, y, art.data[(h - 1 - y) * w + x]);
            return art;
        }

        private static Pixels Frame(int w, int h, bool primary, bool moss = true)
        {
            var art = new Pixels(w, h);
            art.Rect(3, 4, w - 6, h - 4, Ink);
            art.Rect(1, 3, w - 2, h - 7, Ink);
            art.Rect(3, 1, w - 6, h - 5, Hex(0x0b242b));
            art.Rect(5, 2, w - 10, h - 7, Hex(0x82a66a));
            art.Rect(4, 4, w - 8, h - 10, Hex(0x5c814a));
            for (int y = 5; y < h - 7; y++) for (int x = 6; x < w - 6; x++)
            {
                Color color = primary ? Color.Lerp(Hex(0x567f36), Hex(0x315c2e), (float)y / h)
                    : Color.Lerp(Hex(0x1c3d35), Hex(0x102d2d), (float)y / h);
                int grain = Noise(x, y) % 200;
                if (grain < 7) color *= 0.88f;
                if (grain > 196) color *= 1.1f;
                art.Set(x, y, color);
            }
            art.Rect(8, 4, w - 16, 1, primary ? Hex(0xe3ebad) : Hex(0x688d56));
            art.Rect(8, h - 7, w - 16, 1, primary ? Hex(0x37ba8d) : Hex(0x416c4d));
            if (!moss) return art;
            for (int i = 0; i < 26; i++)
            {
                int dx = Noise(i, 4) % 18;
                int dy = Noise(i, 9) % 8;
                art.Rect(5 + dx, 2 + dy, 2, 2, Greens[2 + Noise(i, 10) % 3]);
                art.Rect(w - 8 - dx, 2 + dy, 2, 2, Greens[1 + Noise(i, 6) % 4]);
            }
            art.Line(5, 8, 3, h - 11, 1, Greens[2]);
            art.Rect(7, 8, 2, 5, Greens[3]);
            art.Rect(w - 11, 8, 2, 4, Greens[2]);
            Rock(art, 10, h - 7, 6);
            Rock(art, w - 11, h - 7, 6);
            return art;
        }

        private static readonly Dictionary<char, string[]> Glyphs = new Dictionary<char, string[]>
        {
            ['D'] = new[] {"1111100","1111110","1100111","1100011","1100011","1100011","1100111","1111110","1111100"},
            ['I'] = new[] {"1111111","1111111","0011100","0011100","0011100","0011100","0011100","1111111","1111111"},
            ['N'] = new[] {"1100011","1110011","1111011","1111011","1101111","1101111","1100111","1100011","1100011"},
            ['O'] = new[] {"0111110","1111111","1110111","1100011","1100011","1100011","1110111","1111111","0111110"},
            ['R'] = new[] {"1111110","1111111","1100011","1100011","1111110","1111100","1101110","1100111","1100011"},
            ['U'] = new[] {"1100011","1100011","1100011","1100011","1100011","1100011","1110111","1111111","0111110"},
            ['S'] = new[] {"0111111","1111111","1100000","1111100","0111110","0000111","0000011","1111111","1111110"},
            ['H'] = new[] {"1100011","1100011","1100011","1111111","1111111","1100011","1100011","1100011","1100011"}
        };

        private static Pixels Logo()
        {
            var art = new Pixels(340, 166);
            art.Poly(Ink, P(48, 13), P(286, 15), P(311, 65), P(301, 143), P(276, 161), P(57, 160), P(31, 132), P(30, 53));
            art.Poly(Hex(0x4c3e31), P(44, 75), P(296, 75), P(303, 135), P(288, 151), P(46, 149), P(34, 117));
            for (int i = 0; i < 48; i++)
            {
                int x = 36 + Noise(i, 1) % 266, y = 84 + Noise(i, 3) % 64;
                art.Rect(x, y, 4 + Noise(i, 5) % 12, 2, Hex(0x302f2b));
            }
            for (int side = 0; side < 2; side++)
            {
                int x = side == 0 ? 40 : 300;
                art.Line(x, 38, x + (side == 0 ? 4 : -4), 151, 4, Ink);
                art.Line(x, 38, x + (side == 0 ? 4 : -4), 151, 1, Greens[1]);
                for (int i = 0; i < 8; i++)
                {
                    int y = 24 + i * 17;
                    Leaf(art, x + (side == 0 ? -10 : 10), y, 10, side == 0 ? -1 : 1);
                    if (i % 2 == 0) Leaf(art, x + (side == 0 ? 8 : -8), y + 7, 7, side == 0 ? 1 : -1);
                }
            }
            Word(art, "DINO", 61, 9, true);
            Word(art, "RUSH", 61, 79, false);
            // Tiny sapling above the stone word.
            art.Line(169, 16, 170, 0, 1, Ink);
            art.Line(170, 15, 171, 2, 0, Greens[2]);
            Leaf(art, 164, 4, 5, -1);
            Leaf(art, 177, 5, 5, 1);
            return art;
        }

        private static void Word(Pixels art, string word, int left, int top, bool stone)
        {
            const int scale = 7;
            var mask = new bool[art.width * art.height];
            for (int letter = 0; letter < word.Length; letter++)
            {
                string[] glyph = Glyphs[word[letter]];
                for (int y = 0; y < 9; y++) for (int x = 0; x < 7; x++)
                    if (glyph[y][x] == '1')
                        for (int yy = 0; yy < scale; yy++) for (int xx = 0; xx < scale; xx++)
                            mask[(top + y * scale + yy) * art.width + left + letter * 56 + x * scale + xx] = true;
            }
            // Round the coarse glyph corners into chipped, hand-carved pixel silhouettes.
            var original = (bool[])mask.Clone();
            for (int y = top; y < top + 63; y++) for (int x = left; x < left + 217; x++)
            {
                int nearby = 0;
                for (int oy = -2; oy <= 2; oy++) for (int ox = -2; ox <= 2; ox++)
                    if (x + ox >= 0 && y + oy >= 0 && original[(y + oy) * art.width + x + ox]) nearby++;
                if (nearby < 11) mask[y * art.width + x] = false;
                else if (nearby > 16) mask[y * art.width + x] = true;
            }
            bool At(int x, int y) => x >= 0 && y >= 0 && x < art.width && y < art.height && mask[y * art.width + x];
            for (int y = 0; y < art.height; y++) for (int x = 0; x < art.width; x++)
                if (At(x, y))
                {
                    for (int oy = -3; oy <= 10; oy++) for (int ox = -3; ox <= 5; ox++) art.Set(x + ox, y + oy, Ink);
                }
            for (int depth = 8; depth >= 1; depth--)
                for (int y = 0; y < art.height; y++) for (int x = 0; x < art.width; x++)
                    if (At(x, y)) art.Set(x + 2, y + depth, stone ? Hex(0x686e63) : Hex(0x654735));
            for (int y = 0; y < art.height; y++) for (int x = 0; x < art.width; x++)
            {
                if (!At(x, y)) continue;
                float t = Mathf.Clamp01((y - top) / 63f);
                Color color = stone ? Color.Lerp(Hex(0xeee2bb), Hex(0xb4b19a), t)
                    : Color.Lerp(Hex(0xffc249), Hex(0xc17835), t);
                int grain = Noise(x / 2, y / 2, stone ? 5 : 9) % 180;
                if (grain < 12) color *= stone ? 0.79f : 0.88f;
                if (!At(x - 1, y) || !At(x, y - 1)) color = stone ? Hex(0xfff2cf) : Hex(0xffdb78);
                if (!At(x + 1, y) || !At(x, y + 2)) color *= 0.7f;
                art.Set(x, y, color);
            }
            for (int i = 0; i < 50; i++)
            {
                int x = left + Noise(i, 44) % 217, y = top + 18 + Noise(i, 47) % 44;
                if (At(x, y) && At(x + 2, y + 1)) art.Rect(x, y, 2, 2, stone ? Hex(0x96998b) : Hex(0xb27238));
            }
            if (stone)
            {
                for (int letter = 0; letter < 4; letter++)
                {
                    int x = left + letter * 56 + 9 + Noise(letter, 12) % 25;
                    for (int y = top + 38; y < top + 62; y++)
                    {
                        if (y % 4 == 0) x += Noise(letter, y) % 3 - 1;
                        if (At(x, y)) art.Set(x, y, Hex(0x7c8376));
                        if (At(x + 1, y)) art.Set(x + 1, y, Hex(0xd3cdb0));
                    }
                }
            }
            // Moss follows the top edge, including irregular drips and bright new growth.
            for (int x = left; x < left + 217; x++)
            {
                int y = top;
                while (y < top + 63 && !At(x, y)) y++;
                if (y >= top + 63 || (!stone && x % 53 > 10)) continue;
                int depth = stone ? 2 + Noise(x / 2, 19) % 5 : Noise(x / 2, 7) % 4;
                if (x % 17 < 3) depth += 4;
                for (int d = -2; d < depth; d++)
                    art.Set(x, y + d, Greens[Mathf.Clamp(4 - d / 3 + Noise(x, d) % 2, 1, 4)]);
            }
        }

        private static void Leaf(Pixels art, int x, int y, int size, int direction)
        {
            art.Poly(Ink, P(x - size, y - size / 2), P(x + size * direction, y - size), P(x + size, y + size / 2), P(x, y + size));
            art.Poly(Greens[2], P(x - size + 2, y - size / 2 + 1), P(x + (size - 2) * direction, y - size + 2), P(x + size - 2, y + size / 2 - 1), P(x, y + size - 2));
            art.Line(x - size / 2, y, x + size / 2, y - 1, 0, Greens[4]);
        }

        private static void Rock(Pixels art, int x, int bottom, int size)
        {
            art.Poly(Hex(0x394e51), P(x - size, bottom), P(x - size + 1, bottom - size), P(x - 1, bottom - size - 2), P(x + size - 2, bottom - size + 1), P(x + size, bottom));
            art.Poly(Hex(0x91988a), P(x - size + 1, bottom - 1), P(x - size + 2, bottom - size), P(x - 1, bottom - size - 1), P(x + 1, bottom - 2));
            art.Line(x - size + 3, bottom - size, x - 1, bottom - size - 1, 0, Hex(0xd1cdb0));
            art.Rect(x + 1, bottom - 4, Mathf.Max(2, size / 2), 3, Hex(0x59696a));
        }

        private static Pixels ForestFrame()
        {
            var art = new Pixels(640, 360);
            // A diagonal, shaded trunk and branching canopy frame the upper-left corner.
            art.Poly(Hex(0x193735), P(0, 0), P(94, 0), P(65, 25), P(28, 70), P(0, 133));
            art.Poly(Hex(0x594735), P(0, 13), P(68, 0), P(75, 0), P(19, 69), P(0, 106));
            art.Line(0, 96, 70, 8, 3, Hex(0x795b3c));
            art.Line(5, 82, 50, 30, 1, Hex(0x9a7650));
            for (int i = 0; i < 36; i++)
            {
                int x = Noise(i, 1) % 120 - 12, y = Noise(i, 3) % 68 - 20;
                Foliage(art, x, y, 10 + Noise(i, 4) % 18, false);
            }
            for (int i = 0; i < 25; i++)
                Foliage(art, 573 + Noise(i, 6) % 88, -20 + Noise(i, 8) % 77, 10 + Noise(i, 2) % 22, true);

            // Leave the trail clear. The supplied forest/ground sprites provide its
            // foliage; a second UI grass/log layer clashes with that artwork.
            return art;
        }

        private static void Foliage(Pixels art, int x, int y, int size, bool dark)
        {
            for (int dy = -size - 3; dy <= size + 3; dy++) for (int dx = -size - 3; dx <= size + 3; dx++)
            {
                int jaggedRadius = size + Noise((x + dx) / 3, (y + dy) / 3) % 7 - 3;
                if (dx * dx + dy * dy > jaggedRadius * jaggedRadius) continue;
                art.Set(x + dx, y + dy, Hex(dark ? 0x0c3439u : 0x173d38u));
            }
            for (int i = 0; i < 34; i++)
            {
                int dx = Noise(i, x) % (size * 2) - size, dy = Noise(i, y) % (size * 2) - size;
                if (dx * dx + dy * dy > (size - 3) * (size - 3)) continue;
                int r = 2 + Noise(i, y + 7) % 4;
                Color32 color = dark ? Hex(i % 3 == 0 ? 0x1e4841u : 0x143d3du) : Greens[1 + Noise(i, x + y) % 2];
                art.Disc(x + dx, y + dy, r, color);
                if (!dark)
                {
                    art.Line(x + dx - r + 1, y + dy - 1, x + dx, y + dy - r + 1, 0, Greens[2 + i % 2]);
                    if (i % 4 == 0) art.Rect(x + dx - 1, y + dy - r, 3, 2, Greens[4]);
                }
            }
        }

        private static Pixels Icon(string name)
        {
            var art = new Pixels(28, 28);
            Color32 color = Hex(name == "Play" ? 0xf0f3cbu : 0xb8d79bu);
            if (name == "Play") art.Poly(color, P(6, 3), P(23, 14), P(6, 25));
            if (name == "Pause")
            {
                art.Rect(6, 4, 5, 20, color);
                art.Rect(17, 4, 5, 20, color);
            }
            if (name == "Restart")
            {
                art.Line(8, 5, 20, 5, 1, color);
                art.Line(20, 5, 24, 10, 1, color);
                art.Line(24, 10, 24, 20, 1, color);
                art.Line(24, 20, 19, 24, 1, color);
                art.Line(19, 24, 7, 24, 1, color);
                art.Line(7, 24, 3, 19, 1, color);
                art.Poly(color, P(3, 2), P(3, 13), P(14, 13));
            }
            if (name == "Jump")
            {
                art.Poly(color, P(3, 13), P(14, 2), P(25, 13));
                art.Rect(10, 11, 8, 15, color);
            }
            if (name == "Gear")
            {
                art.Disc(14, 14, 9, color);
                for (int i = 0; i < 8; i++)
                {
                    float a = i * Mathf.PI / 4;
                    art.Rect(12 + Mathf.RoundToInt(Mathf.Cos(a) * 10), 12 + Mathf.RoundToInt(Mathf.Sin(a) * 10), 5, 5, color);
                }
                art.Disc(14, 14, 4, Hex(0x1a3c34));
            }
            if (name == "Dino")
            {
                art.Rect(11, 3, 10, 8, color); art.Rect(8, 7, 11, 14, color);
                art.Rect(18, 7, 7, 5, color); art.Rect(5, 17, 13, 6, color);
                art.Rect(5, 23, 5, 3, color); art.Rect(15, 22, 4, 4, color);
                art.Rect(2, 19, 4, 4, color); art.Rect(18, 5, 2, 2, Ink);
                art.Rect(19, 13, 4, 2, color);
            }
            if (name == "Quit")
            {
                art.Rect(13, 3, 12, 23, color); art.Rect(15, 5, 7, 18, Hex(0x183a33));
                art.Rect(2, 12, 14, 5, color); art.Poly(color, P(10, 7), P(19, 14), P(10, 22));
            }
            if (name == "Speaker" || name == "Muted")
            {
                art.Rect(3, 10, 7, 10, color); art.Poly(color, P(8, 9), P(17, 3), P(17, 26), P(8, 21));
                if (name == "Speaker")
                {
                    art.Line(20, 8, 22, 11, 0, color); art.Line(22, 11, 22, 18, 0, color); art.Line(22, 18, 20, 21, 0, color);
                    art.Line(24, 5, 26, 10, 0, color); art.Line(26, 10, 26, 19, 0, color); art.Line(26, 19, 24, 24, 0, color);
                }
                else { art.Line(20, 10, 26, 19, 1, color); art.Line(26, 10, 20, 19, 1, color); }
            }
            return art;
        }
    }
}
