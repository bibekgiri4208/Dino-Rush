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
    public sealed class RunnerScreenLayoutPlayModeTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private ForestRunnerGame game;
        private RunnerHud hud;
        private RectTransform root;
        private Camera world, uiCamera;
        private Canvas canvas;
        private RenderTexture target;
        private bool hadBest;
        private int oldBest;

        [UnitySetUp]
        public IEnumerator Load()
        {
            hadBest = PlayerPrefs.HasKey("DinoRush.Forest.HighScore");
            oldBest = PlayerPrefs.GetInt("DinoRush.Forest.HighScore");
            yield return SceneManager.LoadSceneAsync("ForestRun");
            game = Object.FindAnyObjectByType<ForestRunnerGame>();
            game.enabled = false;
            hud = (RunnerHud)Get("hud");
            root = (RectTransform)game.transform.Find("Forest runner UI/16 by 9 safe content");
            canvas = game.GetComponentInChildren<Canvas>();
            world = Camera.main;
            uiCamera = new GameObject("Virtual screen UI camera", typeof(Camera)).GetComponent<Camera>();
            uiCamera.enabled = false;
            uiCamera.orthographic = true;
            uiCamera.orthographicSize = 5.625f;
            uiCamera.transform.position = new Vector3(0, 0, -10);
            uiCamera.cullingMask = 1 << 5;
            foreach (BaseInputModule input in EventSystem.current.GetComponents<BaseInputModule>()) input.enabled = false;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = uiCamera;
            canvas.planeDistance = 1f;
            yield return new WaitForSecondsRealtime(0.35f);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            if (world != null) world.targetTexture = null;
            if (uiCamera != null) { uiCamera.targetTexture = null; Object.Destroy(uiCamera.gameObject); }
            if (target != null) { target.Release(); Object.Destroy(target); }
            if (game != null) Object.Destroy(game.gameObject);
            yield return null;
            if (hadBest) PlayerPrefs.SetInt("DinoRush.Forest.HighScore", oldBest);
            else PlayerPrefs.DeleteKey("DinoRush.Forest.HighScore");
            PlayerPrefs.Save();
        }

        [UnityTest]
        public IEnumerator PortraitPhonesReflowMenusMapCardsHudAndTouchControlsIntoTheSafeArea()
        {
            var cases = new[] {
                new Rect(0, 0, 320, 568), new Rect(0, 24, 360, 752), new Rect(0, 100, 1080, 2180)
            };
            var resolutions = new[] { new Vector2Int(320, 568), new Vector2Int(360, 800), new Vector2Int(1080, 2400) };
            for (int i = 0; i < cases.Length; i++)
            {
                game.ReturnToMenu();
                RunnerScreenLayout layout = Apply(resolutions[i].x, resolutions[i].y, cases[i]);
                yield return new WaitForSecondsRealtime(0.35f);
                AssertVisibleUi(layout);
                if (i == 1)
                {
                    yield return RunnerDisplayCapture.Capture("responsive-portrait-title", 360, 800, cases[i]);
                    layout = Apply(resolutions[i].x, resolutions[i].y, cases[i]);
                    Click(root.Find("Forest Main Menu/Main menu buttons/Shop").GetComponent<Button>());
                    yield return new WaitForSecondsRealtime(0.3f);
                    AssertVisibleUi(layout);
                    yield return RunnerDisplayCapture.Capture("responsive-portrait-dinos", 360, 800, cases[i]);
                    layout = Apply(resolutions[i].x, resolutions[i].y, cases[i]);
                    const string dialog = "Forest Main Menu/Menu dialog/Moss framed dialog/";
                    Click(root.Find(dialog + "Back to main menu").GetComponent<Button>());
                    Click(root.Find("Forest Main Menu/Main menu buttons/Options").GetComponent<Button>());
                    yield return new WaitForSecondsRealtime(0.3f);
                    AssertVisibleUi(layout);
                    yield return RunnerDisplayCapture.Capture("responsive-portrait-options", 360, 800, cases[i]);
                    layout = Apply(resolutions[i].x, resolutions[i].y, cases[i]);
                    Click(root.Find(dialog + "Back to main menu").GetComponent<Button>());
                }
                Click(root.Find("Forest Main Menu/Main menu buttons/Play").GetComponent<Button>());
                yield return new WaitForSecondsRealtime(0.3f);
                AssertVisibleUi(layout);
                Click(root.Find("Forest Main Menu/Menu dialog/Moss framed dialog/Mode selection page/Endless mode").GetComponent<Button>());
                yield return new WaitForSecondsRealtime(0.3f);
                AssertVisibleUi(layout);
                const string maps = "Forest Main Menu/Map selector/Map selection panel/";
                Bounds forest = RectTransformUtility.CalculateRelativeRectTransformBounds(root, root.Find(maps + "Choose Forest map"));
                Bounds desert = RectTransformUtility.CalculateRelativeRectTransformBounds(root, root.Find(maps + "Choose Desert map"));
                Bounds jungle = RectTransformUtility.CalculateRelativeRectTransformBounds(root, root.Find(maps + "Choose Jungle map"));
                Bounds snow = RectTransformUtility.CalculateRelativeRectTransformBounds(root, root.Find(maps + "Choose Snow Mountain map"));
                Assert.That(forest.max.x, Is.LessThan(desert.min.x));
                Assert.That(jungle.max.y, Is.LessThan(forest.min.y));
                Assert.That(snow.max.y, Is.LessThan(jungle.min.y));
                Assert.That(snow.center.x, Is.EqualTo(0f).Within(1f), "The fifth map centers on its own row.");
                if (i == 1) yield return RunnerDisplayCapture.Capture("responsive-portrait-map-selector", 360, 800, cases[i]);
                layout = Apply(resolutions[i].x, resolutions[i].y, cases[i]);
                Click(root.Find(maps + "Start selected map").GetComponent<Button>());
                yield return new WaitForSecondsRealtime(0.3f);
                Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.AwaitingStart));
                AssertVisibleUi(layout);
                game.PrimaryAction();
                yield return new WaitForSecondsRealtime(0.3f);
                Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Running));
                AssertVisibleUi(layout);
                Assert.That(root.Find("Jump").gameObject.activeInHierarchy, Is.False);
                Assert.That(root.Find("Duck").gameObject.activeInHierarchy, Is.False);
                game.Jump();
                typeof(ForestRunnerGame).GetMethod("Step", Private).Invoke(game, new object[] { RunnerRules.SimulationStep });
                Assert.That(((RunnerPlayer)Get("player")).Grounded, Is.False);
                game.TogglePause();
                yield return new WaitForSecondsRealtime(0.3f);
                AssertVisibleUi(layout);
                if (i == 1)
                {
                    yield return RunnerDisplayCapture.Capture("responsive-portrait-paused", 360, 800, cases[i]);
                    layout = Apply(resolutions[i].x, resolutions[i].y, cases[i]);
                }
                Click(root.Find("Forest Pause Menu/Resume run").GetComponent<Button>());
                Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Running));
                if (i == 1) yield return RunnerDisplayCapture.Capture("responsive-portrait-running", 360, 800, cases[i]);
            }
        }

        [UnityTest]
        public IEnumerator ResizingAndRotationPreserveRunStateHealthAndAuthoredLandscapePositions()
        {
            game.StartRun();
            typeof(ForestRunnerGame).GetMethod("TakeDamage", Private).Invoke(game, null);
            int lives = game.Lives, score = game.Score, map = game.SelectedMapIndex;
            int children = root.childCount;
            var cases = new[] {
                new Vector2Int(360, 800), new Vector2Int(2560, 1080), new Vector2Int(1024, 768),
                new Vector2Int(1080, 2400), new Vector2Int(5120, 1440), new Vector2Int(1280, 720)
            };
            game.TogglePause();
            foreach (Vector2Int resolution in cases)
            {
                RunnerScreenLayout layout = Apply(resolution.x, resolution.y, new Rect(0, 0, resolution.x, resolution.y));
                yield return new WaitForSecondsRealtime(0.3f);
                Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Paused));
                Assert.That(game.Lives, Is.EqualTo(lives));
                Assert.That(game.Score, Is.EqualTo(score));
                Assert.That(game.SelectedMapIndex, Is.EqualTo(map));
                Assert.That(root.childCount, Is.EqualTo(children), "Resizing must reuse widgets, not duplicate the UI.");
                AssertVisibleUi(layout);
            }
            game.TogglePause();
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(((RectTransform)root.Find("Life hearts")).anchoredPosition, Is.EqualTo(new Vector2(-492, 309)));
            Assert.That(root.Find("Jump").gameObject.activeInHierarchy, Is.False);
            Assert.That(root.Find("Duck").gameObject.activeInHierarchy, Is.False);
            Assert.That(root.Find("Top left badge").gameObject.activeInHierarchy, Is.False);
            Assert.That(game.Lives, Is.EqualTo(RunnerHealth.MaximumLives - 1));
            yield return RunnerDisplayCapture.Capture("responsive-ultrawide", 2560, 1080);
        }

        [UnityTest]
        public IEnumerator SixteenByTenFillsTheScreenWithExtraSceneryAndNoViewportBorders()
        {
            game.StartRun();
            RunnerScreenLayout layout = Apply(1280, 800, new Rect(0, 0, 1280, 800));
            yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(world.rect, Is.EqualTo(new Rect(0, 0, 1, 1)));
            Assert.That(world.aspect, Is.EqualTo(1.6f).Within(0.0001f));
            Assert.That(world.orthographicSize, Is.EqualTo(6.25f).Within(0.0001f));
            Assert.That(world.orthographicSize * world.aspect, Is.EqualTo(10f).Within(0.0001f));
            Assert.That(((RectTransform)root.Find("Life hearts")).anchoredPosition.y, Is.EqualTo(349f));
            Assert.That(root.Find("Jump").gameObject.activeInHierarchy, Is.False);
            Assert.That(root.Find("Duck").gameObject.activeInHierarchy, Is.False);
            foreach (Transform border in canvas.transform.Find("Viewport borders"))
                Assert.That(border.gameObject.activeSelf, Is.False, "16:10 must not have letterbox borders.");
            foreach (SpriteRenderer sprite in game.transform.Find("Forest environment").GetComponentsInChildren<SpriteRenderer>())
                if (sprite.sortingOrder == -100)
                    Assert.That(sprite.bounds.max.y, Is.GreaterThanOrEqualTo(world.transform.position.y + world.orthographicSize - 0.001f));
            AssertVisibleUi(layout);
            yield return RunnerDisplayCapture.Capture("responsive-16-by-10-running", 1280, 800);
        }

        private RunnerScreenLayout Apply(int width, int height, Rect safe)
        {
            world.targetTexture = null;
            uiCamera.targetTexture = null;
            if (target != null) { target.Release(); Object.Destroy(target); }
            target = new RenderTexture(width, height, 24);
            target.Create();
            world.targetTexture = uiCamera.targetTexture = target;
            RunnerScreenLayout layout = RunnerScreenLayout.Calculate(width, height, safe);
            game.ApplyScreenLayout(layout);
            Canvas.ForceUpdateCanvases();
            return layout;
        }

        private void AssertVisibleUi(RunnerScreenLayout layout)
        {
            Canvas.ForceUpdateCanvases();
            var eggCounter = (RectTransform)root.Find("Egg counter");
            if (eggCounter.gameObject.activeInHierarchy)
            {
                Rect bounds = Pixels(eggCounter);
                Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(layout.SafeArea.xMin - 1f));
                Assert.That(bounds.xMax, Is.LessThanOrEqualTo(layout.SafeArea.xMax + 1f));
                Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(layout.SafeArea.yMin - 1f));
                Assert.That(bounds.yMax, Is.LessThanOrEqualTo(layout.SafeArea.yMax + 1f));
                foreach (string name in new[] { "Life hearts", "Stage progress", "Scoreboard", "Sound", "Pause" })
                    Assert.That(bounds.Overlaps(Pixels((RectTransform)root.Find(name))), Is.False,
                        "The egg counter must not cover " + name);
            }
            var stage = (RectTransform)root.Find("Stage progress");
            if (stage.gameObject.activeInHierarchy)
            {
                Rect bounds = Pixels(stage);
                Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(layout.SafeArea.xMin - 1f));
                Assert.That(bounds.xMax, Is.LessThanOrEqualTo(layout.SafeArea.xMax + 1f));
                Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(layout.SafeArea.yMin - 1f));
                Assert.That(bounds.yMax, Is.LessThanOrEqualTo(layout.SafeArea.yMax + 1f));
                foreach (string name in new[] { "Life hearts", "Scoreboard", "Sound", "Pause" })
                    Assert.That(bounds.Overlaps(Pixels((RectTransform)root.Find(name))), Is.False, "Progress must not cover " + name);
                foreach (Graphic graphic in stage.GetComponentsInChildren<Graphic>())
                    Assert.That(graphic.raycastTarget, Is.False, "Progress must not intercept jump taps.");
            }
            foreach (Button button in root.GetComponentsInChildren<Button>())
            {
                Rect pixels = Pixels((RectTransform)button.transform);
                Assert.That(pixels.xMin, Is.GreaterThanOrEqualTo(layout.SafeArea.xMin - 1f), button.name);
                Assert.That(pixels.xMax, Is.LessThanOrEqualTo(layout.SafeArea.xMax + 1f), button.name);
                Assert.That(pixels.yMin, Is.GreaterThanOrEqualTo(layout.SafeArea.yMin - 1f), button.name);
                Assert.That(pixels.yMax, Is.LessThanOrEqualTo(layout.SafeArea.yMax + 1f), button.name);
                // The title SFX button intentionally sits behind a submenu's blocking scrim.
                if (button.name == "SFX toggle" && hud.MainMenuDialogOpen) continue;
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = pixels.center }, hits);
                Assert.That(hits.Count, Is.GreaterThan(0), button.name + " must remain tappable.");
                Assert.That(hits[0].gameObject, Is.EqualTo(button.gameObject), button.name + " is covered by another widget.");
            }
            foreach (Text text in root.GetComponentsInChildren<Text>())
            {
                Assert.That(text.preferredWidth, Is.LessThanOrEqualTo(text.rectTransform.rect.width + 1f), text.name);
                Assert.That(text.preferredHeight, Is.LessThanOrEqualTo(text.rectTransform.rect.height + 1f), text.name);
            }
        }

        private Rect Pixels(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private void Click(Button button)
        {
            var pointer = new PointerEventData(EventSystem.current) { position = Pixels((RectTransform)button.transform).center };
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }

        private object Get(string name) => typeof(ForestRunnerGame).GetField(name, Private).GetValue(game);
    }
}
