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
    public sealed class RunnerMinimalUiTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private ForestRunnerGame game;
        private Transform root;
        private RunnerHud hud;
        private bool hadBest, hadMap, hadSound;
        private int oldBest, oldSound;
        private string oldMap;

        [UnitySetUp]
        public IEnumerator Load()
        {
            hadBest = PlayerPrefs.HasKey("DinoRush.Forest.HighScore");
            oldBest = PlayerPrefs.GetInt("DinoRush.Forest.HighScore");
            hadMap = PlayerPrefs.HasKey(RunnerMapCatalog.SelectionKey);
            oldMap = PlayerPrefs.GetString(RunnerMapCatalog.SelectionKey);
            hadSound = PlayerPrefs.HasKey("DinoRush.Muted");
            oldSound = PlayerPrefs.GetInt("DinoRush.Muted");
            yield return SceneManager.LoadSceneAsync("ForestRun");
            game = Object.FindAnyObjectByType<ForestRunnerGame>();
            game.enabled = false;
            root = game.transform.Find("Forest runner UI/16 by 9 safe content");
            hud = (RunnerHud)typeof(ForestRunnerGame).GetField("hud", Private).GetValue(game);
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
            if (hadSound) PlayerPrefs.SetInt("DinoRush.Muted", oldSound);
            else PlayerPrefs.DeleteKey("DinoRush.Muted");
            PlayerPrefs.Save();
        }

        [UnityTest]
        public IEnumerator PauseIsOneFocusedCardWithWorkingResumeRestartAndMainMenuActions()
        {
            game.StartRun();
            Set("best", 5678);
            Set("distance", 1234f);
            hud.UpdateScore(1234, 5678, 0, RunnerRules.Speed(0));
            game.TogglePause();
            yield return new WaitForSecondsRealtime(0.35f);
            Transform card = root.Find("Forest Pause Menu");
            Assert.That(card.Find("Pause title").GetComponent<Text>().text, Is.EqualTo("PAUSED"));
            Assert.That(card.Find("Paused score").GetComponent<Text>().text, Is.EqualTo(1234.ToString("N0")));
            Assert.That(card.Find("Paused best").GetComponent<Text>().text, Is.EqualTo(5678.ToString("N0")));
            Assert.That(card.GetComponentsInChildren<Button>().Length, Is.EqualTo(3));
            Assert.That(card.GetComponentInChildren<Slider>(), Is.Not.Null);
            Assert.That(card.GetComponentsInChildren<Text>().Length, Is.EqualTo(10));
            AssertFocusedModal(card);
            int score = game.Score;
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(game.Score, Is.EqualTo(score));
            yield return ForestRunTests.Capture("minimal-pause");
            yield return ForestRunTests.Capture("minimal-pause-tablet", 1024, 768);
            yield return ForestRunTests.Capture("minimal-pause-wide", 1920, 820);
            Click("Forest Pause Menu/Resume run");
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Running));
            Assert.That(game.Score, Is.EqualTo(score));
            AssertRunningWithoutHints();
            game.TogglePause();
            yield return null;
            Click("Forest Pause Menu/Restart run");
            Assert.That(game.Score, Is.Zero);
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Running));
            game.TogglePause();
            yield return null;
            Click("Forest Pause Menu/Quit run");
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Ready));
            game.StartRun();
            AssertRunningWithoutHints();
        }

        [UnityTest]
        public IEnumerator ResultsShowScoreBestEggsAndTwoActionsAndKeepTheChosenMap()
        {
            game.SelectMap(3);
            game.StartRun();
            Set("best", 500);
            Set("bestAtRunStart", 500);
            Set("distance", 1234f);
            typeof(ForestRunnerGame).GetMethod("EndRun", Private).Invoke(game, null);
            Set("timeSinceCrash", 1f);
            yield return new WaitForSecondsRealtime(0.35f);
            Transform card = root.Find("Menu card");
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.GameOver));
            Assert.That(card.Find("Title").GetComponent<Text>().text, Is.EqualTo("GAME OVER"));
            Assert.That(card.Find("Eyebrow").GetComponent<Text>().text, Is.EqualTo("NEW BEST"));
            Assert.That(card.Find("Eyebrow").GetComponent<Text>().color, Is.EqualTo((Color)new Color32(255, 210, 117, 255)));
            Assert.That(card.Find("Results score").GetComponent<Text>().text, Is.EqualTo(1234.ToString("N0")));
            Assert.That(card.Find("Results best").GetComponent<Text>().text, Is.EqualTo(1234.ToString("N0")));
            Assert.That(card.GetComponentsInChildren<Button>().Length, Is.EqualTo(2));
            Assert.That(card.GetComponentsInChildren<Text>().Length, Is.EqualTo(9));
            AssertFocusedModal(card);
            yield return ForestRunTests.Capture("minimal-results-record");
            yield return ForestRunTests.Capture("minimal-results-tablet", 1024, 768);
            yield return ForestRunTests.Capture("minimal-results-wide", 1920, 820);
            Click("Menu card/Primary action");
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Running));
            Assert.That(game.SelectedMapName, Is.EqualTo("Rural"));
            Assert.That(game.Score, Is.Zero);
            Set("distance", 42f);
            typeof(ForestRunnerGame).GetMethod("EndRun", Private).Invoke(game, null);
            yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(card.Find("Eyebrow").GetComponent<Text>().text, Is.EqualTo("RURAL"));
            Assert.That(card.Find("Eyebrow").GetComponent<Text>().color, Is.EqualTo((Color)new Color32(174, 203, 152, 255)));
            Assert.That(card.Find("Results best").GetComponent<Text>().text, Is.EqualTo(1234.ToString("N0")));
            yield return ForestRunTests.Capture("minimal-results");
            Click("Menu card/Results main menu");
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Ready));
            Assert.That(PlayerPrefs.GetInt("DinoRush.Forest.HighScore"), Is.EqualTo(1234));
        }

        [UnityTest]
        public IEnumerator InstructionsStayHiddenInOptionsAsWellAsTheGameAndModals()
        {
            game.StartRun();
            AssertRunningWithoutHints();
            yield return new WaitForSecondsRealtime(0.35f);
            yield return ForestRunTests.Capture("minimal-gameplay");
            game.ReturnToMenu();
            yield return null;
            Click("Forest Main Menu/Main menu buttons/Options");
            Text help = root.Find("Forest Main Menu/Menu dialog/Moss framed dialog/Options page/Controls help").GetComponent<Text>();
            Assert.That(help.gameObject.activeSelf, Is.False);
            Assert.That(help.text, Is.Empty);
            yield return ForestRunTests.Capture("options-without-instructions");
        }

        [UnityTest]
        public IEnumerator UtilityDialogsFitTheirContentAndRestoreTheLargeShopAfterSwitchingAndRotation()
        {
            Transform dialog = root.Find("Forest Main Menu/Menu dialog/Moss framed dialog");
            var sizes = new[] { new Vector2Int(1280, 720), new Vector2Int(360, 800) };
            foreach (Vector2Int size in sizes)
            {
                bool portrait = size.x < size.y;
                RunnerScreenLayout layout = RunnerScreenLayout.Calculate(size.x, size.y, new Rect(0, 0, size.x, size.y));
                // Pointer raycasts use the real Game view. Virtual dimensions are for layout/capture assertions.
                NativeLayout();
                Click("Forest Main Menu/Main menu buttons/Options");
                game.ApplyScreenLayout(layout);
                yield return new WaitForSecondsRealtime(0.3f);
                Assert.That(((RectTransform)dialog).sizeDelta, Is.EqualTo(portrait ? new Vector2(560, 680) : new Vector2(480, 496)));
                var sfx = (RectTransform)dialog.Find("Options page/Toggle sound effects");
                var redeem = (RectTransform)dialog.Find("Options page/Redeem gift");
                var optionsBack = (RectTransform)dialog.Find("Back to main menu");
                Assert.That(dialog.Find("Options page/Controls help").gameObject.activeSelf, Is.False);
                Assert.That(redeem.sizeDelta, Is.EqualTo(sfx.sizeDelta), "Redeem belongs in the same full-width action list as SFX.");
                Assert.That(redeem.anchoredPosition.x, Is.Zero);
                Assert.That(optionsBack.anchoredPosition.x, Is.Zero);
                Assert.That(redeem.anchoredPosition.y + redeem.rect.height / 2,
                    Is.LessThan(sfx.anchoredPosition.y - sfx.rect.height / 2));
                Assert.That(optionsBack.anchoredPosition.y, Is.LessThan(redeem.anchoredPosition.y));
                Assert.That(redeem.Find("Icon").GetComponent<RunnerGiftGraphic>().raycastTarget, Is.False);
                AssertCompactContents(dialog);
                NativeLayout();
                int soundBefore = PlayerPrefs.GetInt("DinoRush.Muted");
                Click("Forest Main Menu/Menu dialog/Moss framed dialog/Options page/Toggle sound effects");
                Assert.That(PlayerPrefs.GetInt("DinoRush.Muted"), Is.EqualTo(1 - soundBefore));
                Assert.That(dialog.Find("Options page/Toggle sound effects").GetComponentInChildren<Text>().text,
                    Is.EqualTo(soundBefore == 0 ? "SFX: OFF" : "SFX: ON"));
                Click("Forest Main Menu/Menu dialog/Moss framed dialog/Options page/Toggle sound effects");
                yield return RunnerDisplayCapture.Capture(portrait ? "compact-options-portrait" : "compact-options", size.x, size.y);
                NativeLayout();
                Click("Forest Main Menu/Menu dialog/Moss framed dialog/Back to main menu");
                yield return null; // Re-enabled title buttons register with the raycaster on the next UI frame.
                Click("Forest Main Menu/Quit");
                game.ApplyScreenLayout(layout);
                yield return new WaitForSecondsRealtime(0.3f);
                Assert.That(((RectTransform)dialog).sizeDelta, Is.EqualTo(portrait ? new Vector2(560, 320) : new Vector2(448, 272)));
                Transform back = dialog.Find("Back to main menu");
                Assert.That(back.GetComponentInChildren<Text>().text, Is.EqualTo("CANCEL"));
                Assert.That(((RectTransform)back).anchoredPosition.x, Is.LessThan(0));
                Assert.That(((RectTransform)dialog.Find("Quit confirmation page/Confirm quit")).anchoredPosition.x, Is.GreaterThan(0));
                AssertCompactContents(dialog);
                yield return RunnerDisplayCapture.Capture(portrait ? "compact-quit-portrait" : "compact-quit", size.x, size.y);
                NativeLayout();
                Click("Forest Main Menu/Menu dialog/Moss framed dialog/Back to main menu");
                yield return null;
                Click("Forest Main Menu/Main menu buttons/Shop");
                game.ApplyScreenLayout(layout);
                yield return null;
                Assert.That(((RectTransform)dialog).sizeDelta, Is.EqualTo(portrait ? new Vector2(640, 1360) : new Vector2(960, 744)));
                Assert.That(back.GetComponentInChildren<Text>().text, Is.EqualTo("BACK"));
                NativeLayout();
                Click("Forest Main Menu/Menu dialog/Moss framed dialog/Back to main menu");
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator CompactPauseSurvivesRotationAndKeepsReadableTappableControlsAndRunProgress()
        {
            game.StartRun();
            Set("distance", 1234f);
            hud.UpdateScore(1234, 5678, 0, RunnerRules.Speed(0));
            game.TogglePause();
            Transform pause = root.Find("Forest Pause Menu");
            foreach (Vector2Int size in new[] { new Vector2Int(1280, 720), new Vector2Int(320, 568), new Vector2Int(1280, 720) })
            {
                bool portrait = size.x < size.y;
                game.ApplyScreenLayout(RunnerScreenLayout.Calculate(size.x, size.y, new Rect(0, 0, size.x, size.y)));
                yield return new WaitForSecondsRealtime(0.3f);
                Assert.That(((RectTransform)pause).sizeDelta, Is.EqualTo(portrait ? new Vector2(520, 556) : new Vector2(420, 400)));
                foreach (Button button in pause.GetComponentsInChildren<Button>())
                    Assert.That(((RectTransform)button.transform).rect.height, Is.GreaterThanOrEqualTo(portrait ? 100 : 48));
                AssertCompactContents(pause);
                Assert.That(game.Score, Is.EqualTo(1234));
                Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Paused));
                yield return RunnerDisplayCapture.Capture(portrait ? "compact-pause-portrait" : "compact-pause", size.x, size.y);
            }
            Click("Forest Pause Menu/Resume run");
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Running));
            Assert.That(game.Score, Is.EqualTo(1234));
        }

        private static void AssertCompactContents(Transform panel)
        {
            Rect bounds = ((RectTransform)panel).rect;
            foreach (Text text in panel.GetComponentsInChildren<Text>())
            {
                Assert.That(text.preferredWidth, Is.LessThanOrEqualTo(text.rectTransform.rect.width + 1), text.name);
                Assert.That(text.preferredHeight, Is.LessThanOrEqualTo(text.rectTransform.rect.height + 1), text.name);
            }
            foreach (Button button in panel.GetComponentsInChildren<Button>())
            {
                Bounds buttonBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(panel, button.transform);
                Assert.That(buttonBounds.min.x, Is.GreaterThanOrEqualTo(bounds.xMin - 1), button.name);
                Assert.That(buttonBounds.max.x, Is.LessThanOrEqualTo(bounds.xMax + 1), button.name);
                Assert.That(buttonBounds.min.y, Is.GreaterThanOrEqualTo(bounds.yMin - 1), button.name);
                Assert.That(buttonBounds.max.y, Is.LessThanOrEqualTo(bounds.yMax + 1), button.name);
            }
        }

        private void NativeLayout() => game.ApplyScreenLayout(RunnerScreenLayout.Calculate(Screen.width, Screen.height, Screen.safeArea));

        [UnityTest]
        public IEnumerator IllustratedModesStayCompactAndReflowWithoutStartingTheRun()
        {
            Click("Forest Main Menu/Main menu buttons/Play");
            Transform panel = root.Find("Forest Main Menu/Menu dialog/Moss framed dialog");
            Transform modes = panel.Find("Mode selection page");
            Button story = modes.Find("Story mode").GetComponent<Button>();
            Button endless = modes.Find("Endless mode").GetComponent<Button>();
            Assert.That(story.interactable, Is.False);
            Assert.That(endless.interactable, Is.True);
            Assert.That(story.transform.Find("Mode status").GetComponent<Text>().text, Is.EqualTo("COMING SOON"));
            foreach (Vector2Int size in new[] { new Vector2Int(1280, 720), new Vector2Int(320, 568), new Vector2Int(1280, 720) })
            {
                bool portrait = size.x < size.y;
                game.ApplyScreenLayout(RunnerScreenLayout.Calculate(size.x, size.y, new Rect(0, 0, size.x, size.y)));
                yield return new WaitForSecondsRealtime(0.3f);
                Assert.That(((RectTransform)panel).sizeDelta, Is.EqualTo(portrait ? new Vector2(600, 640) : new Vector2(560, 400)));
                AssertCompactContents(panel);
                Bounds left = RectTransformUtility.CalculateRelativeRectTransformBounds(panel, story.transform);
                Bounds right = RectTransformUtility.CalculateRelativeRectTransformBounds(panel, endless.transform);
                if (portrait) Assert.That(right.max.y, Is.LessThan(left.min.y));
                else Assert.That(left.max.x, Is.LessThan(right.min.x));
                foreach (Button button in new[] { story, endless })
                {
                    Image art = button.transform.Find("Mode illustration").GetComponent<Image>();
                    Assert.That(art.sprite, Is.Not.Null);
                    Assert.That(art.sprite.texture.filterMode, Is.EqualTo(FilterMode.Point));
                    Assert.That(art.raycastTarget, Is.False);
                    Assert.That(art.transform.Find("Trail dino").GetComponent<Image>().sprite, Is.Not.Null);
                }
                Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Ready));
                yield return RunnerDisplayCapture.Capture(portrait ? "compact-modes-portrait" : "compact-modes", size.x, size.y);
            }
            NativeLayout();
            Click("Forest Main Menu/Menu dialog/Moss framed dialog/Mode selection page/Endless mode");
            yield return null;
            Assert.That(modes.gameObject.activeInHierarchy, Is.False);
            Assert.That(GameObject.Find("Map selection panel"), Is.Not.Null);
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Ready));
            Click("Forest Main Menu/Map selector/Map selection panel/Map selector back");
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(modes.gameObject.activeInHierarchy, Is.True);
            Assert.That(((RectTransform)panel).sizeDelta, Is.EqualTo(new Vector2(560, 400)));
        }

        private void AssertRunningWithoutHints()
        {
            foreach (string name in new[] { "Jump", "Duck", "Controls", "Keyboard controls", "Duck/Drop hint", "Top left badge" })
                Assert.That(root.Find(name).gameObject.activeInHierarchy, Is.False, name);
            foreach (string name in new[] { "Pause", "Sound", "Scoreboard", "Life hearts", "Egg counter", "Stage progress" })
                Assert.That(root.Find(name).gameObject.activeInHierarchy, Is.True, name);
        }

        private void AssertFocusedModal(Transform card)
        {
            foreach (string name in new[] { "Top left badge", "Life hearts", "Egg counter", "Stage progress", "Scoreboard", "Sound", "Jump", "Duck", "Controls", "Keyboard controls", "Trail stats frame" })
                Assert.That(root.Find(name).gameObject.activeInHierarchy, Is.False, name + " crowds the modal");
            Assert.That(root.Find("Menu scrim").GetComponent<Image>().raycastTarget, Is.True);
            Assert.That(card.GetComponent<CanvasGroup>().blocksRaycasts, Is.True);
            foreach (Text text in card.GetComponentsInChildren<Text>())
            {
                Assert.That(text.text, Does.Not.Contain("SPACE").And.Not.Contain("ESC").And.Not.Contain("BREATHER").And.Not.Contain("ENDLESS"));
                Assert.That(text.font.fontNames, Does.Contain("Press Start 2P"));
                Assert.That(text.preferredWidth, Is.LessThanOrEqualTo(text.rectTransform.rect.width + 1), text.name);
                Assert.That(text.preferredHeight, Is.LessThanOrEqualTo(text.rectTransform.rect.height + 1), text.name);
            }
            foreach (Button button in card.GetComponentsInChildren<Button>())
            {
                Assert.That(button.GetComponent<Image>().sprite.name, Is.EqualTo("ButtonPlay").Or.EqualTo("ButtonForest"));
                Assert.That(button.GetComponent<Image>().sprite.texture.filterMode, Is.EqualTo(FilterMode.Point));
                Assert.That(button.GetComponent<RunnerUiButtonFeedback>(), Is.Not.Null);
                Assert.That(((RectTransform)button.transform).rect.height, Is.GreaterThanOrEqualTo(48));
            }
        }

        private void Set(string name, object value) => typeof(ForestRunnerGame).GetField(name, Private).SetValue(game, value);

        private void Click(string path)
        {
            Canvas.ForceUpdateCanvases();
            Button button = root.Find(path).GetComponent<Button>();
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, button.transform.TransformPoint(Vector3.zero)) };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits.Count, Is.GreaterThan(0), path);
            Assert.That(hits[0].gameObject, Is.EqualTo(button.gameObject), path + " is blocked");
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }
    }
}
