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
        private bool hadWallet;
        private int previousWallet;
        private bool hadMissions;
        private string previousMissions;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnitySetUp]
        public IEnumerator LoadForest()
        {
            hadMissions = PlayerPrefs.HasKey(RunnerDailyMissions.SaveKey);
            previousMissions = PlayerPrefs.GetString(RunnerDailyMissions.SaveKey);
            hadWallet = PlayerPrefs.HasKey(RunnerShopProgress.WalletKey);
            previousWallet = PlayerPrefs.GetInt(RunnerShopProgress.WalletKey);
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
            if (hadWallet) PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, previousWallet);
            else PlayerPrefs.DeleteKey(RunnerShopProgress.WalletKey);
            if (hadMissions) PlayerPrefs.SetString(RunnerDailyMissions.SaveKey, previousMissions);
            else PlayerPrefs.DeleteKey(RunnerDailyMissions.SaveKey);
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

        [UnityTest]
        public IEnumerator DoubleJumpUsesKeyboardAndWorldTapsAndResetsOnlyOnLandingOrRestart()
        {
            InputSettings.BackgroundBehavior previousBackground = InputSystem.settings.backgroundBehavior;
            InputSettings.EditorInputBehaviorInPlayMode previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            Keyboard original = Keyboard.current;
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            _ = keyboard.spaceKey.wasPressedThisFrame;
            try
            {
                SetKeys(keyboard, Key.Space);
                Step(24);
                Assert.That(player.JumpsRemaining, Is.EqualTo(1));
                Assert.That(WindBurst.ActiveCount, Is.Zero, "A ground jump must not create an air burst.");
                SetKeys(keyboard); // The air jump requires a fresh press, not a held button.
                game.TogglePause();
                float pausedHeight = player.Feet.y;
                game.Jump();
                yield return null;
                Assert.That(player.Feet.y, Is.EqualTo(pausedHeight));
                Assert.That(player.JumpsRemaining, Is.EqualTo(1));
                game.TogglePause();

                int dirtCursor = (int)typeof(ForestRunnerGame).GetField("nextDirtParticle", Private).GetValue(game);
                Vector2 point = Camera.main.WorldToScreenPoint(Vector3.zero);
                Invoke("PointerAction", point);
                Step(1);
                Assert.That(player.JumpsRemaining, Is.Zero);
                Assert.That(WindBurst.ActiveCount, Is.EqualTo(1));
                Assert.That(player.Feet.y, Is.GreaterThan(pausedHeight));
                Assert.That((int)typeof(ForestRunnerGame).GetField("nextDirtParticle", Private).GetValue(game),
                    Is.EqualTo(dirtCursor), "The air jump must not emit ground dirt in midair.");
                player.Render(0f, true, false, 1f);
                Assert.That(game.transform.Find("Player/Blue dino player").GetComponent<SpriteRenderer>().sprite.name,
                    Is.EqualTo(game.SelectedDinoName + " dino frame 11"));

                SetKeys(keyboard, Key.Space);
                Step(1);
                Assert.That(player.JumpsRemaining, Is.Zero, "A third press cannot jump again in midair.");
                Assert.That(WindBurst.ActiveCount, Is.EqualTo(1), "A rejected third jump must not create another burst.");
                SetKeys(keyboard);
                Step(180);
                Assert.That(player.Grounded, Is.True);
                Assert.That(player.JumpsRemaining, Is.EqualTo(2));
                game.Jump();
                Step(1);
                game.Jump();
                Step(1);
                Assert.That(player.JumpsRemaining, Is.Zero);
                game.RestartRun();
                Assert.That(player.Grounded, Is.True);
                Assert.That(player.JumpsRemaining, Is.EqualTo(2));
                game.ReturnToMenu();
                game.Jump();
                Assert.That(player.JumpsRemaining, Is.EqualTo(2), "Menu input must not consume a jump.");
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                if (original != null && original.added) original.MakeCurrent();
                InputSystem.settings.backgroundBehavior = previousBackground;
                InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
            }
        }

        private RunnerDoubleJumpBurst WindBurst => (RunnerDoubleJumpBurst)typeof(ForestRunnerGame)
            .GetField("doubleJumpBurst", Private).GetValue(game);

        [UnityTest]
        public IEnumerator DoubleJumpWindStaysCompactNearFeetFreezesOnPauseAndClearsOnTransitions()
        {
            game.Jump();
            Step(24);
            game.Jump();
            Step(1);
            Assert.That(WindBurst.ActiveCount, Is.EqualTo(1));
            Transform pool = game.transform.Find("Double jump wind bursts");
            var parts = pool.GetComponentsInChildren<SpriteRenderer>(true);
            int capacity = parts.Length;
            SpriteRenderer ring = parts[0];
            Vector3 initialPosition = ring.transform.position;
            float initialWidth = ring.transform.localScale.x;
            float initialAlpha = ring.color.a;
            Sprite initialFrame = ring.sprite;
            Assert.That(parts[1].gameObject.activeSelf, Is.False, "The secondary crest should arrive after the first.");
            Assert.That(initialPosition.x, Is.EqualTo(player.Feet.x));
            Assert.That(initialPosition.y, Is.EqualTo(player.Feet.y + 0.06f).Within(0.001f));
            Assert.That(initialWidth, Is.LessThan(0.9f));
            var dino = game.transform.Find("Player/Blue dino player").GetComponent<SpriteRenderer>();
            foreach (SpriteRenderer part in parts)
            {
                Assert.That(part.sortingOrder, Is.LessThan(dino.sortingOrder));
                Assert.That(part.sprite.texture.filterMode, Is.EqualTo(FilterMode.Point));
            }
            Step(12);
            player.Render(0f, true, false, 1f);
            Assert.That(ring.transform.localScale.x, Is.GreaterThan(initialWidth));
            Assert.That(ring.color.a, Is.LessThan(initialAlpha));
            Assert.That(ring.color.a, Is.GreaterThan(0.5f), "The shockwave crest should stay readable through the initial expansion.");
            Assert.That(ring.sprite, Is.Not.EqualTo(initialFrame), "The crest must curl and break up, not just scale a fixed sprite.");
            Assert.That(parts[1].gameObject.activeSelf, Is.True);
            Assert.That(ring.transform.position.y, Is.GreaterThan(initialPosition.y));
            Assert.That(player.Feet.y - ring.transform.position.y, Is.InRange(0.1f, 0.6f), "The wave stays near the feet but has independent motion.");
            Assert.That(ring.transform.localScale.x, Is.LessThan(2.25f));
            yield return RunnerDisplayCapture.Capture("double-jump-wind-burst", 1280, 720);
            yield return RunnerDisplayCapture.Capture("double-jump-wind-burst-phone", 360, 800);

            game.TogglePause();
            Vector3 pausedScale = ring.transform.localScale;
            Color pausedColor = ring.color;
            Sprite pausedFrame = ring.sprite;
            Vector3 pausedPosition = ring.transform.position;
            game.Jump();
            Step(60);
            yield return null;
            Assert.That(ring.transform.localScale, Is.EqualTo(pausedScale));
            Assert.That(ring.color, Is.EqualTo(pausedColor));
            Assert.That(ring.sprite, Is.EqualTo(pausedFrame));
            Assert.That(ring.transform.position, Is.EqualTo(pausedPosition));
            Assert.That(WindBurst.ActiveCount, Is.EqualTo(1));
            game.TogglePause();
            Step(18);
            Assert.That(ring.gameObject.activeSelf, Is.True);
            Assert.That(Mathf.Abs(player.Feet.y - ring.transform.position.y), Is.LessThan(0.6f));
            Assert.That(ring.sprite, Is.Not.EqualTo(pausedFrame));
            player.Render(0f, true, false, 1f);
            yield return RunnerDisplayCapture.Capture("double-jump-wind-breakup", 1280, 720);
            Assert.That(ring.transform.localScale.x, Is.LessThanOrEqualTo(2.25f));
            Step(60);
            Assert.That(WindBurst.ActiveCount, Is.Zero);
            foreach (SpriteRenderer part in parts) Assert.That(part.gameObject.activeSelf, Is.False);

            for (int transition = 0; transition < 3; transition++)
            {
                game.RestartRun();
                typeof(ForestRunnerGame).GetField("spawnTimer", Private).SetValue(game, float.MaxValue);
                game.Jump(); Step(1);
                game.Jump(); Step(1);
                Assert.That(WindBurst.ActiveCount, Is.EqualTo(1));
                if (transition == 0) game.RestartRun();
                else if (transition == 1) Invoke("EndRun");
                else game.ReturnToMenu();
                Assert.That(WindBurst.ActiveCount, Is.Zero);
                Assert.That(pool.GetComponentsInChildren<SpriteRenderer>(true).Length, Is.EqualTo(capacity));
            }
        }

        [UnityTest]
        public IEnumerator WindWispsCurlStretchAndDissolveEvenWhenTheFeetStayStill()
        {
            WindBurst.Emit(player.Feet);
            WindBurst.Tick(0.04f, player.Feet);
            var parts = game.transform.Find("Double jump wind bursts").GetComponentsInChildren<SpriteRenderer>(true);
            SpriteRenderer wisp = parts[2];
            Vector3 position = wisp.transform.position, scale = wisp.transform.localScale;
            Quaternion rotation = wisp.transform.rotation;
            Sprite frame = parts[0].sprite;
            WindBurst.Tick(0.06f, player.Feet);
            Assert.That(wisp.transform.position, Is.Not.EqualTo(position));
            Assert.That(wisp.transform.localScale, Is.Not.EqualTo(scale));
            Assert.That(Quaternion.Angle(wisp.transform.rotation, rotation), Is.GreaterThan(10f));
            Assert.That(parts[0].sprite, Is.Not.EqualTo(frame));
            WindBurst.Tick(0.2f, player.Feet);
            Assert.That(wisp.gameObject.activeSelf, Is.False, "Wisps should dissipate before the final ring finishes.");
            WindBurst.Tick(RunnerDoubleJumpBurst.Duration, player.Feet);
            Assert.That(WindBurst.ActiveCount, Is.Zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator WindBurstPoolIsBoundedIndependentOfRandomAndDisposesGeneratedArt()
        {
            Transform pool = game.transform.Find("Double jump wind bursts");
            var parts = pool.GetComponentsInChildren<SpriteRenderer>(true);
            Sprite ring = parts[0].sprite, wisp = parts[2].sprite;
            Texture2D ringTexture = ring.texture, wispTexture = wisp.texture;
            int crestPixels = 0, cyanPixels = 0, contourPixels = 0;
            foreach (Color32 pixel in ringTexture.GetPixels32())
            {
                if (pixel.a == 255 && pixel.r > 230) crestPixels++;
                if (pixel.a == 255 && pixel.r == 100) cyanPixels++;
                if (pixel.a > 0 && pixel.a < 255) contourPixels++;
            }
            Assert.That(crestPixels, Is.GreaterThan(400), "The white crest must be a substantial band, not a hairline ring.");
            Assert.That(cyanPixels, Is.GreaterThan(100));
            Assert.That(contourPixels, Is.GreaterThan(100), "A narrow contour preserves definition on bright backdrops.");
            Assert.That(ringTexture.GetPixel(ringTexture.width / 2, ringTexture.height / 2).a, Is.Zero,
                "The center stays clear so the effect reads as a shockwave rather than a solid disc.");
            Random.State state = Random.state;
            float expected = Random.value;
            Random.state = state;
            for (int i = 0; i < 30; i++) WindBurst.Emit(new Vector3(i, i * 0.1f, 0));
            WindBurst.Tick(0.1f, player.Feet);
            Assert.That(Random.value, Is.EqualTo(expected));
            Random.state = state;
            Assert.That(WindBurst.ActiveCount, Is.EqualTo(3));
            Assert.That(pool.GetComponentsInChildren<SpriteRenderer>(true).Length, Is.EqualTo(parts.Length));
            WindBurst.Tick(RunnerDoubleJumpBurst.Duration, player.Feet);
            Assert.That(WindBurst.ActiveCount, Is.Zero);
            Object.Destroy(game.gameObject);
            yield return null;
            Assert.That(ring == null && wisp == null && ringTexture == null && wispTexture == null, Is.True);
        }

        private void SetKeys(Keyboard keyboard, params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
            InputSystem.Update();
            Invoke("ReadInput");
        }

        [UnityTest]
        public IEnumerator TouchSwipeDucksWithoutAButtonOrAccidentalJumpAndTapStillJumps()
        {
            Touchscreen original = Touchscreen.current;
            var background = InputSystem.settings.backgroundBehavior;
            var editor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            Touchscreen screen = InputSystem.AddDevice<Touchscreen>();
            InputSystem.EnableDevice(screen);
            screen.MakeCurrent();
            try
            {
                Vector2 point = Camera.main.WorldToScreenPoint(Vector3.zero);
                Vector2 down = point - Vector2.up * Mathf.Max(60, Mathf.Min(Screen.width, Screen.height) * 0.2f);
                Touch(UnityEngine.InputSystem.TouchPhase.Began, point);
                Assert.That(Touchscreen.current, Is.EqualTo(screen));
                Assert.That(screen.primaryTouch.press.isPressed, Is.True, "Synthetic touch-down must reach the primary touch: enabled=" + screen.enabled + ", primary=" + screen.primaryTouch.phase.ReadValue() + ", finger=" + screen.touches[0].phase.ReadValue());
                Assert.That((bool)typeof(ForestRunnerGame).GetField("touchGestureActive", Private).GetValue(game), Is.True,
                    "Gesture must start on the playfield: " + point + " / " + Camera.main.pixelRect + " / " + screen.primaryTouch.phase.ReadValue());
                Step(1);
                Assert.That(player.Grounded, Is.True, "A potential swipe must not consume a jump on touch-down.");
                Touch(UnityEngine.InputSystem.TouchPhase.Moved, down);
                Assert.That(screen.primaryTouch.position.ReadValue().y, Is.EqualTo(down.y));
                Step(1);
                Assert.That(player.IsDucking, Is.True);
                Assert.That(player.Grounded, Is.True);
                AssertMovementButtonsHidden();
                Touch(UnityEngine.InputSystem.TouchPhase.Ended, down);
                Step(1);
                Assert.That(player.IsDucking, Is.False);
                Assert.That(player.Grounded, Is.True, "Finishing a duck gesture must not jump.");
                Touch(UnityEngine.InputSystem.TouchPhase.Began, point);
                Touch(UnityEngine.InputSystem.TouchPhase.Moved, down);
                Assert.That(player.IsDucking, Is.True);
                game.TogglePause();
                Assert.That(player.IsDucking, Is.False);
                Touch(UnityEngine.InputSystem.TouchPhase.Ended, down);
                game.TogglePause();
                Touch(UnityEngine.InputSystem.TouchPhase.Began, point);
                Touch(UnityEngine.InputSystem.TouchPhase.Canceled, point);
                Step(1);
                Assert.That(player.Grounded, Is.True);
                Touch(UnityEngine.InputSystem.TouchPhase.Began, point);
                Touch(UnityEngine.InputSystem.TouchPhase.Ended, point);
                Step(1);
                Assert.That(player.Grounded, Is.False, "A normal world tap must still jump.");
                Assert.That(player.JumpsRemaining, Is.EqualTo(1));
            }
            finally
            {
                InputSystem.RemoveDevice(screen);
                if (original != null && original.added) original.MakeCurrent();
                InputSystem.settings.backgroundBehavior = background;
                InputSystem.settings.editorInputBehaviorInPlayMode = editor;
            }
            yield return null;

            void Touch(UnityEngine.InputSystem.TouchPhase phase, Vector2 position)
            {
                InputSystem.QueueStateEvent(screen, new TouchState { touchId = 1, phase = phase, position = position });
                InputSystem.Update();
                Invoke("ReadInput");
            }
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
