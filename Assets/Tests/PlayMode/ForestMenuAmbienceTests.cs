using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DinoRush.Tests
{
    public sealed class ForestMenuAmbienceTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private ForestRunnerGame game;
        private ForestMainMenu menu;
        private ForestMenuAmbience ambience;
        private Transform pool;
        private bool hadMap;
        private string oldMap;

        [UnitySetUp]
        public IEnumerator Load()
        {
            hadMap = PlayerPrefs.HasKey(RunnerMapCatalog.SelectionKey);
            oldMap = PlayerPrefs.GetString(RunnerMapCatalog.SelectionKey);
            yield return SceneManager.LoadSceneAsync("ForestRun");
            game = Object.FindAnyObjectByType<ForestRunnerGame>();
            game.enabled = false;
            menu = game.GetComponentInChildren<ForestMainMenu>(true);
            ambience = game.GetComponent<ForestMenuAmbience>();
            Assert.That(ambience, Is.Not.Null);
            ambience.enabled = false;
            ambience.Simulate(0f);
            pool = game.transform.Find("Forest effects/Title sunbeams and distant birds");
            Assert.That(pool, Is.Not.Null);
            yield return new WaitForSecondsRealtime(0.35f);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            if (game != null) Object.Destroy(game.gameObject);
            yield return null;
            if (hadMap) PlayerPrefs.SetString(RunnerMapCatalog.SelectionKey, oldMap);
            else PlayerPrefs.DeleteKey(RunnerMapCatalog.SelectionKey);
            PlayerPrefs.Save();
        }

        private void Advance(float seconds)
        {
            for (int i = 0; i < Mathf.CeilToInt(seconds * 10); i++) ambience.Simulate(0.1f);
        }

        [UnityTest]
        public IEnumerator SoftLightDriftsAndSmallBirdsFlapAcrossTheDistantSky()
        {
            SpriteRenderer[] renderers = pool.GetComponentsInChildren<SpriteRenderer>(true);
            Assert.That(renderers.Length, Is.EqualTo(7));
            Assert.That(pool.GetComponentsInChildren<Collider2D>(true), Is.Empty);
            Assert.That(pool.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(pool.GetComponentsInChildren<Graphic>(true), Is.Empty);
            var beam = pool.Find("Soft canopy sunbeam 1").GetComponent<SpriteRenderer>();
            Assert.That(beam.sprite.texture.filterMode, Is.EqualTo(FilterMode.Bilinear));
            Vector3 before = beam.transform.position;
            Color color = beam.color;
            Advance(9);
            Assert.That(beam.transform.position, Is.Not.EqualTo(before));
            Assert.That(beam.color, Is.Not.EqualTo(color));
            foreach (SpriteRenderer renderer in renderers)
                Assert.That(renderer.sortingOrder, Is.LessThan(0), "Ambience stays behind the trail, dino, and menu.");
            var bird = pool.Find("Distant bird 1").GetComponent<SpriteRenderer>();
            Assert.That(bird.gameObject.activeInHierarchy, Is.True);
            Assert.That(bird.color.a, Is.GreaterThan(0.1f));
            Assert.That(bird.sprite.texture.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(bird.bounds.size.x, Is.LessThan(0.3f), "Birds should look distant, not compete with the dino.");
            Assert.That(bird.transform.position.y, Is.GreaterThan(Camera.main.transform.position.y + 2));
            Sprite wing = bird.sprite;
            Vector3 position = bird.transform.position;
            bool flapped = false;
            for (int i = 0; i < 5; i++)
            {
                ambience.Simulate(0.1f);
                flapped |= bird.sprite != wing;
            }
            Assert.That(flapped, Is.True);
            Assert.That(bird.transform.position.x, Is.Not.EqualTo(position.x));
            yield return ForestRunTests.Capture("menu-sunbeams-and-birds");
            Assert.That(game.Score, Is.Zero);
            Assert.That(game.EggsCollected, Is.Zero);
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Ready));
        }

        [UnityTest]
        public IEnumerator EffectsHideAndFreezeInSubmenusAndAllNonTitleStates()
        {
            Advance(9);
            var beam = pool.Find("Soft canopy sunbeam 1");
            var bird = pool.Find("Distant bird 1");
            SpriteRenderer[] original = pool.GetComponentsInChildren<SpriteRenderer>(true);
            for (int page = 0; page < 3; page++)
            {
                Vector3 beamPosition = beam.position, birdPosition = bird.position;
                if (page == 0) menu.OpenModeSelector();
                else if (page == 1) menu.OpenMapSelector();
                else menu.OpenDailyMissions();
                ambience.Simulate(0.1f);
                Assert.That(pool.gameObject.activeSelf, Is.False);
                Advance(5);
                Assert.That(beam.position, Is.EqualTo(beamPosition));
                Assert.That(bird.position, Is.EqualTo(birdPosition));
                menu.CloseDialog(); ambience.Simulate(0f);
                Assert.That(pool.gameObject.activeSelf, Is.True);
                Assert.That(bird.position, Is.EqualTo(birdPosition));
            }
            game.SelectMap(game.MapCatalog.Find("snow"));
            game.PrepareRun(); ambience.Simulate(0.1f);
            Assert.That(pool.gameObject.activeSelf, Is.False, "Tap to Start must not inherit the title effects.");
            game.StartRun(); ambience.Simulate(0.1f);
            Assert.That(pool.gameObject.activeSelf, Is.False);
            game.TogglePause(); ambience.Simulate(0.1f);
            Assert.That(pool.gameObject.activeSelf, Is.False);
            game.TogglePause();
            typeof(ForestRunnerGame).GetMethod("EndRun", Private).Invoke(game, null);
            ambience.Simulate(0.1f);
            Assert.That(pool.gameObject.activeSelf, Is.False);
            game.ReturnToMenu(); ambience.Simulate(0f);
            Assert.That(pool.gameObject.activeSelf, Is.True, "The title is forest even when another map is selected.");
            Assert.That(pool.GetComponentsInChildren<SpriteRenderer>(true), Is.EqualTo(original));
            yield return null;
        }

        [UnityTest]
        public IEnumerator FlocksLeaveQuietGapsReuseTheirPoolAndNeverConsumeGameplayRandomness()
        {
            Random.State state = Random.state;
            SpriteRenderer[] original = pool.GetComponentsInChildren<SpriteRenderer>(true);
            var bird = pool.Find("Distant bird 1").GetComponent<SpriteRenderer>();
            float expected = Random.value;
            Random.state = state;
            int departures = 0, quietFrames = 0;
            bool wasFlying = false;
            try
            {
                for (int i = 0; i < 2400; i++)
                {
                    ambience.Simulate(0.1f);
                    bool flying = bird.gameObject.activeSelf;
                    if (wasFlying && !flying) departures++;
                    if (!flying) quietFrames++;
                    wasFlying = flying;
                }
                Assert.That(Random.value, Is.EqualTo(expected));
            }
            finally { Random.state = state; }
            Assert.That(departures, Is.GreaterThanOrEqualTo(4));
            Assert.That(quietFrames, Is.GreaterThan(500), "Flocks need peaceful gaps, not constant bird traffic.");
            Assert.That(pool.childCount, Is.EqualTo(7));
            Assert.That(pool.GetComponentsInChildren<SpriteRenderer>(true), Is.EqualTo(original));
            yield return null;
        }

        [UnityTest]
        public IEnumerator RotationFitsTheCameraAndDisposingTheEffectCleansUpGeneratedArt()
        {
            Advance(9);
            foreach (Vector2Int size in new[] { new Vector2Int(1280, 720), new Vector2Int(360, 800), new Vector2Int(1024, 768) })
            {
                game.ApplyScreenLayout(RunnerScreenLayout.Calculate(size.x, size.y, new Rect(0, 24, size.x, size.y - 48)));
                ambience.Simulate(0f);
                foreach (SpriteRenderer bird in pool.GetComponentsInChildren<SpriteRenderer>())
                {
                    if (!bird.name.StartsWith("Distant bird")) continue;
                    Vector3 point = Camera.main.WorldToViewportPoint(bird.transform.position);
                    Assert.That(point.x, Is.InRange(0f, 1f));
                    Assert.That(point.y, Is.InRange(0.75f, 0.92f));
                }
                Assert.That(pool.childCount, Is.EqualTo(7));
            }
            game.ApplyScreenLayout(RunnerScreenLayout.Calculate(Screen.width, Screen.height, Screen.safeArea));
            ambience.Simulate(0f);
            yield return ForestRunTests.Capture("menu-sunbeams-tablet", 1024, 768);
            yield return ForestRunTests.Capture("menu-sunbeams-portrait", 360, 800);
            SpriteRenderer[] renderers = pool.GetComponentsInChildren<SpriteRenderer>(true);
            Sprite sunbeam = renderers[0].sprite;
            Texture2D sunTexture = sunbeam.texture;
            Sprite birdFrame = pool.Find("Distant bird 1").GetComponent<SpriteRenderer>().sprite;
            Texture2D birdTexture = birdFrame.texture;
            Object.Destroy(ambience);
            yield return null; yield return null;
            Assert.That(pool == null, Is.True);
            Assert.That(sunbeam == null && sunTexture == null, Is.True);
            Assert.That(birdFrame == null && birdTexture == null, Is.True);
        }
    }
}
