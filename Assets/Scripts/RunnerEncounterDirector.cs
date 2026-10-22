using System;
using UnityEngine;

namespace DinoRush
{
    public enum RunnerPattern { Single, Double, LongJump, QuickSteps, Triple, Burst }

    public readonly struct RunnerObstacleSpawn
    {
        public readonly int SpriteIndex;
        public readonly float Offset, Width, Height;

        public RunnerObstacleSpawn(int spriteIndex, float offset, float width, float height)
        {
            SpriteIndex = spriteIndex;
            Offset = offset;
            Width = width;
            Height = height;
        }
    }

    public sealed class RunnerEncounter
    {
        public RunnerPattern Pattern { get; internal set; }
        public RunnerObstacleSpawn[] Obstacles { get; internal set; }
        public float Spacing { get; internal set; }
        public bool Breather { get; internal set; }
    }

    /// <summary>Per-run randomness, progressive encounter vocabulary and physics-bounded spacing.</summary>
    public sealed class RunnerEncounterDirector
    {
        private readonly System.Random random;
        private readonly float[] aspects;
        private int encounters;
        private int lastSprite = -1;
        private RunnerPattern previous;
        private int repeats;
        private int hardStreak;
        private int untilBreather;

        public RunnerEncounterDirector(int seed, float[] spriteAspectRatios)
        {
            if (spriteAspectRatios == null || spriteAspectRatios.Length == 0)
                throw new ArgumentException("At least one obstacle sprite is required.", nameof(spriteAspectRatios));
            aspects = (float[])spriteAspectRatios.Clone();
            foreach (float aspect in aspects)
                if (float.IsNaN(aspect) || float.IsInfinity(aspect) || aspect <= 0f)
                    throw new ArgumentException("Sprite aspect ratios must be finite and positive.", nameof(spriteAspectRatios));
            random = new System.Random(seed);
            untilBreather = random.Next(4, 8);
        }

        public RunnerEncounter Next(float elapsed)
        {
            int level = RunnerRules.Level(elapsed);
            float difficulty = Mathf.Clamp01(elapsed / 105f);
            float speed = RunnerRules.Speed(elapsed);
            // Encounters enter from offscreen. Reserve recovery distance for the
            // faster pace at arrival, not just the speed when they were spawned.
            float arrivalSpeed = RunnerRules.Speed(elapsed + 6f);
            bool breather = encounters >= 3 && --untilBreather <= 0;
            RunnerPattern pattern = encounters < 3 ? RunnerPattern.Single : breather ?
                (previous == RunnerPattern.Single && repeats >= 2 ? RunnerPattern.Double : RunnerPattern.Single) : Pick(level);
            if (breather) untilBreather = random.Next(4, 8);
            repeats = encounters > 0 && pattern == previous ? repeats + 1 : 1;
            previous = pattern;
            hardStreak = IsRapid(pattern) ? hardStreak + 1 : 0;

            int count = pattern == RunnerPattern.Single ? 1 :
                pattern == RunnerPattern.Triple || pattern == RunnerPattern.Burst ? 3 : 2;
            bool rapid = IsRapid(pattern);
            var spawns = new RunnerObstacleSpawn[count];
            float cursor = 0f;
            for (int i = 0; i < count; i++)
            {
                int sprite = PickSprite();
                float height = Range(0.68f + difficulty * 0.18f, 0.96f + difficulty * 0.24f);
                float width = aspects[sprite] * height;
                float widthLimit = count == 1 || rapid ? RunnerRules.MaximumObstacleWidth :
                    pattern == RunnerPattern.LongJump ? 1.8f : 1.35f;
                if (width > widthLimit)
                {
                    height *= widthLimit / width;
                    width = widthLimit;
                }
                if (i > 0)
                    cursor += rapid ? arrivalSpeed * Range(0.94f, 1.18f) :
                        (spawns[i - 1].Width + width) * 0.5f + Range(
                            pattern == RunnerPattern.LongJump ? 0.65f : 0.12f,
                            pattern == RunnerPattern.LongJump ? 1.1f : 0.4f);
                spawns[i] = new RunnerObstacleSpawn(sprite, cursor, width, height);
            }

            if (!rapid && count > 1)
            {
                // A clustered encounter is one jump, not an impossible wall. Fit
                // its full artwork inside a conservative airborne clearance window.
                float span = cursor + (spawns[0].Width + spawns[count - 1].Width) * 0.5f;
                float budget = Mathf.Max(RunnerRules.MaximumObstacleWidth, speed * 0.48f - RunnerRules.PlayerWidth);
                float scale = Mathf.Min(1f, budget / span);
                for (int i = 0; i < count; i++)
                {
                    RunnerObstacleSpawn item = spawns[i];
                    spawns[i] = new RunnerObstacleSpawn(item.SpriteIndex, item.Offset * scale,
                        item.Width * scale, item.Height * scale);
                }
                cursor *= scale;
            }

            float recovery = Range(Mathf.Lerp(1.3f, 0.92f, difficulty), Mathf.Lerp(2.0f, 1.25f, difficulty));
            if (breather) recovery += Range(0.35f, 0.7f);
            encounters++;
            return new RunnerEncounter
            {
                Pattern = pattern,
                Obstacles = spawns,
                Spacing = cursor + arrivalSpeed * recovery,
                Breather = breather
            };
        }

        private RunnerPattern Pick(int level)
        {
            // Weighted choices rather than a repeating playlist. Allow one repeat,
            // but never three identical patterns or three rapid encounters in a row.
            int single = level >= 4 ? 2 : 4;
            int doubles = level == 1 ? 1 : 3;
            int total = single + doubles + (level >= 2 ? 4 : 0) + (level >= 3 ? 2 : 0) + (level >= 4 ? 2 : 0);
            for (int attempt = 0; attempt < 32; attempt++)
            {
                int choice = random.Next(total);
                RunnerPattern pattern;
                if (choice < single) pattern = RunnerPattern.Single;
                else if ((choice -= single) < doubles) pattern = RunnerPattern.Double;
                else if ((choice -= doubles) < 2) pattern = RunnerPattern.LongJump;
                else if ((choice -= 2) < 2) pattern = RunnerPattern.QuickSteps;
                else if ((choice -= 2) < 2) pattern = RunnerPattern.Triple;
                else pattern = RunnerPattern.Burst;
                if (repeats >= 2 && pattern == previous) continue;
                if (hardStreak >= 2 && IsRapid(pattern)) continue;
                return pattern;
            }
            return previous == RunnerPattern.Single ? RunnerPattern.Double : RunnerPattern.Single;
        }

        private int PickSprite()
        {
            int choice = random.Next(lastSprite < 0 || aspects.Length == 1 ? aspects.Length : aspects.Length - 1);
            if (aspects.Length > 1 && lastSprite >= 0 && choice >= lastSprite) choice++;
            lastSprite = choice;
            return choice;
        }

        private float Range(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());
        private static bool IsRapid(RunnerPattern pattern) => pattern == RunnerPattern.QuickSteps || pattern == RunnerPattern.Burst;
    }
}
