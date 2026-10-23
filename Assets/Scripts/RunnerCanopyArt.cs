using System.Collections.Generic;
using UnityEngine;

namespace DinoRush
{
    /// <summary>A compact, irregular thorn branch suspended by two thin, non-solid vines.</summary>
    public static class RunnerCanopyArt
    {
        public static Sprite Create(List<Sprite> sprites, List<Texture2D> textures)
        {
            const int width = 48, height = 280;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Low thorn canopy", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int leftVine = 10 + y / 18 % 3 - 1, rightVine = 37 - y / 21 % 3;
                bool vine = x >= leftVine && x <= leftVine + 1 || x >= rightVine && x <= rightVine + 1;
                bool vineLeaf = y % 19 < 3 && (Mathf.Abs(x - leftVine) <= 3 || Mathf.Abs(x - rightVine) <= 3);
                if (vine || vineLeaf) pixels[y * width + x] = vineLeaf ?
                    new Color32(112, 153, 66, 255) : new Color32(59, 93, 44, 255);
                if (y >= 16) continue;

                int branchBase = 3 + x / 8 % 3;
                int branchTop = branchBase + 5 + (x > 6 && x < 41 ? 2 : 0);
                bool wood = x >= 2 && x <= 45 && y >= branchBase && y <= branchTop;
                bool thorn = y < branchBase && (x % 12 == 5 || x % 12 == 6) && x > 3 && x < 44;
                bool leaf = ((x - 9) * (x - 9) / 2 + (y - 12) * (y - 12) < 17) ||
                    ((x - 37) * (x - 37) / 2 + (y - 12) * (y - 12) < 17);
                if (wood) pixels[y * width + x] = y == branchBase || y == branchTop ?
                    new Color32(69, 43, 28, 255) : (x + y * 3) % 13 < 3 ?
                    new Color32(185, 126, 58, 255) : new Color32(128, 81, 43, 255);
                if (thorn) pixels[y * width + x] = new Color32(255, 223, 143, 255);
                if (leaf) pixels[y * width + x] = y < 12 ? new Color32(51, 98, 46, 255) : new Color32(126, 166, 69, 255);
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0), 20f,
                0, SpriteMeshType.FullRect);
            sprite.name = "Low thorn canopy";
            sprites.Add(sprite);
            textures.Add(texture);
            return sprite;
        }
    }
}
