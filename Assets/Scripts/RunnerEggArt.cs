using System.Collections.Generic;
using UnityEngine;

namespace DinoRush
{
    /// <summary>Original, small pixel-art eggs and gold glints; supplied artwork is never modified.</summary>
    public static class RunnerEggArt
    {
        public static Sprite CreateEgg(List<Object> assets)
        {
            var texture = new Texture2D(16, 20, TextureFormat.RGBA32, false)
            {
                name = "Speckled dinosaur egg", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[16 * 20];
            int[] widths = { 0, 2, 3, 4, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 5, 5, 4, 2, 0 };
            for (int y = 0; y < 20; y++)
            for (int x = 0; x < 16; x++)
            {
                int left = 8 - widths[y], right = 7 + widths[y];
                if (x < left || x > right) continue;
                bool edge = x == left || x == right || y == 1 || y == 18;
                Color32 color = edge ? new Color32(93, 70, 40, 255) :
                    x > 10 || y > 15 ? new Color32(213, 164, 79, 255) :
                    x < 6 && y < 10 ? new Color32(255, 249, 202, 255) : new Color32(245, 218, 145, 255);
                bool spot = x >= 5 && x <= 7 && y >= 6 && y <= 7 ||
                    x >= 9 && x <= 11 && y >= 10 && y <= 12 || x >= 4 && x <= 5 && y >= 13 && y <= 14;
                if (!edge && spot) color = new Color32(115, 151, 95, 255);
                pixels[(19 - y) * 16 + x] = color;
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return SaveSprite(texture, 32f, assets);
        }

        public static Sprite CreateSparkle(List<Object> assets)
        {
            var texture = new Texture2D(7, 7, TextureFormat.RGBA32, false)
            {
                name = "Egg pickup sparkle", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
            };
            for (int y = 0; y < 7; y++)
            for (int x = 0; x < 7; x++)
            {
                int dx = Mathf.Abs(x - 3), dy = Mathf.Abs(y - 3);
                bool filled = dx + dy <= 1 || dx == 0 || dy == 0;
                texture.SetPixel(x, y, filled ? Color.white : Color.clear);
            }
            texture.Apply();
            return SaveSprite(texture, 24f, assets);
        }

        private static Sprite SaveSprite(Texture2D texture, float ppu, List<Object> assets)
        {
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                Vector2.one * 0.5f, ppu, 0, SpriteMeshType.FullRect);
            sprite.name = texture.name;
            assets.Add(sprite);
            assets.Add(texture);
            return sprite;
        }
    }
}
