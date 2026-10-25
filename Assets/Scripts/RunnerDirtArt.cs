using System.Collections.Generic;
using UnityEngine;

namespace DinoRush
{
    /// <summary>Shaded, point-filtered clods, feathered cloud silhouettes, ground haze, and fine grit.</summary>
    public static class RunnerDirtArt
    {
        public static Sprite[] CreateSprites(List<Object> assets)
        {
            string[][] shapes =
            {
                new[] { "........", "..####..", ".######.", ".######.", "..#####.", "..####..", "...##...", "........" },
                new[] {
                    "................", "................", ".....+++........", "...++###++......",
                    "..+##@@@##+.....", ".+##@@@@@##++...", ".+##@@@@@@###+..", "+###@@@@@@@###+.",
                    "+###@@@@@@@####+", ".+###@@@@@#####+", "..+############+", "...+##########+.",
                    "....++######++..", "......++++++....", "................", "................" },
                new[] { "........", "........", "...###..", "..####..", "..###...", "...#....", "........", "........" },
                new[] {
                    "................", "................", ".........++.....", ".......++##+....",
                    "......+##@@#+...", "......+#@@@##+..", "...++..+##@@##+.", "..+##+..+##@@#+.",
                    ".+####+++##@@##+", "+##@@@###@@@###+", "+##@@@@@@@@####+", ".+##@@@@@#####+.",
                    "..+#########++..", "...++#####++....", ".....+++++......", "................" },
                new[] { "................", "................", "...++++.........", ".++####+++......",
                    "+#########++++..", "+###@@@#######+.", ".++#########++..", "...+++++++++...." },
                new[] { ".##.", "####", ".###", "..#." }
            };
            string[] names = { "Dirt clod", "Dirt puff", "Dirt fleck", "Dirt curl", "Dirt ground haze", "Dirt grain" };
            var sprites = new Sprite[shapes.Length];
            for (int i = 0; i < shapes.Length; i++)
            {
                int width = shapes[i][0].Length, height = shapes[i].Length;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    name = names[i] + " texture", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
                };
                for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    char cell = shapes[i][y][x];
                    bool cloud = i == 1 || i == 3 || i == 4;
                    float shade = Mathf.Lerp(1f, 0.62f, y / (float)(height - 1));
                    if ((x + y * 3) % 7 == 0) shade *= 0.9f;
                    float alpha = cell == '+' ? 0.28f : cell == '@' ? 1f : cloud ? 0.7f : 1f;
                    texture.SetPixel(x, height - 1 - y, cell == '.' ? Color.clear : new Color(shade, shade, shade, alpha));
                }
                texture.Apply();
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, width, height), Vector2.one * 0.5f, width, 0, SpriteMeshType.FullRect);
                sprite.name = names[i];
                sprites[i] = sprite;
                assets.Add(texture);
                assets.Add(sprite);
            }
            return sprites;
        }
    }
}
