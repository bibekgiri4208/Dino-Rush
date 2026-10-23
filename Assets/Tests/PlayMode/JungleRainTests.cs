using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DinoRush.Tests
{
    public sealed class JungleRainTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private ForestRunnerGame game;
        private JungleRain rain;
        private Transform pool;
        private bool hadMap;
        private string previousMap;

        [UnitySetUp]
        public IEnumerator Load()
        {
            hadMap = PlayerPrefs.HasKey(RunnerMapCatalog.SelectionKey);
            previousMap = PlayerPrefs.GetString(RunnerMapCatalog.SelectionKey);
            PlayerPrefs.SetString(RunnerMapCatalog.SelectionKey, "jungle");
            yield return SceneManager.LoadSceneAsync("ForestRun");
            game = Object.FindAnyObjectByType<ForestRunnerGame>();
            game.enabled = false;
            rain = game.GetComponent<JungleRain>();
            Assert.That(rain, Is.Not.Null);
            rain.enabled = false;
            rain.Simulate(0f);
            Assert.That(game.transform.Find("Forest effects/Jungle rain"), Is.Null, "The forest title must not have rain.");
            game.PrepareRun();
            rain.Simulate(0f);
            pool = game.transform.Find("Forest effects/Jungle rain");
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
        public IEnumerator RainFallsWhileWaitingAndLeansBackWithTheAcceleratingCourse()
        {
            SpriteRenderer[] drops = Drops();
            var before = new Vector3[drops.Length];
            for (int i = 0; i < drops.Length; i++) before[i] = drops[i].transform.position;
            rain.Simulate(0.02f);
            int falling = 0;
            for (int i = 0; i < drops.Length; i++)
            {
                Vector3 after = drops[i].transform.position;
                if (after.x >= before[i].x || after.y >= before[i].y) continue; // Ignore incoming-edge recycling.
                Assert.That(before[i].y - after.y, Is.GreaterThan(before[i].x - after.x), "Idle rain should fall, not float like snow.");
                Assert.That(drops[i].transform.localScale.y, Is.GreaterThan(0.15f));
                falling++;
            }
            Assert.That(falling, Is.GreaterThan(70));
            Assert.That(game.Score, Is.Zero);
            Assert.That(game.Lives, Is.EqualTo(5));
            game.StartRun();
            var slowTravel = new float[drops.Length];
            for (int i = 0; i < drops.Length; i++) before[i] = drops[i].transform.position;
            rain.Simulate(0.02f);
            Quaternion slowAngle = drops[0].transform.rotation;
            for (int i = 0; i < drops.Length; i++)
            {
                slowTravel[i] = drops[i].transform.position.y < before[i].y
                    ? before[i].x - drops[i].transform.position.x : -1f;
                before[i] = drops[i].transform.position;
            }
            typeof(ForestRunnerGame).GetField("elapsed", Private).SetValue(game, 105f);
            rain.Simulate(0.02f);
            Assert.That(drops[0].transform.rotation, Is.Not.EqualTo(slowAngle), "Streaks must align with their changing velocity.");
            int compared = 0;
            for (int i = 0; i < drops.Length; i++)
            {
                float fastTravel = before[i].x - drops[i].transform.position.x;
                if (slowTravel[i] <= 0f || fastTravel <= 0f || drops[i].transform.position.y >= before[i].y) continue;
                Assert.That(fastTravel, Is.GreaterThan(slowTravel[i] * 1.5f));
                compared++;
            }
            Assert.That(compared, Is.GreaterThan(65));
            for (int i = 0; i < 1800; i++) rain.Simulate(0.1f);
            Assert.That(Drops(), Is.EqualTo(drops));
            Assert.That(pool.childCount, Is.EqualTo(116));
            Assert.That(pool.GetComponentsInChildren<Collider2D>(true), Is.Empty);
            Assert.That(pool.GetComponentsInChildren<Collider>(true), Is.Empty);
            int splashes = 0;
            foreach (SpriteRenderer renderer in pool.GetComponentsInChildren<SpriteRenderer>(true))
            {
                Assert.That(renderer.sortingOrder, Is.LessThan(15));
                Assert.That(renderer.sprite.texture.filterMode, Is.EqualTo(FilterMode.Point));
                if (!renderer.name.StartsWith("Rain splash") || !renderer.gameObject.activeSelf) continue;
                Assert.That(renderer.transform.position.y, Is.EqualTo(RunnerRules.GroundY + 0.02f).Within(0.001f));
                splashes++;
            }
            Assert.That(splashes, Is.GreaterThan(0), "Rain must produce small pooled ground splashes.");
            yield return new WaitForSecondsRealtime(0.35f);
            yield return ForestRunTests.Capture("jungle-rain");
        }

        [UnityTest]
        public IEnumerator PauseAndResultsFreezeStreaksAndSplashesAndRestartReusesThePool()
        {
            game.StartRun();
            for (int i = 0; i < 20; i++) rain.Simulate(0.02f);
            SpriteRenderer[] items = pool.GetComponentsInChildren<SpriteRenderer>(true);
            var positions = new Vector3[items.Length];
            var rotations = new Quaternion[items.Length];
            var scales = new Vector3[items.Length];
            var colors = new Color[items.Length];
            var active = new bool[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                positions[i] = items[i].transform.position;
                rotations[i] = items[i].transform.rotation;
                scales[i] = items[i].transform.localScale;
                colors[i] = items[i].color;
                active[i] = items[i].gameObject.activeSelf;
            }
            game.TogglePause();
            rain.enabled = true;
            yield return new WaitForSecondsRealtime(0.2f);
            rain.enabled = false;
            rain.Simulate(0.1f);
            for (int i = 0; i < items.Length; i++)
            {
                Assert.That(items[i].transform.position, Is.EqualTo(positions[i]));
                Assert.That(items[i].transform.rotation, Is.EqualTo(rotations[i]));
                Assert.That(items[i].transform.localScale, Is.EqualTo(scales[i]));
                Assert.That(items[i].color, Is.EqualTo(colors[i]));
                Assert.That(items[i].gameObject.activeSelf, Is.EqualTo(active[i]));
            }
            game.TogglePause();
            rain.Simulate(0.02f);
            Assert.That(items[0].transform.position, Is.Not.EqualTo(positions[0]));
            typeof(ForestRunnerGame).GetMethod("EndRun", Private).Invoke(game, null);
            Vector3 stopped = items[0].transform.position;
            rain.Simulate(0.1f);
            Assert.That(items[0].transform.position, Is.EqualTo(stopped));
            game.RestartRun();
            rain.Simulate(0.02f);
            Assert.That(pool.GetComponentsInChildren<SpriteRenderer>(true), Is.EqualTo(items));
            Assert.That(items[0].transform.position, Is.Not.EqualTo(stopped));
        }

        [UnityTest]
        public IEnumerator RainFillsResizedViewsWithoutCreatingExtraRenderers()
        {
            SpriteRenderer[] drops = Drops();
            var camera = (Camera)typeof(JungleRain).GetField("worldCamera", Private).GetValue(rain);
            foreach (Vector2Int size in new[] { new Vector2Int(1280, 800), new Vector2Int(1024, 768), new Vector2Int(360, 800) })
            {
                game.ApplyScreenLayout(RunnerScreenLayout.Calculate(size.x, size.y, new Rect(0, 0, size.x, size.y)));
                for (int i = 0; i < 100; i++) rain.Simulate(0.02f);
                float halfWidth = camera.orthographicSize * camera.aspect + 0.5f;
                int visible = 0;
                foreach (SpriteRenderer drop in drops)
                {
                    Assert.That(drop.transform.position.x, Is.InRange(camera.transform.position.x - halfWidth - 0.001f,
                        camera.transform.position.x + halfWidth + 0.001f));
                    Assert.That(drop.transform.position.y, Is.InRange(RunnerRules.GroundY + 0.019f,
                        camera.transform.position.y + camera.orthographicSize + 0.501f));
                    if (drop.color.a > 0.05f) visible++;
                }
                Assert.That(visible, Is.GreaterThan(60));
                Assert.That(pool.childCount, Is.EqualTo(116));
                yield return RunnerDisplayCapture.Capture("jungle-rain-" + size.x + "x" + size.y, size.x, size.y);
            }
        }

        [UnityTest]
        public IEnumerator RainIsJungleOnlyPreservesGameplayRandomnessAndCleansUpItsArt()
        {
            SpriteRenderer[] items = pool.GetComponentsInChildren<SpriteRenderer>(true);
            for (int map = 0; map < game.MapCatalog.Count; map++)
            {
                game.ReturnToMenu();
                rain.Simulate(0f);
                Assert.That(pool.gameObject.activeSelf, Is.False);
                game.SelectMap(map);
                game.PrepareRun();
                rain.Simulate(0f);
                Assert.That(pool.gameObject.activeSelf, Is.EqualTo(game.MapCatalog[map].id == "jungle"));
                game.StartRun();
                rain.Simulate(0.02f);
                Assert.That(pool.gameObject.activeSelf, Is.EqualTo(game.MapCatalog[map].id == "jungle"));
            }
            game.ReturnToMenu();
            game.SelectMap(game.MapCatalog.Find("jungle"));
            game.StartRun();
            Random.State state = Random.state;
            try
            {
                float expected = Random.value;
                Random.state = state;
                for (int i = 0; i < 1200; i++) rain.Simulate(0.1f);
                Assert.That(Random.value, Is.EqualTo(expected));
            }
            finally { Random.state = state; }
            Assert.That(pool.GetComponentsInChildren<SpriteRenderer>(true), Is.EqualTo(items));
            Sprite streak = items[0].sprite;
            Sprite splash = items[items.Length - 1].sprite;
            Texture2D streakTexture = streak.texture, splashTexture = splash.texture;
            Object.Destroy(rain);
            yield return null;
            yield return null;
            Assert.That(pool == null, Is.True);
            Assert.That(streak == null && splash == null, Is.True);
            Assert.That(streakTexture == null && splashTexture == null, Is.True);
        }

        private SpriteRenderer[] Drops()
        {
            var drops = new SpriteRenderer[96];
            for (int i = 0; i < drops.Length; i++) drops[i] = pool.GetChild(i).GetComponent<SpriteRenderer>();
            return drops;
        }
    }
}
