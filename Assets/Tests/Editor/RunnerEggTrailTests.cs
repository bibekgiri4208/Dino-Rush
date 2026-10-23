using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DinoRush.Tests
{
    public sealed class RunnerEggTrailTests
    {
        [TestCase(0f)]
        [TestCase(40f)]
        [TestCase(120f)]
        public void TrailsStayClearOfObstaclesAndFitReachableJumpRoutes(float elapsed)
        {
            var encounters = new RunnerEncounterDirector(147, new[] { 1f, 1.5f, 2f });
            float speed = RunnerRules.Speed(elapsed + 6f);
            int highRoutes = 0;
            for (int n = 0; n < 180; n++)
            {
                RunnerEncounter encounter = encounters.Next(elapsed);
                foreach (RunnerEggTrailKind kind in System.Enum.GetValues(typeof(RunnerEggTrailKind)))
                {
                    RunnerEggTrail trail = RunnerEggTrailDirector.Create(encounter, speed, kind);
                    Assert.That(trail.Eggs.Length, Is.InRange(1, 8));
                    foreach (Vector2 egg in trail.Eggs)
                    {
                        Rect pickup = RunnerEggTrailDirector.PickupBox(egg + Vector2.up * RunnerRules.GroundY);
                        foreach (RunnerObstacleSpawn obstacle in encounter.Obstacles)
                            Assert.That(pickup.Overlaps(obstacle.Hitbox(obstacle.Offset)),
                                Is.False, $"{trail.Kind} egg clips a {encounter.Pattern} obstacle.");
                        Assert.That(egg.y, Is.InRange(0.5f, 5.5f));
                        Assert.That(egg.x, Is.LessThan(encounter.Spacing - 1f));
                    }
                    if (trail.Kind == RunnerEggTrailKind.Low) continue;
                    bool high = trail.Kind == RunnerEggTrailKind.HighArc;
                    if (high)
                    {
                        highRoutes++;
                        float singleReach = RunnerRules.JumpSpeed * RunnerRules.JumpSpeed / (2f * RunnerRules.Gravity)
                            + RunnerRules.PlayerHeight + 0.1f + RunnerEggTrailDirector.PickupRadius;
                        Assert.That(System.Array.Exists(trail.Eggs, egg => egg.y > singleReach), Is.True,
                            "High routes should reward the second jump, not just an ordinary jump.");
                    }
                    float flight = high ? RunnerEggTrailDirector.DoubleFlightTime : RunnerEggTrailDirector.ApexTime * 2f;
                    for (float t = 0f; t < flight; t += RunnerRules.SimulationStep)
                    {
                        Rect player = RunnerRules.PlayerHitbox(RunnerEggTrailDirector.FlightHeight(t, high));
                        foreach (RunnerObstacleSpawn obstacle in encounter.Obstacles)
                        {
                            float x = RunnerRules.PlayerX + obstacle.Offset - trail.TakeoffOffset - speed * t;
                            Assert.That(player.Overlaps(obstacle.Hitbox(x)),
                                Is.False, $"{trail.Kind} advertises an unsafe {encounter.Pattern} route at {t}.");
                        }
                    }
                }
            }
            Assert.That(highRoutes, Is.GreaterThan(0));
        }

        [Test]
        public void TrailsAreSeededAndNeverConsumeGameplayRandomness()
        {
            Random.State saved = Random.state;
            try
            {
                var encounters = new RunnerEncounterDirector(42, new[] { 1f });
                var a = new RunnerEggTrailDirector(9127);
                var b = new RunnerEggTrailDirector(9127);
                var kinds = new HashSet<RunnerEggTrailKind>();
                Random.InitState(26);
                float expected = Random.value;
                Random.InitState(26);
                for (int i = 0; i < 100; i++)
                {
                    RunnerEncounter encounter = encounters.Next(0f);
                    RunnerEggTrail first = a.Next(encounter, 6.8f);
                    RunnerEggTrail second = b.Next(encounter, 6.8f);
                    Assert.That(first.Kind, Is.EqualTo(second.Kind));
                    Assert.That(first.Eggs, Is.EqualTo(second.Eggs));
                    if (i < 2) Assert.That(first.Kind, Is.EqualTo(RunnerEggTrailKind.Low));
                    kinds.Add(first.Kind);
                }
                Assert.That(kinds.Count, Is.EqualTo(3));
                Assert.That(Random.value, Is.EqualTo(expected));
            }
            finally { Random.state = saved; }
        }

        [Test]
        public void EggArtIsPointFilteredTransparentAndOwnedByTheCaller()
        {
            var assets = new List<Object>();
            try
            {
                Sprite egg = RunnerEggArt.CreateEgg(assets);
                Sprite sparkle = RunnerEggArt.CreateSparkle(assets);
                Assert.That(assets.Count, Is.EqualTo(4));
                Assert.That(egg.texture.filterMode, Is.EqualTo(FilterMode.Point));
                Assert.That(egg.texture.GetPixel(0, 0).a, Is.Zero);
                Assert.That(egg.texture.GetPixel(8, 10).a, Is.EqualTo(1f));
                Assert.That(sparkle.texture.GetPixel(3, 3).a, Is.EqualTo(1f));
                Assert.That(sparkle.texture.GetPixel(0, 0).a, Is.Zero);
            }
            finally { foreach (Object asset in assets) Object.DestroyImmediate(asset); }
        }

        [Test]
        public void PickupHaloHasAClearCenterAndAPointFilteredRing()
        {
            var assets = new List<Object>();
            try
            {
                Sprite halo = RunnerEggArt.CreatePickupHalo(assets);
                Assert.That(assets.Count, Is.EqualTo(2));
                Assert.That(halo.texture.filterMode, Is.EqualTo(FilterMode.Point));
                Assert.That(halo.texture.GetPixel(16, 16).a, Is.Zero);
                Assert.That(halo.texture.GetPixel(0, 0).a, Is.Zero);
                Assert.That(halo.texture.GetPixel(29, 16).a, Is.EqualTo(1f));
                Assert.That(halo.texture.GetPixel(28, 16).a, Is.InRange(0.2f, 0.3f));
            }
            finally { foreach (Object asset in assets) Object.DestroyImmediate(asset); }
        }
    }
}
