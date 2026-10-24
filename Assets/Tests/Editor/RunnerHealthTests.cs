using NUnit.Framework;

namespace DinoRush.Tests
{
    public sealed class RunnerHealthTests
    {
        [Test]
        public void ThreeSeparateHitsDepleteExactlyThreeHearts()
        {
            var health = new RunnerHealth();
            Assert.That(health.Lives, Is.EqualTo(3));
            for (int remaining = 2; remaining >= 0; remaining--)
            {
                Assert.That(health.TryTakeHit(), Is.True);
                Assert.That(health.Lives, Is.EqualTo(remaining));
                health.Step(RunnerHealth.InvulnerabilityDuration);
            }
            Assert.That(health.TryTakeHit(), Is.False);
            Assert.That(health.Lives, Is.Zero);
        }

        [Test]
        public void GracePeriodRejectsRepeatedHitsAndOnlyAdvancesWithSimulation()
        {
            var health = new RunnerHealth();
            health.TryTakeHit();
            for (int i = 0; i < 100; i++) Assert.That(health.TryTakeHit(), Is.False);
            Assert.That(health.Lives, Is.EqualTo(2));
            health.Step(RunnerHealth.InvulnerabilityDuration - 0.01f);
            Assert.That(health.TryTakeHit(), Is.False);
            health.Step(0.02f);
            Assert.That(health.TryTakeHit(), Is.True);
            Assert.That(health.Lives, Is.EqualTo(1));
        }

        [Test]
        public void ResetRestoresAllHeartsAndClearsInvulnerability()
        {
            var health = new RunnerHealth();
            health.TryTakeHit();
            health.Reset();
            Assert.That(health.Lives, Is.EqualTo(3));
            Assert.That(health.IsInvulnerable, Is.False);
            Assert.That(health.TryTakeHit(), Is.True);
        }
    }
}
