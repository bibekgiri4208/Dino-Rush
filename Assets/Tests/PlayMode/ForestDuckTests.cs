using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DinoRush.Tests
{
    public sealed class ForestDuckTests
    {
        private ForestRunnerGame game;
        private RunnerPlayer player;
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
        public IEnumerator DuckFastDropsAndTheFormerButtonAreaNowAllowsWorldJumps()
        {
            game.Jump();
            Step(30);
            Assert.That(player.Grounded, Is.False);
            float before = player.Feet.y;
            game.SetDuckHeld(true);
            Assert.That(player.IsDucking, Is.True);
            player.Render(0f, true, false, 1f);
            yield return ForestRunTests.Capture("forest-duck-midair");
            game.SetDuckHeld(false);
            Step(1);
            Assert.That(player.Feet.y, Is.LessThan(before));
            Step(24);
            Assert.That(player.Grounded, Is.True);
            Assert.That(player.IsDucking, Is.False);
            Canvas.ForceUpdateCanvases();
            var formerButton = (RectTransform)game.transform.Find("Forest runner UI/16 by 9 safe content/Duck");
            Vector2 point = RectTransformUtility.WorldToScreenPoint(null, formerButton.TransformPoint(formerButton.rect.center));
            Invoke("PointerAction", point);
            Step(1);
            Assert.That(player.Grounded, Is.False, "The removed Duck button must not intercept tap-to-jump.");
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Running));
        }

        [UnityTest]
        public IEnumerator GroundDuckReleasesOnPauseFocusMenusAndResultsWithoutVisibleControls()
        {
            game.SetDuckHeld(true);
            Assert.That(player.IsDucking, Is.True);
            player.Render(0.1f, true, false, 1f);
            Assert.That(player.Hitbox.height, Is.EqualTo(RunnerRules.DuckHeight));
            Assert.That(game.transform.Find("Player/Blue dino player").GetComponent<SpriteRenderer>().sprite.name,
                Does.Match(game.SelectedDinoName + " dino frame (18|19|20|21|22|23)"));
            yield return ForestRunTests.Capture("forest-duck-ground");
            yield return ForestRunTests.Capture("forest-duck-tablet", 1024, 768);
            yield return ForestRunTests.Capture("forest-duck-wide", 1920, 820);
            game.TogglePause();
            AssertMovementButtonsHidden();
            Assert.That(player.IsDucking, Is.False);
            game.SetDuckHeld(true);
            Assert.That(player.IsDucking, Is.False);
            game.TogglePause();
            AssertMovementButtonsHidden();
            game.SetDuckHeld(true);
            Assert.That(player.IsDucking, Is.True);
            game.SetDuckHeld(false);
            Assert.That(player.IsDucking, Is.False);
            game.SetDuckHeld(true);
            Invoke("OnApplicationFocus", false);
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Paused));
            Assert.That(player.IsDucking, Is.False);
            game.RestartRun();
            Assert.That(player.IsDucking, Is.False);
            game.SetDuckHeld(true);
            game.ReturnToMenu();
            AssertMovementButtonsHidden();
            Assert.That(player.IsDucking, Is.False);
            game.StartRun();
            game.SetDuckHeld(true);
            Invoke("EndRun");
            AssertMovementButtonsHidden();
            Assert.That(player.IsDucking, Is.False);
        }

        [UnityTest]
        public IEnumerator DownAndSDuckAndPausePreservesAnInProgressDrop()
        {
            Keyboard original = Keyboard.current;
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                SetKeys(keyboard, Key.DownArrow);
                Assert.That(player.IsDucking, Is.True);
                SetKeys(keyboard, Key.S);
                Assert.That(player.IsDucking, Is.True);
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

        private void AssertMovementButtonsHidden()
        {
            Transform root = game.transform.Find("Forest runner UI/16 by 9 safe content");
            Assert.That(root.Find("Jump").gameObject.activeInHierarchy, Is.False);
            Assert.That(root.Find("Duck").gameObject.activeInHierarchy, Is.False);
        }
    }
}
