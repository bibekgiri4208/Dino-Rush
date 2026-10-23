using NUnit.Framework;
using UnityEngine;

namespace DinoRush.Tests
{
    public sealed class RunnerRulesTests
    {
        [Test]
        public void PaceIncreasesButHasACap()
        {
            Assert.That(RunnerRules.Speed(0), Is.EqualTo(6.8f).Within(0.001f));
            Assert.That(RunnerRules.Speed(50), Is.GreaterThan(RunnerRules.Speed(0)));
            Assert.That(RunnerRules.Speed(10000), Is.EqualTo(RunnerRules.MaximumSpeed).Within(0.001f));
        }

        [TestCase(12f, 8.8f)]
        [TestCase(28f, 11f)]
        [TestCase(48f, 13.4f)]
        [TestCase(75f, 15.3f)]
        [TestCase(105f, 16.8f)]
        public void StagePaceRampsContinuouslyIntoAMoreDemandingLateGame(float elapsed, float speed)
        {
            Assert.That(RunnerRules.Speed(elapsed), Is.EqualTo(speed).Within(0.001f));
            Assert.That(RunnerRules.Speed(elapsed - 0.001f), Is.EqualTo(speed).Within(0.001f),
                "Stage transitions must not create abrupt speed jumps.");
            Assert.That(RunnerRules.Speed(elapsed + 0.001f), Is.EqualTo(speed).Within(0.001f));
            Assert.That(RunnerRules.Speed(-10f), Is.EqualTo(RunnerRules.Speed(0f)));
        }

        [TestCase(0f, 0f, 12f, 0)]
        [TestCase(6f, 0.5f, 6f, 0)]
        [TestCase(12f, 0f, 16f, 0)]
        [TestCase(20f, 0.5f, 8f, 0)]
        [TestCase(90f, 0.5f, 15f, 0)]
        [TestCase(105f, 0f, 30f, 1)]
        [TestCase(120f, 0.5f, 15f, 1)]
        [TestCase(135f, 0f, 30f, 2)]
        [TestCase(180f, 0.5f, 15f, 3)]
        public void StageProgressAndEndlessLapsHaveClearBoundaries(float elapsed, float progress, float remaining, int lap)
        {
            Assert.That(RunnerRules.LevelProgress(elapsed), Is.EqualTo(progress).Within(0.0001f));
            Assert.That(RunnerRules.SecondsToNextStage(elapsed), Is.EqualTo(remaining).Within(0.0001f));
            Assert.That(RunnerRules.EndlessLap(elapsed), Is.EqualTo(lap));
        }

        [TestCase(6.8f)]
        [TestCase(10f)]
        [TestCase(13.2f)]
        [TestCase(16.8f)]
        public void WidestTallestObstacleCanBeClearedAtEveryPace(float speed)
        {
            float flight = 2f * RunnerRules.JumpSpeed / RunnerRules.Gravity;
            for (float t = 0; t < flight; t += RunnerRules.SimulationStep)
            {
                float height = RunnerRules.JumpSpeed * t - 0.5f * RunnerRules.Gravity * t * t;
                float obstacleX = RunnerRules.PlayerX + speed * (flight * 0.5f - t);
                Assert.That(RunnerRules.PlayerHitbox(height).Overlaps(RunnerRules.ObstacleHitbox(
                    obstacleX, RunnerRules.MaximumObstacleWidth, RunnerRules.MaximumObstacleHeight)), Is.False,
                    $"Collision at t={t}, speed={speed}");
            }
        }

        [TestCase(6.8f)]
        [TestCase(13.2f)]
        [TestCase(16.8f)]
        public void SpawnIntervalAllowsLandingAndRecovery(float speed)
        {
            float flight = 2f * RunnerRules.JumpSpeed / RunnerRules.Gravity;
            Assert.That(RunnerRules.MinimumSpawnInterval(speed), Is.GreaterThan(flight + 0.3f));
        }

        [Test]
        public void GroundedPlayerCollidesWithObstacleInTheirLane()
        {
            Assert.That(RunnerRules.PlayerHitbox(0).Overlaps(
                RunnerRules.ObstacleHitbox(RunnerRules.PlayerX, 1f, 1f)), Is.True);
        }

        [TestCase(0.1f)]
        [TestCase(0.4f)]
        [TestCase(0.6f)]
        public void DoubleJumpBoostsDuringAscentApexOrDescentButNeverAllowsAThirdJump(float jumpTime)
        {
            var playerObject = new GameObject("Test player", typeof(SpriteRenderer));
            var shadow = new GameObject("Test shadow");
            var texture = new Texture2D(24, 24);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 24, 24), Vector2.one * 0.5f, 24);
            var frames = new Sprite[24];
            for (int i = 0; i < frames.Length; i++) frames[i] = sprite;
            try
            {
                var player = new RunnerPlayer(playerObject.GetComponent<SpriteRenderer>(), frames, shadow.transform);
                Assert.That(player.JumpsRemaining, Is.EqualTo(2));
                player.QueueJump();
                player.Step(RunnerRules.SimulationStep, out bool firstJump, out _);
                Assert.That(firstJump, Is.True);
                for (float t = RunnerRules.SimulationStep; t < jumpTime; t += RunnerRules.SimulationStep)
                    player.Step(RunnerRules.SimulationStep, out _, out _);
                Assert.That(player.Grounded, Is.False);
                Assert.That(player.JumpsRemaining, Is.EqualTo(1), "A single press must not consume the air jump automatically.");
                float before = player.Feet.y;
                player.QueueJump();
                player.Step(RunnerRules.SimulationStep, out bool secondJump, out _);
                Assert.That(secondJump, Is.True);
                Assert.That(player.Feet.y - before, Is.EqualTo(RunnerRules.JumpSpeed * RunnerRules.SimulationStep
                    - 0.5f * RunnerRules.Gravity * RunnerRules.SimulationStep * RunnerRules.SimulationStep).Within(0.0001f));
                Assert.That(player.JumpsRemaining, Is.Zero);
                player.QueueJump();
                player.Step(RunnerRules.SimulationStep, out bool thirdJump, out _);
                Assert.That(thirdJump, Is.False);
                for (int i = 0; i < 200; i++)
                {
                    player.Step(RunnerRules.SimulationStep, out bool extraJump, out _);
                    Assert.That(extraJump, Is.False, "An exhausted jump request must expire without an automatic bounce.");
                }
                Assert.That(player.Grounded, Is.True);
                Assert.That(player.JumpsRemaining, Is.EqualTo(2));
                player.QueueJump();
                player.Step(RunnerRules.SimulationStep, out bool nextJump, out _);
                Assert.That(nextJump, Is.True);
                player.QueueJump();
                player.Step(RunnerRules.SimulationStep, out bool nextAirJump, out _);
                Assert.That(nextAirJump, Is.True, "Landing restores both jumps, not just the ground jump.");
                player.QueueJump();
                player.Reset();
                Assert.That(player.Grounded, Is.True);
                Assert.That(player.JumpsRemaining, Is.EqualTo(2));
                player.Step(RunnerRules.SimulationStep, out bool resetJump, out _);
                Assert.That(resetJump, Is.False, "Reset must also clear any buffered third jump.");
            }
            finally
            {
                Object.DestroyImmediate(playerObject);
                Object.DestroyImmediate(shadow);
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
            }
        }
    }
}
