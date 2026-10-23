using UnityEngine;

namespace DinoRush
{
    /// <summary>Shared, testable rules for the single-lane forest run.</summary>
    public static class RunnerRules
    {
        // Seat feet and obstacle bases slightly inside the grass, not on its top edge.
        public const float GroundY = -3.12f;
        public const float PlayerX = -5.8f;
        public const float JumpSpeed = 11.5f;
        public const int MaxJumps = 2;
        public const float Gravity = 29f;
        public const float FastFallSpeed = 15f;
        public const float FastFallGravity = 58f;
        public const float PlayerWidth = 0.86f;
        public const float PlayerHeight = 1.3f;
        public const float DuckHeight = 0.8f;
        public const float MaximumObstacleWidth = 2.3f;
        public const float MaximumObstacleHeight = 1.2f;
        public const float SimulationStep = 1f / 120f;
        public const float EndlessLapSeconds = 30f;
        private static readonly float[] LevelStarts = { 0f, 12f, 28f, 48f, 75f, 105f };

        public static int Level(float elapsed)
        {
            for (int i = LevelStarts.Length - 1; i > 0; i--)
                if (elapsed >= LevelStarts[i]) return i + 1;
            return 1;
        }

        public static string LevelHint(int level)
        {
            switch (level)
            {
                case 2: return "LONG LEAPS";
                case 3: return "CLUSTER CROSSING";
                case 4: return "QUICK RHYTHM";
                case 5: return "BURST TRAIL";
                case 6: return "WILD RUN";
                default: return "FIRST STEPS";
            }
        }

        public static float LevelStart(int level) => LevelStarts[Mathf.Clamp(level, 1, 6) - 1];

        // The final stage stays endless, with measurable laps instead of a permanently full bar.
        public static int EndlessLap(float elapsed) => elapsed < LevelStart(6) ? 0 :
            1 + Mathf.FloorToInt((elapsed - LevelStart(6)) / EndlessLapSeconds);

        public static float LevelProgress(float elapsed)
        {
            int level = Level(elapsed);
            return level == 6 ? Mathf.Repeat(Mathf.Max(0f, elapsed - LevelStart(6)), EndlessLapSeconds) / EndlessLapSeconds :
                Mathf.InverseLerp(LevelStart(level), LevelStart(level + 1), elapsed);
        }

        public static float SecondsToNextStage(float elapsed)
        {
            int level = Level(elapsed);
            return level == 6 ? EndlessLapSeconds * (1f - LevelProgress(elapsed)) :
                LevelStart(level + 1) - Mathf.Max(0f, elapsed);
        }

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
