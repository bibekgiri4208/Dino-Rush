using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DinoRush.Tests
{
    public sealed class RunnerMapTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private ForestRunnerGame game;
        private bool hadMap, hadDino, hadBest;
        private string oldMap, oldDino;
        private int oldBest;

        [UnitySetUp]
        public IEnumerator Load()
        {
            hadMap = PlayerPrefs.HasKey(RunnerMapCatalog.SelectionKey);
            oldMap = PlayerPrefs.GetString(RunnerMapCatalog.SelectionKey);
            hadDino = PlayerPrefs.HasKey(RunnerDinoRoster.SelectionKey);
            oldDino = PlayerPrefs.GetString(RunnerDinoRoster.SelectionKey);
            hadBest = PlayerPrefs.HasKey("DinoRush.Forest.HighScore");
            oldBest = PlayerPrefs.GetInt("DinoRush.Forest.HighScore");
            PlayerPrefs.SetString(RunnerMapCatalog.SelectionKey, "forest");
            yield return Reload();
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            if (game != null) Object.Destroy(game.gameObject);
            yield return null;
            if (hadMap) PlayerPrefs.SetString(RunnerMapCatalog.SelectionKey, oldMap);
            else PlayerPrefs.DeleteKey(RunnerMapCatalog.SelectionKey);
            if (hadDino) PlayerPrefs.SetString(RunnerDinoRoster.SelectionKey, oldDino);
            else PlayerPrefs.DeleteKey(RunnerDinoRoster.SelectionKey);
            if (hadBest) PlayerPrefs.SetInt("DinoRush.Forest.HighScore", oldBest);
            else PlayerPrefs.DeleteKey("DinoRush.Forest.HighScore");
            PlayerPrefs.Save();
        }

        [UnityTest]
        public IEnumerator PlayOpensModesAndOnlyEndlessOpensMapsWithoutStarting()
        {
            Click("Play");
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Ready));
            ForestMainMenu menu = game.GetComponentInChildren<ForestMainMenu>();
            Assert.That(menu.DialogOpen, Is.True);
            Assert.That(GameObject.Find("Map selection panel"), Is.Null);
            Assert.That(GameObject.Find("Story mode").GetComponent<Button>().interactable, Is.False);
            yield return null;
            Click("Story mode");
            game.PrimaryAction();
            Assert.That(GameObject.Find("Endless mode"), Is.Not.Null, "Story and Space must leave the mode submenu open.");
            Assert.That(GameObject.Find("Map selection panel"), Is.Null);
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Ready));
            yield return new WaitForSecondsRealtime(0.3f);
            yield return ForestRunTests.Capture("mode-selector");
            yield return RunnerDisplayCapture.Capture("mode-selector-portrait", 360, 800);
            yield return ForestRunTests.Capture("mode-selector-tablet", 1024, 768);
            Click("Back to main menu");
            Assert.That(menu.DialogOpen, Is.False);
            game.PrimaryAction(); // The keyboard shortcut must open the same mode submenu.
            Assert.That(GameObject.Find("Endless mode"), Is.Not.Null);
            yield return null;
            Click("Endless mode");
            Assert.That(GameObject.Find("Mode selection page"), Is.Null);
            game.PrimaryAction();
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Ready), "Space must not skip map selection.");
            yield return new WaitForSecondsRealtime(0.3f);
            for (int i = 0; i < game.MapCatalog.Count; i++)
            {
                Click("Choose " + game.MapCatalog[i].displayName + " map");
                Assert.That(game.SelectedMapIndex, Is.EqualTo(i));
                foreach (string control in new[] { "Jump", "Duck" })
                    Assert.That(game.transform.Find("Forest runner UI/16 by 9 safe content/" + control).gameObject.activeInHierarchy,
                        Is.False, "Movement buttons must stay removed on every map.");
                Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Ready));
                Assert.That(PlayerPrefs.GetString(RunnerMapCatalog.SelectionKey), Is.EqualTo(game.MapCatalog[i].id));
                Image card = GameObject.Find("Choose " + game.MapCatalog[i].displayName + " map").GetComponent<Image>();
                Assert.That(card.sprite.name, Is.EqualTo("HudJump"));
                Assert.That(card.transform.Find("Map preview").GetComponent<Image>().sprite, Is.EqualTo(game.MapCatalog[i].preview));
            }
            foreach (Text text in menu.GetComponentsInChildren<Text>())
            {
                Assert.That(text.preferredWidth, Is.LessThanOrEqualTo(text.rectTransform.rect.width + 1), text.name);
                Assert.That(text.preferredHeight, Is.LessThanOrEqualTo(text.rectTransform.rect.height + 1), text.name);
            }
            yield return ForestRunTests.Capture("map-selector");
            yield return ForestRunTests.Capture("map-selector-tablet", 1024, 768);
            yield return ForestRunTests.Capture("map-selector-wide", 1920, 820);
            Click("Map selector back");
            Assert.That(menu.DialogOpen, Is.True);
            Assert.That(GameObject.Find("Endless mode"), Is.Not.Null, "Map Back returns to the mode submenu.");
            Assert.That(GameObject.Find("Map selection panel"), Is.Null);
            yield return null;
            Click("Endless mode");
            Assert.That(GameObject.Find("Map selection panel"), Is.Not.Null);
            yield return null;
            Click("Map selector back");
            yield return null;
            Click("Back to main menu");
            Assert.That(menu.DialogOpen, Is.False);
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Ready));
            yield return null;
            game.PrimaryAction();
            Assert.That(menu.DialogOpen, Is.True);
            var hud = (RunnerHud)Get("hud");
            hud.CloseMainMenuDialog(); // The same handler used by Esc / P in Ready.
            Assert.That(menu.DialogOpen, Is.False);
            Assert.That(((ICollection)Get("mapLayerCache")).Count, Is.Zero, "Choosing a preview must not spawn a run behind the menu.");
        }

        [UnityTest]
        public IEnumerator EveryMapRunsWithTheChosenDinoAndRetainsItsThemeThroughPauseAndRetry()
        {
            game.SelectDino(2);
            var player = (RunnerPlayer)Get("player");
            Rect hitbox = player.Hitbox;
            for (int i = 0; i < game.MapCatalog.Count; i++)
            {
                Click("Play");
                yield return null;
                Click("Endless mode");
                yield return null;
                Click("Choose " + game.MapCatalog[i].displayName + " map");
                Click("Start selected map");
                Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.AwaitingStart));
                game.PrimaryAction();
                Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Running));
                Assert.That(game.SelectedMapIndex, Is.EqualTo(i));
                Assert.That(player.Hitbox, Is.EqualTo(hitbox));
                Assert.That(game.transform.Find("Player/Blue dino player").GetComponent<SpriteRenderer>().sprite.texture,
                    Is.EqualTo(game.DinoRoster[2].Sheet));
                Assert.That(game.transform.Find("Forest runner UI/16 by 9 safe content/Top left badge/Brand").GetComponent<Text>().text,
                    Is.EqualTo(game.MapCatalog[i].displayName.ToUpperInvariant() + " RUN"));
                IList layers = (IList)Get("layers");
                Assert.That(layers.Count, Is.EqualTo(game.MapCatalog[i].layers.Length));
                if (i > 0)
                {
                    for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
                    {
                        RunnerMapCatalog.MapLayer spec = game.MapCatalog[i].layers[layerIndex];
                        foreach (Transform tile in Tiles(layers[layerIndex]))
                        {
                            Assert.That(tile.position.y, Is.EqualTo(spec.bottom).Within(0.0001f), tile.name);
                            if (layerIndex == 0)
                                Assert.That(tile.localScale.y, Is.GreaterThanOrEqualTo(spec.height), "Sky may extend to fill taller screens.");
                            else Assert.That(tile.localScale.y, Is.EqualTo(spec.height).Within(0.0001f), tile.name);
                            if (spec.stationary) Assert.That(tile.position.x, Is.EqualTo(spec.x).Within(0.0001f), tile.name);
                        }
                    }
                }
                var soil = game.transform.Find("Forest environment/Earth below the trail").GetComponent<SpriteRenderer>();
                Assert.That(soil.color, Is.EqualTo(Color.white), "The authored soil texture already contains the map palette.");
                Assert.That(soil.sprite.texture.name, Does.StartWith(game.MapCatalog[i].id + "_soil"));
                int rendererCount = game.GetComponentsInChildren<SpriteRenderer>(true).Length;
                object last = layers[layers.Count - 1];
                Transform[] tiles = Tiles(last);
                Assert.That(tiles[0].GetComponent<SpriteRenderer>().bounds.max.y, Is.EqualTo(-2.96f).Within(0.001f));
                for (int frame = 0; frame < 120; frame++) Step();
                Assert.That(game.Score, Is.GreaterThan(0));
                Assert.That(game.transform.Find("Obstacle pool").GetComponentsInChildren<SpriteRenderer>().Length, Is.GreaterThan(0));
                game.Jump();
                for (int frame = 0; frame < 16; frame++) Step();
                Assert.That(player.Grounded, Is.False);
                game.SetDuckHeld(true);
                for (int frame = 0; frame < 45; frame++) Step();
                Assert.That(player.Grounded, Is.True, "Fast drops must land identically on every map.");
                game.SetDuckHeld(false);
                game.SelectMap((i + 1) % game.MapCatalog.Count);
                Assert.That(game.SelectedMapIndex, Is.EqualTo(i), "Maps cannot change during a run.");
                game.TogglePause();
                game.SelectMap((i + 1) % game.MapCatalog.Count);
                Assert.That(game.SelectedMapIndex, Is.EqualTo(i));
                yield return new WaitForSecondsRealtime(0.04f);
                game.TogglePause();
                game.RestartRun();
                Assert.That(game.GetComponentsInChildren<SpriteRenderer>(true).Length, Is.EqualTo(rendererCount), "Retries reuse existing map tiles.");
                Assert.That(game.SelectedMapIndex, Is.EqualTo(i));
                yield return new WaitForSecondsRealtime(0.35f);
                yield return ForestRunTests.Capture("map-" + game.MapCatalog[i].id + "-running");
                if (i == 1) yield return ForestRunTests.Capture("map-desert-tablet", 1024, 768);
                if (i == 3) yield return ForestRunTests.Capture("map-rural-wide", 1920, 820);
                if (game.MapCatalog[i].id == "snow")
                {
                    yield return RunnerDisplayCapture.Capture("map-snow-16-by-10", 1280, 800);
                    yield return RunnerDisplayCapture.Capture("map-snow-tablet", 1024, 768);
                    yield return RunnerDisplayCapture.Capture("map-snow-portrait", 360, 800);
                }
                typeof(ForestRunnerGame).GetMethod("EndRun", Private).Invoke(game, null);
                Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.GameOver));
                Assert.That(game.SelectedMapIndex, Is.EqualTo(i));
                game.RestartRun();
                Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Running));
                Assert.That(game.GetComponentsInChildren<SpriteRenderer>(true).Length, Is.EqualTo(rendererCount));
                game.ReturnToMenu();
                Assert.That(((IList)Get("layers")).Count, Is.EqualTo(7), "The title screen restores its original forest backdrop.");
                Assert.That(game.SelectedMapIndex, Is.EqualTo(i));
                foreach (object old in layers)
                    if (i > 0) foreach (Transform tile in Tiles(old)) Assert.That(tile.gameObject.activeSelf, Is.False);
                yield return null;
            }
            int total = game.GetComponentsInChildren<SpriteRenderer>(true).Length;
            for (int i = game.MapCatalog.Count - 1; i >= 0; i--) { game.SelectMap(i); game.StartRun(); game.ReturnToMenu(); }
            Assert.That(game.GetComponentsInChildren<SpriteRenderer>(true).Length, Is.EqualTo(total));
        }

        [UnityTest]
        public IEnumerator NewMapLayersStaySeamlessAndCoverTheViewportAcrossRepeatedWraps()
        {
            MethodInfo scroll = typeof(ForestRunnerGame).GetMethod("Scroll", Private);
            for (int map = 1; map < game.MapCatalog.Count; map++)
            {
                game.SelectMap(map);
                game.StartRun();
                foreach (object layer in (IList)Get("layers"))
                {
                    System.Type type = layer.GetType();
                    Transform[] tiles = Tiles(layer);
                    Vector3 stationaryPosition = tiles[0].position;
                    bool stationary = (bool)type.GetField("stationary").GetValue(layer);
                    float stride = (float)type.GetField("width").GetValue(layer);
                    float speed = (float)type.GetField("speed").GetValue(layer);
                    SpriteRenderer first = tiles[0].GetComponent<SpriteRenderer>();
                    float pixelWidth = tiles[0].lossyScale.x / first.sprite.pixelsPerUnit;
                    foreach (float phase in new[] { 0f, 0.5f, 0.9999f, 1.0001f, 50.25f, 100.9999f })
                    {
                        if (!stationary) type.GetField("offset").SetValue(layer, 0f);
                        scroll.Invoke(game, new object[] { stationary ? 100f : phase * stride / speed });
                        if (stationary) { Assert.That(tiles[0].position, Is.EqualTo(stationaryPosition)); continue; }
                        for (int i = 1; i < tiles.Length; i++)
                            Assert.That(tiles[i - 1].GetComponent<SpriteRenderer>().bounds.max.x - tiles[i].GetComponent<SpriteRenderer>().bounds.min.x,
                                Is.EqualTo(pixelWidth).Within(0.0003f), first.name);
                        Assert.That(first.bounds.min.x, Is.LessThan(-10.1f));
                        Assert.That(tiles[tiles.Length - 1].GetComponent<SpriteRenderer>().bounds.max.x, Is.GreaterThan(10.1f));
                    }
                }
                foreach (float travel in new[] { 0f, 140f, 420f })
                {
                    foreach (object layer in (IList)Get("layers"))
                    {
                        System.Type type = layer.GetType();
                        type.GetField("offset").SetValue(layer, (float)type.GetField("width").GetValue(layer) * 0.25f);
                    }
                    scroll.Invoke(game, new object[] { travel });
                    yield return ForestRunTests.Capture("map-" + game.MapCatalog[map].id + "-framing-scroll-" + (int)travel);
                }
                game.ReturnToMenu();
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator MapChoiceSurvivesReloadAndUnknownIdsFallBackToForest()
        {
            game.SelectMap(game.MapCatalog.Find("snow"));
            yield return Reload();
            Assert.That(game.SelectedMapName, Is.EqualTo("Snow Mountain"));
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Ready));
            Click("Play");
            yield return null;
            Click("Endless mode");
            yield return null;
            Assert.That(GameObject.Find("Choose Snow Mountain map").GetComponent<Image>().sprite.name, Is.EqualTo("HudJump"));
            Click("Start selected map");
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.AwaitingStart));
            game.PrimaryAction();
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Running));
            game.ReturnToMenu();
            PlayerPrefs.SetString(RunnerMapCatalog.SelectionKey, "unknown-map");
            yield return Reload();
            Assert.That(game.SelectedMapIndex, Is.Zero);
            game.SelectMap(-1);
            game.SelectMap(game.MapCatalog.Count);
            Assert.That(game.SelectedMapName, Is.EqualTo("Forest"));
        }

        private IEnumerator Reload()
        {
            yield return SceneManager.LoadSceneAsync("ForestRun");
            game = Object.FindAnyObjectByType<ForestRunnerGame>();
            game.enabled = false;
            foreach (BaseInputModule input in EventSystem.current.GetComponents<BaseInputModule>()) input.enabled = false;
            yield return new WaitForSecondsRealtime(0.35f);
        }

        private object Get(string name) => typeof(ForestRunnerGame).GetField(name, Private).GetValue(game);
        private void Step() => typeof(ForestRunnerGame).GetMethod("Step", Private).Invoke(game, new object[] { RunnerRules.SimulationStep });
        private static Transform[] Tiles(object layer) => (Transform[])layer.GetType().GetField("tiles").GetValue(layer);

        private static void Click(string name)
        {
            Canvas.ForceUpdateCanvases();
            Button button = GameObject.Find(name).GetComponent<Button>();
            var rect = (RectTransform)button.transform;
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)) };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits.Count, Is.GreaterThan(0), name);
            Assert.That(hits[0].gameObject, Is.EqualTo(button.gameObject), name + " is blocked");
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }
    }
}
