using System.IO;
using UnityEditor;
using UnityEngine;

namespace DinoRush.Editor
{
    /// <summary>Small point-filtered sprites for the reference-inspired gameplay HUD.</summary>
    internal static class ForestHudArt
    {
        internal const string Directory = "Assets/UI/ForestHud";
        private static Color32 C(uint hex) => PixelMenuArt.Hex(hex);
        private static Vector2Int P(int x, int y) => new Vector2Int(x, y);

        internal static void Generate()
        {
            if (!AssetDatabase.IsValidFolder(Directory)) AssetDatabase.CreateFolder("Assets/UI", "ForestHud");
            Save("HudPanel", Panel(130, 36, false), new Vector4(8, 8, 8, 8));
            Save("HudJump", Panel(124, 44, true), new Vector4(10, 10, 10, 10));
            Save("HudControl", Panel(32, 32, true), new Vector4(7, 7, 7, 7));
            Save("HudPace", Panel(170, 22, false), new Vector4(7, 7, 7, 7));
            Save("HudKey", Key(), new Vector4(4, 4, 4, 4));
            Save("HudMeter", Meter(), new Vector4(2, 1, 2, 1));
            Save("HudLeaves", Leaves());
            Save("HudPine", Pine());
            Save("HudTrophy", Trophy());
            Save("HudCrown", Crown());
            Save("HudBottomFade", Fade());
            Save("HudJumpArrow", Icon("Jump"));
            foreach (string icon in new[] { "Speaker", "Muted", "Pause", "Dino" })
                Save("Hud" + icon, Icon(icon));
        }

        internal static Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Directory}/{name}.png");

        private static void Save(string name, PixelMenuArt.Pixels art, Vector4 border = default)
        {
            string path = $"{Directory}/{name}.png";
            if (File.Exists(path)) return; // Preserve hand-edited HUD art on subsequent theme updates.
            var image = new Texture2D(art.width, art.height, TextureFormat.RGBA32, false);
            image.SetPixels32(art.data);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            Object.DestroyImmediate(image);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spriteBorder = border;
            importer.filterMode = name == "HudBottomFade" ? FilterMode.Bilinear : FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        private static void Rounded(PixelMenuArt.Pixels art, int inset, int radius, Color32 color)
        {
            int right = art.width - inset, bottom = art.height - inset;
            for (int y = inset; y < bottom; y++)
            for (int x = inset; x < right; x++)
            {
                int dx = Mathf.Max(inset + radius - x, x - (right - radius - 1), 0);
                int dy = Mathf.Max(inset + radius - y, y - (bottom - radius - 1), 0);
                if (dx * dx + dy * dy <= radius * radius) art.Set(x, y, color);
            }
        }

        private static PixelMenuArt.Pixels Panel(int width, int height, bool green)
        {
            var art = new PixelMenuArt.Pixels(width, height);
            Rounded(art, 0, 6, C(0x0a242b));
            Rounded(art, 1, 5, C(green ? 0x6e9652u : 0x577456u));
            Rounded(art, 2, 4, C(green ? 0x355b38u : 0x243f42u));
            Rounded(art, 3, 3, C(green ? 0x2e522fu : 0x132d37u));
            for (int y = 4; y < height - 4; y++)
            for (int x = 5; x < width - 5; x++)
            {
                Color shade = green ? Color.Lerp(C(0x426c35), C(0x24452e), (float)y / height)
                    : Color.Lerp(C(0x19343e), C(0x102630), (float)y / height);
                art.Set(x, y, shade);
            }
            art.Rect(7, 2, width - 14, 1, C(green ? 0x9bbb65u : 0x6f875eu));
            art.Rect(7, height - 3, width - 14, 1, C(green ? 0x4e753fu : 0x29454au));
            return art;
        }

        private static PixelMenuArt.Pixels Leaves()
        {
            var art = new PixelMenuArt.Pixels(24, 24);
            art.Line(4, 4, 19, 20, 1, C(0x325338));
            for (int i = 0; i < 4; i++)
            {
                int x = 5 + i * 4, y = 4 + i * 4;
                art.Poly(C(0x699638), P(x, y + 3), P(x - 6, y + 1), P(x - 5, y - 4), P(x, y - 2), P(x + 2, y + 2));
                art.Poly(C(0xa4c94d), P(x, y), P(x - 5, y - 2), P(x - 4, y - 4), P(x, y - 2));
                art.Poly(C(0x8dbb40), P(x + 1, y + 4), P(x + 2, y - 2), P(x + 6, y - 4), P(x + 7, y + 1), P(x + 4, y + 5));
                art.Rect(x + 3, y - 2, 2, 3, C(0xc2d966));
            }
            return art;
        }

        private static PixelMenuArt.Pixels Pine()
        {
            var art = new PixelMenuArt.Pixels(32, 44);
            art.Rect(13, 28, 7, 15, C(0x26392e));
            art.Rect(15, 28, 4, 15, C(0x8d6537));
            art.Rect(15, 30, 2, 12, C(0xb48a46));
            for (int tier = 3; tier >= 0; tier--)
            {
                int top = 1 + tier * 7, half = 6 + tier * 3, bottom = top + 13;
                art.Poly(C(0x173e32), P(16, top), P(16 + half, bottom), P(16 + half - 2, bottom + 2), P(16 - half, bottom + 2));
                art.Poly(C(0x398338), P(16, top + 1), P(16 + half - 2, bottom), P(16 - half + 2, bottom));
                art.Poly(C(0x6ba936), P(16, top + 1), P(16, bottom - 2), P(16 - half + 2, bottom));
                art.Line(15, top + 3, 12 - tier * 2, bottom - 2, 0, C(0xb0ce50));
            }
            return art;
        }

        private static PixelMenuArt.Pixels Trophy()
        {
            var art = new PixelMenuArt.Pixels(28, 28);
            Color32 gold = C(0xf6b52f), light = C(0xffe47e), shade = C(0xc77d17);
            art.Rect(2, 5, 6, 10, shade); art.Rect(20, 5, 6, 10, shade);
            art.Rect(3, 5, 5, 2, light); art.Rect(21, 5, 4, 2, light);
            art.Rect(4, 7, 3, 5, C(0x19343e)); art.Rect(21, 7, 3, 5, C(0x19343e));
            art.Poly(shade, P(7, 3), P(21, 3), P(20, 15), P(16, 19), P(12, 19), P(8, 15));
            art.Poly(gold, P(8, 3), P(20, 3), P(19, 14), P(15, 18), P(12, 17), P(9, 13));
            art.Rect(8, 3, 12, 2, light); art.Rect(10, 6, 2, 7, light);
            art.Rect(13, 17, 3, 7, gold); art.Rect(9, 24, 11, 2, shade);
            art.Rect(10, 23, 9, 2, light);
            return art;
        }

        private static PixelMenuArt.Pixels Crown()
        {
            var art = new PixelMenuArt.Pixels(28, 28);
            art.Poly(C(0xc77d17), P(3, 8), P(9, 14), P(14, 5), P(19, 14), P(25, 8), P(23, 24), P(5, 24));
            art.Poly(C(0xffc53c), P(4, 8), P(9, 15), P(14, 5), P(19, 15), P(24, 8), P(22, 21), P(6, 21));
            art.Line(6, 22, 22, 22, 0, C(0xffe481));
            art.Rect(13, 8, 2, 9, C(0xffe481)); art.Rect(7, 15, 2, 4, C(0xffe481));
            return art;
        }

        private static PixelMenuArt.Pixels Key()
        {
            var art = new PixelMenuArt.Pixels(40, 20);
            Rounded(art, 0, 3, C(0x40565a)); Rounded(art, 1, 2, C(0x677679));
            Rounded(art, 2, 1, C(0x1c2d34));
            art.Rect(4, 17, 32, 1, C(0x34484d));
            return art;
        }

        private static PixelMenuArt.Pixels Meter()
        {
            var art = new PixelMenuArt.Pixels(40, 8);
            Rounded(art, 0, 2, C(0x4d7046)); Rounded(art, 1, 1, C(0x0b2429));
            return art;
        }

        private static PixelMenuArt.Pixels Fade()
        {
            var art = new PixelMenuArt.Pixels(2, 96);
            for (int y = 0; y < art.height; y++)
            {
                Color32 shade = C(0x041820);
                shade.a = (byte)Mathf.RoundToInt(235 * Mathf.SmoothStep(0, 1, y / 95f));
                art.Rect(0, y, 2, 1, shade);
            }
            return art;
        }

        private static PixelMenuArt.Pixels Icon(string name)
        {
            var art = new PixelMenuArt.Pixels(28, 28);
            Color32 cream = C(0xf5edbc);
            if (name == "Speaker" || name == "Muted")
            {
                art.Rect(3, 10, 7, 9, cream);
                art.Poly(cream, P(8, 10), P(17, 3), P(17, 25), P(8, 19));
                if (name == "Speaker")
                {
                    art.Line(20, 8, 22, 11, 0, cream); art.Line(22, 11, 22, 18, 0, cream);
                    art.Line(22, 18, 20, 21, 0, cream);
                    art.Line(24, 5, 26, 10, 0, cream); art.Line(26, 10, 26, 19, 0, cream);
                    art.Line(26, 19, 24, 24, 0, cream);
                }
                else { art.Line(20, 10, 26, 19, 1, cream); art.Line(26, 10, 20, 19, 1, cream); }
            }
            if (name == "Pause") { art.Rect(6, 4, 5, 20, cream); art.Rect(17, 4, 5, 20, cream); }
            if (name == "Jump")
            {
                art.Poly(cream, P(3, 13), P(14, 2), P(25, 13)); art.Rect(10, 11, 8, 15, cream);
            }
            if (name == "Dino")
            {
                art.Rect(12, 3, 10, 8, cream); art.Rect(9, 9, 10, 12, cream);
                art.Rect(19, 7, 6, 5, cream); art.Rect(5, 17, 13, 6, cream);
                art.Rect(4, 22, 5, 4, cream); art.Rect(15, 22, 4, 4, cream);
                art.Rect(2, 17, 4, 5, cream); art.Set(19, 5, C(0x19343e));
            }
            return art;
        }
    }
}
