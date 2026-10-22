using System.Collections.Generic;
using UnityEngine;

namespace DinoRush
{
    /// <summary>Small point-filtered silhouettes, tinted to soil colors by the particle pool.</summary>
    public static class RunnerDirtArt
    {
        public static Sprite[] CreateSprites(List<Object> assets)
        {
            string[][] shapes =
            {
                new[] { "........", "..####..", ".######.", ".######.", "..#####.", "..####..", "...##...", "........" },
                new[] { "...##...", "..####..", ".######.", "########", "########", ".######.", "..####..", "........" },
                new[] { "........", "........", "...###..", "..####..", "..###...", "...#....", "........", "........" }
            };
            string[] names = { "Dirt clod", "Dirt puff", "Dirt fleck" };
            var sprites = new Sprite[shapes.Length];
            for (int i = 0; i < shapes.Length; i++)
            {
                var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false)
                {
                    name = names[i] + " texture", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
                };
                for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                {
                    float shade = y < 3 ? 1f : (x + y) % 3 == 0 ? 0.7f : 0.86f;
                    texture.SetPixel(x, 7 - y, shapes[i][y][x] == '#' ? new Color(shade, shade, shade, 1) : Color.clear);
                }
                texture.Apply();
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 8, 8), Vector2.one * 0.5f, 8, 0, SpriteMeshType.FullRect);
                sprite.name = names[i];
                sprites[i] = sprite;
                assets.Add(texture);
                assets.Add(sprite);
            }
            return sprites;
        }
    }
}
