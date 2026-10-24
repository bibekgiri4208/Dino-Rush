using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DinoRush
{
    /// <summary>Bounded, reusable pickups and sparkles, driven only by the running simulation.</summary>
    public sealed class RunnerEggCollectibles : IDisposable
    {
        public const int PoolSize = 64;
        public const int SparklePoolSize = 72;
        private enum SparkleKind { Star, Flash, Halo, Shard }
        private sealed class Egg
        {
            public SpriteRenderer renderer;
            public Vector2 position;
            public bool active;
        }
        private sealed class Sparkle
        {
            public SpriteRenderer renderer;
            public Vector2 velocity;
            public float life, duration, size, rotation, spin, phase;
            public Color tint;
            public SparkleKind kind;
        }

        private readonly Egg[] eggs = new Egg[PoolSize];
        private readonly Sparkle[] sparkles = new Sparkle[SparklePoolSize];
        private readonly List<Object> assets = new List<Object>();
        private readonly Transform root;
        private readonly Action<Vector3> pickup;
        private RunnerEggTrailDirector director;
        private float animationTime;
        private int sparkleCursor;
        private readonly Sprite sparkleSprite, haloSprite, shardSprite;
        public Sprite EggSprite { get; }
        public int Collected { get; private set; }
        public int ActiveCount { get; private set; }

        public RunnerEggCollectibles(Transform owner, Material material, Action<Vector3> pickup)
        {
            this.pickup = pickup;
            root = new GameObject("Collectible eggs").transform;
            root.SetParent(owner, false);
            EggSprite = RunnerEggArt.CreateEgg(assets);
            sparkleSprite = RunnerEggArt.CreateSparkle(assets);
            haloSprite = RunnerEggArt.CreatePickupHalo(assets);
            shardSprite = RunnerEggArt.CreatePickupShard(assets);
            for (int i = 0; i < eggs.Length; i++)
                eggs[i] = new Egg { renderer = Renderer("Pooled egg " + i, EggSprite, material, 25) };
            for (int i = 0; i < sparkles.Length; i++)
                sparkles[i] = new Sparkle { renderer = Renderer("Pickup sparkle " + i, sparkleSprite, material, 35) };
            Reset(0);
        }

        public void Reset(int seed)
        {
            director = new RunnerEggTrailDirector(seed);
            Collected = ActiveCount = sparkleCursor = 0;
            animationTime = 0f;
            foreach (Egg egg in eggs) { egg.active = false; egg.renderer.gameObject.SetActive(false); }
            foreach (Sparkle sparkle in sparkles) { sparkle.life = 0f; sparkle.renderer.gameObject.SetActive(false); }
        }

        public void SpawnIntro()
        {
            // A safe first reward before the first obstacle reaches the player.
            for (int i = 0; i < 4; i++) Spawn(new Vector2(2f + i * 0.85f, RunnerRules.GroundY + RunnerEggTrailDirector.LowHeight));
        }

        public void SpawnEncounter(RunnerEncounter encounter, float origin, float arrivalSpeed)
        {
            RunnerEggTrail trail = director.Next(encounter, arrivalSpeed);
            SpawnTrail(trail, origin);
        }

        public bool SpawnTrail(RunnerEggTrail trail, float origin)
        {
            if (trail == null || trail.Eggs == null) throw new ArgumentException("A trail needs egg positions.", nameof(trail));
            if (eggs.Length - ActiveCount < trail.Eggs.Length) return false; // Never show a partial route.
            foreach (Vector2 point in trail.Eggs) Spawn(new Vector2(origin + point.x, RunnerRules.GroundY + point.y));
            return true;
        }

        private void Spawn(Vector2 position)
        {
            foreach (Egg egg in eggs)
            {
                if (egg.active) continue;
                egg.position = position;
                egg.active = true;
                egg.renderer.transform.position = position;
                egg.renderer.transform.localScale = Vector3.one;
                egg.renderer.gameObject.SetActive(true);
                ActiveCount++;
                return;
            }
        }

        public void Step(float dt, float travel, Rect player)
        {
            animationTime += dt;
            foreach (Sparkle sparkle in sparkles)
            {
                if (sparkle.life <= 0f) continue;
                sparkle.life = Mathf.Max(0f, sparkle.life - dt);
                if (sparkle.life > sparkle.duration) continue; // Stagger the small after-glints.
                sparkle.renderer.transform.position += (Vector3)(sparkle.velocity * dt + Vector2.left * travel * 0.12f);
                sparkle.velocity *= Mathf.Exp(-dt * 4.5f);
                if (sparkle.kind == SparkleKind.Star) sparkle.velocity += Vector2.down * (dt * 0.8f);
                if (sparkle.kind == SparkleKind.Shard) sparkle.velocity += Vector2.down * (dt * 2.4f);
                sparkle.rotation += sparkle.spin * dt;
                RenderSparkle(sparkle);
            }
            for (int i = 0; i < eggs.Length; i++)
            {
                Egg egg = eggs[i];
                if (!egg.active) continue;
                egg.position.x -= travel;
                Vector2 visible = egg.position + Vector2.up * (Mathf.Sin(animationTime * 4f + i * 0.7f) * 0.045f);
                egg.renderer.transform.position = visible;
                egg.renderer.transform.localScale = Vector3.one * (1f + Mathf.Sin(animationTime * 3f + i) * 0.035f);
                if (player.Overlaps(RunnerEggTrailDirector.PickupBox(visible)))
                {
                    Deactivate(egg);
                    Collected++;
                    Burst(visible);
                    pickup?.Invoke(visible);
                }
                else if (egg.position.x < -12f) Deactivate(egg);
            }
        }

        private void Deactivate(Egg egg)
        {
            egg.active = false;
            egg.renderer.gameObject.SetActive(false);
            ActiveCount--;
        }

        private void Burst(Vector2 position)
        {
            for (int i = 0; i < 10; i++)
            {
                Sparkle sparkle = sparkles[sparkleCursor];
                sparkleCursor = (sparkleCursor + 1) % sparkles.Length;
                sparkle.kind = i == 0 ? SparkleKind.Flash : i == 1 ? SparkleKind.Halo : i >= 6 ? SparkleKind.Shard : SparkleKind.Star;
                float angle = (i - 2) * Mathf.PI / 4f + Collected * 2.39996f;
                bool small = i % 2 == 0;
                sparkle.duration = i == 0 ? 0.16f : i == 1 ? 0.32f : small ? 0.56f : 0.42f;
                float delay = i >= 2 && small ? 0.045f : 0f;
                sparkle.life = sparkle.duration + delay;
                sparkle.size = i == 0 ? 1.5f : i == 1 ? 1f : small ? 0.65f : 1.15f;
                sparkle.phase = angle;
                sparkle.rotation = angle * Mathf.Rad2Deg;
                sparkle.spin = small ? 150f : -110f;
                sparkle.tint = i == 0 ? new Color(1f, 0.98f, 0.8f) :
                    small ? new Color(1f, 0.76f, 0.24f) : new Color(1f, 0.94f, 0.63f);
                sparkle.velocity = i < 2 ? Vector2.zero :
                    new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (small ? 2.8f : 3.6f) + Vector2.up * 0.9f;
                sparkle.renderer.sprite = i == 1 ? haloSprite : i >= 6 ? shardSprite : sparkleSprite;
                sparkle.renderer.transform.position = position;
                RenderSparkle(sparkle);
            }
        }

        private static void RenderSparkle(Sparkle sparkle)
        {
            bool visible = sparkle.life > 0f && sparkle.life <= sparkle.duration;
            sparkle.renderer.gameObject.SetActive(visible);
            if (!visible) return;
            float age = 1f - sparkle.life / sparkle.duration;
            float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.12f, 1f, age));
            float size;
            if (sparkle.kind == SparkleKind.Halo)
            {
                size = Mathf.Lerp(0.2f, 1.1f, 1f - (1f - age) * (1f - age));
                fade *= 0.55f;
            }
            else if (sparkle.kind == SparkleKind.Flash)
                size = Mathf.Lerp(1f, 0.15f, age);
            else if (sparkle.kind == SparkleKind.Shard)
                size = Mathf.Lerp(0.9f, 0.05f, age);
            else
            {
                float twinkle = 0.82f + 0.18f * Mathf.Sin(age * Mathf.PI * 4f + sparkle.phase);
                size = Mathf.Lerp(0.7f, 0.1f, age) + Mathf.Sin(Mathf.Min(1f, age / 0.22f) * Mathf.PI) * 0.35f;
                fade *= twinkle;
            }
            Color color = sparkle.tint;
            color.a = fade;
            sparkle.renderer.color = color;
            sparkle.renderer.transform.localScale = Vector3.one * (size * sparkle.size);
            sparkle.renderer.transform.localRotation = Quaternion.Euler(0, 0,
                sparkle.kind == SparkleKind.Halo ? 0f : sparkle.rotation);
        }

        private SpriteRenderer Renderer(string name, Sprite sprite, Material material, int order)
        {
            var renderer = new GameObject(name, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            renderer.transform.SetParent(root, false);
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.sortingOrder = order;
            renderer.gameObject.SetActive(false);
            return renderer;
        }

        public void Dispose()
        {
            if (root != null) Object.Destroy(root.gameObject);
            foreach (Object asset in assets) if (asset != null) Object.Destroy(asset);
            assets.Clear();
        }
    }
}
