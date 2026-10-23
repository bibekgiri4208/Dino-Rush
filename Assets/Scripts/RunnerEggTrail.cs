using System;
using UnityEngine;

namespace DinoRush
{
    public enum RunnerEggTrailKind { Low, HighArc, Risky }

    public sealed class RunnerEggTrail
    {
        public RunnerEggTrailKind Kind { get; internal set; }
        // Coordinates are relative to the encounter's first obstacle and the ground.
        public Vector2[] Eggs { get; internal set; }
        public float TakeoffOffset { get; internal set; }
        public float SecondJumpTime { get; internal set; }
    }

    /// <summary>Optional routes around unchanged encounters; never consumes gameplay's random stream.</summary>
    public sealed class RunnerEggTrailDirector
    {
        private readonly System.Random random;
        private int trails;
        private RunnerEggTrailKind previous;
        public const float PickupRadius = 0.25f;
        public const float LowHeight = 0.7f;
        public static float ApexTime => RunnerRules.JumpSpeed / RunnerRules.Gravity;
        public static float DoubleFlightTime => ApexTime + (RunnerRules.JumpSpeed +
            Mathf.Sqrt(2f * RunnerRules.JumpSpeed * RunnerRules.JumpSpeed)) / RunnerRules.Gravity;

        public RunnerEggTrailDirector(int seed) => random = new System.Random(seed);

        public RunnerEggTrail Next(RunnerEncounter encounter, float speed)
        {
            RunnerEggTrailKind kind = trails < 2 ? RunnerEggTrailKind.Low :
                (RunnerEggTrailKind)(((int)previous + 1 + random.Next(2)) % 3);
            RunnerEggTrail trail = Create(encounter, speed, kind);
            previous = trail.Kind;
            trails++;
            return trail;
        }

        public static RunnerEggTrail Create(RunnerEncounter encounter, float speed, RunnerEggTrailKind kind)
        {
            if (encounter == null || encounter.Obstacles == null || encounter.Obstacles.Length == 0)
                throw new ArgumentException("Egg trails need a complete obstacle encounter.", nameof(encounter));
            if (float.IsNaN(speed) || float.IsInfinity(speed) || speed <= 0f)
                throw new ArgumentOutOfRangeException(nameof(speed));
            RunnerObstacleSpawn first = encounter.Obstacles[0];
            RunnerObstacleSpawn last = encounter.Obstacles[encounter.Obstacles.Length - 1];
            bool rapid = encounter.Pattern == RunnerPattern.QuickSteps || encounter.Pattern == RunnerPattern.Burst;
            float center = rapid ? first.Offset :
                (first.Offset - first.Width * 0.5f + last.Offset + last.Width * 0.5f) * 0.5f;
            // A double jump needs room to land before the following encounter. Rapid
            // patterns keep their single-jump rhythm instead of advertising an unsafe route.
            if (kind == RunnerEggTrailKind.HighArc && (rapid ||
                center + speed * (DoubleFlightTime - ApexTime) + 1.6f > encounter.Spacing))
                kind = RunnerEggTrailKind.Risky;

            var trail = new RunnerEggTrail { Kind = kind, SecondJumpTime = -1f };
            if (kind == RunnerEggTrailKind.Low)
            {
                float start = last.Offset + last.Width * 0.5f + 1.25f;
                float end = encounter.Spacing - 1.65f;
                int count = Mathf.Min(4, Mathf.FloorToInt((end - start) / 0.85f) + 1);
                trail.Eggs = new Vector2[Mathf.Max(0, count)];
                float width = Mathf.Max(0, count - 1) * 0.85f;
                start += Mathf.Max(0f, (end - start - width) * 0.5f);
                for (int i = 0; i < trail.Eggs.Length; i++)
                    trail.Eggs[i] = new Vector2(start + i * 0.85f, LowHeight);
                return trail;
            }

            trail.TakeoffOffset = center - speed * ApexTime;
            bool high = kind == RunnerEggTrailKind.HighArc;
            if (high) trail.SecondJumpTime = ApexTime;
            int samples = high ? 8 : 6;
            trail.Eggs = new Vector2[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = Mathf.Lerp(0.14f, high ? 1.17f : 0.67f, i / (float)(samples - 1));
                trail.Eggs[i] = new Vector2(trail.TakeoffOffset + speed * t,
                    FlightHeight(t, high) + LowHeight);
            }
            return trail;
        }

        public static float FlightHeight(float time, bool doubleJump)
        {
            float baseHeight = 0f;
            if (doubleJump && time >= ApexTime)
            {
                baseHeight = RunnerRules.JumpSpeed * ApexTime - 0.5f * RunnerRules.Gravity * ApexTime * ApexTime;
                time -= ApexTime;
            }
            return Mathf.Max(0f, baseHeight + RunnerRules.JumpSpeed * time - 0.5f * RunnerRules.Gravity * time * time);
        }

        public static Rect PickupBox(Vector2 position) => new Rect(position.x - PickupRadius,
            position.y - PickupRadius, PickupRadius * 2f, PickupRadius * 2f);
    }
}
