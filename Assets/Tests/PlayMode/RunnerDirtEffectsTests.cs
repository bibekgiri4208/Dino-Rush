using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DinoRush.Tests
{
    public sealed class RunnerDirtEffectsTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private ForestRunnerGame game;
        private RunnerPlayer player;
        private SpriteRenderer[] particles;
        private bool hadBest, hadMap, hadMissions;
        private int oldBest;
        private string oldMap, oldMissions;

        [UnitySetUp] public IEnumerator Load()
        {
            hadBest = PlayerPrefs.HasKey(RunnerAccountProgress.HighScoreKey); oldBest = PlayerPrefs.GetInt(RunnerAccountProgress.HighScoreKey);
            hadMap = PlayerPrefs.HasKey(RunnerMapCatalog.SelectionKey); oldMap = PlayerPrefs.GetString(RunnerMapCatalog.SelectionKey);
            hadMissions = PlayerPrefs.HasKey(RunnerDailyMissions.SaveKey); oldMissions = PlayerPrefs.GetString(RunnerDailyMissions.SaveKey);
            yield return SceneManager.LoadSceneAsync("ForestRun");
            game = Object.FindAnyObjectByType<ForestRunnerGame>(); game.enabled = false;
            player = (RunnerPlayer)typeof(ForestRunnerGame).GetField("player", Private).GetValue(game);
            particles = game.transform.Find("Forest effects/Trail dust pool").GetComponentsInChildren<SpriteRenderer>(true);
        }

        [UnityTearDown] public IEnumerator Restore()
        {
            if (game != null) Object.Destroy(game.gameObject);
            yield return null;
            if (hadBest) PlayerPrefs.SetInt(RunnerAccountProgress.HighScoreKey, oldBest); else PlayerPrefs.DeleteKey(RunnerAccountProgress.HighScoreKey);
            if (hadMap) PlayerPrefs.SetString(RunnerMapCatalog.SelectionKey, oldMap); else PlayerPrefs.DeleteKey(RunnerMapCatalog.SelectionKey);
            if (hadMissions) PlayerPrefs.SetString(RunnerDailyMissions.SaveKey, oldMissions); else PlayerPrefs.DeleteKey(RunnerDailyMissions.SaveKey);
            PlayerPrefs.Save();
        }

        private void Invoke(string method, params object[] args) => typeof(ForestRunnerGame).GetMethod(method, Private).Invoke(game, args);
        private void Emit(int count, string kind)
        {
            var type = typeof(ForestRunnerGame).GetNestedType("DirtEmission", BindingFlags.NonPublic);
            Invoke("EmitDirt", player.Feet, count, System.Enum.Parse(type, kind));
            player.Render(0, true, false, 1f);
        }
        private void Start()
        {
            game.StartRun();
            typeof(ForestRunnerGame).GetField("spawnTimer", Private).SetValue(game, float.MaxValue);
        }
        private void Step(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Invoke("Step", RunnerRules.SimulationStep);
                Invoke("UpdateParticles", RunnerRules.SimulationStep);
                player.Render(RunnerRules.SimulationStep, true, false, 1f);
            }
        }
        private int Live()
        {
            int count = 0; foreach (var particle in particles) if (particle.gameObject.activeSelf) count++; return count;
        }

        [UnityTest] public IEnumerator LayeredLandingDustSpreadsBothWaysAndDoesNotConsumeGameplayRandomness()
        {
            Start();
            Random.State state = Random.state; float expected = Random.value; Random.state = state;
            Emit(36, "Landing");
            Assert.That(Random.value, Is.EqualTo(expected)); Random.state = state;
            Assert.That(Live(), Is.EqualTo(36));
            var shapes = new HashSet<string>(); bool left = false, right = false;
            foreach (var particle in particles)
            {
                if (!particle.gameObject.activeSelf) continue;
                shapes.Add(particle.sprite.name);
                left |= particle.transform.position.x < player.Feet.x;
                right |= particle.transform.position.x > player.Feet.x;
                Assert.That(particle.sprite.texture.filterMode, Is.EqualTo(FilterMode.Point));
                if (particle.sprite.name.Contains("puff") || particle.sprite.name.Contains("curl") || particle.sprite.name.Contains("haze"))
                    Assert.That(particle.sortingOrder, Is.LessThan(20), "Clouds should not obscure the dino.");
            }
            Assert.That(shapes.Count, Is.EqualTo(6)); Assert.That(left && right, Is.True);
            Invoke("UpdateParticles", 0.1f);
            foreach (var particle in particles)
                if (particle.gameObject.activeSelf) Assert.That(particle.transform.position.y, Is.GreaterThanOrEqualTo(RunnerRules.GroundY + 0.024f));
            yield return new WaitForSecondsRealtime(0.3f);
            yield return RunnerDisplayCapture.Capture("dust-detailed-landing", 1280, 720);
            yield return RunnerDisplayCapture.Capture("dust-detailed-landing-phone", 360, 800);
        }

        [UnityTest] public IEnumerator RunningDustIsDenserAndPoolSaturationStillPrioritizesLandingWithoutAllocating()
        {
            Start(); Step(36);
            Assert.That(Live(), Is.GreaterThanOrEqualTo(15));
            yield return new WaitForSecondsRealtime(0.3f);
            yield return RunnerDisplayCapture.Capture("dust-detailed-running", 1280, 720);
            for (int i = 0; i < 30; i++) Emit(5, "Running");
            Assert.That(Live(), Is.EqualTo(ForestRunnerGame.DirtPoolCapacity));
            Emit(36, "Landing");
            int haze = 0;
            foreach (var particle in particles) if (particle.gameObject.activeSelf && particle.sprite.name == "Dirt ground haze") haze++;
            Assert.That(haze, Is.EqualTo(6));
            Assert.That(particles[0].transform.parent.GetComponentsInChildren<SpriteRenderer>(true).Length, Is.EqualTo(ForestRunnerGame.DirtPoolCapacity));
            game.ReturnToMenu(); Assert.That(Live(), Is.Zero);
        }

        [UnityTest] public IEnumerator FastLandingCreatesAMoreSubstantialBurstAndAirJumpsNeverEmitGroundDust()
        {
            Start(); game.Jump(); Step(1);
            for (int i = 0; i < 240 && !player.Grounded; i++) Step(1);
            Assert.That(player.Grounded, Is.True);
            int normalCount = Live(); float normalSpeed = player.LandingSpeed;
            game.RestartRun(); typeof(ForestRunnerGame).GetField("spawnTimer", Private).SetValue(game, float.MaxValue);
            game.Jump(); Step(42);
            int cursor = (int)typeof(ForestRunnerGame).GetField("nextDirtParticle", Private).GetValue(game);
            game.Jump(); Step(1);
            Assert.That((int)typeof(ForestRunnerGame).GetField("nextDirtParticle", Private).GetValue(game), Is.EqualTo(cursor));
            Step(40); game.SetDuckHeld(true);
            for (int i = 0; i < 240 && !player.Grounded; i++) Step(1);
            Assert.That(player.Grounded, Is.True);
            Assert.That(player.LandingSpeed, Is.GreaterThan(normalSpeed));
            Assert.That(Live(), Is.GreaterThan(normalCount));
            Assert.That(Live(), Is.LessThanOrEqualTo(ForestRunnerGame.DirtPoolCapacity));
            yield return new WaitForSecondsRealtime(0.3f);
            yield return RunnerDisplayCapture.Capture("dust-detailed-fast-landing", 1280, 720);
        }

        [UnityTest] public IEnumerator DetailedDustUsesEveryMapsTintAndKeepsItsPoolAcrossMapChanges()
        {
            for (int map = 0; map < game.MapCatalog.Count; map++)
            {
                game.ReturnToMenu(); game.SelectMap(map); Start(); Emit(36, "Landing");
                Assert.That(Live(), Is.EqualTo(36));
                Assert.That(particles[0].transform.parent.childCount, Is.EqualTo(ForestRunnerGame.DirtPoolCapacity));
                Invoke("UpdateParticles", 0.06f);
                Color tint = game.MapCatalog[map].dustTint;
                var palette = new[] { new Color32(100, 76, 48, 255), new Color32(137, 105, 64, 255),
                    new Color32(173, 142, 91, 255), new Color32(192, 163, 111, 255), new Color32(211, 184, 139, 255) };
                foreach (var particle in particles)
                {
                    if (!particle.gameObject.activeSelf) continue;
                    bool matches = false;
                    foreach (Color baseColor in palette)
                    {
                        Color expected = baseColor * tint;
                        matches |= Mathf.Abs(expected.r - particle.color.r) < 0.001f &&
                            Mathf.Abs(expected.g - particle.color.g) < 0.001f && Mathf.Abs(expected.b - particle.color.b) < 0.001f;
                    }
                    Assert.That(matches, Is.True, "Dust must retain map-specific soil/snow coloring.");
                }
                yield return new WaitForSecondsRealtime(0.3f);
                yield return RunnerDisplayCapture.Capture("dust-map-" + map, 1280, 720);
            }
            game.ReturnToMenu(); Assert.That(Live(), Is.Zero);
        }
    }
}
