using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DinoRush.Tests
{
    public sealed class RunnerTrailParticleTests
    {
        private static Dictionary<uint, ParticleSystem.Particle> Snapshot(RunnerTrailParticles effect)
        {
            var buffer = new ParticleSystem.Particle[RunnerTrailParticles.Capacity];
            int count = effect.GetParticles(buffer);
            var result = new Dictionary<uint, ParticleSystem.Particle>();
            for (int i = 0; i < count; i++) result.Add(buffer[i].randomSeed, buffer[i]);
            return result;
        }

        [UnityTest]
        public IEnumerator ExistingParticlesStayAtTheirOwnAltitudeWhileJumpAndLandingSpawnNewParticles()
        {
            foreach (RunnerTrailKind kind in new[] { RunnerTrailKind.Smoke, RunnerTrailKind.Fire, RunnerTrailKind.Electric, RunnerTrailKind.Frost, RunnerTrailKind.Blossom, RunnerTrailKind.Cosmic })
            {
                var root = new GameObject("Particle jump regression");
                using (var effect = new RunnerTrailParticles(root.transform))
                {
                    try
                    {
                        effect.SetKind(kind);
                        for (int i = 0; i < 4; i++) effect.Advance(0.1f, Vector2.zero, false, 5, true);
                        var grounded = Snapshot(effect);
                        Assert.That(grounded.Count, Is.GreaterThan(2));
                        effect.Advance(0.1f, new Vector2(0, 2), false, 5, true);
                        var jumping = Snapshot(effect);
                        int old = 0, high = 0;
                        foreach (var pair in jumping)
                        {
                            if (grounded.TryGetValue(pair.Key, out var before))
                            {
                                old++;
                                Assert.That(Mathf.Abs(pair.Value.position.y - before.position.y), Is.LessThan(0.4f), kind + " old particles must not rise with the emitter");
                            }
                            else if (pair.Value.position.y > 2) high++;
                        }
                        Assert.That(old, Is.GreaterThan(0));
                        Assert.That(high, Is.GreaterThan(0), kind + " must create fresh particles at the airborne dino");
                        effect.Advance(0.1f, Vector2.zero, true, 5, true);
                        effect.Advance(0.07f, Vector2.zero, true, 5, true); // A full emission interval after touching down.
                        var landing = Snapshot(effect);
                        int retainedHigh = 0, newLow = 0;
                        foreach (var pair in landing)
                        {
                            if (jumping.TryGetValue(pair.Key, out var before))
                            {
                                Assert.That(Mathf.Abs(pair.Value.position.y - before.position.y), Is.LessThan(0.4f), kind + " landing must not pull old particles down");
                                if (pair.Value.position.y > 1.8f) retainedHigh++;
                            }
                            else if (pair.Value.position.y < 0.8f) newLow++;
                        }
                        Assert.That(retainedHigh, Is.GreaterThan(0), kind + " retains airborne particles after landing");
                        Assert.That(newLow, Is.GreaterThan(0), kind + " emits low particles from the crouched dino");
                        Assert.That(effect.System.main.simulationSpace, Is.EqualTo(ParticleSystemSimulationSpace.World));
                    }
                    finally { Object.Destroy(root); }
                }
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator NativeParticlesDriftIndividuallyFreezeWithoutSimulationAndEventuallyDisappear()
        {
            foreach (RunnerTrailKind kind in new[] { RunnerTrailKind.Smoke, RunnerTrailKind.Fire, RunnerTrailKind.Electric, RunnerTrailKind.Frost, RunnerTrailKind.Blossom, RunnerTrailKind.Cosmic })
            {
                var root = new GameObject("Particle motion regression");
                Random.State random = Random.state;
                using (var effect = new RunnerTrailParticles(root.transform))
                {
                    try
                    {
                        effect.SetKind(kind);
                        for (int i = 0; i < 10; i++) effect.Advance(0.1f, Vector2.zero, false, 5, true);
                        Assert.That(effect.ActiveCount, Is.InRange(3, RunnerTrailParticles.Capacity));
                        Assert.That(effect.Texture.width, Is.EqualTo(64), "Only a small individual particle sprite, never a whole-trail image.");
                        var before = Snapshot(effect);
                        effect.Advance(0.1f, Vector2.zero, false, 5, false);
                        var after = Snapshot(effect);
                        int moving = 0, rotating = 0;
                        foreach (var pair in after)
                            if (before.TryGetValue(pair.Key, out var previous))
                            {
                                Assert.That(pair.Value.remainingLifetime, Is.LessThan(previous.remainingLifetime));
                                if (Vector3.Distance(pair.Value.position, previous.position) > 0.01f) moving++;
                                if (Mathf.Abs(pair.Value.rotation - previous.rotation) > 0.01f) rotating++;
                            }
                        Assert.That(moving, Is.GreaterThan(1));
                        Assert.That(rotating, Is.GreaterThan(1));
                        Assert.That(effect.System.isPaused, Is.True);
                        Assert.That(Random.state, Is.EqualTo(random), "Cosmetics must not consume gameplay RNG.");
                        float clock = effect.Clock;
                        effect.Advance(0, new Vector2(30, 30), false, 10, true);
                        yield return new WaitForSecondsRealtime(0.12f);
                        var paused = Snapshot(effect);
                        Assert.That(effect.Clock, Is.EqualTo(clock));
                        Assert.That(paused.Count, Is.EqualTo(after.Count));
                        foreach (var pair in paused) Assert.That(pair.Value.position, Is.EqualTo(after[pair.Key].position), "A custom game pause must also pause native particles.");
                        for (int i = 0; i < 20; i++) effect.Advance(0.1f, Vector2.zero, false, 5, false);
                        Assert.That(effect.ActiveCount, Is.Zero);
                        effect.Advance(0.1f, Vector2.zero, false, 5, true);
                        Assert.That(effect.ActiveCount, Is.GreaterThan(0));
                        effect.Clear();
                        Assert.That(effect.ActiveCount, Is.Zero);
                    }
                    finally { Object.Destroy(root); }
                }
                yield return null;
            }
        }
    }
}
