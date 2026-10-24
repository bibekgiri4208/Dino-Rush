using System.Collections.Generic;
using UnityEngine;

namespace DinoRush
{
    /// <summary>Gnarled shaded timber, moss, leaf clusters and readable downward thorns; upper vines are decorative.</summary>
    public static class RunnerCanopyArt
    {
        public static Sprite Create(List<Sprite> sprites, List<Texture2D> textures)
        {
            const int width = 96, height = 320;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Low thorn canopy", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                Color32 color = new Color32(0, 0, 0, 0);
                int leftVine = 22 + Mathf.RoundToInt(Mathf.Sin(y * 0.075f) * 2);
                int rightVine = 74 + Mathf.RoundToInt(Mathf.Sin(y * 0.055f + 2) * 3);
                if (y > 28 && (Mathf.Abs(x - leftVine) <= 1 || Mathf.Abs(x - rightVine) <= 1))
                    color = new Color32(65, 96, 48, 200);
                if (y > 48 && y % 58 < 5 && (Mathf.Abs(x - leftVine - 3) < 3 || Mathf.Abs(x - rightVine + 3) < 3))
                    color = new Color32(103, 139, 63, 220);
                if (y < 46)
                {
                    int bottom = 12 + Mathf.RoundToInt(Mathf.Sin(x * 0.11f) * 2);
                    int top = 27 + Mathf.RoundToInt(Mathf.Sin(x * 0.09f + 1) * 2);
                    int edge = x < 7 ? 7 - x : x > 88 ? x - 88 : 0;
                    bool wood = x >= 1 && x < 95 && y >= bottom + edge && y <= top - edge;
                    if (wood)
                    {
                        color = y <= bottom + edge + 1 || y >= top - edge - 1 ? new Color32(44, 31, 25, 255) :
                            y < bottom + 5 ? new Color32(80, 47, 30, 255) : y > top - 5 ? new Color32(182, 124, 61, 255) : new Color32(130, 78, 40, 255);
                        int grain = (y + Mathf.RoundToInt(Mathf.Sin(x * 0.13f) * 2)) % 6;
                        if (grain == 0 && x % 23 > 3 && y > bottom + 2 && y < top - 2) color = new Color32(74, 44, 29, 255);
                        float knot = (x - 61) * (x - 61) / 36f + (y - 20) * (y - 20) / 9f;
                        if (knot < 1.6f) color = knot < 0.45f ? new Color32(44, 31, 25, 255) : knot < 0.9f ? new Color32(196, 141, 76, 255) : new Color32(88, 52, 32, 255);
                    }
                    // Five tapered warm thorns make the dangerous lower edge readable against all maps.
                    for (int thorn = 0; thorn < 5; thorn++)
                    {
                        int center = 14 + thorn * 17, tip = thorn % 2 == 0 ? 0 : 3;
                        if (y >= tip && y <= 15 && Mathf.Abs(x - center) <= (y - tip) * 0.32f)
                            color = Mathf.Abs(x - center) >= (y - tip) * 0.32f - 1 ? new Color32(105, 49, 31, 255) :
                                x <= center ? new Color32(255, 219, 143, 255) : new Color32(200, 117, 61, 255);
                    }
                    if (x > 5 && x < 91 && y >= top - 1 && y <= top + 2 + (x / 5 % 3))
                        color = y > top + 1 ? new Color32(151, 181, 73, 255) : new Color32(66, 108, 45, 255);
                    // Serrated clusters sit above the timber, leaving the space below entirely clear.
                    for (int leaf = 0; leaf < 5; leaf++)
                    {
                        int cx = 9 + leaf * 19, cy = 31 + leaf % 2 * 4;
                        float oval = (x - cx) * (x - cx) / 85f + (y - cy) * (y - cy) / 30f;
                        if (oval < 1 && (x + y) % 7 != 0)
                            color = oval > 0.72f ? new Color32(35, 71, 40, 255) : y > cy ? new Color32(145, 181, 73, 255) : new Color32(74, 124, 49, 255);
                    }
                }
                pixels[y * width + x] = color;
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0), 40f,
                0, SpriteMeshType.FullRect);
            sprite.name = "Low thorn canopy";
            sprites.Add(sprite);
            textures.Add(texture);
            return sprite;
        }
    }
}
