using System;
using UnityEngine;
using UnityEngine.UI;

namespace DinoRush
{
    /// <summary>Real world-space Unity particles. Only the emitter follows the player; old particles never teleport.</summary>
    public sealed class RunnerTrailParticles : IDisposable
    {
        public const int Capacity = 80;
        private readonly GameObject root;
        private readonly bool visible;
        private readonly ParticleSystem.Particle[] particles = new ParticleSystem.Particle[Capacity];
        private readonly Material material;
        private System.Random random;
        private Vector2 previousFeet;
        private bool hasPose, disposed;
        private float emission;
        private float interval, lifetime, lifetimeRange, sizeMin, sizeRange, rise, riseRange;
        public ParticleSystem System { get; }
        public ParticleSystemRenderer Renderer { get; }
        public RunnerTrailKind Kind { get; private set; }
        public float Clock { get; private set; }
        public Vector2 EmitterPosition { get; private set; }
        public int ActiveCount => System != null ? System.particleCount : 0;
        public Texture2D Texture => RunnerTrailParticleArt.Get(Kind);

        public RunnerTrailParticles(Transform parent, SpriteRenderer source = null)
        {
            RunnerTrailParticleArt.Acquire();
            root = new GameObject("Drifting cosmetic particles") { hideFlags = HideFlags.DontSave };
            root.transform.SetParent(parent, false);
            System = root.AddComponent<ParticleSystem>();
            System.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = System.main;
            main.playOnAwake = false; main.loop = true; main.duration = 5;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.maxParticles = Capacity;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            var emit = System.emission; emit.enabled = false;
            var shape = System.shape; shape.enabled = false;
            System.useAutoRandomSeed = false; System.randomSeed = 31337;
            Renderer = root.GetComponent<ParticleSystemRenderer>();
            Renderer.renderMode = ParticleSystemRenderMode.Billboard;
            Renderer.sortMode = ParticleSystemSortMode.OldestInFront;
            visible = source != null;
            if (source != null)
            {
                material = new Material(source.sharedMaterial) { name = "Floating dino particle material" };
                Renderer.sharedMaterial = material;
                Renderer.sortingLayerID = source.sortingLayerID;
                Renderer.sortingOrder = source.sortingOrder - 1;
            }
            Renderer.enabled = false;
        }

        public void SetKind(RunnerTrailKind kind)
        {
            if (Kind == kind) return;
            Clear(); Kind = kind;
            if (!RunnerTrailProgress.Valid(kind)) return;
            random = new System.Random(317 + (int)kind * 37);
            interval = 0.037f; lifetime = 0.6f; lifetimeRange = 0.5f;
            sizeMin = 0.4f; sizeRange = 0.3f; rise = -0.11f; riseRange = 0.55f;
            switch (kind)
            {
                case RunnerTrailKind.Smoke: interval = 0.055f; break;
                case RunnerTrailKind.Fire: interval = 0.027f; sizeMin = 0.18f; sizeRange = 0.18f; rise = 0.3f; riseRange = 0.8f; break;
                case RunnerTrailKind.Electric: lifetime = 0.25f; lifetimeRange = 0.32f; sizeMin = 0.22f; sizeRange = 0.28f; break;
                case RunnerTrailKind.Frost: interval = 0.045f; lifetime = 0.8f; lifetimeRange = 0.4f; sizeMin = 0.24f; sizeRange = 0.2f; rise = -0.35f; riseRange = 0.3f; break;
                case RunnerTrailKind.Blossom: interval = 0.06f; lifetime = 0.9f; lifetimeRange = 0.5f; sizeMin = 0.22f; sizeRange = 0.25f; rise = 0.05f; riseRange = 0.4f; break;
                case RunnerTrailKind.Cosmic: interval = 0.04f; lifetime = 0.65f; lifetimeRange = 0.35f; sizeMin = 0.28f; sizeRange = 0.24f; rise = 0.15f; riseRange = 0.35f; break;
            }
            if (material != null) material.mainTexture = Texture;
            var main = System.main;
            main.gravityModifier = kind == RunnerTrailKind.Fire ? -0.035f : 0;
            var noise = System.noise;
            noise.enabled = true; noise.separateAxes = true; noise.frequency = 0.8f; noise.damping = true;
            noise.strengthX = kind == RunnerTrailKind.Smoke ? 0.14f : 0.09f;
            noise.strengthY = kind == RunnerTrailKind.Fire ? 0.23f : kind == RunnerTrailKind.Blossom ? 0.35f : 0.16f;
            noise.strengthZ = 0;
            noise.positionAmount = 0.5f; noise.rotationAmount = 0; noise.sizeAmount = 0;
            var rotation = System.rotationOverLifetime;
            rotation.enabled = true;
            float spin = kind == RunnerTrailKind.Smoke ? 2.2f : kind == RunnerTrailKind.Frost || kind == RunnerTrailKind.Blossom ? 1.6f : 4;
            rotation.z = new ParticleSystem.MinMaxCurve(-spin, spin);
            var size = System.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(new Keyframe(0, 0.35f), new Keyframe(0.12f, 1), new Keyframe(0.65f, 0.85f), new Keyframe(1, 0.12f)));
            var colors = System.colorOverLifetime; colors.enabled = true;
            var gradient = new Gradient();
            if (kind == RunnerTrailKind.Fire)
                gradient.SetKeys(new[] { new GradientColorKey(new Color(1, 0.95f, 0.55f), 0), new GradientColorKey(new Color(1, 0.4f, 0.065f), 0.55f), new GradientColorKey(new Color(0.7f, 0.12f, 0.02f), 1) },
                    new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.08f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0, 1) });
            else if (kind == RunnerTrailKind.Electric)
                gradient.SetKeys(new[] { new GradientColorKey(new Color(0.8f, 0.98f, 1), 0), new GradientColorKey(new Color(0.3f, 0.7f, 1), 1) },
                    new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.08f), new GradientAlphaKey(0.3f, 0.35f), new GradientAlphaKey(0.95f, 0.55f), new GradientAlphaKey(0, 1) });
            else if (kind == RunnerTrailKind.Frost || kind == RunnerTrailKind.Blossom || kind == RunnerTrailKind.Cosmic)
            {
                Color start = kind == RunnerTrailKind.Blossom ? new Color(1, 0.8f, 0.9f) : kind == RunnerTrailKind.Cosmic ? new Color(1, 0.85f, 1) : new Color(0.85f, 1, 1);
                Color end = kind == RunnerTrailKind.Blossom ? new Color(1, 0.35f, 0.6f) : kind == RunnerTrailKind.Cosmic ? new Color(0.85f, 0.65f, 1) : new Color(0.3f, 0.75f, 1);
                gradient.SetKeys(new[] { new GradientColorKey(start, 0), new GradientColorKey(end, 1) },
                    new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.12f), new GradientAlphaKey(kind == RunnerTrailKind.Cosmic ? 0.3f : 0.85f, 0.45f), new GradientAlphaKey(0.9f, 0.65f), new GradientAlphaKey(0, 1) });
            }
            else
                gradient.SetKeys(new[] { new GradientColorKey(new Color(0.9f, 0.97f, 1), 0), new GradientColorKey(new Color(0.65f, 0.85f, 0.95f), 1) },
                    new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(0.85f, 0.16f), new GradientAlphaKey(0.55f, 0.6f), new GradientAlphaKey(0, 1) });
            colors.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        public void Advance(float dt, Vector2 feet, bool ducking, float scrollSpeed, bool emit)
        {
            if (disposed || !RunnerTrailProgress.Valid(Kind)) return;
            dt = Mathf.Clamp(dt, 0, 0.1f);
            if (dt <= 0) return;
            Clock += dt;
            if (!hasPose) { previousFeet = feet; hasPose = true; }
            var velocity = System.velocityOverLifetime;
            velocity.enabled = true; velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = -Mathf.Min(Mathf.Max(scrollSpeed, 0) * 0.55f, 6);
            velocity.y = 0; velocity.z = 0;
            int steps = Mathf.Max(1, Mathf.CeilToInt(dt / (1 / 60f)));
            float step = dt / steps;
            for (int i = 0; i < steps; i++)
            {
                // Simulate pauses the native system, so the game owns time (including custom pause).
                System.Simulate(step, false, false, false);
                Vector2 at = Vector2.Lerp(previousFeet, feet, (i + 1f) / steps);
                EmitterPosition = at + new Vector2(-0.28f, ducking ? 0.32f : 0.65f);
                if (!emit) { emission = 0; continue; }
                emission += step;
                while (emission >= interval)
                {
                    emission -= interval;
                    float r = Next(), s = Next();
                    Vector2 position = at + new Vector2(-0.2f - r * 0.28f, (ducking ? 0.12f : 0.16f) + s * (ducking ? 0.5f : 1.1f));
                    var particle = new ParticleSystem.EmitParams {
                        position = new Vector3(position.x, position.y, 0),
                        velocity = new Vector3(-0.25f - Next() * 0.65f, rise + Next() * riseRange, 0),
                        startLifetime = lifetime + Next() * lifetimeRange,
                        startSize = sizeMin + Next() * sizeRange,
                        rotation = (Next() - 0.5f) * 360,
                        startColor = Color.white,
                        randomSeed = (uint)random.Next(1, int.MaxValue)
                    };
                    System.Emit(particle, 1);
                }
            }
            previousFeet = feet;
            Renderer.enabled = visible && ActiveCount > 0;
            // Emission/render activation can wake Unity's native clock; never leave it auto-playing.
            System.Pause(false);
        }
        private float Next() => (float)random.NextDouble();
        public int GetParticles(ParticleSystem.Particle[] target) => System.GetParticles(target);
        public void DrawPreview(VertexHelper mesh, float scale, Vector2 offset)
        {
            mesh.Clear();
            int count = System.GetParticles(particles);
            for (int i = 0; i < count; i++)
            {
                ParticleSystem.Particle p = particles[i];
                Vector2 center = (Vector2)p.position * scale + offset;
                float half = p.GetCurrentSize(System) * scale * 0.5f, a = p.rotation * Mathf.Deg2Rad;
                Vector2 x = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * half, y = new Vector2(-x.y, x.x);
                Color color = p.GetCurrentColor(System);
                int index = mesh.currentVertCount;
                mesh.AddVert(center - x - y, color, new Vector2(0, 0)); mesh.AddVert(center - x + y, color, new Vector2(0, 1));
                mesh.AddVert(center + x + y, color, new Vector2(1, 1)); mesh.AddVert(center + x - y, color, new Vector2(1, 0));
                mesh.AddTriangle(index, index + 1, index + 2); mesh.AddTriangle(index, index + 2, index + 3);
            }
        }
        public void Clear()
        {
            System.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            emission = Clock = 0; hasPose = false; Renderer.enabled = false;
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (Application.isPlaying)
            {
                if (material != null) UnityEngine.Object.Destroy(material);
                UnityEngine.Object.Destroy(root);
            }
            else
            {
                if (material != null) UnityEngine.Object.DestroyImmediate(material);
                UnityEngine.Object.DestroyImmediate(root);
            }
            RunnerTrailParticleArt.Release();
        }
    }
}
