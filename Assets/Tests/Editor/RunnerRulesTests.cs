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
            Assert.That(RunnerRules.Speed(10000), Is.EqualTo(13.2f).Within(0.001f));
        }

        [TestCase(6.8f)]
        [TestCase(10f)]
        [TestCase(13.2f)]
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

        [Test]
        public void JumpBufferDoesNotAllowADoubleJump()
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
                player.QueueJump();
                player.Step(0.1f, out bool firstJump, out _);
                Assert.That(firstJump, Is.True);
                player.QueueJump();
                player.Step(0.1f, out bool secondJump, out _);
                Assert.That(secondJump, Is.False);
                for (int i = 0; i < 100; i++) player.Step(RunnerRules.SimulationStep, out _, out _);
                Assert.That(player.Grounded, Is.True);
                player.QueueJump();
                player.Step(RunnerRules.SimulationStep, out bool nextJump, out _);
                Assert.That(nextJump, Is.True);
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
