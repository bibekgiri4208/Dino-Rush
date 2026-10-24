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

        [UnityTest]
        public IEnumerator PickupSparklesFlashExpandTwinkleAndFadeWithoutChangingAwardsOrGameplayRandomness()
        {
            CollectFirstEgg();
            SpriteRenderer flash = pool.Find("Pickup sparkle 0").GetComponent<SpriteRenderer>();
            SpriteRenderer halo = pool.Find("Pickup sparkle 1").GetComponent<SpriteRenderer>();
            SpriteRenderer delayed = pool.Find("Pickup sparkle 2").GetComponent<SpriteRenderer>();
            SpriteRenderer star = pool.Find("Pickup sparkle 3").GetComponent<SpriteRenderer>();
            SpriteRenderer shard = pool.Find("Pickup sparkle 7").GetComponent<SpriteRenderer>();
            Assert.That(flash.gameObject.activeSelf, Is.True);
            Assert.That(halo.sprite.name, Is.EqualTo("Egg pickup halo"));
            Assert.That(shard.sprite.name, Is.EqualTo("Egg pickup gold shard"));
            Assert.That(shard.sprite.texture.filterMode, Is.EqualTo(FilterMode.Point));
            Vector3 shardStart = shard.transform.position;
            Assert.That(halo.gameObject.activeSelf, Is.True);
            Assert.That(delayed.gameObject.activeSelf, Is.False, "After-glints should be staggered, not one uniform explosion.");
            float haloStartSize = halo.transform.localScale.x;
            float flashStartSize = flash.transform.localScale.x;
            Vector3 starStart = star.transform.position;
            Color starStartColor = star.color;
            Rect noPlayer = new Rect(-1000, -1000, 1, 1);
            for (int step = 0; step < 10; step++) eggs.Step(RunnerRules.SimulationStep, 0f, noPlayer);
            Assert.That(halo.transform.localScale.x, Is.GreaterThan(haloStartSize));
            Assert.That(flash.transform.localScale.x, Is.LessThan(flashStartSize));
            Assert.That(delayed.gameObject.activeSelf, Is.True);
            Assert.That(star.transform.position, Is.Not.EqualTo(starStart));
            Assert.That(star.color, Is.Not.EqualTo(starStartColor));
            Assert.That(star.transform.localRotation, Is.Not.EqualTo(Quaternion.identity));
            Assert.That(shard.transform.position, Is.Not.EqualTo(shardStart));
            yield return RunnerDisplayCapture.Capture("egg-sparkle-burst", 1280, 720);
            yield return RunnerDisplayCapture.Capture("egg-sparkle-burst-portrait", 360, 800);
            for (int step = 0; step < 15; step++) eggs.Step(RunnerRules.SimulationStep, 0f, noPlayer);
            Assert.That(flash.gameObject.activeSelf, Is.False, "The central flash should be brief.");
            Assert.That(halo.gameObject.activeSelf, Is.True);
            for (int step = 0; step < 75; step++) eggs.Step(RunnerRules.SimulationStep, 0f, noPlayer);
            for (int i = 0; i < RunnerEggCollectibles.SparklePoolSize; i++)
                Assert.That(pool.Find("Pickup sparkle " + i).gameObject.activeSelf, Is.False,
                    "All flash, halo and delayed sparkle renderers must expire.");
            Assert.That(game.EggsCollected, Is.EqualTo(1));

            Random.State saved = Random.state;
            try
            {
                Random.InitState(731);
                float expected = Random.value;
                Random.InitState(731);
                eggs.Reset(42);
                eggs.SpawnIntro();
                eggs.Step(RunnerRules.SimulationStep, 0f, new Rect(-100, -100, 200, 200));
                for (int step = 0; step < 90; step++) eggs.Step(RunnerRules.SimulationStep, 0f, noPlayer);
                Assert.That(Random.value, Is.EqualTo(expected), "Sparkle variation must not affect obstacle randomness.");
                Assert.That(game.EggsCollected, Is.EqualTo(4));
                Assert.That(pool.GetComponentsInChildren<SpriteRenderer>(true).Length,
                    Is.EqualTo(RunnerEggCollectibles.PoolSize + RunnerEggCollectibles.SparklePoolSize));
            }
            finally { Random.state = saved; }
            CollectFirstEgg();
            game.RestartRun();
            Assert.That(flash.gameObject.activeSelf || halo.gameObject.activeSelf || delayed.gameObject.activeSelf,
                Is.False, "Restart must clear even the delayed sparkle particles.");
        }

        [UnityTest]
        public IEnumerator CounterTextStaysInsideTheFrameDuringPickupPulsesAtEveryCountAndOrientation()
        {
            game.StartRun();
            var hud = (RunnerHud)Get("hud");
            var frame = (RectTransform)ui.Find("Egg counter");
            Text number = frame.Find("Egg count").GetComponent<Text>();
            Text caption = frame.Find("Egg caption").GetComponent<Text>();
            Vector2Int[] resolutions = { new Vector2Int(1280, 720), new Vector2Int(1024, 768), new Vector2Int(360, 800) };
            foreach (Vector2Int resolution in resolutions)
            {
                game.ApplyScreenLayout(RunnerScreenLayout.Calculate(resolution.x, resolution.y,
                    new Rect(0, 0, resolution.x, resolution.y)));
                foreach (int count in new[] { 4, 999, 9999, 99999, int.MaxValue })
                {
                    hud.UpdateEggs(count);
                    hud.Tick(0.11f); // Peak of the pickup pulse, not just the resting layout.
                    Canvas.ForceUpdateCanvases();
                    var corners = new Vector3[4];
                    number.rectTransform.GetWorldCorners(corners);
                    foreach (Vector3 corner in corners)
                    {
                        Vector3 local = frame.InverseTransformPoint(corner);
                        Assert.That(local.x, Is.InRange(frame.rect.xMin + 14f, frame.rect.xMax - 14f),
                            $"{count} escapes the counter's horizontal padding at {resolution}.");
                        Assert.That(local.y, Is.InRange(frame.rect.yMin + 5f, frame.rect.yMax - 5f));
                    }
                    Assert.That(number.preferredWidth, Is.LessThanOrEqualTo(number.rectTransform.rect.width + 1f));
                    Assert.That(number.cachedTextGenerator.characterCountVisible, Is.EqualTo(number.text.Length),
                        "Every digit (including the capped count) must remain visible.");
                    number.rectTransform.GetWorldCorners(corners);
                    float numberLeft = frame.InverseTransformPoint(corners[0]).x;
                    caption.rectTransform.GetWorldCorners(corners);
                    Assert.That(numberLeft, Is.GreaterThan(frame.InverseTransformPoint(corners[2]).x),
                        "The enlarged number must not overlap the EGGS caption.");
                }
            }
            yield return RunnerDisplayCapture.Capture("eggs-counter-pulse", 1280, 720);
            yield return RunnerDisplayCapture.Capture("eggs-counter-pulse-portrait", 360, 800);
        }

        [UnityTest]
        public IEnumerator PickupsFlyFromTheirWorldPositionIntoTheCounterAndPulseOnArrival()
        {
            CollectFirstEgg();
            var hud = (RunnerHud)Get("hud");
            var flights = (RunnerEggFlights)typeof(RunnerHud).GetField("eggFlights", Private).GetValue(hud);
            var layer = (RectTransform)ui.Find("Egg pickup flights");
            var flying = (RectTransform)layer.Find("Flying egg 0");
            Text number = ui.Find("Egg counter/Egg count").GetComponent<Text>();
            Canvas.ForceUpdateCanvases();
            Vector2 actual = RectTransformUtility.WorldToScreenPoint(null, flying.position);
            Vector2 source = Camera.main.WorldToScreenPoint(pool.Find("Pooled egg 0").position);
            Assert.That(Vector2.Distance(actual, source), Is.LessThan(1.5f), "The UI egg starts exactly where the world egg was collected.");
            Assert.That(flights.ActiveCount, Is.EqualTo(1));
            Assert.That(game.EggsCollected, Is.EqualTo(1));
            Assert.That(number.text, Is.EqualTo("001"), "Awards must not depend on an animation finishing.");
            Assert.That(number.transform.localScale, Is.EqualTo(Vector3.one), "The counter pulses on arrival, not on departure.");
            Vector3 target = ui.Find("Egg counter/Egg icon").position;
            float before = Vector3.Distance(flying.position, target);
            hud.Tick(0.25f);
            Assert.That(Vector3.Distance(flying.position, target), Is.LessThan(before));
            Assert.That(flying.localScale.x, Is.LessThan(1f), "The egg shrinks as it enters the counter.");
            yield return RunnerDisplayCapture.Capture("egg-flight-midway", 1280, 720);
            for (int i = 0; i < 120 && flights.ActiveCount > 0; i++) hud.Tick(RunnerRules.SimulationStep);
            Assert.That(flights.ActiveCount, Is.Zero);
            Assert.That(flying.gameObject.activeSelf, Is.False);
            Assert.That(number.transform.localScale.x, Is.GreaterThan(1f));
            Assert.That(game.EggsCollected, Is.EqualTo(1), "The arrival pulse must never award the egg twice.");
        }

        [UnityTest]
        public IEnumerator FlightPathsLiftTowardTheCounterWithoutLoopingInFrontOfTheDino()
        {
            game.StartRun();
            var hud = (RunnerHud)Get("hud");
            var flights = (RunnerEggFlights)typeof(RunnerHud).GetField("eggFlights", Private).GetValue(hud);
            var layer = (RectTransform)ui.Find("Egg pickup flights");
            var flying = (RectTransform)layer.Find("Flying egg 0");
            Vector2Int[] resolutions = { new Vector2Int(1280, 720), new Vector2Int(360, 800) };
            foreach (Vector2Int resolution in resolutions)
            {
                game.ApplyScreenLayout(RunnerScreenLayout.Calculate(resolution.x, resolution.y,
                    new Rect(0, 0, resolution.x, resolution.y)));
                foreach (float height in new[] { 0.7f, 4f })
                {
                    flights.Clear();
                    flights.Play(player.Feet + Vector3.up * height, Camera.main);
                    Vector2 start = flying.anchoredPosition;
                    Vector2 target = layer.InverseTransformPoint(ui.Find("Egg counter/Egg icon").position);
                    Vector2 previous = start;
                    Assert.That(target.y, Is.GreaterThan(start.y));
                    for (int step = 0; step < 60; step++)
                    {
                        flights.Tick(0.01f);
                        foreach (Graphic image in layer.GetComponentsInChildren<Graphic>())
                        {
                            if (image is Text || image.name == "Egg counter arrival") continue;
                            Vector2 position = image.rectTransform.anchoredPosition;
                            Assert.That(position.x, Is.InRange(Mathf.Min(start.x, target.x) - 0.1f,
                                Mathf.Max(start.x, target.x) + 0.1f),
                                "Flying eggs and their trails must not bow away from the counter in front of the dino.");
                            Assert.That(position.y, Is.InRange(start.y - 0.1f, target.y + 0.1f));
                        }
                        Assert.That(flying.anchoredPosition.y, Is.GreaterThanOrEqualTo(previous.y - 0.1f),
                            "The pickup should lift directly toward the counter without dipping back down.");
                        previous = flying.anchoredPosition;
                    }
                }
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator FlightsPauseReflowOnRotationAndClearOnResultsRestartAndMenusWithoutLosingAwards()
        {
            CollectFirstEgg();
            var hud = (RunnerHud)Get("hud");
            var flights = (RunnerEggFlights)typeof(RunnerHud).GetField("eggFlights", Private).GetValue(hud);
            var layer = (RectTransform)ui.Find("Egg pickup flights");
            var flying = (RectTransform)layer.Find("Flying egg 0");
            hud.Tick(0.22f);
            game.TogglePause();
            Vector2 paused = flying.anchoredPosition;
            hud.Tick(2f);
            Assert.That(flying.anchoredPosition, Is.EqualTo(paused));
            Assert.That(flights.ActiveCount, Is.EqualTo(1));
            Assert.That(flying.gameObject.activeInHierarchy, Is.False, "Flying eggs must not draw over pause menus.");
            game.ApplyScreenLayout(RunnerScreenLayout.Calculate(360, 800, new Rect(0, 32, 360, 736)));
            game.TogglePause();
            Assert.That(flying.gameObject.activeInHierarchy, Is.True);
            yield return RunnerDisplayCapture.Capture("egg-flight-portrait", 360, 800, new Rect(0, 32, 360, 736));
            game.ApplyScreenLayout(RunnerScreenLayout.Calculate(360, 800, new Rect(0, 32, 360, 736)));
            hud.Tick(0.38f);
            Vector2 target = layer.InverseTransformPoint(ui.Find("Egg counter/Egg icon").position);
            Assert.That(Vector2.Distance(flying.anchoredPosition, target), Is.LessThan(8f),
                "The flight must aim at the counter's new safe-area position, not its old landscape position.");
            hud.Tick(0.03f);
            Assert.That(flights.ActiveCount, Is.Zero);

            CollectFirstEgg();
            Assert.That(flights.ActiveCount, Is.EqualTo(1));
            Invoke("EndRun");
            Assert.That(flights.ActiveCount, Is.Zero);
            Assert.That(game.EggsCollected, Is.EqualTo(1));
            Assert.That(ui.Find("Menu card/Collected eggs").GetComponent<Text>().text, Is.EqualTo("EGGS COLLECTED: 001"));
            hud.Tick(1f);
            Assert.That(flights.ActiveCount, Is.Zero);
            CollectFirstEgg();
            game.RestartRun();
            Assert.That(flights.ActiveCount, Is.Zero);
            Assert.That(game.EggsCollected, Is.Zero);
            CollectFirstEgg();
            game.ReturnToMenu();
            Assert.That(flights.ActiveCount, Is.Zero);
            Assert.That(layer.gameObject.activeInHierarchy, Is.False);
            game.PrepareRun();
            Assert.That(flights.ActiveCount, Is.Zero);
            Assert.That(game.EggsCollected, Is.Zero);
        }

        [UnityTest]
        public IEnumerator FlightPoolHandlesBurstsWithoutExtraObjectsDuplicateAwardsOrBlockingInput()
        {
            game.StartRun();
            var hud = (RunnerHud)Get("hud");
            var flights = (RunnerEggFlights)typeof(RunnerHud).GetField("eggFlights", Private).GetValue(hud);
            Transform layer = ui.Find("Egg pickup flights");
            int images = layer.GetComponentsInChildren<Image>(true).Length;
            int objects = layer.GetComponentsInChildren<Transform>(true).Length;
            Assert.That(images, Is.EqualTo(RunnerEggFlights.PoolSize));
            Assert.That(layer.GetComponentsInChildren<RunnerEggPickupGraphic>(true).Length, Is.EqualTo(RunnerEggFlights.PoolSize * 6 + 1));
            Assert.That(layer.GetComponentsInChildren<Text>(true).Length, Is.EqualTo(RunnerEggFlights.PoolSize));
            Assert.That(layer.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
            foreach (Graphic image in layer.GetComponentsInChildren<Graphic>(true)) Assert.That(image.raycastTarget, Is.False);
            eggs.Reset(42);
            RunnerEncounter sample = new RunnerEncounterDirector(42, new[] { 1f }).Next(0f);
            RunnerEggTrail low = RunnerEggTrailDirector.Create(sample, 6.8f, RunnerEggTrailKind.Low);
            while (eggs.ActiveCount < RunnerEggCollectibles.PoolSize) eggs.SpawnTrail(low, 11.7f);
            eggs.Step(RunnerRules.SimulationStep, 0f, new Rect(-100, -100, 200, 200));
            Assert.That(game.EggsCollected, Is.EqualTo(RunnerEggCollectibles.PoolSize));
            Assert.That(flights.ActiveCount, Is.EqualTo(RunnerEggFlights.PoolSize));
            for (int i = 0; i < 90; i++) hud.Tick(RunnerRules.SimulationStep);
            Assert.That(flights.ActiveCount, Is.Zero);
            Assert.That(layer.GetComponentsInChildren<Image>(true).Length, Is.EqualTo(images));
            Assert.That(layer.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(objects));
            Assert.That(game.EggsCollected, Is.EqualTo(RunnerEggCollectibles.PoolSize));
            yield return null;
        }

        [UnityTest]
        public IEnumerator PickupPopShowsFloatingPlusOneAndGoldCometThenRingsAndBouncesTheCounter()
        {
            CollectFirstEgg();
            var hud = (RunnerHud)Get("hud");
            Transform layer = ui.Find("Egg pickup flights");
            Text popup = layer.Find("Egg pickup +1 0").GetComponent<Text>();
            var flying = (RectTransform)layer.Find("Flying egg 0");
            var glow = layer.Find("Egg flight glow 0").GetComponent<RunnerEggPickupGraphic>();
            Transform ring = layer.Find("Egg counter arrival");
            Transform icon = ui.Find("Egg counter/Egg icon");
            Assert.That(popup.gameObject.activeInHierarchy, Is.True);
            Assert.That(popup.text, Is.EqualTo("+1"));
            Assert.That(popup.color.a, Is.EqualTo(1));
            Assert.That(glow.gameObject.activeInHierarchy, Is.True);
            Assert.That(ring.gameObject.activeSelf, Is.False);
            float startSize = flying.localScale.x;
            float startY = popup.rectTransform.anchoredPosition.y;
            hud.Tick(0.06f);
            Assert.That(flying.localScale.x, Is.GreaterThan(startSize), "The egg should pop before sweeping toward the HUD.");
            Assert.That(popup.rectTransform.anchoredPosition.y, Is.GreaterThan(startY));
            yield return RunnerDisplayCapture.Capture("egg-pickup-pop", 1280, 720);
            hud.Tick(0.15f);
            Assert.That(layer.Find("Egg flight glint 0-4").gameObject.activeInHierarchy, Is.True);
            yield return RunnerDisplayCapture.Capture("egg-pickup-comet", 1280, 720);
            yield return RunnerDisplayCapture.Capture("egg-pickup-comet-portrait", 360, 800);
            hud.Tick(0.24f);
            Assert.That(popup.gameObject.activeSelf, Is.False, "The short +1 popup must not follow the egg across the playfield.");
            hud.Tick(0.18f);
            Assert.That(ring.gameObject.activeInHierarchy, Is.True);
            Assert.That(icon.localScale.x, Is.GreaterThan(1));
            Assert.That(icon.localRotation, Is.Not.EqualTo(Quaternion.identity));
            Vector2 target = layer.InverseTransformPoint(icon.position);
            Assert.That(Vector2.Distance(((RectTransform)ring).anchoredPosition, target), Is.LessThan(1));
            yield return RunnerDisplayCapture.Capture("egg-pickup-arrival", 1280, 720);
            Vector3 arrivalScale = icon.localScale;
            game.ApplyScreenLayout(RunnerScreenLayout.Calculate(360, 800, new Rect(0, 32, 360, 736)));
            target = layer.InverseTransformPoint(icon.position);
            Assert.That(Vector2.Distance(((RectTransform)ring).anchoredPosition, target), Is.LessThan(1),
                "Arrival feedback must reflow even when no egg is still flying.");
            Assert.That(icon.localScale, Is.EqualTo(arrivalScale));
            yield return RunnerDisplayCapture.Capture("egg-pickup-arrival-portrait", 360, 800);
            game.TogglePause();
            Vector3 pausedScale = icon.localScale;
            hud.Tick(1);
            Assert.That(icon.localScale, Is.EqualTo(pausedScale));
            Assert.That(ring.gameObject.activeInHierarchy, Is.False);
            game.TogglePause();
            hud.Tick(0.4f);
            Assert.That(ring.gameObject.activeSelf, Is.False);
            Assert.That(icon.localScale, Is.EqualTo(Vector3.one));
            Assert.That(icon.localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(game.EggsCollected, Is.EqualTo(1));
            CollectFirstEgg();
            hud.Tick(0.63f);
            game.RestartRun();
            Assert.That(ring.gameObject.activeSelf, Is.False);
            Assert.That(popup.gameObject.activeSelf, Is.False);
            Assert.That(glow.gameObject.activeSelf, Is.False);
            Assert.That(icon.localScale, Is.EqualTo(Vector3.one));
        }

        [UnityTest]
        public IEnumerator FloatingPickupLabelsStayReadableInsideTheScreenAndClearOnRotationAndRetry()
        {
            game.StartRun();
            var hud = (RunnerHud)Get("hud");
            var flights = (RunnerEggFlights)typeof(RunnerHud).GetField("eggFlights", Private).GetValue(hud);
            var layer = (RectTransform)ui.Find("Egg pickup flights");
            Text popup = layer.Find("Egg pickup +1 0").GetComponent<Text>();
            foreach (Vector2Int resolution in new[] { new Vector2Int(1280, 720), new Vector2Int(360, 800) })
            {
                game.ApplyScreenLayout(RunnerScreenLayout.Calculate(resolution.x, resolution.y,
                    new Rect(0, 32, resolution.x, resolution.y - 64)));
                foreach (float height in new[] { 0.7f, 4f })
                {
                    flights.Clear();
                    flights.Play(player.Feet + Vector3.up * height, Camera.main);
                    hud.Tick(0.1f);
                    Canvas.ForceUpdateCanvases();
                    Assert.That(popup.cachedTextGenerator.characterCountVisible, Is.EqualTo(2));
                    var corners = new Vector3[4];
                    popup.rectTransform.GetWorldCorners(corners);
                    foreach (Vector3 corner in corners)
                    {
                        Vector3 local = layer.InverseTransformPoint(corner);
                        Assert.That(local.x, Is.InRange(layer.rect.xMin, layer.rect.xMax));
                        Assert.That(local.y, Is.InRange(layer.rect.yMin, layer.rect.yMax));
                    }
                }
            }
            game.RestartRun();
            Assert.That(popup.gameObject.activeSelf, Is.False);
            Assert.That(flights.ActiveCount, Is.Zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator OriginalPickupSoundIsReusedRespectsMuteAndDisposesOnSceneExit()
        {
            var audio = (RunnerAudio)Get("audioCues");
            var pickup = (AudioClip)typeof(RunnerAudio).GetField("pickup", Private).GetValue(audio);
            Assert.That(pickup.length, Is.EqualTo(0.09f).Within(0.001f));
            Assert.That(pickup.name, Is.EqualTo("Egg pickup"));
            AudioSource source = game.GetComponent<AudioSource>();
            bool wasMuted = audio.Muted;
            try
            {
                if (!audio.Muted) audio.ToggleMute();
                for (int i = 1; i <= 40; i++) audio.Pickup();
                Assert.That(source.mute, Is.True);
                Assert.That(typeof(RunnerAudio).GetField("pickup", Private).GetValue(audio), Is.SameAs(pickup));
            }
            finally { if (audio.Muted != wasMuted) audio.ToggleMute(); }
            Object.Destroy(game.gameObject);
            yield return null;
            Assert.That(pickup == null, Is.True);
        }

        private void CollectFirstEgg()
        {
            game.StartRun();
            Set("spawnTimer", float.MaxValue);
            for (int frame = 0; frame < 240 && game.EggsCollected == 0; frame++) Step(1);
            Assert.That(game.EggsCollected, Is.EqualTo(1));
        }

        private object Get(string field) => typeof(ForestRunnerGame).GetField(field, Private).GetValue(game);
        private void Set(string field, object value) => typeof(ForestRunnerGame).GetField(field, Private).SetValue(game, value);
        private void Invoke(string method, params object[] args) => typeof(ForestRunnerGame).GetMethod(method, Private).Invoke(game, args);
        private void Step(int frames) { for (int i = 0; i < frames; i++) Invoke("Step", RunnerRules.SimulationStep); }
    }
}
