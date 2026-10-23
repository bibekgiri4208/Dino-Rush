using UnityEngine;

namespace DinoRush
{
    /// <summary>Per-run hearts and a simulation-time grace period after each hit.</summary>
    public sealed class RunnerHealth
    {
        public const int MaximumLives = 5;
        public const float InvulnerabilityDuration = 1.2f;

        public int Lives { get; private set; } = MaximumLives;
        public float InvulnerabilityRemaining { get; private set; }
        public bool IsInvulnerable => InvulnerabilityRemaining > 0f;

        public void Reset()
        {
            Lives = MaximumLives;
            InvulnerabilityRemaining = 0f;
        }

        public void Step(float dt)
        {
            InvulnerabilityRemaining = Mathf.Max(0f, InvulnerabilityRemaining - dt);
        }

        public bool TryTakeHit()
        {
            if (Lives == 0 || IsInvulnerable) return false;
            Lives--;
            InvulnerabilityRemaining = InvulnerabilityDuration;
            return true;
        }
    }
}
