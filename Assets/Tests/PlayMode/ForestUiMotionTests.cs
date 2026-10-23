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
    public sealed class ForestUiMotionTests
    {
        private ForestRunnerGame game;
        private Transform root;
        private float previousTimeScale;
        private bool hadSound, hadBest;
        private int previousSound, previousBest;

        [UnitySetUp]
        public IEnumerator Load()
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 1f;
            hadSound = PlayerPrefs.HasKey("DinoRush.Muted");
            previousSound = PlayerPrefs.GetInt("DinoRush.Muted");
            hadBest = PlayerPrefs.HasKey("DinoRush.Forest.HighScore");
            previousBest = PlayerPrefs.GetInt("DinoRush.Forest.HighScore");
            yield return SceneManager.LoadSceneAsync("ForestRun");
            game = Object.FindAnyObjectByType<ForestRunnerGame>();
            game.enabled = false;
            root = game.transform.Find("Forest runner UI/16 by 9 safe content");
            // Drive deterministic pointer events without the real desktop cursor entering test controls.
            foreach (BaseInputModule input in EventSystem.current.GetComponents<BaseInputModule>()) input.enabled = false;
            yield return new WaitForSecondsRealtime(0.4f);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = previousTimeScale;
            if (game != null) Object.Destroy(game.gameObject);
            yield return null;
            if (hadSound) PlayerPrefs.SetInt("DinoRush.Muted", previousSound);
            else PlayerPrefs.DeleteKey("DinoRush.Muted");
            if (hadBest) PlayerPrefs.SetInt("DinoRush.Forest.HighScore", previousBest);
            else PlayerPrefs.DeleteKey("DinoRush.Forest.HighScore");
            PlayerPrefs.Save();
        }

        [UnityTest]
        public IEnumerator MouseHoverPressAndReleaseAnimateArtworkWithoutMovingTheHitArea()
        {
            Button button = Control("Forest Main Menu/Main menu buttons/Play");
            var feedback = button.GetComponent<RunnerUiButtonFeedback>();
            var rect = (RectTransform)button.transform;
            var label = (RectTransform)rect.Find("Label");
            Vector2 position = rect.anchoredPosition, size = rect.sizeDelta, labelPosition = label.anchoredPosition;
            Vector3 scale = rect.localScale, labelScale = label.localScale;
            PointerEventData mouse = Pointer(-1);
            ExecuteEvents.Execute(button.gameObject, mouse, ExecuteEvents.pointerEnterHandler);
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(feedback.IsHovered, Is.True);
            Assert.That(label.localScale.x, Is.GreaterThan(labelScale.x));
            Assert.That(label.anchoredPosition.y, Is.GreaterThan(labelPosition.y));
            Assert.That(button.GetComponent<Outline>().enabled, Is.True);
            Assert.That(button.GetComponent<Outline>().effectColor.a, Is.GreaterThan(0.1f));
            yield return ForestRunTests.Capture("ui-motion-menu-hover");

            ExecuteEvents.Execute(button.gameObject, mouse, ExecuteEvents.pointerDownHandler);
            yield return new WaitForSecondsRealtime(0.14f);
            Assert.That(feedback.IsPressed, Is.True);
            Assert.That(label.localScale.x, Is.LessThan(labelScale.x));
            Assert.That(label.anchoredPosition.y, Is.LessThan(labelPosition.y));
            Assert.That(rect.anchoredPosition, Is.EqualTo(position));
            Assert.That(rect.sizeDelta, Is.EqualTo(size));
            Assert.That(rect.localScale, Is.EqualTo(scale));
            AssertRaycast(button, new Vector2(rect.rect.xMin + 1f, 0));
            yield return ForestRunTests.Capture("ui-motion-menu-pressed");

            ExecuteEvents.Execute(button.gameObject, mouse, ExecuteEvents.pointerExitHandler);
            Assert.That(feedback.IsHovered, Is.False);
            Assert.That(feedback.IsPressed, Is.True, "Dragging outside must not release a held control.");
            ExecuteEvents.Execute(button.gameObject, mouse, ExecuteEvents.pointerUpHandler);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(feedback.IsPressed, Is.False);
            Assert.That(label.anchoredPosition, Is.EqualTo(labelPosition));
            Assert.That(label.localScale, Is.EqualTo(labelScale));
            Assert.That(button.GetComponent<Outline>().enabled, Is.False);
        }

        [UnityTest]
        public IEnumerator ClickPulseKeepsActionsImmediateAndDisabledButtonsStayQuiet()
        {
            Button sound = Control("Forest Main Menu/SFX toggle");
            var feedback = sound.GetComponent<RunnerUiButtonFeedback>();
            int muted = PlayerPrefs.GetInt("DinoRush.Muted");
            ExecuteEvents.Execute(sound.gameObject, Pointer(-1), ExecuteEvents.pointerClickHandler);
            Assert.That(PlayerPrefs.GetInt("DinoRush.Muted"), Is.EqualTo(1 - muted), "Clicks must not wait for the effect.");
            yield return new WaitForSecondsRealtime(0.08f);
            Assert.That(sound.GetComponent<Outline>().effectColor.a, Is.GreaterThan(0.3f));
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(sound.GetComponent<Outline>().enabled, Is.False);

            sound.interactable = false;
            ExecuteEvents.Execute(sound.gameObject, Pointer(-1), ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(sound.gameObject, Pointer(-1), ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(sound.gameObject, Pointer(-1), ExecuteEvents.pointerClickHandler);
            Assert.That(feedback.IsHovered || feedback.IsPressed, Is.False);
            Assert.That(PlayerPrefs.GetInt("DinoRush.Muted"), Is.EqualTo(1 - muted));
            sound.interactable = true;
            PointerEventData rightClick = Pointer(-2);
            rightClick.button = PointerEventData.InputButton.Right;
            ExecuteEvents.Execute(sound.gameObject, rightClick, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(sound.gameObject, rightClick, ExecuteEvents.pointerClickHandler);
            Assert.That(feedback.IsPressed, Is.False);
            Assert.That(PlayerPrefs.GetInt("DinoRush.Muted"), Is.EqualTo(1 - muted));
            ExecuteEvents.Execute(sound.gameObject, Pointer(-1), ExecuteEvents.pointerEnterHandler);
            yield return null;
            feedback.SendMessage("OnApplicationFocus", false);
            Assert.That(feedback.IsHovered || feedback.IsPressed, Is.False);
            Assert.That(sound.GetComponent<Outline>().enabled, Is.False);

            Button play = Control("Forest Main Menu/Main menu buttons/Play");
            ExecuteEvents.Execute(play.gameObject, Pointer(-1), ExecuteEvents.pointerClickHandler);
            ExecuteEvents.Execute(Control("Forest Main Menu/Menu dialog/Moss framed dialog/Mode selection page/Endless mode").gameObject,
                Pointer(-1), ExecuteEvents.pointerClickHandler);
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Ready));
            yield return null;
            ExecuteEvents.Execute(Control("Forest Main Menu/Map selector/Map selection panel/Start selected map").gameObject,
                Pointer(-1), ExecuteEvents.pointerClickHandler);
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.AwaitingStart));
            game.PrimaryAction();
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Running));
            Assert.That(play.GetComponent<RunnerUiButtonFeedback>().IsPressed, Is.False);
        }

        [UnityTest]
        public IEnumerator RetiredMovementControlsStayHiddenAndDoNotAnimateOnKeyboardInput()
        {
            game.StartRun();
            Button duck = Control("Duck");
            var feedback = duck.GetComponent<RunnerUiButtonFeedback>();
            Assert.That(duck.gameObject.activeInHierarchy, Is.False);
            Assert.That(feedback.IsPressed, Is.False);
            game.SetDuckHeld(true);
            Assert.That(feedback.IsPressed, Is.False, "Retired controls must not react to gameplay input.");
            game.TogglePause();
            Assert.That(feedback.IsHovered || feedback.IsPressed, Is.False);
            game.TogglePause();
            Assert.That(feedback.IsPressed, Is.False);
            game.Jump();
            yield return new WaitForSecondsRealtime(0.08f);
            Assert.That(Control("Jump").gameObject.activeInHierarchy, Is.False);
            Assert.That(duck.gameObject.activeInHierarchy, Is.False);
            game.ReturnToMenu();
            game.StartRun();
            Assert.That(feedback.IsPressed, Is.False);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(Control("Jump").GetComponent<Outline>().enabled, Is.False);
        }

        [UnityTest]
        public IEnumerator ModalEntrancesUseUnscaledTimeAndInterruptedTransitionsNeverDrift()
        {
            game.StartRun();
            Time.timeScale = 0f;
            game.TogglePause();
            Transform pause = root.Find("Forest Pause Menu");
            var reveal = pause.GetComponent<RunnerUiReveal>();
            CanvasGroup group = pause.GetComponent<CanvasGroup>();
            Assert.That(reveal.IsRevealing, Is.True);
            Assert.That(group.alpha, Is.LessThan(1f));
            Assert.That(group.blocksRaycasts && group.interactable, Is.True);
            yield return new WaitForSecondsRealtime(0.32f);
            Assert.That(reveal.IsRevealing, Is.False);
            Assert.That(group.alpha, Is.EqualTo(1f));
            Assert.That(pause.localScale, Is.EqualTo(Vector3.one));
            Assert.That(((RectTransform)pause).anchoredPosition, Is.EqualTo(Vector2.zero));
            AssertRaycast(Control("Forest Pause Menu/Resume run"), Vector2.zero);
            yield return ForestRunTests.Capture("ui-motion-pause");
            game.ReturnToMenu();

            Button options = Control("Forest Main Menu/Main menu buttons/Options");
            Button back = Control("Forest Main Menu/Menu dialog/Moss framed dialog/Back to main menu");
            RectTransform dialog = (RectTransform)back.transform.parent;
            for (int i = 0; i < 6; i++)
            {
                options.onClick.Invoke();
                Assert.That(dialog.GetComponent<RunnerUiReveal>().IsRevealing, Is.True);
                yield return null; // Newly enabled graphics register with the raycaster on the next UI frame.
                AssertRaycast(back, Vector2.zero);
                yield return new WaitForSecondsRealtime(0.025f);
                back.onClick.Invoke();
                Assert.That(dialog.anchoredPosition, Is.EqualTo(Vector2.zero));
                Assert.That(dialog.localScale, Is.EqualTo(Vector3.one));
                Assert.That(dialog.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
            }
            options.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.3f);
            yield return ForestRunTests.Capture("ui-motion-options-tablet", 1024, 768);
            yield return ForestRunTests.Capture("ui-motion-options-wide", 1920, 820);
        }

        [UnityTest]
        public IEnumerator EffectsAreIdempotentAndToastsReplayWithoutChangingGameplay()
        {
            int controls = root.GetComponentsInChildren<Button>(true).Length;
            RunnerUiEffects.Install((RectTransform)root);
            RunnerUiEffects.Install((RectTransform)root);
            Assert.That(root.GetComponentsInChildren<Button>(true).Length, Is.EqualTo(controls));
            Assert.That(root.GetComponentsInChildren<RunnerUiButtonFeedback>(true).Length, Is.EqualTo(controls));
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
                Assert.That(button.GetComponents<Outline>().Length, Is.EqualTo(1), button.name);
            game.StartRun();
            var hud = (RunnerHud)typeof(ForestRunnerGame).GetField("hud", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
            var toast = root.Find("Milestone toast").GetComponent<Text>();
            hud.Toast("LEVEL 02 / KEEP GOING");
            Assert.That(toast.GetComponent<RunnerUiReveal>().IsRevealing, Is.True);
            yield return new WaitForSecondsRealtime(0.1f);
            hud.Toast("LEVEL 03 / NEW CHALLENGE");
            Assert.That(toast.text, Is.EqualTo("LEVEL 03 / NEW CHALLENGE"));
            Assert.That(toast.GetComponent<CanvasGroup>().alpha, Is.LessThan(0.3f));
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(toast.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
            Assert.That(game.Score, Is.Zero);
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Running));
        }

        private Button Control(string path) => root.Find(path).GetComponent<Button>();

        private static PointerEventData Pointer(int id) => new PointerEventData(EventSystem.current)
        {
            pointerId = id,
            button = PointerEventData.InputButton.Left
        };

        private static void AssertRaycast(Button button, Vector2 localPoint)
        {
            Canvas.ForceUpdateCanvases();
            var pointer = Pointer(-1);
            pointer.position = RectTransformUtility.WorldToScreenPoint(null, button.transform.TransformPoint(localPoint));
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits.Count, Is.GreaterThan(0), button.name);
            Assert.That(hits[0].gameObject, Is.EqualTo(button.gameObject), button.name + " is blocked or its target moved");
        }
    }
}
