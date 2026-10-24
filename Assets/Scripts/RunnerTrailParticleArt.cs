using UnityEngine;

namespace DinoRush
{
    /// <summary>Small individual ember/current/wind-blade sprites, never a complete trail image.</summary>
    internal static class RunnerTrailParticleArt
    {
        private const int Size = 64;
        private static readonly Texture2D[] textures = new Texture2D[RunnerTrailProgress.Count];
        private static int users;
        internal static void Acquire() => users++;
        internal static Texture2D Get(RunnerTrailKind kind)
        {
            int index = (int)kind - 1;
            if (index < 0 || index >= textures.Length) return Texture2D.whiteTexture;
            if (textures[index] != null) return textures[index];
            var pixels = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float distance;
                    if (kind == RunnerTrailKind.Fire)
                    {
                        Vector2 d = p - new Vector2(32, 32);
                        distance = Mathf.Abs(d.x) * 0.95f + Mathf.Abs(d.y) * 0.55f;
                    }
                    else if (kind == RunnerTrailKind.Electric)
                    {
                        distance = Mathf.Min(Segment(p, new Vector2(8, 43), new Vector2(26, 36)), Segment(p, new Vector2(26, 36), new Vector2(20, 26)));
                        distance = Mathf.Min(distance, Segment(p, new Vector2(20, 26), new Vector2(39, 30)));
                        distance = Mathf.Min(distance, Segment(p, new Vector2(39, 30), new Vector2(32, 19)));
                        distance = Mathf.Min(distance, Segment(p, new Vector2(32, 19), new Vector2(55, 12)));
                        distance = Mathf.Min(distance, Segment(p, new Vector2(39, 30), new Vector2(50, 39)));
                    }
                    else if (kind == RunnerTrailKind.Frost)
                    {
                        distance = 100;
                        Vector2 center = new Vector2(32, 32);
                        for (int arm = 0; arm < 6; arm++)
                        {
                            float angle = arm * Mathf.PI / 3;
                            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                            Vector2 side = new Vector2(-direction.y, direction.x);
                            distance = Mathf.Min(distance, Segment(p, center, center + direction * 24));
                            for (int branch = 0; branch < 2; branch++)
                            {
                                Vector2 join = center + direction * (12 + branch * 6);
                                distance = Mathf.Min(distance, Segment(p, join, join - direction * 5 + side * 5));
                                distance = Mathf.Min(distance, Segment(p, join, join - direction * 5 - side * 5));
                            }
                        }
                    }
                    else if (kind == RunnerTrailKind.Blossom)
                    {
                        Vector2 d = p - new Vector2(32, 32);
                        float outline = Mathf.Sqrt(d.x * d.x / 225 + d.y * d.y / 529);
                        distance = Mathf.Max(0, outline - 0.75f) * 22;
                    }
                    else if (kind == RunnerTrailKind.Cosmic)
                    {
                        Vector2 d = p - new Vector2(32, 32);
                        float diamond = Mathf.Abs(d.x) + Mathf.Abs(d.y);
                        distance = Mathf.Min(Mathf.Abs(d.x) * 2 + Mathf.Abs(d.y) * 0.15f,
                            Mathf.Abs(d.y) * 2 + Mathf.Abs(d.x) * 0.15f);
                        distance = Mathf.Max(distance, (diamond - 24) * 2);
                    }
                    else
                    {
                        distance = 100;
                        Vector2 previous = new Vector2(32, 31) + new Vector2(Mathf.Cos(-0.5f), Mathf.Sin(-0.5f)) * 22;
                        for (int i = 1; i <= 36; i++)
                        {
                            float t = i / 36f, a = -0.5f + t * 4.9f, r = 22 * (1 - t * 0.72f);
                            Vector2 next = new Vector2(32, 31) + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                            float taper = Mathf.Sin(t * Mathf.PI) * 0.85f + 0.15f;
                            distance = Mathf.Min(distance, Segment(p, previous, next) / taper);
                            previous = next;
                        }
                    }
                    float core = Mathf.Exp(-distance * distance / (kind == RunnerTrailKind.Fire ? 35 : kind == RunnerTrailKind.Cosmic ? 8 : 2.3f));
                    float glow = Mathf.Exp(-distance * distance / (kind == RunnerTrailKind.Fire ? 85 : kind == RunnerTrailKind.Cosmic ? 36 : 19));
                    Color edge = kind == RunnerTrailKind.Fire ? new Color(1, 0.38f, 0.055f) : kind == RunnerTrailKind.Electric ? new Color(0.22f, 0.62f, 1) :
                        kind == RunnerTrailKind.Blossom ? new Color(1, 0.38f, 0.65f) : kind == RunnerTrailKind.Cosmic ? new Color(0.9f, 0.65f, 1) : new Color(0.7f, 0.88f, 1);
                    Color color = Color.Lerp(edge, Color.white, Mathf.Clamp01(core * 1.5f));
                    if (kind == RunnerTrailKind.Blossom) color = Color.Lerp(edge, new Color(1, 0.83f, 0.91f), Mathf.Clamp01((p.x + p.y) / 80));
                    color.a = Mathf.Clamp01(core * 0.92f + glow * 0.22f);
                    if (color.a < 0.008f) color = Color.clear;
                    pixels[y * Size + x] = color;
                }
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { name = kind + " individual particle", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            texture.SetPixels(pixels); texture.Apply(false, true);
            textures[index] = texture;
            return texture;
        }
        private static float Segment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 direction = b - a;
            return Vector2.Distance(p, a + direction * Mathf.Clamp01(Vector2.Dot(p - a, direction) / direction.sqrMagnitude));
        }
        internal static void Release()
        {
            if (--users > 0) return;
            users = 0;
            for (int i = 0; i < textures.Length; i++)
            {
                if (textures[i] != null)
                {
                    if (Application.isPlaying) Object.Destroy(textures[i]); else Object.DestroyImmediate(textures[i]);
                    textures[i] = null;
                }
            }
        }
    }
}
