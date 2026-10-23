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
        private bool hadBest, hadMap;
        private int oldBest;
        private string oldMap;

        [UnitySetUp]
        public IEnumerator Load()
        {
            hadBest = PlayerPrefs.HasKey("DinoRush.Forest.HighScore");
            oldBest = PlayerPrefs.GetInt("DinoRush.Forest.HighScore");
            hadMap = PlayerPrefs.HasKey(RunnerMapCatalog.SelectionKey);
            oldMap = PlayerPrefs.GetString(RunnerMapCatalog.SelectionKey);
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
            Assert.That(card.GetComponentsInChildren<Text>().Length, Is.EqualTo(9));
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
        public IEnumerator ResultsShowOnlyScoreBestAndTwoActionsAndKeepTheChosenMap()
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
            Assert.That(card.GetComponentsInChildren<Text>().Length, Is.EqualTo(8));
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
        public IEnumerator InstructionsStayInOptionsRatherThanOverTheGameOrModals()
        {
            game.StartRun();
            AssertRunningWithoutHints();
            yield return new WaitForSecondsRealtime(0.35f);
            yield return ForestRunTests.Capture("minimal-gameplay");
            game.ReturnToMenu();
            yield return null;
            Click("Forest Main Menu/Main menu buttons/Options");
            Text help = root.Find("Forest Main Menu/Menu dialog/Moss framed dialog/Options page/Controls help").GetComponent<Text>();
            Assert.That(help.gameObject.activeInHierarchy, Is.True);
            Assert.That(help.text, Does.Contain("JUMP").And.Contain("DUCK").And.Contain("PAUSE"));
        }

        private void AssertRunningWithoutHints()
        {
            foreach (string name in new[] { "Jump", "Duck", "Controls", "Keyboard controls", "Duck/Drop hint", "Top left badge" })
                Assert.That(root.Find(name).gameObject.activeInHierarchy, Is.False, name);
            foreach (string name in new[] { "Pause", "Sound", "Scoreboard", "Life hearts", "Stage progress" })
                Assert.That(root.Find(name).gameObject.activeInHierarchy, Is.True, name);
        }

        private void AssertFocusedModal(Transform card)
        {
            foreach (string name in new[] { "Top left badge", "Life hearts", "Stage progress", "Scoreboard", "Sound", "Jump", "Duck", "Controls", "Keyboard controls", "Trail stats frame" })
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
