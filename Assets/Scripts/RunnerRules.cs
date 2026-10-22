using UnityEngine;

namespace DinoRush
{
    /// <summary>Shared, testable rules for the single-lane forest run.</summary>
    public static class RunnerRules
    {
        public const float GroundY = -3f;
        public const float PlayerX = -5.8f;
        public const float JumpSpeed = 11.5f;
        public const float Gravity = 29f;
        public const float FastFallSpeed = 15f;
        public const float FastFallGravity = 58f;
        public const float PlayerWidth = 0.86f;
        public const float PlayerHeight = 1.3f;
        public const float DuckHeight = 0.8f;
        public const float MaximumObstacleWidth = 2.3f;
        public const float MaximumObstacleHeight = 1.2f;
        public const float SimulationStep = 1f / 120f;

        public static float Speed(float elapsed)
        {
            return Mathf.Lerp(6.8f, 13.2f, Mathf.Clamp01(elapsed / 100f));
        }

        public static float MinimumSpawnInterval(float speed)
        {
            // A full jump, recovery time, and the widest obstacle's footprint.
            return 2f * JumpSpeed / Gravity + 0.3f + MaximumObstacleWidth / speed;
        }

        public static Rect PlayerHitbox(float height, bool ducking = false)
        {
            return new Rect(PlayerX - PlayerWidth * 0.5f, GroundY + height + 0.1f,
                PlayerWidth, ducking ? DuckHeight : PlayerHeight);
        }

        public static Rect ObstacleHitbox(float x, float width, float height)
        {
            // Inset from the artwork so transparent corners are forgiving.
            return new Rect(x - width * 0.39f, GroundY + 0.04f, width * 0.78f, height * 0.85f);
        }
    }
}
