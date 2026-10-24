using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DinoRush.Tests
{
    public sealed class ForestLeafWindTests
    {
        private ForestRunnerGame game;
        private ForestLeafWind wind;
        private Transform pool;
        private bool hadMap;
        private string previousMap;

        [UnitySetUp]
        public IEnumerator Load()
        {
            hadMap = PlayerPrefs.HasKey(RunnerMapCatalog.SelectionKey);
            previousMap = PlayerPrefs.GetString(RunnerMapCatalog.SelectionKey);
            PlayerPrefs.SetString(RunnerMapCatalog.SelectionKey, "forest");
            yield return SceneManager.LoadSceneAsync("ForestRun");
            game = Object.FindAnyObjectByType<ForestRunnerGame>();
            game.enabled = false;
            wind = game.GetComponent<ForestLeafWind>();
            Assert.That(wind, Is.Not.Null);
            wind.enabled = false;
            wind.Simulate(0f);
            pool = game.transform.Find("Forest effects/Windblown leaves");
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
        public IEnumerator SuppliedLeavesDriftFlutterAndRecycleWithoutCollisionsOrExtraObjects()
        {
            SpriteRenderer[] leaves = pool.GetComponentsInChildren<SpriteRenderer>(true);
            Assert.That(leaves.Length, Is.EqualTo(24));
            Assert.That(pool.GetComponentsInChildren<Collider2D>(true), Is.Empty);
            Assert.That(pool.GetComponentsInChildren<Collider>(true), Is.Empty);
            foreach (SpriteRenderer leaf in leaves)
            {
                Assert.That(leaf.sprite.texture.name, Is.EqualTo("leaf"));
                Assert.That(leaf.sprite.pivot, Is.EqualTo(new Vector2(8, 12.5f)));
                Assert.That(leaf.sortingOrder, Is.LessThan(15), "Leaves must not obscure the player or obstacles.");
            }
            game.StartRun();
            wind.Simulate(0f);
            Vector3 position = leaves[0].transform.position;
            Quaternion rotation = leaves[0].transform.rotation;
            Vector3 scale = leaves[0].transform.localScale;
            for (int i = 0; i < 5; i++) wind.Simulate(0.1f);
            Assert.That(leaves[0].transform.position.x, Is.LessThan(position.x));
            Assert.That(leaves[0].transform.rotation, Is.Not.EqualTo(rotation));
            Assert.That(leaves[0].transform.localScale, Is.Not.EqualTo(scale));
            Assert.That(game.Score, Is.Zero);
            Assert.That(game.Lives, Is.EqualTo(RunnerHealth.MaximumLives));
            yield return new WaitForSecondsRealtime(0.35f);
            yield return ForestRunTests.Capture("forest-wind-leaves");
            for (int i = 0; i < 1800; i++) wind.Simulate(0.1f);
            Assert.That(pool.childCount, Is.EqualTo(24));
            Assert.That(pool.GetComponentsInChildren<SpriteRenderer>(true), Is.EqualTo(leaves));
            int visible = 0;
            foreach (SpriteRenderer leaf in leaves)
            {
                Assert.That(leaf.transform.position.y, Is.GreaterThanOrEqualTo(RunnerRules.GroundY + 0.28f - 0.001f));
                Assert.That(leaf.transform.position.x, Is.InRange(-10.61f, 10.61f));
                if (leaf.color.a > 0.05f) visible++;
            }
            Assert.That(visible, Is.GreaterThan(8), "The breeze must continue after many offscreen wraps.");
            yield return ForestRunTests.Capture("forest-wind-leaves-recycled");
            yield return ForestRunTests.Capture("forest-wind-leaves-16-by-10", 1280, 800);
        }

        [UnityTest]
        public IEnumerator LeavesSweepBehindTheRunnerAndAccelerateWithTheCourseInsteadOfHovering()
        {
            game.StartRun();
            wind.Simulate(0f);
            SpriteRenderer[] leaves = pool.GetComponentsInChildren<SpriteRenderer>(true);
            var previous = new float[leaves.Length];
            var swept = new float[leaves.Length];
            for (int i = 0; i < leaves.Length; i++) previous[i] = leaves[i].transform.position.x;
            int crossings = 0;
            for (int frame = 0; frame < 180; frame++)
            {
                wind.Simulate(0.1f);
                for (int i = 0; i < leaves.Length; i++)
                {
                    float x = leaves[i].transform.position.x;
                    if (x < previous[i])
                    {
                        swept[i] += previous[i] - x;
                        if (previous[i] >= RunnerRules.PlayerX && x < RunnerRules.PlayerX) crossings++;
                    }
                    previous[i] = x;
                }
            }
            foreach (float distance in swept)
                Assert.That(distance, Is.GreaterThan(game.CurrentSpeed * 0.48f * 18f), "Even distant leaves must visibly sweep backward.");
            Assert.That(crossings, Is.GreaterThan(60), "Leaves should repeatedly pass the dino and exit behind it, not loiter nearby.");

            var slowTravel = new float[leaves.Length];
            var before = new float[leaves.Length];
            for (int i = 0; i < leaves.Length; i++) before[i] = leaves[i].transform.position.x;
            wind.Simulate(0.1f);
            for (int i = 0; i < leaves.Length; i++)
            {
                slowTravel[i] = before[i] - leaves[i].transform.position.x;
                before[i] = leaves[i].transform.position.x;
            }
            typeof(ForestRunnerGame).GetField("elapsed", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(game, 105f);
            wind.Simulate(0.1f);
            int compared = 0;
            for (int i = 0; i < leaves.Length; i++)
            {
                float fastTravel = before[i] - leaves[i].transform.position.x;
                if (slowTravel[i] <= 0f || fastTravel <= 0f) continue; // Ignore an offscreen recycle.
                Assert.That(fastTravel, Is.GreaterThan(slowTravel[i] * 1.5f), "Leaf travel must track the accelerating world.");
                compared++;
            }
            Assert.That(compared, Is.GreaterThan(15));
            yield return new WaitForSecondsRealtime(0.35f);
            for (int frame = 0; frame < 4; frame++)
            {
                yield return ForestRunTests.Capture("forest-wind-sweep-" + frame);
                for (int i = 0; i < 3; i++) wind.Simulate(0.1f);
            }
        }

        [UnityTest]
        public IEnumerator PauseAndGameOverFreezeAllMotionAndRestartReusesThePool()
        {
            game.StartRun();
            wind.Simulate(0.1f);
            SpriteRenderer[] leaves = pool.GetComponentsInChildren<SpriteRenderer>(true);
            game.TogglePause();
            var positions = new Vector3[leaves.Length];
            var rotations = new Quaternion[leaves.Length];
            var scales = new Vector3[leaves.Length];
            var colors = new Color[leaves.Length];
            for (int i = 0; i < leaves.Length; i++)
            {
                positions[i] = leaves[i].transform.position;
                rotations[i] = leaves[i].transform.rotation;
                scales[i] = leaves[i].transform.localScale;
                colors[i] = leaves[i].color;
            }
            wind.enabled = true;
            yield return new WaitForSecondsRealtime(0.2f);
            for (int i = 0; i < leaves.Length; i++)
            {
                Assert.That(leaves[i].transform.position, Is.EqualTo(positions[i]));
                Assert.That(leaves[i].transform.rotation, Is.EqualTo(rotations[i]));
                Assert.That(leaves[i].transform.localScale, Is.EqualTo(scales[i]));
                Assert.That(leaves[i].color, Is.EqualTo(colors[i]));
            }
            wind.enabled = false;
            game.TogglePause();
            wind.Simulate(0.1f);
            Assert.That(leaves[0].transform.position, Is.Not.EqualTo(positions[0]));
            typeof(ForestRunnerGame).GetMethod("EndRun", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(game, null);
            Vector3 stopped = leaves[0].transform.position;
            wind.Simulate(0.1f);
            Assert.That(leaves[0].transform.position, Is.EqualTo(stopped));
            game.RestartRun();
            wind.Simulate(0.1f);
            Assert.That(pool.GetComponentsInChildren<SpriteRenderer>(true), Is.EqualTo(leaves));
            Assert.That(leaves[0].transform.position, Is.Not.EqualTo(stopped));
        }

        [UnityTest]
        public IEnumerator OnlyTheForestAndItsTitlePreviewHaveLeavesAndTheCourseRandomStreamIsUntouched()
        {
            SpriteRenderer[] leaves = pool.GetComponentsInChildren<SpriteRenderer>(true);
            for (int map = 1; map < game.MapCatalog.Count; map++)
            {
                game.SelectMap(map);
                game.StartRun();
                wind.Simulate(0f);
                Assert.That(pool.gameObject.activeSelf, Is.False, game.SelectedMapName);
                game.RestartRun();
                wind.Simulate(0.1f);
                Assert.That(pool.gameObject.activeSelf, Is.False);
                game.ReturnToMenu();
                wind.Simulate(0.1f);
                Assert.That(pool.gameObject.activeSelf, Is.True, "The title always shows the forest, regardless of the saved map.");
                Assert.That(pool.GetComponentsInChildren<SpriteRenderer>(true), Is.EqualTo(leaves));
            }
            Random.State state = Random.state;
            try
            {
                float expected = Random.value;
                Random.state = state;
                for (int i = 0; i < 1200; i++) wind.Simulate(0.1f);
                Assert.That(Random.value, Is.EqualTo(expected), "Cosmetic recycling must not change future course seeds.");
            }
            finally { Random.state = state; }
            yield return ForestRunTests.Capture("forest-wind-title");
            Sprite generated = leaves[0].sprite;
            Object.Destroy(wind);
            yield return null;
            yield return null;
            Assert.That(generated == null, Is.True, "The runtime sprite must be cleaned up without deleting the source texture.");
            Assert.That(pool == null, Is.True);
        }
    }
}
