using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DinoRush.Tests
{
    public sealed class RunnerEncounterTests
    {
        private static readonly float[] Aspects = { 0.9f, 1.2f, 1.7f, 2.1f, 2.8f };

        [TestCase(0f, 1)]
        [TestCase(11.99f, 1)]
        [TestCase(12f, 2)]
        [TestCase(28f, 3)]
        [TestCase(48f, 4)]
        [TestCase(75f, 5)]
        [TestCase(105f, 6)]
        [TestCase(10000f, 6)]
        public void DifficultyUnlocksInSixStages(float elapsed, int level)
        {
            Assert.That(RunnerRules.Level(elapsed), Is.EqualTo(level));
        }

        [TestCase(0f)]
        [TestCase(12f)]
        [TestCase(28f)]
        [TestCase(48f)]
        [TestCase(75f)]
        [TestCase(105f)]
        [TestCase(600f)]
        public void ThousandsOfEncountersRespectJumpClearanceAndRecovery(float elapsed)
        {
            float speed = RunnerRules.Speed(elapsed);
            for (int seed = 0; seed < 32; seed++)
            {
                var director = new RunnerEncounterDirector(seed, Aspects);
                for (int sample = 0; sample < 30; sample++)
                {
                    RunnerEncounter encounter = director.Next(elapsed);
                    Assert.That(encounter.Obstacles.Length, Is.InRange(1, 3));
                    float lastOffset = -1f;
                    foreach (RunnerObstacleSpawn item in encounter.Obstacles)
                    {
                        Assert.That(item.Offset, Is.GreaterThan(lastOffset));
                        Assert.That(item.Height, Is.GreaterThan(0).And.LessThanOrEqualTo(RunnerRules.MaximumObstacleHeight));
                        Assert.That(item.Width, Is.GreaterThan(0).And.LessThanOrEqualTo(RunnerRules.MaximumObstacleWidth));
                        Assert.That(item.Width / item.Height, Is.EqualTo(Aspects[item.SpriteIndex]).Within(0.001f));
                        lastOffset = item.Offset;
                    }
                    Assert.That(encounter.Spacing - lastOffset,
                        Is.GreaterThanOrEqualTo(RunnerRules.Speed(elapsed + 6f) * 0.92f - 0.001f));
                    bool rapid = encounter.Pattern == RunnerPattern.QuickSteps || encounter.Pattern == RunnerPattern.Burst;
                    if (rapid)
                    {
                        for (int i = 1; i < encounter.Obstacles.Length; i++)
                            Assert.That((encounter.Obstacles[i].Offset - encounter.Obstacles[i - 1].Offset) / RunnerRules.Speed(elapsed + 6f),
                                Is.GreaterThanOrEqualTo(0.939f));
                        foreach (RunnerObstacleSpawn item in encounter.Obstacles)
                            AssertJumpClearance(new[] { item }, item.Offset, speed, seed, encounter.Pattern);
                    }
                    else AssertJumpClearance(encounter.Obstacles, Center(encounter.Obstacles), speed, seed, encounter.Pattern);
                }
            }
        }

        [Test]
        public void WarmupThenWeightedVarietyUnlocksWithoutLongRepetitions()
        {
            var director = new RunnerEncounterDirector(147, Aspects);
            for (int i = 0; i < 3; i++) Assert.That(director.Next(0).Pattern, Is.EqualTo(RunnerPattern.Single));
            RunnerPattern previous = RunnerPattern.Single;
            int streak = 0, rapidStreak = 0, breathers = 0;
            var seen = new HashSet<RunnerPattern>();
            var spacings = new HashSet<int>();
            for (int i = 0; i < 500; i++)
            {
                RunnerEncounter encounter = director.Next(120);
                streak = encounter.Pattern == previous ? streak + 1 : 1;
                previous = encounter.Pattern;
                rapidStreak = encounter.Pattern == RunnerPattern.QuickSteps || encounter.Pattern == RunnerPattern.Burst ? rapidStreak + 1 : 0;
                Assert.That(streak, Is.LessThanOrEqualTo(2));
                Assert.That(rapidStreak, Is.LessThanOrEqualTo(2));
                if (encounter.Breather) breathers++;
                seen.Add(encounter.Pattern);
                spacings.Add(Mathf.RoundToInt(encounter.Spacing * 100));
            }
            Assert.That(seen.Count, Is.EqualTo(6));
            Assert.That(spacings.Count, Is.GreaterThan(150));
            Assert.That(breathers, Is.InRange(50, 125));
            var early = new RunnerEncounterDirector(147, Aspects);
            for (int i = 0; i < 100; i++)
                Assert.That(early.Next(0).Pattern, Is.EqualTo(RunnerPattern.Single).Or.EqualTo(RunnerPattern.Double));
        }

        [Test]
        public void LateGameIsDenserThanWarmupWithoutRemovingBreathingRoom()
        {
            float earlyRate = Density(0), lateRate = Density(120);
            Assert.That(lateRate, Is.GreaterThan(earlyRate * 1.6f), "Progression must change the challenge, not just the badge.");
        }

        [Test]
        public void SeedReplaysIndependentlyOfParticlesAndDifferentSeedsChangeTheCourse()
        {
            var first = new RunnerEncounterDirector(42, Aspects);
            var second = new RunnerEncounterDirector(42, Aspects);
            var different = new RunnerEncounterDirector(73, Aspects);
            Random.State saved = Random.state;
            int differences = 0;
            try
            {
                for (int i = 0; i < 100; i++)
                {
                    RunnerEncounter a = first.Next(i * 2f);
                    for (int particle = 0; particle < 36; particle++) Random.Range(0, 10000);
                    RunnerEncounter b = second.Next(i * 2f), c = different.Next(i * 2f);
                    Assert.That(b.Pattern, Is.EqualTo(a.Pattern));
                    Assert.That(b.Spacing, Is.EqualTo(a.Spacing));
                    Assert.That(b.Obstacles, Is.EqualTo(a.Obstacles));
                    if (c.Spacing != a.Spacing) differences++;
                }
            }
            finally { Random.state = saved; }
            Assert.That(differences, Is.GreaterThan(90));
        }

        [TestCase(17)]
        [TestCase(42)]
        [TestCase(73)]
        [TestCase(147)]
        public void FullAcceleratingCoursesHaveAContinuousPlayableJumpRoute(int seed)
        {
            var director = new RunnerEncounterDirector(seed, Aspects);
            var obstacles = new List<RunnerObstacleSpawn>();
            var targets = new List<float>();
            var dino = new GameObject("Course test dino", typeof(SpriteRenderer));
            var shadow = new GameObject("Course test shadow");
            var texture = new Texture2D(24, 24);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 24, 24), Vector2.zero, 24);
            var frames = new Sprite[24];
            for (int i = 0; i < frames.Length; i++) frames[i] = sprite;
            var player = new RunnerPlayer(dino.GetComponent<SpriteRenderer>(), frames, shadow.transform);
            float distance = 0, nextSpawn = 0;
            int target = 0, cleared = 0;
            try
            {
                for (int step = 1; step <= 180 * 120; step++)
                {
                    float elapsed = step * RunnerRules.SimulationStep;
                    float speed = RunnerRules.Speed(elapsed);
                    distance += speed * RunnerRules.SimulationStep;
                    if (elapsed >= 0.7f && distance >= nextSpawn)
                    {
                        RunnerEncounter encounter = director.Next(elapsed);
                        float origin = distance + 11.7f - RunnerRules.PlayerX;
                        foreach (RunnerObstacleSpawn item in encounter.Obstacles)
                            obstacles.Add(new RunnerObstacleSpawn(item.SpriteIndex, origin + item.Offset, item.Width, item.Height));
                        if (encounter.Pattern == RunnerPattern.QuickSteps || encounter.Pattern == RunnerPattern.Burst)
                            foreach (RunnerObstacleSpawn item in encounter.Obstacles) targets.Add(origin + item.Offset);
                        else targets.Add(origin + Center(encounter.Obstacles));
                        nextSpawn = distance + encounter.Spacing;
                    }
                    float apex = RunnerRules.JumpSpeed / RunnerRules.Gravity;
                    float lead = (speed + RunnerRules.Speed(elapsed + apex)) * 0.5f * apex;
                    if (target < targets.Count && player.Grounded && targets[target] - distance <= lead)
                    {
                        player.QueueJump();
                        target++;
                    }
                    player.Step(RunnerRules.SimulationStep, out _, out _);
                    foreach (RunnerObstacleSpawn item in obstacles)
                    {
                        float x = RunnerRules.PlayerX + item.Offset - distance;
                        if (x < RunnerRules.PlayerX - 4f || x > RunnerRules.PlayerX + 4f) continue;
                        if (player.Hitbox.Overlaps(RunnerRules.ObstacleHitbox(x, item.Width, item.Height)))
                            Assert.Fail($"No continuous route: seed {seed}, t={elapsed}, obstacle={item.Offset}, target={target}.");
                    }
                    while (cleared < obstacles.Count && obstacles[cleared].Offset + 2f < distance) cleared++;
                }
                Assert.That(cleared, Is.GreaterThan(150));
                Assert.That(target, Is.GreaterThan(100));
            }
            finally
            {
                Object.DestroyImmediate(dino);
                Object.DestroyImmediate(shadow);
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
            }
        }

        private static float Center(RunnerObstacleSpawn[] items) =>
            (items[0].Offset - items[0].Width * 0.5f + items[items.Length - 1].Offset + items[items.Length - 1].Width * 0.5f) * 0.5f;

        private static void AssertJumpClearance(RunnerObstacleSpawn[] items, float center, float speed, int seed, RunnerPattern pattern)
        {
            float apex = RunnerRules.JumpSpeed / RunnerRules.Gravity;
            for (float t = 0; t < 2f * apex; t += RunnerRules.SimulationStep)
            {
                float height = RunnerRules.JumpSpeed * t - 0.5f * RunnerRules.Gravity * t * t;
                foreach (RunnerObstacleSpawn item in items)
                {
                    float x = RunnerRules.PlayerX + item.Offset - center + speed * (apex - t);
                    if (RunnerRules.PlayerHitbox(height).Overlaps(RunnerRules.ObstacleHitbox(x, item.Width, item.Height)))
                        Assert.Fail($"Unjumpable {pattern} at speed {speed}, seed {seed}, t={t}.");
                }
            }
        }

        private static float Density(float elapsed)
        {
            var director = new RunnerEncounterDirector(42, Aspects);
            float seconds = 0;
            int count = 0;
            for (int i = 0; i < 500; i++)
            {
                RunnerEncounter encounter = director.Next(elapsed);
                count += encounter.Obstacles.Length;
                seconds += encounter.Spacing / RunnerRules.Speed(elapsed);
            }
            return count / seconds;
        }
    }
}
