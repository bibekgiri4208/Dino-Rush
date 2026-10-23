using UnityEngine;

namespace DinoRush
{
    /// <summary>Seamless pixel-art subsoil. Uses a private seed, never gameplay randomness.</summary>
    public static class RunnerSoilArt
    {
        public const int Width = 1024;
        public const int Height = 512;
        public const float PixelsPerUnit = 85f;
        public const float Top = -3.5f;

        public static Texture2D CreateTexture(Color surface, string mapId)
        {
            var pixels = new Color32[Width * Height];
            Color earth = Color.Lerp(surface, new Color(0.23f, 0.20f, 0.13f), 0.30f);
            for (int depth = 0; depth < Height; depth++)
            for (int x = 0; x < Width; x++)
            {
                float wave = Mathf.Sin(x * Mathf.PI * 2f / Width * 3f) * 5f
                    + Mathf.Sin(x * Mathf.PI * 2f / Width * 7f) * 2f;
                int stratum = Mathf.FloorToInt((depth + wave) / 29f);
                float grain = Noise(x / 3, depth / 2, 17) * 0.09f
                    + Noise(x / 16, depth / 7, 31) * 0.08f;
                float band = Noise(0, stratum, 43) * 0.10f;
                float shade = Mathf.Lerp(1f, 0.57f, depth / (float)Height) + grain + band;
                // The original grass strip overlaps the first 39 pixels; ease out of its dark edge.
                Color color = Color.Lerp(surface * 0.88f, earth, Mathf.Clamp01((depth - 34f) / 66f)) * shade;
                if ((depth + Mathf.RoundToInt(wave)) % 29 == 0) color *= 0.92f;
                color.a = 1f;
                pixels[(Height - 1 - depth) * Width + x] = color;
            }

            var random = new System.Random(7319);
            // Fine sediment clusters, with occasional larger, faceted pebbles rather than TV noise.
            for (int i = 0; i < 1700; i++)
            {
                int x = random.Next(Width), depth = random.Next(42, Height);
                int width = random.Next(1, 5), height = random.Next(1, 3);
                Color color = Sample(pixels, x, depth) * (i % 3 == 0 ? 1.22f : 0.82f);
                for (int dy = 0; dy < height; dy++)
                for (int dx = 0; dx < width; dx++) Put(pixels, x + dx, depth + dy, color);
            }
            for (int i = 0; i < 160; i++)
            {
                int x = random.Next(Width), depth = random.Next(58, Height - 9);
                int width = random.Next(4, 12), height = random.Next(3, 7);
                Color stone = Color.Lerp(Sample(pixels, x, depth), surface, 0.35f) * 1.18f;
                for (int dy = 0; dy < height; dy++)
                for (int dx = 0; dx < width; dx++)
                {
                    if ((dy == 0 || dy == height - 1) && (dx < 2 || dx >= width - 2)) continue;
                    Put(pixels, x + dx + 1, depth + dy + 1, stone * 0.57f);
                    Put(pixels, x + dx, depth + dy, stone * (dy < 2 ? 1.14f : 0.87f));
                }
            }
            Color root = Color.Lerp(earth, new Color(0.32f, 0.29f, 0.16f), 0.35f);
            for (int i = 0; i < (mapId == "desert" ? 8 : 20); i++)
            {
                int x = random.Next(Width), depth = random.Next(28, 52);
                int length = random.Next(18, 68), direction = i % 2 == 0 ? 1 : -1;
                for (int j = 0; j < length; j++)
                {
                    if (j % 3 == 0) x += direction;
                    if (random.Next(11) == 0) direction = -direction;
                    Put(pixels, x + 1, depth + j, root * 0.55f);
                    Put(pixels, x, depth + j, root * Mathf.Lerp(1f, 0.65f, j / (float)length));
                    if (j == length / 2)
                        for (int twig = 1; twig < 9; twig++)
                            Put(pixels, x + twig * direction, depth + j + twig / 2, root * 0.75f);
                }
            }

            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
            {
                name = mapId + "_soil", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat
            };
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        private static float Noise(int x, int y, int seed)
        {
            unchecked
            {
                uint value = (uint)(x * 374761393 + y * 668265263 + seed * 1274126177);
                value = (value ^ (value >> 13)) * 1274126177;
                return (value & 65535) / 65535f - 0.5f;
            }
        }

        private static Color Sample(Color32[] pixels, int x, int depth) => pixels[(Height - 1 - depth) * Width + x];

        private static void Put(Color32[] pixels, int x, int depth, Color color)
        {
            if (depth < 0 || depth >= Height) return;
            x = (x % Width + Width) % Width;
            color.a = 1f;
            pixels[(Height - 1 - depth) * Width + x] = color;
        }
    }
}
