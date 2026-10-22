using NUnit.Framework;
using UnityEngine;

namespace DinoRush.Tests
{
    public sealed class RunnerDuckTests
    {
        private GameObject dino, shadow;
        private Texture2D texture;
        private Sprite[] frames;
        private RunnerPlayer player;

        [SetUp]
        public void CreatePlayer()
        {
            dino = new GameObject("Duck test player", typeof(SpriteRenderer));
            shadow = new GameObject("Duck test shadow");
            texture = new Texture2D(24, 24);
            frames = new Sprite[24];
            for (int i = 0; i < frames.Length; i++)
                frames[i] = Sprite.Create(texture, new Rect(0, 0, 24, 24), new Vector2(0.5f, 0), 24);
            player = new RunnerPlayer(dino.GetComponent<SpriteRenderer>(), frames, shadow.transform);
        }

        [TearDown]
        public void DestroyPlayer()
        {
            Object.DestroyImmediate(dino);
            Object.DestroyImmediate(shadow);
            foreach (Sprite frame in frames) Object.DestroyImmediate(frame);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void GroundDuckUsesCrouchFramesAndShorterHitboxWithoutSinkingOrAvoidingGroundObstacles()
        {
            Rect standing = player.Hitbox;
            player.SetDucking(true);
            player.Render(0.05f, true, false, 1f);
            Assert.That(player.IsDucking, Is.True);
            Assert.That(player.Hitbox.height, Is.EqualTo(RunnerRules.DuckHeight));
            Assert.That(player.Hitbox.yMin, Is.EqualTo(standing.yMin));
            Assert.That(player.Hitbox.width, Is.EqualTo(standing.width));
            Assert.That(player.Feet.y, Is.EqualTo(RunnerRules.GroundY));
            Assert.That(dino.GetComponent<SpriteRenderer>().sprite, Is.EqualTo(frames[18]));
            Assert.That(player.Hitbox.Overlaps(RunnerRules.ObstacleHitbox(RunnerRules.PlayerX, 1f, 1f)), Is.True);
            Rect overhead = new Rect(RunnerRules.PlayerX - 0.5f, standing.yMin + 1f, 1f, 0.2f);
            Assert.That(standing.Overlaps(overhead), Is.True);
            Assert.That(player.Hitbox.Overlaps(overhead), Is.False);
            player.SetDucking(false);
            Assert.That(player.Hitbox, Is.EqualTo(standing));
        }

        [TestCase(0.1f)]
        [TestCase(0.39f)]
        [TestCase(0.6f)]
        public void TappingDuckDuringAscentApexOrDescentCommitsAFastLanding(float jumpTime)
        {
            player.QueueJump();
            for (float t = 0; t < jumpTime; t += RunnerRules.SimulationStep)
                player.Step(RunnerRules.SimulationStep, out _, out _);
            Assert.That(player.Grounded, Is.False);
            float before = player.Feet.y;
            player.SetDucking(true);
            player.SetDucking(false);
            Assert.That(player.IsDucking, Is.True, "Releasing early must not cancel the drop.");
            player.QueueJump();
            player.Step(RunnerRules.SimulationStep, out bool jumped, out _);
            Assert.That(jumped, Is.False);
            Assert.That(player.Feet.y, Is.LessThan(before - RunnerRules.FastFallSpeed * RunnerRules.SimulationStep));
            int landings = 0;
            float landTime = 0f;
            for (int i = 0; i < 36; i++)
            {
                player.Step(RunnerRules.SimulationStep, out jumped, out bool landed);
                Assert.That(jumped, Is.False, "Fast fall must not queue a landing bounce.");
                if (landed) { landings++; landTime = (i + 2) * RunnerRules.SimulationStep; }
                Assert.That(player.Feet.y, Is.GreaterThanOrEqualTo(RunnerRules.GroundY));
            }
            Assert.That(landings, Is.EqualTo(1));
            Assert.That(landTime, Is.LessThan(0.16f));
            Assert.That(player.Grounded, Is.True);
            Assert.That(player.IsDucking, Is.False);
            player.QueueJump();
            player.Step(RunnerRules.SimulationStep, out jumped, out _);
            Assert.That(jumped, Is.True, "Jump must work immediately after a released fast drop lands.");
        }

        [Test]
        public void HoldingDuckClearsBufferedJumpAndStaysCrouchedAfterLanding()
        {
            player.QueueJump();
            player.SetDucking(true);
            player.Step(RunnerRules.SimulationStep, out bool jumped, out _);
            Assert.That(jumped, Is.False);
            player.QueueJump();
            player.SetDucking(false);
            player.Step(RunnerRules.SimulationStep, out jumped, out _);
            Assert.That(jumped, Is.False, "Releasing duck must not replay a blocked jump.");
            player.QueueJump();
            player.Step(0.2f, out _, out _);
            player.SetDucking(true);
            for (int i = 0; i < 30; i++) player.Step(RunnerRules.SimulationStep, out _, out _);
            Assert.That(player.Grounded, Is.True);
            Assert.That(player.IsDucking, Is.True);
            player.SetDucking(false);
            Assert.That(player.IsDucking, Is.False);
        }

        [Test]
        public void ResetClearsHeldDuckFastFallAndBufferedJump()
        {
            player.QueueJump();
            player.Step(0.2f, out _, out _);
            player.SetDucking(true);
            player.Reset();
            Assert.That(player.Grounded, Is.True);
            Assert.That(player.IsDucking, Is.False);
            Assert.That(player.Hitbox.height, Is.EqualTo(RunnerRules.PlayerHeight));
            player.Step(RunnerRules.SimulationStep, out bool jumped, out bool landed);
            Assert.That(jumped || landed, Is.False);
            player.QueueJump();
            player.Step(RunnerRules.SimulationStep, out jumped, out _);
            Assert.That(jumped, Is.True);
        }
    }
}
