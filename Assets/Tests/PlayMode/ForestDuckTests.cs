using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DinoRush.Tests
{
    public sealed class ForestDuckTests
    {
        private ForestRunnerGame game;
        private RunnerPlayer player;
        private GameObject duck;
        private bool hadBest, hadSound;
        private int previousBest, previousSound;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnitySetUp]
        public IEnumerator LoadForest()
        {
            hadBest = PlayerPrefs.HasKey("DinoRush.Forest.HighScore");
            previousBest = PlayerPrefs.GetInt("DinoRush.Forest.HighScore");
            hadSound = PlayerPrefs.HasKey("DinoRush.Muted");
            previousSound = PlayerPrefs.GetInt("DinoRush.Muted");
            yield return SceneManager.LoadSceneAsync("ForestRun");
            game = Object.FindAnyObjectByType<ForestRunnerGame>();
            game.StartRun();
            game.enabled = false; // Exercise the real simulation without wall-clock/shader timing.
            player = (RunnerPlayer)typeof(ForestRunnerGame).GetField("player", Private).GetValue(game);
            duck = game.transform.Find("Forest runner UI/16 by 9 safe content/Duck").gameObject;
            typeof(ForestRunnerGame).GetField("spawnTimer", Private).SetValue(game, float.MaxValue);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator RestorePrefs()
        {
            if (game != null) Object.Destroy(game.gameObject);
            yield return null;
            if (hadBest) PlayerPrefs.SetInt("DinoRush.Forest.HighScore", previousBest);
            else PlayerPrefs.DeleteKey("DinoRush.Forest.HighScore");
            if (hadSound) PlayerPrefs.SetInt("DinoRush.Muted", previousSound);
            else PlayerPrefs.DeleteKey("DinoRush.Muted");
            PlayerPrefs.Save();
        }

        [UnityTest]
        public IEnumerator TouchDuckFastDropsAndDoesNotTriggerTheWorldJumpShortcut()
        {
            game.Jump();
            Step(30);
            Assert.That(player.Grounded, Is.False);
            float before = player.Feet.y;
            PointerEventData pointer = Pointer(4);
            Press(pointer);
            Assert.That(player.IsDucking, Is.True);
            player.Render(0f, true, false, 1f);
            yield return ForestRunTests.Capture("forest-duck-midair");
            Release(pointer);
            Invoke("PointerAction", pointer.position);
            Step(1);
            Assert.That(player.Feet.y, Is.LessThan(before));
            Step(24);
            Assert.That(player.Grounded, Is.True);
            Assert.That(player.IsDucking, Is.False);
            Invoke("PointerAction", pointer.position);
            Step(1);
            Assert.That(player.Grounded, Is.True, "Pressing Duck must not also queue the tap-anywhere jump.");
            game.Jump();
            Step(1);
            Assert.That(player.Grounded, Is.False);
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Running));
        }

        [UnityTest]
        public IEnumerator GroundDuckSupportsMultipleTouchesAndHidesAndReleasesOnPauseMenusAndResults()
        {
            PointerEventData first = Pointer(4), second = Pointer(5);
            Press(first);
            Press(second);
            Release(first);
            Assert.That(player.IsDucking, Is.True, "The other finger is still holding Duck.");
            player.Render(0.1f, true, false, 1f);
            Assert.That(player.Hitbox.height, Is.EqualTo(RunnerRules.DuckHeight));
            Assert.That(game.transform.Find("Player/Blue dino player").GetComponent<SpriteRenderer>().sprite.name,
                Does.Match("Blue dino frame (18|19|20|21|22|23)"));
            yield return ForestRunTests.Capture("forest-duck-ground");
            yield return ForestRunTests.Capture("forest-duck-tablet", 1024, 768);
            yield return ForestRunTests.Capture("forest-duck-wide", 1920, 820);
            game.TogglePause();
            Assert.That(duck.activeInHierarchy, Is.False);
            Assert.That(player.IsDucking, Is.False);
            game.SetDuckHeld(true);
            Assert.That(player.IsDucking, Is.False);
            game.TogglePause();
            Assert.That(duck.activeInHierarchy, Is.True);
            Press(first);
            Assert.That(player.IsDucking, Is.True);
            Release(first);
            Assert.That(player.IsDucking, Is.False, "Hiding the control must clear every old pointer.");
            Press(first);
            Invoke("OnApplicationFocus", false);
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Paused));
            Assert.That(player.IsDucking, Is.False);
            game.RestartRun();
            Assert.That(player.IsDucking, Is.False);
            Press(first);
            game.ReturnToMenu();
            Assert.That(duck.activeInHierarchy, Is.False);
            Assert.That(player.IsDucking, Is.False);
            game.StartRun();
            Press(first);
            Invoke("EndRun");
            Assert.That(duck.activeInHierarchy, Is.False);
            Assert.That(player.IsDucking, Is.False);
        }

        [UnityTest]
        public IEnumerator DownAndSCombineWithTouchAndPausePreservesAnInProgressDrop()
        {
            Keyboard original = Keyboard.current;
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                SetKeys(keyboard, Key.DownArrow);
                Assert.That(player.IsDucking, Is.True);
                PointerEventData pointer = Pointer(8);
                Press(pointer);
                SetKeys(keyboard);
                Assert.That(player.IsDucking, Is.True, "Touch remains held after releasing the keyboard.");
                SetKeys(keyboard, Key.S);
                Release(pointer);
                Assert.That(player.IsDucking, Is.True, "Keyboard remains held after releasing touch.");
                SetKeys(keyboard, Key.S, Key.DownArrow);
                SetKeys(keyboard, Key.DownArrow);
                Assert.That(player.IsDucking, Is.True);
                SetKeys(keyboard);
                Assert.That(player.IsDucking, Is.False);
                game.Jump();
                Step(24);
                SetKeys(keyboard, Key.S);
                SetKeys(keyboard);
                float height = player.Feet.y;
                game.TogglePause();
                yield return new WaitForSeconds(0.05f);
                Assert.That(player.Feet.y, Is.EqualTo(height));
                Assert.That(player.IsDucking, Is.True, "A committed drop resumes after pausing.");
                game.TogglePause();
                Step(24);
                Assert.That(player.Grounded, Is.True);
                Assert.That(player.IsDucking, Is.False);
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                if (original != null && original.added) original.MakeCurrent();
            }
        }

        private void SetKeys(Keyboard keyboard, params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
            InputSystem.Update();
            Invoke("ReadInput");
        }

        private void Step(int count)
        {
            for (int i = 0; i < count; i++) Invoke("Step", RunnerRules.SimulationStep);
        }

        private void Invoke(string method, params object[] args) =>
            typeof(ForestRunnerGame).GetMethod(method, Private).Invoke(game, args);

        private PointerEventData Pointer(int id)
        {
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)duck.transform;
            var pointer = new PointerEventData(EventSystem.current)
            {
                pointerId = id,
                position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)),
                button = PointerEventData.InputButton.Left
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits.Count, Is.GreaterThan(0));
            Assert.That(hits[0].gameObject, Is.EqualTo(duck), "Duck must not be blocked by decorative UI.");
            return pointer;
        }

        private void Press(PointerEventData pointer) => ExecuteEvents.Execute(duck, pointer, ExecuteEvents.pointerDownHandler);
        private void Release(PointerEventData pointer) => ExecuteEvents.Execute(duck, pointer, ExecuteEvents.pointerUpHandler);
    }
}
