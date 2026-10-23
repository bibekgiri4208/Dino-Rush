using System.Collections;
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
    public sealed class RunnerTapToStartTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private ForestRunnerGame game;
        private RunnerPlayer player;
        private Transform root;
        private bool hadMap, hadDino, hadBest;
        private string previousMap, previousDino;
        private int previousBest;
        private InputSettings.BackgroundBehavior previousBackground;
        private InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;

        [UnitySetUp]
        public IEnumerator Load()
        {
            previousBackground = InputSystem.settings.backgroundBehavior;
            previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            // Batch tests have no focused Game view; route synthetic presses through player buffers.
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            hadMap = PlayerPrefs.HasKey(RunnerMapCatalog.SelectionKey);
            previousMap = PlayerPrefs.GetString(RunnerMapCatalog.SelectionKey);
            hadDino = PlayerPrefs.HasKey(RunnerDinoRoster.SelectionKey);
            previousDino = PlayerPrefs.GetString(RunnerDinoRoster.SelectionKey);
            hadBest = PlayerPrefs.HasKey("DinoRush.Forest.HighScore");
            previousBest = PlayerPrefs.GetInt("DinoRush.Forest.HighScore");
            yield return SceneManager.LoadSceneAsync("ForestRun");
            game = Object.FindAnyObjectByType<ForestRunnerGame>();
            game.enabled = false;
            root = game.transform.Find("Forest runner UI/16 by 9 safe content");
            player = (RunnerPlayer)Get("player");
            foreach (BaseInputModule input in EventSystem.current.GetComponents<BaseInputModule>()) input.enabled = false;
            yield return new WaitForSecondsRealtime(0.35f);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            if (game != null) Object.Destroy(game.gameObject);
            yield return null;
            InputSystem.settings.backgroundBehavior = previousBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
            if (hadMap) PlayerPrefs.SetString(RunnerMapCatalog.SelectionKey, previousMap);
            else PlayerPrefs.DeleteKey(RunnerMapCatalog.SelectionKey);
            if (hadDino) PlayerPrefs.SetString(RunnerDinoRoster.SelectionKey, previousDino);
            else PlayerPrefs.DeleteKey(RunnerDinoRoster.SelectionKey);
            if (hadBest) PlayerPrefs.SetInt("DinoRush.Forest.HighScore", previousBest);
            else PlayerPrefs.DeleteKey("DinoRush.Forest.HighScore");
            PlayerPrefs.Save();
        }

        [UnityTest]
        public IEnumerator EveryMapWaitsWithoutScrollingScoringOrSpawningUntilAFreshTap()
        {
            game.SelectDino(2);
            for (int map = 0; map < game.MapCatalog.Count; map++)
            {
                game.SelectMap(map);
                Click("Forest Main Menu/Main menu buttons/Play");
                Assert.That(root.Find("Forest Main Menu/Map selector/Map selection panel/Start selected map/Label").GetComponent<Text>().text,
                    Is.EqualTo("CONTINUE"));
                Click("Forest Main Menu/Map selector/Map selection panel/Start selected map");
                Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.AwaitingStart));
                Invoke("PointerAction", new Vector2(Screen.width / 2f, Screen.height / 2f));
                Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.AwaitingStart), "The map confirmation must not click through.");
                Transform tile = game.transform.Find("Forest environment").GetComponentInChildren<SpriteRenderer>().transform;
                Vector3 position = tile.position;
                game.enabled = true;
                yield return new WaitForSecondsRealtime(0.35f);
                game.enabled = false;
                for (int i = 0; i < 20; i++) Invoke("Step", RunnerRules.SimulationStep);
                Assert.That(tile.position, Is.EqualTo(position), "The course must stay still while waiting.");
                Assert.That(game.Score, Is.Zero);
                Assert.That(Get("elapsed"), Is.EqualTo(0f));
                Assert.That(game.Lives, Is.EqualTo(5));
                Assert.That(player.Grounded, Is.True);
                Assert.That(game.SelectedMapIndex, Is.EqualTo(map));
                Assert.That(game.SelectedDinoIndex, Is.EqualTo(2));
                Assert.That(root.Find("Tap to start/Start title").GetComponent<Text>().text, Is.EqualTo("TAP TO START"));
                Assert.That(root.Find("Tap to start/Selected map").GetComponent<Text>().text,
                    Is.EqualTo(game.SelectedMapName.ToUpperInvariant()));
                foreach (string name in new[] { "Forest Main Menu", "Menu scrim", "Menu card", "Jump", "Duck", "Pause", "Scoreboard", "Life hearts", "Stage progress" })
                    Assert.That(root.Find(name).gameObject.activeInHierarchy, Is.False, name);
                Assert.That(game.transform.Find("Obstacle pool").GetComponentsInChildren<SpriteRenderer>(), Is.Empty);
                Assert.That(game.transform.Find("Forest effects/Trail dust pool").GetComponentsInChildren<SpriteRenderer>(), Is.Empty);
                foreach (Graphic graphic in root.Find("Tap to start").GetComponentsInChildren<Graphic>())
                    Assert.That(graphic.raycastTarget, Is.False);
                yield return ForestRunTests.Capture("tap-to-start-" + game.MapCatalog[map].id);
                if (map == 0)
                {
                    yield return ForestRunTests.Capture("tap-to-start-portrait", 360, 800);
                    yield return ForestRunTests.Capture("tap-to-start-16-by-10", 1280, 800);
                }
                object director = Get("encounterDirector");
                Invoke("PointerAction", new Vector2(Screen.width / 2f, Screen.height / 2f));
                Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Running));
                Assert.That(Get("encounterDirector"), Is.SameAs(director), "Starting should use the prepared course, not reset it twice.");
                Assert.That(root.Find("Tap to start").gameObject.activeInHierarchy, Is.False);
                Invoke("Step", RunnerRules.SimulationStep);
                Assert.That(player.Grounded, Is.True, "The starting tap must not also jump.");
                Invoke("PointerAction", new Vector2(Screen.width / 2f, Screen.height / 2f));
                Invoke("Step", RunnerRules.SimulationStep);
                Assert.That(player.Grounded, Is.False, "Subsequent taps still jump.");
                game.ReturnToMenu();
                yield return new WaitForSecondsRealtime(0.35f);
            }
        }

        [UnityTest]
        public IEnumerator MouseTouchAndKeyboardCanStartIncludingOutsideTheWorldViewport()
        {
            Mouse previousMouse = Mouse.current;
            Touchscreen previousTouch = Touchscreen.current;
            Keyboard previousKeyboard = Keyboard.current;
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            Touchscreen touch = InputSystem.AddDevice<Touchscreen>();
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            // Register edge tracking before injecting the first synthetic button press.
            _ = mouse.leftButton.wasPressedThisFrame;
            _ = touch.primaryTouch.press.wasPressedThisFrame;
            _ = keyboard.spaceKey.wasPressedThisFrame;
            try
            {
                for (int mode = 0; mode < 3; mode++)
                {
                    game.PrepareRun();
                    yield return new WaitForSecondsRealtime(0.05f);
                    Assert.That(Time.frameCount, Is.GreaterThan((int)Get("preparedInputFrame")));
                    Rect original = Camera.main.rect;
                    Camera.main.rect = new Rect(0.2f, 0.25f, 0.6f, 0.5f);
                    Vector2 margin = new Vector2(Screen.width * 0.05f, Screen.height * 0.05f);
                    Assert.That(Camera.main.pixelRect.Contains(margin), Is.False);
                    if (mode == 0) InputSystem.QueueStateEvent(mouse, new MouseState { position = margin, buttons = 1 });
                    if (mode == 1) InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Began, position = margin });
                    if (mode == 2) InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
                    InputSystem.Update();
                    if (mode == 0) Assert.That(mouse.leftButton.wasPressedThisFrame, Is.True);
                    if (mode == 1) Assert.That(touch.primaryTouch.press.wasPressedThisFrame, Is.True);
                    if (mode == 2) Assert.That(keyboard.spaceKey.wasPressedThisFrame, Is.True);
                    Invoke("ReadInput");
                    Camera.main.rect = original;
                    Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Running), "Input mode " + mode);
                    Invoke("Step", RunnerRules.SimulationStep);
                    Assert.That(player.Grounded, Is.True);
                    InputSystem.QueueStateEvent(mouse, new MouseState());
                    InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = margin });
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    InputSystem.Update();
                    game.ReturnToMenu();
                }
            }
            finally
            {
                InputSystem.RemoveDevice(mouse);
                InputSystem.RemoveDevice(touch);
                InputSystem.RemoveDevice(keyboard);
                if (previousMouse != null && previousMouse.added) previousMouse.MakeCurrent();
                if (previousTouch != null && previousTouch.added) previousTouch.MakeCurrent();
                if (previousKeyboard != null && previousKeyboard.added) previousKeyboard.MakeCurrent();
            }
        }

        [UnityTest]
        public IEnumerator EscapeCancelsTheWaitingScreenWithoutLosingMapOrDinoSelection()
        {
            game.SelectMap(3);
            game.SelectDino(1);
            game.PrepareRun();
            yield return new WaitForSecondsRealtime(0.05f);
            game.Jump();
            game.SetDuckHeld(true);
            game.TogglePause();
            game.RestartRun();
            Invoke("OnApplicationFocus", false);
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.AwaitingStart));
            Assert.That(player.IsDucking, Is.False);
            Keyboard previous = Keyboard.current;
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            _ = keyboard.escapeKey.wasPressedThisFrame;
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                InputSystem.Update();
                Assert.That(keyboard.escapeKey.wasPressedThisFrame, Is.True);
                Invoke("ReadInput");
                Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Ready));
                Assert.That(game.SelectedMapIndex, Is.EqualTo(3));
                Assert.That(game.SelectedDinoIndex, Is.EqualTo(1));
                Assert.That(root.Find("Tap to start").gameObject.activeInHierarchy, Is.False);
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                if (previous != null && previous.added) previous.MakeCurrent();
            }
        }

        private object Get(string field) => typeof(ForestRunnerGame).GetField(field, Private).GetValue(game);
        private void Invoke(string method, params object[] args) => typeof(ForestRunnerGame).GetMethod(method, Private).Invoke(game, args);
        private void Click(string path)
        {
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(root.Find(path).gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }
    }
}
