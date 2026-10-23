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
        public const int SparklePoolSize = 36;
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
            public float life;
        }

        private readonly Egg[] eggs = new Egg[PoolSize];
        private readonly Sparkle[] sparkles = new Sparkle[SparklePoolSize];
        private readonly List<Object> assets = new List<Object>();
        private readonly Transform root;
        private readonly Action pickup;
        private RunnerEggTrailDirector director;
        private float animationTime;
        private int sparkleCursor;
        public Sprite EggSprite { get; }
        public int Collected { get; private set; }
        public int ActiveCount { get; private set; }

        public RunnerEggCollectibles(Transform owner, Material material, Action pickup)
        {
            this.pickup = pickup;
            root = new GameObject("Collectible eggs").transform;
            root.SetParent(owner, false);
            EggSprite = RunnerEggArt.CreateEgg(assets);
            Sprite sparkleSprite = RunnerEggArt.CreateSparkle(assets);
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
                sparkle.renderer.gameObject.SetActive(sparkle.life > 0f);
                sparkle.renderer.transform.position += (Vector3)(sparkle.velocity * dt + Vector2.left * travel * 0.2f);
                sparkle.velocity += Vector2.down * (dt * 3f);
                float remaining = sparkle.life / 0.38f;
                sparkle.renderer.color = new Color(1f, 0.87f, 0.42f, remaining);
                sparkle.renderer.transform.localScale = Vector3.one * Mathf.Lerp(0.25f, 0.9f, remaining);
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
                    pickup?.Invoke();
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
            for (int i = 0; i < 6; i++)
            {
                Sparkle sparkle = sparkles[sparkleCursor++ % sparkles.Length];
                float angle = i * Mathf.PI / 3f;
                sparkle.life = 0.38f;
                sparkle.velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 1.8f;
                sparkle.renderer.transform.position = position;
                sparkle.renderer.transform.localScale = Vector3.one * 0.9f;
                sparkle.renderer.color = new Color(1f, 0.9f, 0.5f, 1f);
                sparkle.renderer.gameObject.SetActive(true);
            }
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
