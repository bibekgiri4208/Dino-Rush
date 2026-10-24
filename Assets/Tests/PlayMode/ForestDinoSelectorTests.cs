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
    public sealed class ForestDinoSelectorTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private ForestRunnerGame game;
        private bool hadDino, hadBest, hadMap;
        private string previousDino, previousMap;
        private int previousBest;

        [UnitySetUp]
        public IEnumerator LoadForest()
        {
            hadDino = PlayerPrefs.HasKey(RunnerDinoRoster.SelectionKey);
            previousDino = PlayerPrefs.GetString(RunnerDinoRoster.SelectionKey);
            hadBest = PlayerPrefs.HasKey("DinoRush.Forest.HighScore");
            previousBest = PlayerPrefs.GetInt("DinoRush.Forest.HighScore");
            hadMap = PlayerPrefs.HasKey(RunnerMapCatalog.SelectionKey);
            previousMap = PlayerPrefs.GetString(RunnerMapCatalog.SelectionKey);
            PlayerPrefs.SetString(RunnerDinoRoster.SelectionKey, "blue");
            yield return Reload();
        }

        [UnityTearDown]
        public IEnumerator RestorePrefs()
        {
            if (game != null) Object.Destroy(game.gameObject);
            yield return null;
            if (hadDino) PlayerPrefs.SetString(RunnerDinoRoster.SelectionKey, previousDino);
            else PlayerPrefs.DeleteKey(RunnerDinoRoster.SelectionKey);
            if (hadBest) PlayerPrefs.SetInt("DinoRush.Forest.HighScore", previousBest);
            else PlayerPrefs.DeleteKey("DinoRush.Forest.HighScore");
            if (hadMap) PlayerPrefs.SetString(RunnerMapCatalog.SelectionKey, previousMap);
            else PlayerPrefs.DeleteKey(RunnerMapCatalog.SelectionKey);
            PlayerPrefs.Save();
        }

        [UnityTest]
        public IEnumerator EveryDinoIsSelectableAndKeepsItsRunningJumpingAndDuckFrames()
        {
            SpriteRenderer renderer = game.transform.Find("Player/Blue dino player").GetComponent<SpriteRenderer>();
            var player = (RunnerPlayer)typeof(ForestRunnerGame).GetField("player", Private).GetValue(game);
            Rect standardHitbox = player.Hitbox;
            for (int i = 0; i < game.DinoRoster.Count; i++)
            {
                RunnerDinoRoster.Dino dino = game.DinoRoster[i];
                Click("Shop");
                game.GetComponentInChildren<ForestMainMenu>().ShopCollection.ShowIndex(i);
                yield return null; // Newly enabled card graphics register with the raycaster on the next UI frame.
                Click("Select " + dino.DisplayName + " dino");
                Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Ready));
                Assert.That(game.SelectedDinoIndex, Is.EqualTo(i));
                Assert.That(game.SelectedDinoName, Is.EqualTo(dino.DisplayName));
                Assert.That(PlayerPrefs.GetString(RunnerDinoRoster.SelectionKey), Is.EqualTo(dino.Id));
                Assert.That(GameObject.Find("Selected status").GetComponent<Text>().text, Is.EqualTo(dino.DisplayName.ToUpperInvariant() + " DINO SELECTED"));
                for (int j = (i / 4) * 4; j < Mathf.Min((i / 4 + 1) * 4, game.DinoRoster.Count); j++)
                {
                    GameObject card = GameObject.Find("Select " + game.DinoRoster[j].DisplayName + " dino");
                    Assert.That(card.GetComponent<Image>().sprite.name, Is.EqualTo(j == i ? "HudJump" : "HudPanel"));
                    Assert.That(card.transform.Find("Dino portrait").GetComponent<Image>().sprite, Is.EqualTo(game.DinoRoster[j].Portrait));
                }
                Assert.That(renderer.sprite.texture, Is.EqualTo(dino.Sheet));
                Assert.That(dino.Portrait.texture.filterMode, Is.EqualTo(FilterMode.Point));
                Assert.That(renderer.sprite.rect.size, Is.EqualTo(new Vector2(24, 24)));
                Assert.That(renderer.sprite.pivot, Is.EqualTo(new Vector2(12, 2)));
                Assert.That((Vector2)renderer.sprite.bounds.size, Is.EqualTo(Vector2.one));
                Assert.That(player.Hitbox, Is.EqualTo(standardHitbox));
                game.PrimaryAction();
                Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Ready), "Space must not start a run through Shop.");
                if (i == 3)
                {
                    AssertSelectorLayout();
                    yield return ForestRunTests.Capture("menu-dino-selector");
                    yield return ForestRunTests.Capture("menu-dino-selector-tablet", 1024, 768);
                    yield return ForestRunTests.Capture("menu-dino-selector-wide", 1920, 820);
                }
                Click("Back to main menu");
                yield return null;
                Click("Play");
                yield return null;
                Click("Endless mode");
                yield return null;
                Click("Start selected map");
                Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.AwaitingStart));
                game.PrimaryAction();
                player.Render(0.1f, true, false, 1f);
                Assert.That(renderer.sprite.name, Does.Match(dino.DisplayName + " dino frame [4-9]$"));
                if (i >= 4) yield return RunnerDisplayCapture.Capture("new-dino-" + dino.Id, 1280, 720);
                game.Jump();
                Step();
                player.Render(0f, true, false, 1f);
                Assert.That(renderer.sprite.name, Is.EqualTo(dino.DisplayName + " dino frame 11"));
                game.SetDuckHeld(true);
                player.Render(0f, true, false, 1f);
                Assert.That(renderer.sprite.name, Does.Match(dino.DisplayName + " dino frame (18|19|20|21|22|23)$"));
                for (int frame = 0; frame < 24; frame++) Step();
                Assert.That(player.Grounded, Is.True);
                game.SetDuckHeld(false);
                game.SelectDino((i + 1) % game.DinoRoster.Count);
                Assert.That(game.SelectedDinoIndex, Is.EqualTo(i), "Do not switch characters during a run.");
                game.TogglePause();
                game.SelectDino((i + 1) % game.DinoRoster.Count);
                Assert.That(game.SelectedDinoIndex, Is.EqualTo(i));
                game.RestartRun();
                Assert.That(renderer.sprite.texture, Is.EqualTo(dino.Sheet));
                game.ReturnToMenu();
                Assert.That(renderer.sprite.texture, Is.EqualTo(dino.Sheet));
                yield return null;
            }
            var cache = (Dictionary<int, Sprite[]>)typeof(ForestRunnerGame).GetField("dinoFrameCache", Private).GetValue(game);
            Assert.That(cache.Count, Is.EqualTo(game.DinoRoster.Count));
            Sprite[] red = cache[1];
            game.SelectDino(1);
            game.SelectDino(2);
            game.SelectDino(1);
            Assert.That(cache.Count, Is.EqualTo(game.DinoRoster.Count));
            Assert.That(cache[1], Is.SameAs(red), "Selecting again must reuse the existing frame set.");
        }

        [UnityTest]
        public IEnumerator SelectionSurvivesSceneReloadAndUnknownSavedIdsFallBackToBlue()
        {
            Click("Shop");
            yield return null;
            Click("Select Yellow dino");
            yield return Reload();
            Assert.That(game.SelectedDinoName, Is.EqualTo("Yellow"));
            Assert.That(game.transform.Find("Player/Blue dino player").GetComponent<SpriteRenderer>().sprite.texture,
                Is.EqualTo(game.DinoRoster[2].Sheet));
            Click("Shop");
            yield return null;
            Assert.That(GameObject.Find("Selected status").GetComponent<Text>().text, Is.EqualTo("YELLOW DINO SELECTED"));
            Click("Select Green dino");
            yield return Reload();
            Assert.That(game.SelectedDinoName, Is.EqualTo("Green"));
            PlayerPrefs.SetString(RunnerDinoRoster.SelectionKey, "unknown-or-removed");
            yield return Reload();
            Assert.That(game.SelectedDinoIndex, Is.Zero);
            Assert.That(game.SelectedDinoName, Is.EqualTo("Blue"));
            game.SelectDino(-1);
            game.SelectDino(game.DinoRoster.Count);
            Assert.That(game.SelectedDinoIndex, Is.Zero);
        }

        private IEnumerator Reload()
        {
            yield return SceneManager.LoadSceneAsync("ForestRun");
            game = Object.FindAnyObjectByType<ForestRunnerGame>();
            game.enabled = false;
            Assert.That(game.DinoRoster.Count, Is.EqualTo(10));
            yield return null;
        }

        [UnityTest]
        public IEnumerator EveryNewDinoRunsAndJumpsOnEveryMapWithTheSameCollisionBounds()
        {
            var player = (RunnerPlayer)typeof(ForestRunnerGame).GetField("player", Private).GetValue(game);
            SpriteRenderer renderer = game.transform.Find("Player/Blue dino player").GetComponent<SpriteRenderer>();
            Rect normal = player.Hitbox;
            for (int dino = 4; dino < game.DinoRoster.Count; dino++)
                for (int map = 0; map < game.MapCatalog.Count; map++)
                {
                    game.SelectDino(dino);
                    game.SelectMap(map);
                    game.StartRun();
                    Assert.That(player.Hitbox, Is.EqualTo(normal));
                    Assert.That(renderer.sprite.texture, Is.EqualTo(game.DinoRoster[dino].Sheet));
                    game.Jump();
                    Step();
                    Assert.That(player.Grounded, Is.False);
                    player.Render(0, true, false, 1);
                    Assert.That(renderer.sprite.name, Does.EndWith("frame 11"));
                    game.ReturnToMenu();
                }
            yield return null;
        }

        private void Step() => typeof(ForestRunnerGame).GetMethod("Step", Private).Invoke(game, new object[] { RunnerRules.SimulationStep });

        private void AssertSelectorLayout()
        {
            Canvas.ForceUpdateCanvases();
            ForestMainMenu menu = game.GetComponentInChildren<ForestMainMenu>();
            foreach (Text text in menu.GetComponentsInChildren<Text>())
            {
                Assert.That(text.preferredWidth, Is.LessThanOrEqualTo(text.rectTransform.rect.width + 1), text.name);
                Assert.That(text.preferredHeight, Is.LessThanOrEqualTo(text.rectTransform.rect.height + 1), text.name);
            }
            Transform choices = GameObject.Find("Dino choices").transform;
            var boundsOnPage = new List<Bounds>();
            foreach (Transform card in choices)
            {
                if (!card.gameObject.activeSelf) continue;
                Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(choices, card);
                foreach (Bounds other in boundsOnPage) Assert.That(bounds.Intersects(other), Is.False);
                boundsOnPage.Add(bounds);
                Assert.That(bounds.min.x, Is.GreaterThanOrEqualTo(-256));
                Assert.That(bounds.max.x, Is.LessThanOrEqualTo(256));
            }
        }

        private static void Click(string name)
        {
            Canvas.ForceUpdateCanvases();
            Button button = GameObject.Find(name).GetComponent<Button>();
            var rect = (RectTransform)button.transform;
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)),
                button = PointerEventData.InputButton.Left
            };
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
