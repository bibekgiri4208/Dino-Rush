using UnityEngine;

namespace DinoRush
{
    /// <summary>The emitter follows the player; live world-space particles keep their own position and velocity.</summary>
    public sealed class RunnerPlayerTrail : MonoBehaviour
    {
        private RunnerTrailParticles particles;
        public int ActiveCount => particles != null ? particles.ActiveCount : 0;
        public float Clock => particles != null ? particles.Clock : 0;
        public RunnerTrailKind Kind => particles != null ? particles.Kind : RunnerTrailKind.None;
        public ParticleSystem System => particles != null ? particles.System : null;
        public ParticleSystemRenderer Renderer => particles != null ? particles.Renderer : null;
        public Vector2 EmitterPosition => particles != null ? particles.EmitterPosition : Vector2.zero;
        public void Initialize(SpriteRenderer player) => particles = new RunnerTrailParticles(transform, player);
        public void Tick(float dt, Vector2 feet, bool ducking, float speed, bool running)
        {
            if (particles == null) return;
            particles.SetKind(RunnerTrailProgress.Equipped);
            particles.Advance(dt, feet, ducking, speed, running);
        }
        public void Clear() => particles?.Clear();
        private void OnDestroy() => particles?.Dispose();
    }
}
