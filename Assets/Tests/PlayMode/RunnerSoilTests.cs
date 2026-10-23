using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DinoRush.Tests
{
    public sealed class RunnerSoilTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private ForestRunnerGame game;
        private SpriteRenderer soil;
        private string previousMap;
        private bool hadMap;

        [UnitySetUp]
        public IEnumerator Load()
        {
            hadMap = PlayerPrefs.HasKey(RunnerMapCatalog.SelectionKey);
            previousMap = PlayerPrefs.GetString(RunnerMapCatalog.SelectionKey);
            yield return SceneManager.LoadSceneAsync("ForestRun");
            game = Object.FindAnyObjectByType<ForestRunnerGame>();
            game.enabled = false;
            soil = game.transform.Find("Forest environment/Earth below the trail").GetComponent<SpriteRenderer>();
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
        public IEnumerator AllMapsHaveCrispSoilDownToTheBottomWithoutStretchingOrNewRenderers()
        {
            Sprite[] cached = new Sprite[game.MapCatalog.Count];
            for (int pass = 0; pass < 2; pass++)
            for (int map = 0; map < game.MapCatalog.Count; map++)
            {
                game.SelectMap(map);
                game.PrepareRun();
                Assert.That(soil.sprite.texture.name, Is.EqualTo(game.MapCatalog[map].id + "_soil"));
                if (pass == 0) cached[map] = soil.sprite;
                else Assert.That(soil.sprite, Is.SameAs(cached[map]));
                Assert.That(soil.sprite.pixelsPerUnit, Is.EqualTo(85f));
                Assert.That(soil.sprite.texture.filterMode, Is.EqualTo(FilterMode.Point));
                Assert.That(soil.drawMode, Is.EqualTo(SpriteDrawMode.Tiled));
                Assert.That(soil.transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(soil.bounds.max.y, Is.EqualTo(RunnerSoilArt.Top).Within(0.001f));
                Assert.That(soil.bounds.max.y, Is.GreaterThan(-3.96f), "Overlap the grass strip so no background leaks through.");
                int renderers = game.GetComponentsInChildren<SpriteRenderer>(true).Length;
                var sizes = new[] { new Vector2Int(1280, 720), new Vector2Int(1280, 800), new Vector2Int(1024, 768), new Vector2Int(360, 800) };
                foreach (Vector2Int size in sizes)
                {
                    game.ApplyScreenLayout(RunnerScreenLayout.Calculate(size.x, size.y, new Rect(0, 0, size.x, size.y)));
                    Assert.That(soil.bounds.min.y, Is.LessThan(-Camera.main.orthographicSize - 0.25f));
                    Assert.That(soil.transform.localScale, Is.EqualTo(Vector3.one));
                    for (int i = 0; i < 60; i++)
                    {
                        Invoke("Scroll", 0.7f);
                        Assert.That(soil.bounds.min.x, Is.LessThan(-10.5f));
                        Assert.That(soil.bounds.max.x, Is.GreaterThan(10.5f));
                    }
                }
                Assert.That(game.GetComponentsInChildren<SpriteRenderer>(true).Length, Is.EqualTo(renderers));
                if (pass == 0)
                {
                    yield return new WaitForSecondsRealtime(0.3f);
                    yield return RunnerDisplayCapture.Capture("soil-" + game.MapCatalog[map].id, 1280, 720);
                    if (map == 0)
                    {
                        yield return RunnerDisplayCapture.Capture("soil-forest-16-by-10", 1280, 800);
                        yield return RunnerDisplayCapture.Capture("soil-forest-tablet", 1024, 768);
                    }
                }
                game.ReturnToMenu();
            }
        }

        [UnityTest]
        public IEnumerator SoilTravelsWithTheTrailAndFreezesWhileWaitingPausedOrGameOver()
        {
            game.PrepareRun();
            Vector3 waiting = soil.transform.position;
            game.enabled = true;
            yield return new WaitForSecondsRealtime(0.12f);
            game.enabled = false;
            Assert.That(soil.transform.position, Is.EqualTo(waiting));
            game.PrimaryAction();
            Invoke("Scroll", 0.4f);
            Assert.That(soil.transform.position.x, Is.EqualTo(waiting.x - 0.4f).Within(0.001f));
            float period = soil.sprite.bounds.size.x;
            Invoke("Scroll", period);
            Assert.That(soil.transform.position.x, Is.EqualTo(waiting.x - 0.4f).Within(0.001f), "Whole tiles must wrap without a visual jump.");
            game.TogglePause();
            Vector3 paused = soil.transform.position;
            game.enabled = true;
            yield return new WaitForSecondsRealtime(0.12f);
            game.enabled = false;
            Assert.That(soil.transform.position, Is.EqualTo(paused));
            game.TogglePause();
            Invoke("EndRun");
            game.enabled = true;
            yield return new WaitForSecondsRealtime(0.12f);
            game.enabled = false;
            Assert.That(soil.transform.position, Is.EqualTo(paused));
            game.ReturnToMenu();
            Assert.That(soil.sprite.texture.name, Is.EqualTo("forest_soil"));
        }

        private void Invoke(string method, params object[] args) => typeof(ForestRunnerGame).GetMethod(method, Private).Invoke(game, args);
    }
}
