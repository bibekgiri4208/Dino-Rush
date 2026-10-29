using System;
using System.Collections.Generic;
using UnityEngine;

namespace DinoRush
{
    /// <summary>Small original pixel props; no downloaded assets or changes to the supplied sprites.</summary>
    public sealed class RunnerStoryArt : IDisposable
    {
        private readonly List<UnityEngine.Object> assets = new List<UnityEngine.Object>();
        public Sprite Token { get; }
        public Sprite Guardian { get; }
        public Sprite Egg { get; }
        public Sprite Gate { get; }
        public Sprite Ruin { get; }
        public Sprite Star { get; }
        public Sprite Nest { get; }
        public Sprite Tree { get; }
        public Sprite Crack { get; }
        public Sprite Footprint { get; }
        public Sprite Ribbon { get; }
        public Sprite Dust { get; }
        public Sprite Wall { get; }
        public Sprite Chasm { get; }
        public Sprite GuardianBody { get; }
        public Sprite GuardianNeck { get; }
        public Sprite GuardianHead { get; }
        public Sprite[] HatchlingFrames { get; }
        public RunnerStoryArt()
        {
            Token = Draw("Trail compass", 24, 24, (x, y) =>
            {
                int d = Mathf.Abs(x - 12) + Mathf.Abs(y - 12);
                if (d > 11) return Color.clear;
                if (d > 8) return new Color32(30, 100, 139, 255);
                if (x == 12 || y == 12 || d < 3) return new Color32(241, 255, 238, 255);
                return new Color32(89, 226, 255, 255);
            });
            Guardian = Draw("Mountain guardian", 64, 64, (x, y) =>
            {
                bool body = Ellipse(x, y, 28, 21, 18, 13), neck = x >= 37 && x < 48 && y >= 23 && y < 50;
                bool head = Ellipse(x, y, 48, 50, 12, 8), snout = x >= 48 && x < 63 && y >= 46 && y < 52;
                bool legs = (x >= 16 && x < 24 || x >= 35 && x < 43) && y >= 3 && y < 20;
                bool tail = x < 17 && y >= 13 && y < 15 + x / 2;
                bool crest = x >= 40 && x < 54 && y >= 54 && y < 61 - Mathf.Abs(x - 45);
                if (!(body || neck || head || snout || legs || tail || crest)) return Color.clear;
                if (x >= 51 && x < 54 && y >= 50 && y < 53) return new Color32(255, 222, 126, 255);
                if (y < 8 || (x + y * 3) % 17 < 2) return new Color32(27, 51, 61, 255);
                return y > 30 ? new Color32(72, 105, 116, 255) : new Color32(49, 79, 87, 255);
            });
            Egg = Draw("New beginning egg", 24, 24, (x, y) =>
            {
                if (!Ellipse(x, y, 12, 10, 8 - y / 8, 10)) return Color.clear;
                if ((x < 9 && y > 9 && y < 14) || (x > 13 && x < 17 && y > 4 && y < 8))
                    return new Color32(83, 153, 144, 255);
                return x < 9 ? new Color32(255, 247, 202, 255) : new Color32(217, 224, 168, 255);
            });
            Gate = Draw("Ancient valley gate", 48, 48, (x, y) =>
            {
                bool pillar = (x > 2 && x < 11 || x > 36 && x < 45) && y < 40;
                bool arch = y > 34 && y < 45 && x > 4 && x < 44;
                if (!(pillar || arch)) return Color.clear;
                return (x + y) % 9 == 0 || y % 8 == 0 ? new Color32(63, 86, 87, 255) : new Color32(169, 186, 168, 255);
            });
            Ruin = Draw("Migration trail tablet", 48, 48, (x, y) =>
            {
                if (x < 4 || x > 43 || y < 2 || y > 43 - Mathf.Abs(x - 24) / 8) return Color.clear;
                if (y > 31 && y < 35 || y > 18 && y < 22 && x % 9 < 5 || y > 6 && y < 10 && x > 12 && x < 36)
                    return new Color32(97, 204, 188, 255);
                return (x + y * 3) % 13 == 0 ? new Color32(72, 94, 85, 255) : new Color32(124, 142, 115, 255);
            });
            Star = Draw("Story achievement star", 24, 24, (x, y) =>
            {
                float a = Mathf.Atan2(y - 12, x - 12), r = new Vector2(x - 12, y - 12).magnitude;
                float edge = 7.5f + 3.5f * Mathf.Cos(a * 5 - Mathf.PI * 0.5f);
                if (r > edge) return Color.clear;
                return r > edge - 2 ? new Color32(158, 104, 35, 255) : x + y > 22 ? new Color32(255, 228, 137, 255) : new Color32(236, 179, 63, 255);
            });
            Nest = Draw("Woven nesting hollow", 64, 24, (x, y) =>
            {
                if (!Ellipse(x, y, 32, 10, 30, 10)) return Color.clear;
                bool hollow = Ellipse(x, y, 32, 14, 22, 5);
                if (hollow) return new Color32(53, 47, 30, 255);
                return (x * 3 + y * 7) % 11 < 4 ? new Color32(172, 141, 71, 255) : new Color32(106, 86, 42, 255);
            });
            Tree = Draw("Breakable forest tree", 64, 128, (x, y) =>
            {
                bool trunk = x >= 27 - y / 38 && x <= 37 - y / 42 && y < 105;
                bool branches = y > 46 && y < 80 && Mathf.Abs(x - 32) < (y - 44) / 2 && Mathf.Abs(x - 32) > (y - 52) / 2;
                bool leaves = Ellipse(x, y, 20, 90, 19, 20) || Ellipse(x, y, 42, 96, 21, 22) || Ellipse(x, y, 31, 112, 20, 15);
                if (leaves)
                {
                    int grain = (x * 17 + y * 11) % 23;
                    return grain < 4 ? new Color32(135, 166, 76, 255) : y % 11 < 4 ? new Color32(71, 116, 63, 255) : new Color32(42, 87, 57, 255);
                }
                if (trunk || branches) return x % 7 < 2 ? new Color32(68, 58, 39, 255) : new Color32(121, 98, 58, 255);
                return Color.clear;
            });
            Crack = Draw("Branching earthquake fissure", 128, 24, (x, y) =>
            {
                int center = 12 + (x / 9 % 2 == 0 ? x % 9 / 2 : 4 - x % 9 / 2);
                bool split = Mathf.Abs(y - center) < 2 || x % 25 < 3 && Mathf.Abs(y - center) < 8;
                return split ? new Color32(12, 23, 25, 255) : Color.clear;
            });
            Footprint = Draw("Three-toed tracks", 20, 20, (x, y) =>
            {
                bool foot = Ellipse(x, y, 10, 7, 4, 5) || Ellipse(x, y, 5, 14, 2, 3) ||
                    Ellipse(x, y, 10, 16, 2, 3) || Ellipse(x, y, 15, 14, 2, 3);
                return foot ? new Color32(44, 52, 38, 210) : Color.clear;
            });
            Ribbon = Draw("Sena's leaf trail marker", 24, 32, (x, y) =>
            {
                bool leaf = Ellipse(x, y, 12, 17, 7, 13) && x + y > 14;
                if (!leaf) return Color.clear;
                return Mathf.Abs(x - 12 + (y - 15) / 4) < 2 ? new Color32(224, 224, 121, 255) : new Color32(106, 170, 99, 255);
            });
            Dust = Draw("Soft pixel dust", 16, 16, (x, y) =>
            {
                float radius = new Vector2(x - 7.5f, y - 7.5f).magnitude / 8;
                return radius > 1 ? Color.clear : new Color(1, 1, 1, Mathf.Round((1 - radius) * 4) / 4);
            });
            Wall = Draw("Ancient illustrated wall", 128, 80, (x, y) =>
            {
                if (x < 2 || x > 125 || y < 2 || y > 77 - Mathf.Abs(x - 64) / 30) return Color.clear;
                bool seam = y % 16 == 0 || (x + y / 16 % 2 * 16) % 32 == 0;
                if (seam) return new Color32(58, 77, 66, 255);
                int grain = (x * 29 + y * 19) % 47;
                return grain < 3 ? new Color32(157, 169, 121, 255) : grain < 8 ? new Color32(100, 127, 96, 255) : new Color32(121, 144, 105, 255);
            });
            Chasm = Draw("Broken trail rock faces", 96, 64, (x, y) =>
            {
                int edge = 7 + y / 12 + y / 5 % 3;
                if (x < edge || x > 95 - edge)
                {
                    if (y % 13 < 2 || x % 9 < 2) return new Color32(44, 61, 75, 255);
                    return new Color32(80, 108, 126, 255);
                }
                float depth = y / 64f;
                return new Color(0.035f + depth * 0.025f, 0.075f + depth * 0.04f, 0.105f + depth * 0.05f, 1);
            });
            GuardianBody = Draw("Guardian armored body", 96, 56, (x, y) =>
            {
                bool torso = Ellipse(x, y, 57, 27, 32, 22), tail = x < 43 && y > 8 + x / 9 && y < 12 + x / 2;
                bool legs = (x > 36 && x < 48 || x > 71 && x < 84) && y < 22;
                if (!(torso || tail || legs)) return Color.clear;
                bool plate = y > 36 && (x / 9) % 2 == 0;
                if (plate) return new Color32(136, 159, 148, 255);
                return (x / 5 + y / 4) % 4 == 0 ? new Color32(69, 107, 110, 255) : y < 15 ? new Color32(43, 73, 79, 255) : new Color32(88, 130, 132, 255);
            });
            GuardianNeck = Draw("Guardian articulated neck", 24, 64, (x, y) =>
            {
                int center = 12 + y / 16;
                if (Mathf.Abs(x - center) > 8 - y / 24) return Color.clear;
                return x < center ? new Color32(115, 152, 144, 255) : y % 9 < 2 ? new Color32(51, 87, 94, 255) : new Color32(75, 119, 125, 255);
            });
            GuardianHead = Draw("Guardian expressive head", 64, 40, (x, y) =>
            {
                bool face = Ellipse(x, y, 31, 20, 22, 15) || x > 35 && x < 63 && y > 6 && y < 24;
                bool crest = y > 27 && y < 38 - Mathf.Abs(x - 23) / 2 && x > 10 && x < 38;
                if (!(face || crest)) return Color.clear;
                if (x > 34 && x < 42 && y > 21 && y < 28) return x > 38 ? new Color32(20, 40, 46, 255) : new Color32(255, 218, 118, 255);
                if (y == 10 && x > 41 || x > 56 && y > 17 && y < 20) return new Color32(31, 64, 69, 255);
                return crest || y > 28 ? new Color32(151, 172, 147, 255) : x % 11 < 2 ? new Color32(65, 108, 112, 255) : new Color32(103, 147, 144, 255);
            });
            HatchlingFrames = new Sprite[4];
            for (int frame = 0; frame < HatchlingFrames.Length; frame++)
            {
                int phase = frame;
                HatchlingFrames[frame] = Draw("Guardian hatchling walk " + frame, 32, 32, (x, y) =>
                {
                    bool body = Ellipse(x, y, 14, 12, 9, 7), head = Ellipse(x, y, 23, 23, 7, 7);
                    bool neck = x > 18 && x < 24 && y > 12 && y < 25;
                    bool feet = (x > 9 && x < 13 && y > (phase % 2 == 0 ? 1 : 4) && y < 11) ||
                        (x > 17 && x < 21 && y > (phase % 2 == 0 ? 4 : 1) && y < 11);
                    bool tail = x < 10 && y > 7 && y < 8 + x;
                    if (!(body || head || neck || feet || tail)) return Color.clear;
                    if (x > 25 && x < 28 && y > 24 && y < 27) return new Color32(25, 57, 60, 255);
                    return x + y > 32 ? new Color32(171, 207, 158, 255) : new Color32(99, 157, 137, 255);
                });
            }
        }
        private static bool Ellipse(int x, int y, float cx, float cy, float rx, float ry) =>
            (x - cx) * (x - cx) / (rx * rx) + (y - cy) * (y - cy) / (ry * ry) <= 1;
        private Sprite Draw(string name, int w, int h, Func<int, int, Color> pixel)
        {
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = name, filterMode = FilterMode.Point };
            var colors = new Color[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) colors[y * w + x] = pixel(x, y);
            texture.SetPixels(colors); texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, w, h), Vector2.one * 0.5f, h);
            sprite.name = name; assets.Add(sprite); assets.Add(texture); return sprite;
        }
        public void Dispose()
        {
            foreach (var asset in assets) if (asset != null) UnityEngine.Object.Destroy(asset);
            assets.Clear();
        }
    }
}
