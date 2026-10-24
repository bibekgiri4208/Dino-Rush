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
    public sealed class RunnerMenuThoughtTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private ForestRunnerGame game;
        private ForestMainMenu menu;
        private RunnerMenuThought thought;
        private CanvasGroup group;
        private Text caption;
        private string oldDino;
        private bool hadDino;
        private float oldTimeScale;

        [UnitySetUp]
        public IEnumerator Load()
        {
            hadDino = PlayerPrefs.HasKey(RunnerDinoRoster.SelectionKey);
            oldDino = PlayerPrefs.GetString(RunnerDinoRoster.SelectionKey);
            oldTimeScale = Time.timeScale;
            yield return SceneManager.LoadSceneAsync("ForestRun");
            game = Object.FindAnyObjectByType<ForestRunnerGame>();
            game.enabled = false;
            menu = game.GetComponentInChildren<ForestMainMenu>();
            thought = menu.GetComponentInChildren<RunnerMenuThought>(true);
            Assert.That(thought, Is.Not.Null, "The menu puff must be authored in the saved scene.");
            group = thought.GetComponent<CanvasGroup>();
            caption = thought.GetComponentInChildren<Text>();
            foreach (BaseInputModule input in EventSystem.current.GetComponents<BaseInputModule>()) input.enabled = false;
            thought.enabled = false;
            Set("firstDelay", 0.05f);
            Set("visibleSeconds", 1f);
            Set("quietSeconds", new Vector2(0.35f, 0.35f));
            thought.enabled = true;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = oldTimeScale;
            if (game != null) Object.Destroy(game.gameObject);
            yield return null;
            if (hadDino) PlayerPrefs.SetString(RunnerDinoRoster.SelectionKey, oldDino);
            else PlayerPrefs.DeleteKey(RunnerDinoRoster.SelectionKey);
            PlayerPrefs.Save();
        }

        [UnityTest]
        public IEnumerator ThoughtsFadeTakeBreaksAndDoNotRepeatEvenWithScaledTimeStopped()
        {
            Time.timeScale = 0f;
            Assert.That(group.alpha, Is.Zero);
            yield return WaitForAlpha(true);
            string first = caption.text;
            Assert.That(first, Is.EqualTo("What a beautiful\nday!"));
            yield return WaitForAlpha(false);
            yield return new WaitForSecondsRealtime(0.12f);
            Assert.That(group.alpha, Is.Zero, "Leave a quiet pause between thoughts.");
            yield return WaitForAlpha(true);
            Assert.That(caption.text, Is.Not.EqualTo(first));
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Ready));
            Assert.That(game.Score, Is.Zero);
        }

        [UnityTest]
        public IEnumerator PuffHidesInEverySubmenuAndRunStateAndReturnsOnlyAtTheTitle()
        {
            yield return WaitForAlpha(true);
            menu.OpenMapSelector();
            Assert.That(thought.IsShowing, Is.False);
            menu.CloseDialog();
            Assert.That(group.alpha, Is.Zero);
            foreach (string name in new[] { "Options", "Shop", "Quit" })
            {
                yield return WaitForAlpha(true);
                menu.transform.Find("Main menu buttons/" + name).GetComponent<Button>().onClick.Invoke();
                Assert.That(thought.IsShowing, Is.False, name);
                menu.CloseDialog();
                Assert.That(group.alpha, Is.Zero);
            }
            yield return WaitForAlpha(true);
            game.StartRun();
            Assert.That(thought.IsShowing, Is.False);
            game.TogglePause();
            yield return null;
            Assert.That(thought.IsShowing, Is.False);
            game.TogglePause();
            typeof(ForestRunnerGame).GetMethod("EndRun", Private).Invoke(game, null);
            yield return null;
            Assert.That(thought.IsShowing, Is.False);
            game.ReturnToMenu();
            Assert.That(group.alpha, Is.Zero);
            yield return WaitForAlpha(true);
        }

        [UnityTest]
        public IEnumerator PixelPuffFitsEveryThoughtFollowsAllDinosAndNeverBlocksMenuButtons()
        {
            Set("visibleSeconds", 30f);
            yield return WaitForAlpha(true);
            Assert.That(group.blocksRaycasts, Is.False);
            Assert.That(group.interactable, Is.False);
            Assert.That(thought.GetComponent<Image>().sprite.texture.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(caption.font.fontNames, Does.Contain("Press Start 2P"));
            foreach (Graphic graphic in thought.GetComponentsInChildren<Graphic>()) Assert.That(graphic.raycastTarget, Is.False);
            string current = caption.text;
            foreach (string line in (string[])typeof(RunnerMenuThought).GetField("thoughts", Private).GetValue(thought))
            {
                caption.text = line;
                Assert.That(caption.preferredWidth, Is.LessThanOrEqualTo(caption.rectTransform.rect.width));
                Assert.That(caption.preferredHeight, Is.LessThanOrEqualTo(caption.rectTransform.rect.height));
            }
            caption.text = current;
            for (int i = 0; i < 4; i++)
            {
                game.SelectDino(i);
                yield return null;
                Canvas.ForceUpdateCanvases();
                Bounds puff = RectTransformUtility.CalculateRelativeRectTransformBounds(menu.transform, thought.transform);
                Assert.That(puff.min.x, Is.GreaterThan(-640));
                Assert.That(puff.max.x, Is.LessThan(-190), "Keep clear of the centered buttons.");
                Assert.That(puff.min.y, Is.GreaterThan(-360));
                Assert.That(puff.max.y, Is.LessThan(360));
            }
            yield return ForestRunTests.Capture("menu-dino-thought");
            yield return ForestRunTests.Capture("menu-dino-thought-tablet", 1024, 768);
            yield return ForestRunTests.Capture("menu-dino-thought-wide", 1920, 820);
            menu.transform.Find("Main menu buttons/Options").GetComponent<Button>().onClick.Invoke();
            Assert.That(menu.DialogOpen, Is.True);
            Assert.That(thought.IsShowing, Is.False);
        }

        private IEnumerator WaitForAlpha(bool visible)
        {
            float deadline = Time.realtimeSinceStartup + 4f;
            while ((visible ? group.alpha < 0.95f : group.alpha > 0f) && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(visible ? group.alpha >= 0.95f : group.alpha == 0f, Is.True, "Thought visibility timed out.");
        }

        private void Set(string field, object value) => typeof(RunnerMenuThought).GetField(field, Private).SetValue(thought, value);
    }
}
