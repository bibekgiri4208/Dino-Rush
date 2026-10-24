using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DinoRush.Tests
{
    public sealed class SnowMountainSnowfallTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private ForestRunnerGame game;
        private SnowMountainSnowfall snow;
        private Transform pool;
        private bool hadMap;
        private string previousMap;

        [UnitySetUp]
        public IEnumerator Load()
        {
            hadMap = PlayerPrefs.HasKey(RunnerMapCatalog.SelectionKey);
            previousMap = PlayerPrefs.GetString(RunnerMapCatalog.SelectionKey);
            PlayerPrefs.SetString(RunnerMapCatalog.SelectionKey, "snow");
            yield return SceneManager.LoadSceneAsync("ForestRun");
            game = Object.FindAnyObjectByType<ForestRunnerGame>();
            game.enabled = false;
            snow = game.GetComponent<SnowMountainSnowfall>();
            Assert.That(snow, Is.Not.Null);
            snow.enabled = false;
            snow.Simulate(0f);
            Assert.That(game.transform.Find("Forest effects/Windblown snow"), Is.Null, "The title always displays the forest.");
            game.PrepareRun();
            snow.Simulate(0f);
            pool = game.transform.Find("Forest effects/Windblown snow");
            Assert.That(pool, Is.Not.Null);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            if (game != null) Object.Destroy(game.gameObject);
            yield return null;
            if (hadMap) PlayerPrefs.SetString(RunnerMapCatalog.SelectionKey, previousMap);
            else PlayerPrefs.DeleteKey(RunnerMapCatalog.SelectionKey);
            PlayerPrefs.Save();
        }

        [UnityTest]
        public IEnumerator SnowFallsWhileWaitingThenSweepsFasterWithTheCourseAndRecycles()
        {
            SpriteRenderer[] flakes = pool.GetComponentsInChildren<SpriteRenderer>(true);
            Assert.That(flakes.Length, Is.EqualTo(72));
            Assert.That(pool.GetComponentsInChildren<Collider2D>(true), Is.Empty);
            Assert.That(pool.GetComponentsInChildren<Collider>(true), Is.Empty);
            foreach (SpriteRenderer flake in flakes)
            {
                Assert.That(flake.sortingOrder, Is.LessThan(15));
                Assert.That(flake.sprite.texture.filterMode, Is.EqualTo(FilterMode.Point));
            }
            Vector3 before = flakes[0].transform.position;
            snow.Simulate(0.1f);
            Assert.That(flakes[0].transform.position.x, Is.LessThan(before.x));
            Assert.That(flakes[0].transform.position.y, Is.LessThan(before.y));
            Assert.That(game.Score, Is.Zero);
            game.StartRun();
            var positions = new Vector3[flakes.Length];
            var slowTravel = new float[flakes.Length];
            for (int i = 0; i < flakes.Length; i++) positions[i] = flakes[i].transform.position;
            snow.Simulate(0.1f);
            for (int i = 0; i < flakes.Length; i++)
            {
                slowTravel[i] = positions[i].x - flakes[i].transform.position.x;
                positions[i] = flakes[i].transform.position;
            }
            typeof(ForestRunnerGame).GetField("elapsed", Private).SetValue(game, 105f);
            snow.Simulate(0.1f);
            int compared = 0;
            for (int i = 0; i < flakes.Length; i++)
            {
                float fastTravel = positions[i].x - flakes[i].transform.position.x;
                if (slowTravel[i] <= 0f || fastTravel <= 0f) continue;
                Assert.That(fastTravel, Is.GreaterThan(slowTravel[i] * 1.5f));
                compared++;
            }
            Assert.That(compared, Is.GreaterThan(40));
            for (int frame = 0; frame < 1800; frame++) snow.Simulate(0.1f);
            Assert.That(pool.GetComponentsInChildren<SpriteRenderer>(true), Is.EqualTo(flakes));
            int visible = 0;
            var camera = (Camera)typeof(SnowMountainSnowfall).GetField("worldCamera", Private).GetValue(snow);
            float right = camera.orthographicSize * camera.aspect + 0.4f;
            foreach (SpriteRenderer flake in flakes)
            {
                Assert.That(flake.transform.position.x, Is.InRange(-right - 0.001f, right + 0.001f));
                Assert.That(flake.transform.position.y, Is.InRange(RunnerRules.GroundY + 0.039f,
                    camera.transform.position.y + camera.orthographicSize + 0.401f));
                if (flake.color.a > 0.05f) visible++;
            }
            Assert.That(visible, Is.GreaterThan(30));
            Assert.That(game.Lives, Is.EqualTo(RunnerHealth.MaximumLives));
            yield return new WaitForSecondsRealtime(0.35f);
            yield return ForestRunTests.Capture("mountain-snowfall");
            yield return ForestRunTests.Capture("mountain-snowfall-16-by-10", 1280, 800);
        }

        [UnityTest]
        public IEnumerator PauseAndResultsFreezeSnowAndRestartReusesIt()
        {
            game.StartRun();
            snow.Simulate(0.1f);
            SpriteRenderer[] flakes = pool.GetComponentsInChildren<SpriteRenderer>(true);
            var positions = new Vector3[flakes.Length];
            var rotations = new Quaternion[flakes.Length];
            var colors = new Color[flakes.Length];
            for (int i = 0; i < flakes.Length; i++)
            {
                positions[i] = flakes[i].transform.position;
                rotations[i] = flakes[i].transform.rotation;
                colors[i] = flakes[i].color;
            }
            game.TogglePause();
            snow.enabled = true;
            yield return new WaitForSecondsRealtime(0.2f);
            snow.enabled = false;
            snow.Simulate(0.1f);
            for (int i = 0; i < flakes.Length; i++)
            {
                Assert.That(flakes[i].transform.position, Is.EqualTo(positions[i]));
                Assert.That(flakes[i].transform.rotation, Is.EqualTo(rotations[i]));
                Assert.That(flakes[i].color, Is.EqualTo(colors[i]));
            }
            game.TogglePause();
            snow.Simulate(0.1f);
            Assert.That(flakes[0].transform.position, Is.Not.EqualTo(positions[0]));
            typeof(ForestRunnerGame).GetMethod("EndRun", Private).Invoke(game, null);
            Vector3 stopped = flakes[0].transform.position;
            snow.Simulate(0.1f);
            Assert.That(flakes[0].transform.position, Is.EqualTo(stopped));
            game.RestartRun();
            snow.Simulate(0.1f);
            Assert.That(pool.GetComponentsInChildren<SpriteRenderer>(true), Is.EqualTo(flakes));
            Assert.That(flakes[0].transform.position, Is.Not.EqualTo(stopped));
        }

        [UnityTest]
        public IEnumerator OnlySnowMountainHasSnowAndCosmeticsPreserveRandomnessAndCleanUp()
        {
            SpriteRenderer[] flakes = pool.GetComponentsInChildren<SpriteRenderer>(true);
            for (int map = 0; map < game.MapCatalog.Count; map++)
            {
                game.ReturnToMenu();
                snow.Simulate(0f);
                Assert.That(pool.gameObject.activeSelf, Is.False);
                game.SelectMap(map);
                game.StartRun();
                snow.Simulate(0.1f);
                Assert.That(pool.gameObject.activeSelf, Is.EqualTo(game.MapCatalog[map].id == "snow"));
            }
            game.ReturnToMenu();
            game.SelectMap(game.MapCatalog.Find("snow"));
            game.StartRun();
            Random.State state = Random.state;
            try
            {
                float expected = Random.value;
                Random.state = state;
                for (int i = 0; i < 1200; i++) snow.Simulate(0.1f);
                Assert.That(Random.value, Is.EqualTo(expected));
            }
            finally { Random.state = state; }
            Assert.That(pool.GetComponentsInChildren<SpriteRenderer>(true), Is.EqualTo(flakes));
            Sprite generated = flakes[0].sprite;
            Texture2D texture = generated.texture;
            Object.Destroy(snow);
            yield return null;
            yield return null;
            Assert.That(pool == null, Is.True);
            Assert.That(generated == null, Is.True);
            Assert.That(texture == null, Is.True);
        }
    }
}
