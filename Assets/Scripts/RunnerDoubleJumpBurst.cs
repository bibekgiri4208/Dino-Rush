using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DinoRush
{
    /// <summary>Pooled, animated air crests and curling wisps, advanced only by the run simulation.</summary>
    public sealed class RunnerDoubleJumpBurst : IDisposable
    {
        public const float Duration = 0.36f;
        private const int PoolSize = 3;
        private const int PartsPerBurst = 8;
        private readonly Transform root;
        private readonly Sprite[] ringFrames = new Sprite[8];
        private readonly Sprite wisp;
        private readonly Burst[] bursts = new Burst[PoolSize];
        private int next;

        private sealed class Burst
        {
            public readonly SpriteRenderer[] parts = new SpriteRenderer[PartsPerBurst];
            public Vector3 origin;
            public Vector3 previousFeet;
            public float age = Duration;
        }

        public int ActiveCount
        {
            get
            {
                int count = 0;
                foreach (Burst burst in bursts) if (burst.age < Duration) count++;
                return count;
            }
        }

        public RunnerDoubleJumpBurst(Transform parent, SpriteRenderer player)
        {
            root = new GameObject("Double jump wind bursts").transform;
            root.SetParent(parent, false);
            for (int i = 0; i < ringFrames.Length; i++)
                ringFrames[i] = CreateArt(true, i / (float)(ringFrames.Length - 1));
            wisp = CreateArt(false);
            for (int i = 0; i < bursts.Length; i++)
            {
                var burst = new Burst();
                bursts[i] = burst;
                for (int j = 0; j < PartsPerBurst; j++)
                {
                    var node = new GameObject(j < 2 ? "Wind ring" : "Wind wisp");
                    node.transform.SetParent(root, false);
                    var art = node.AddComponent<SpriteRenderer>();
                    art.sprite = j < 2 ? ringFrames[0] : wisp;
                    art.sharedMaterial = player.sharedMaterial;
                    art.sortingLayerID = player.sortingLayerID;
                    art.sortingOrder = player.sortingOrder - 1;
                    node.SetActive(false);
                    burst.parts[j] = art;
                }
            }
        }

        public void Emit(Vector3 feet)
        {
            Burst burst = bursts[next];
            next = (next + 1) % bursts.Length;
            burst.origin = feet + Vector3.up * 0.06f;
            burst.previousFeet = feet;
            burst.age = 0f;
            Draw(burst);
        }

        public void Tick(float dt, Vector3 feet)
        {
            foreach (Burst burst in bursts)
            {
                if (burst.age >= Duration) continue;
                // Carry some upward momentum without gluing the whole graphic to the player.
                burst.origin += (feet - burst.previousFeet) * 0.8f;
                burst.previousFeet = feet;
                burst.age = Mathf.Min(Duration, burst.age + dt);
                Draw(burst);
            }
        }

        private void Draw(Burst burst)
        {
            for (int i = 0; i < PartsPerBurst; i++)
            {
                SpriteRenderer art = burst.parts[i];
                float delay = i < 2 ? i * 0.045f : (i - 2) % 3 * 0.012f;
                float life = i < 2 ? Duration - 0.045f : 0.25f + (i % 2) * 0.025f;
                float age = burst.age - delay;
                float t = Mathf.Clamp01(age / life);
                bool visible = age >= 0f && age < life;
                art.gameObject.SetActive(visible);
                if (!visible) continue;
                float spread = 1f - Mathf.Pow(1f - t, 3f);
                float fade = Mathf.Pow(1f - t, 0.8f);
                if (i < 2)
                {
                    art.sprite = ringFrames[Mathf.Min(ringFrames.Length - 1, (int)(t * ringFrames.Length))];
                    art.flipX = i == 1;
                    float width = Mathf.Lerp(0.65f, 2.15f, spread) * (i == 0 ? 1f : 0.78f);
                    art.transform.position = burst.origin + new Vector3(-0.12f * t, -t * 0.1f - i * 0.11f, 0);
                    art.transform.localScale = new Vector3(width, width * Mathf.Lerp(0.22f, 0.36f, t), 1f);
                    art.transform.rotation = Quaternion.Euler(0, 0, (i == 0 ? 1 : -1) * Mathf.Lerp(-5f, 9f, t));
                    art.color = new Color(1f, 1f, 1f, fade * (i == 0 ? 1f : 0.85f));
                }
                else
                {
                    float angle = (i - 2) * Mathf.PI / 3f + t * 1.1f;
                    var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * 0.42f, 0);
                    art.transform.position = burst.origin + direction * Mathf.Lerp(0.22f, 1.25f, spread)
                        + Vector3.down * t * 0.1f;
                    art.transform.rotation = Quaternion.Euler(0, 0,
                        Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 25f + t * 40f);
                    float pulse = Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.65f));
                    art.transform.localScale = new Vector3(Mathf.Lerp(0.3f, 0.72f, spread) * Mathf.Pow(1f - t, 0.4f),
                        0.24f * pulse, 1f);
                    art.color = new Color(1f, 1f, 1f, fade * 0.95f);
                }
            }
        }

        public void Clear()
        {
            next = 0;
            foreach (Burst burst in bursts)
            {
                burst.age = Duration;
                foreach (SpriteRenderer art in burst.parts) art.gameObject.SetActive(false);
            }
        }

        private static Sprite CreateArt(bool isRing, float phase = 0f)
        {
            int width = isRing ? 64 : 32, height = isRing ? 64 : 16;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = isRing ? "Double jump wind crest " + Mathf.RoundToInt(phase * 7) : "Double jump tapered wind wisp",
                filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float px = (x + 0.5f) / width * 2f - 1f;
                float py = (y + 0.5f) / height * 2f - 1f;
                Color32 color = new Color32(0, 0, 0, 0);
                if (isRing)
                {
                    float radius = Mathf.Sqrt(px * px + py * py);
                    float angle = Mathf.Atan2(py, px) - phase * 1.6f;
                    // A thick white crest, cyan lip and narrow dark contour stay defined on sky or foliage.
                    // Small tapered breaks and the empty center keep this an air wave, not a platform.
                    float thickness = (0.14f + 0.04f * (0.5f + 0.5f * Mathf.Sin(angle * 2f))) * Mathf.Lerp(1f, 0.35f, phase);
                    float gap = Mathf.Min(AngularDistance(angle, 0.5f), AngularDistance(angle, -2.6f));
                    if (phase > 0.4f) gap = Mathf.Min(gap, AngularDistance(angle, 2f));
                    float opening = Mathf.Lerp(0.06f, 0.55f, phase);
                    float inner = 0.93f - thickness * Mathf.InverseLerp(opening, opening + 0.16f, gap);
                    if (gap > opening && radius <= 0.97f && radius >= inner - 0.025f)
                        color = radius > 0.93f || radius < inner ? new Color32(24, 78, 105, 175) :
                            radius > 0.93f - thickness * 0.28f ? new Color32(100, 223, 255, 255) : new Color32(242, 255, 255, 255);
                }
                else
                {
                    float curve = 0.4f * px * px - 0.16f;
                    float edge = (1f - Mathf.Abs(px)) * 0.62f;
                    float distance = Mathf.Abs(py - curve);
                    if (distance < edge)
                        color = distance > edge * 0.6f ? new Color32(100, 223, 255, 255) : new Color32(242, 255, 255, 255);
                }
                pixels[y * width + x] = color;
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, width, height), Vector2.one * 0.5f,
                width, 0, SpriteMeshType.FullRect);
            sprite.name = texture.name;
            return sprite;
        }

        private static float AngularDistance(float a, float b) =>
            Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, b * Mathf.Rad2Deg)) * Mathf.Deg2Rad;

        public void Dispose()
        {
            Object.Destroy(root.gameObject);
            foreach (Sprite frame in ringFrames)
            {
                Object.Destroy(frame.texture);
                Object.Destroy(frame);
            }
            Object.Destroy(wisp.texture);
            Object.Destroy(wisp);
        }
    }
}
