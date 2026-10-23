using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DinoRush.Tests
{
    public sealed class RunnerEggTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private ForestRunnerGame game;
        private RunnerPlayer player;
        private RunnerEggCollectibles eggs;
        private Transform ui, pool;
        private bool hadBest, hadMap, hadDino;
        private int oldBest;
        private string oldMap, oldDino;

        [UnitySetUp]
        public IEnumerator Load()
        {
            hadBest = PlayerPrefs.HasKey("DinoRush.Forest.HighScore");
            oldBest = PlayerPrefs.GetInt("DinoRush.Forest.HighScore");
            hadMap = PlayerPrefs.HasKey(RunnerMapCatalog.SelectionKey);
            oldMap = PlayerPrefs.GetString(RunnerMapCatalog.SelectionKey);
            hadDino = PlayerPrefs.HasKey(RunnerDinoRoster.SelectionKey);
            oldDino = PlayerPrefs.GetString(RunnerDinoRoster.SelectionKey);
            yield return SceneManager.LoadSceneAsync("ForestRun");
            game = Object.FindAnyObjectByType<ForestRunnerGame>();
            game.enabled = false;
            player = (RunnerPlayer)Get("player");
            eggs = (RunnerEggCollectibles)Get("eggs");
            ui = game.transform.Find("Forest runner UI/16 by 9 safe content");
            pool = game.transform.Find("Collectible eggs");
            foreach (BaseInputModule input in EventSystem.current.GetComponents<BaseInputModule>()) input.enabled = false;
            yield return new WaitForSecondsRealtime(0.35f);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            if (game != null) Object.Destroy(game.gameObject);
            yield return null;
            if (hadBest) PlayerPrefs.SetInt("DinoRush.Forest.HighScore", oldBest);
            else PlayerPrefs.DeleteKey("DinoRush.Forest.HighScore");
            if (hadMap) PlayerPrefs.SetString(RunnerMapCatalog.SelectionKey, oldMap);
            else PlayerPrefs.DeleteKey(RunnerMapCatalog.SelectionKey);
            if (hadDino) PlayerPrefs.SetString(RunnerDinoRoster.SelectionKey, oldDino);
            else PlayerPrefs.DeleteKey(RunnerDinoRoster.SelectionKey);
            PlayerPrefs.Save();
        }

        [UnityTest]
        public IEnumerator IntroPickupsCountOnceFreezeWithPauseAndAppearInResultsWithoutChangingDistanceOrHealth()
        {
            Assert.That(eggs.ActiveCount, Is.Zero);
            Assert.That(ui.Find("Egg counter").gameObject.activeInHierarchy, Is.False);
            game.PrepareRun();
            Assert.That(eggs.ActiveCount, Is.Zero, "Waiting must not spawn or collect eggs.");
            yield return null;
            game.PrimaryAction();
            Set("spawnTimer", float.MaxValue);
            Assert.That(eggs.ActiveCount, Is.EqualTo(4));
            Step(195);
            Assert.That(game.EggsCollected, Is.EqualTo(4));
            Assert.That(eggs.ActiveCount, Is.Zero);
            Assert.That(game.Score, Is.EqualTo(Mathf.FloorToInt((float)Get("distance"))));
            Assert.That(game.Lives, Is.EqualTo(RunnerHealth.MaximumLives));
            Assert.That(ui.Find("Egg counter/Egg count").GetComponent<Text>().text, Is.EqualTo("004"));
            Assert.That(pool.GetComponentsInChildren<SpriteRenderer>().Length, Is.GreaterThan(0), "Pickups leave a sparkle burst.");
            foreach (Graphic graphic in ui.Find("Egg counter").GetComponentsInChildren<Graphic>())
                Assert.That(graphic.raycastTarget, Is.False);
            yield return RunnerDisplayCapture.Capture("eggs-collected", 1280, 720);

            game.TogglePause();
            Vector3 position = pool.Find("Pickup sparkle 18").position;
            int score = game.Score;
            Step(120);
            Assert.That(pool.Find("Pickup sparkle 18").position, Is.EqualTo(position));
            Assert.That(game.EggsCollected, Is.EqualTo(4));
            Assert.That(game.Score, Is.EqualTo(score));
            Assert.That(ui.Find("Egg counter").gameObject.activeInHierarchy, Is.False);
            game.TogglePause();
            Assert.That(ui.Find("Egg counter").gameObject.activeInHierarchy, Is.True);
            Invoke("EndRun");
            Assert.That(ui.Find("Menu card/Collected eggs").GetComponent<Text>().text, Is.EqualTo("EGGS COLLECTED: 004"));
            Step(120);
            Assert.That(game.EggsCollected, Is.EqualTo(4));
            yield return RunnerDisplayCapture.Capture("eggs-results", 1280, 720);
            game.RestartRun();
            Assert.That(game.EggsCollected, Is.Zero);
            Assert.That(eggs.ActiveCount, Is.EqualTo(4));
            game.ReturnToMenu();
            Assert.That(eggs.ActiveCount, Is.Zero);
            Assert.That(game.EggsCollected, Is.Zero);
            Assert.That(pool.GetComponentsInChildren<SpriteRenderer>(), Is.Empty);
        }

        [UnityTest]
        public IEnumerator AllThreeDesignedRoutesCanBeCollectedUsingTheRealJumpSimulation()
        {
            game.StartRun();
            RunnerEncounter encounter = new RunnerEncounterDirector(42, new[] { 1f }).Next(0f);
            const float speed = 6.8f;
            foreach (RunnerEggTrailKind kind in System.Enum.GetValues(typeof(RunnerEggTrailKind)))
            {
                eggs.Reset(42);
                player.Reset();
                RunnerEggTrail trail = RunnerEggTrailDirector.Create(encounter, speed, kind);
                Assert.That(trail.Kind, Is.EqualTo(kind));
                eggs.SpawnTrail(trail, RunnerRules.PlayerX - trail.TakeoffOffset);
                yield return RunnerDisplayCapture.Capture("eggs-" + kind, 1280, 720);
                if (kind == RunnerEggTrailKind.HighArc)
                    yield return RunnerDisplayCapture.Capture("eggs-high-portrait", 360, 800);
                if (kind != RunnerEggTrailKind.Low) player.QueueJump();
                bool second = false;
                for (int frame = 0; frame < 360; frame++)
                {
                    float t = frame * RunnerRules.SimulationStep;
                    if (trail.SecondJumpTime >= 0f && !second && t >= trail.SecondJumpTime)
                    {
                        player.QueueJump();
                        second = true;
                    }
                    player.Step(RunnerRules.SimulationStep, out _, out _);
                    eggs.Step(RunnerRules.SimulationStep, speed * RunnerRules.SimulationStep, player.Hitbox);
                }
                Assert.That(eggs.Collected, Is.EqualTo(trail.Eggs.Length), kind.ToString());
                Assert.That(eggs.ActiveCount, Is.Zero);
                Assert.That(player.Grounded, Is.True);
            }
        }

        [UnityTest]
        public IEnumerator PickupsWorkOnEveryMapAndReuseTheirPoolsWithoutAdvancingGameplayRandomness()
        {
            for (int map = 0; map < game.MapCatalog.Count; map++)
            {
                game.SelectMap(map);
                game.SelectDino(map % game.DinoRoster.Count);
                game.StartRun();
                Set("spawnTimer", float.MaxValue);
                Step(195);
                Assert.That(game.EggsCollected, Is.EqualTo(4), game.SelectedMapName);
                Assert.That(game.Lives, Is.EqualTo(5));
                game.ReturnToMenu();
                yield return null;
            }
            int count = pool.GetComponentsInChildren<SpriteRenderer>(true).Length;
            Assert.That(count, Is.EqualTo(RunnerEggCollectibles.PoolSize + RunnerEggCollectibles.SparklePoolSize));
            RunnerEncounter sample = new RunnerEncounterDirector(42, new[] { 1f }).Next(0f);
            RunnerEggTrail low = RunnerEggTrailDirector.Create(sample, 6.8f, RunnerEggTrailKind.Low);
            eggs.Reset(42);
            while (eggs.ActiveCount < RunnerEggCollectibles.PoolSize)
                Assert.That(eggs.SpawnTrail(low, 11.7f), Is.True);
            Assert.That(eggs.SpawnTrail(low, 11.7f), Is.False, "A full pool must skip the whole trail.");
            eggs.Step(RunnerRules.SimulationStep, 0f, new Rect(-100, -100, 200, 200));
            Assert.That(eggs.Collected, Is.EqualTo(RunnerEggCollectibles.PoolSize));
            eggs.Step(RunnerRules.SimulationStep, 0f, new Rect(-100, -100, 200, 200));
            Assert.That(eggs.Collected, Is.EqualTo(RunnerEggCollectibles.PoolSize), "The same eggs cannot be awarded twice.");
            Random.State saved = Random.state;
            try
            {
                Random.InitState(731);
                float expected = Random.value;
                Random.InitState(731);
                eggs.Reset(731);
                var director = new RunnerEncounterDirector(17, new[] { 1f });
                for (int i = 0; i < 300; i++)
                {
                    eggs.SpawnEncounter(director.Next(i), 11.7f, RunnerRules.Speed(i));
                    eggs.Step(RunnerRules.SimulationStep, 50f, new Rect(-1000, -1000, 1, 1));
                }
                eggs.Step(RunnerRules.SimulationStep, 200f, new Rect(-1000, -1000, 1, 1));
                Assert.That(eggs.ActiveCount, Is.Zero);
                Assert.That(eggs.Collected, Is.Zero, "Offscreen eggs expire; they are not awarded.");
                Assert.That(Random.value, Is.EqualTo(expected));
                Assert.That(pool.GetComponentsInChildren<SpriteRenderer>(true).Length, Is.EqualTo(count));
            }
            finally { Random.state = saved; }
            Sprite sprite = eggs.EggSprite;
            Texture2D texture = sprite.texture;
            Object.Destroy(game.gameObject);
            yield return null;
            Assert.That(sprite == null && texture == null && pool == null, Is.True,
                "Leaving the scene must dispose pickup art and the runtime pools.");
        }

        private object Get(string field) => typeof(ForestRunnerGame).GetField(field, Private).GetValue(game);
        private void Set(string field, object value) => typeof(ForestRunnerGame).GetField(field, Private).SetValue(game, value);
        private void Invoke(string method, params object[] args) => typeof(ForestRunnerGame).GetMethod(method, Private).Invoke(game, args);
        private void Step(int frames) { for (int i = 0; i < frames; i++) Invoke("Step", RunnerRules.SimulationStep); }
    }
}
