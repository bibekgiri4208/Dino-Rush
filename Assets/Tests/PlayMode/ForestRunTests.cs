using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace DinoRush.Tests
{
    public sealed class ForestRunTests
    {
        private const string ScoreKey = "DinoRush.Forest.HighScore";
        private int previousBest;
        private bool hadBest;
        private int previousSound;
        private bool hadSound;
        private ForestRunnerGame game;

        [UnitySetUp]
        public IEnumerator LoadForest()
        {
            hadBest = PlayerPrefs.HasKey(ScoreKey);
            previousBest = PlayerPrefs.GetInt(ScoreKey);
            hadSound = PlayerPrefs.HasKey("DinoRush.Muted");
            previousSound = PlayerPrefs.GetInt("DinoRush.Muted");
            PlayerPrefs.DeleteKey(ScoreKey);
            yield return SceneManager.LoadSceneAsync("ForestRun");
            game = Object.FindAnyObjectByType<ForestRunnerGame>();
            Assert.That(game, Is.Not.Null);
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Ready));
            Assert.That(game.HasAuthoredScene, Is.True);
            Assert.That(game.GetComponentsInChildren<Camera>(true).Length, Is.EqualTo(1));
            Assert.That(game.GetComponentsInChildren<Canvas>(true).Length, Is.EqualTo(1));
            foreach (SpriteRenderer renderer in game.GetComponentsInChildren<SpriteRenderer>())
                Assert.That(renderer.name.StartsWith("Pooled obstacle"), Is.False, "Editor-staged obstacles must be hidden before running.");
        }

        [UnityTearDown]
        public IEnumerator RestorePrefs()
        {
            if (game != null) Object.Destroy(game.gameObject);
            yield return null;
            if (hadBest) PlayerPrefs.SetInt(ScoreKey, previousBest);
            else PlayerPrefs.DeleteKey(ScoreKey);
            if (hadSound) PlayerPrefs.SetInt("DinoRush.Muted", previousSound);
            else PlayerPrefs.DeleteKey("DinoRush.Muted");
            PlayerPrefs.Save();
        }

        [UnityTest]
        public IEnumerator StartPauseJumpCrashAndRetry()
        {
            yield return Capture("forest-ready");
            Click("Play");
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Running));
            // A first render can spend a long frame compiling shaders; gameplay deliberately
            // caps catch-up time, so wait for simulated progress rather than wall-clock time.
            float progressDeadline = Time.realtimeSinceStartup + 10f;
            while (game.Score == 0 && Time.realtimeSinceStartup < progressDeadline) yield return null;
            Assert.That(game.Score, Is.GreaterThan(0));
            game.TogglePause();
            int pausedScore = game.Score;
            yield return new WaitForSeconds(0.2f);
            Assert.That(game.Score, Is.EqualTo(pausedScore));
            game.TogglePause();
            game.Jump();
            yield return new WaitForSeconds(0.16f);
            Assert.That(GameObject.Find("Blue dino player").transform.position.y, Is.GreaterThan(RunnerRules.GroundY + 0.5f));
            yield return Capture("forest-running");
            yield return new WaitForSeconds(5f);
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.GameOver));
            Assert.That(PlayerPrefs.GetInt(ScoreKey), Is.EqualTo(game.Score));
            Assert.That(game.HighScore, Is.GreaterThan(0));
            yield return Capture("forest-game-over");
            AssertUiLayout(game.transform.Find("Forest runner UI/16 by 9 safe content"));
            game.PrimaryAction();
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Running));
            Assert.That(game.Score, Is.Zero);
            Assert.That(game.ObstaclesCleared, Is.Zero);
        }

        [UnityTest]
        public IEnumerator DirtTrailsWhileRunningAndBurstsOnlyOnGroundContact()
        {
            Transform pool = game.transform.Find("Forest effects/Trail dust pool");
            SpriteRenderer[] particles = pool.GetComponentsInChildren<SpriteRenderer>(true);
            Assert.That(particles.Length, Is.EqualTo(36));
            Assert.That(CountLiveDirt(particles), Is.Zero);
            Click("Play");
            float deadline = Time.realtimeSinceStartup + 5f;
            while (game.Score < 2 && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(CountLiveDirt(particles), Is.GreaterThanOrEqualTo(3), "Running must kick up a visible dirt trail.");
            foreach (SpriteRenderer particle in particles)
            {
                if (!particle.gameObject.activeSelf) continue;
                Assert.That(particle.sprite.name, Does.StartWith("Dirt "));
                Assert.That(particle.color.r, Is.GreaterThan(particle.color.g));
                Assert.That(particle.color.g, Is.GreaterThan(particle.color.b));
                Assert.That(particle.transform.position.x, Is.LessThan(RunnerRules.PlayerX), "Running dirt must trail behind the feet.");
            }
            yield return Capture("forest-dirt-running");
            game.Jump();
            Transform dino = GameObject.Find("Blue dino player").transform;
            deadline = Time.realtimeSinceStartup + 5f;
            while (dino.position.y < RunnerRules.GroundY + 1.5f && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(dino.position.y, Is.GreaterThanOrEqualTo(RunnerRules.GroundY + 1.5f));
            while (dino.position.y > RunnerRules.GroundY + 1.4f && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(dino.position.y, Is.GreaterThan(RunnerRules.GroundY + 0.02f));
            Assert.That(CountLiveDirt(particles), Is.Zero, "Old dirt must fade away without emitting new dirt in midair.");
            while (dino.position.y > RunnerRules.GroundY + 0.0001f && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Running));
            Assert.That(CountLiveDirt(particles), Is.GreaterThanOrEqualTo(14), "Landing must create a stronger burst than a footstep.");
            yield return new WaitForSeconds(0.08f);
            yield return Capture("forest-dirt-landing");
            game.TogglePause();
            var positions = new Vector3[particles.Length];
            var colors = new Color[particles.Length];
            var sizes = new Vector3[particles.Length];
            for (int i = 0; i < particles.Length; i++)
            {
                positions[i] = particles[i].transform.position;
                colors[i] = particles[i].color;
                sizes[i] = particles[i].transform.localScale;
            }
            yield return new WaitForSeconds(0.2f);
            for (int i = 0; i < particles.Length; i++)
            {
                Assert.That(particles[i].transform.position, Is.EqualTo(positions[i]));
                Assert.That(particles[i].color, Is.EqualTo(colors[i]));
                Assert.That(particles[i].transform.localScale, Is.EqualTo(sizes[i]));
            }
            game.RestartRun();
            Assert.That(CountLiveDirt(particles), Is.Zero);
            Assert.That(pool.GetComponentsInChildren<SpriteRenderer>(true).Length, Is.EqualTo(36), "Bursts must reuse the pool.");
            game.ReturnToMenu();
            yield return new WaitForSeconds(0.2f);
            Assert.That(CountLiveDirt(particles), Is.Zero, "The title screen must not emit running dirt.");
        }

        private static int CountLiveDirt(SpriteRenderer[] particles)
        {
            int count = 0;
            foreach (SpriteRenderer particle in particles) if (particle.gameObject.activeInHierarchy) count++;
            return count;
        }

        [UnityTest]
        public IEnumerator MainMenuButtonsOpenTheirPanelsAndSoundPersists()
        {
            ForestMainMenu menu = game.GetComponentInChildren<ForestMainMenu>();
            Assert.That(menu, Is.Not.Null);
            Click("Options");
            Assert.That(menu.DialogOpen, Is.True);
            yield return Capture("menu-options");
            game.PrimaryAction();
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Ready), "Space must not start a run through an open dialog.");
            int before = PlayerPrefs.GetInt("DinoRush.Muted", 0);
            Click("Toggle sound effects");
            Assert.That(PlayerPrefs.GetInt("DinoRush.Muted"), Is.EqualTo(1 - before));
            yield return Capture("menu-sound-toggled");
            Click("Back to main menu");
            Assert.That(menu.DialogOpen, Is.False);
            yield return null;
            Click("Customize");
            AssertVisibleMenuLayout();
            Assert.That(GameObject.Find("Selected blue dino"), Is.Not.Null);
            Assert.That(GameObject.Find("Map selection").GetComponent<Text>().text, Is.EqualTo("MAP: FOREST"));
            yield return Capture("menu-customize");
            Click("Back to main menu");
            yield return null;
            Click("Quit");
            AssertVisibleMenuLayout();
            Assert.That(GameObject.Find("Confirm quit"), Is.Not.Null);
            yield return Capture("menu-quit");
            Click("Back to main menu");
            yield return null;
            Click("Play");
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Running));
            Assert.That(menu.gameObject.activeSelf, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PauseMenuResumesRestartsAndQuitsWithoutLosingBest()
        {
            Click("Play");
            float deadline = Time.realtimeSinceStartup + 5f;
            while (game.Score < 6 && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(game.Score, Is.GreaterThanOrEqualTo(6));
            Click("Pause");
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Paused));
            Assert.That(GameObject.Find("Forest Pause Menu"), Is.Not.Null);
            int score = game.Score;
            Vector3 playerPosition = GameObject.Find("Blue dino player").transform.position;
            Transform tree = game.transform.Find("Forest environment").GetComponentInChildren<SpriteRenderer>().transform;
            Vector3 treePosition = tree.position;
            yield return new WaitForSeconds(0.25f);
            Assert.That(game.Score, Is.EqualTo(score));
            Assert.That(GameObject.Find("Blue dino player").transform.position, Is.EqualTo(playerPosition));
            Assert.That(tree.position, Is.EqualTo(treePosition));
            AssertUiLayout(game.transform.Find("Forest runner UI/16 by 9 safe content"));
            yield return Capture("forest-paused");
            yield return Capture("forest-paused-tablet", 1024, 768);
            yield return Capture("forest-paused-wide", 1920, 820);
            Click("Resume run");
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Running));
            Assert.That(game.Score, Is.EqualTo(score));
            yield return null;
            Click("Pause");
            yield return null;
            Click("Restart run");
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Running));
            Assert.That(game.Score, Is.Zero);
            Assert.That(game.ObstaclesCleared, Is.Zero);
            foreach (SpriteRenderer renderer in game.GetComponentsInChildren<SpriteRenderer>())
                Assert.That(renderer.name.StartsWith("Pooled obstacle"), Is.False, "Restart must clear the old obstacle pool.");
            Assert.That(PlayerPrefs.GetInt(ScoreKey), Is.GreaterThanOrEqualTo(score));
            yield return null;
            Click("Pause");
            yield return null;
            Click("Quit run");
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Ready));
            Assert.That(game.Score, Is.Zero);
            Assert.That(game.GetComponentInChildren<ForestMainMenu>(), Is.Not.Null);
            Assert.That(GameObject.Find("Forest Pause Menu"), Is.Null);
            foreach (SpriteRenderer renderer in game.GetComponentsInChildren<SpriteRenderer>())
                Assert.That(renderer.name.StartsWith("Pooled obstacle"), Is.False);
            Assert.That(PlayerPrefs.GetInt(ScoreKey), Is.GreaterThanOrEqualTo(score));
            yield return null;
            Click("Play");
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Running));
            Assert.That(game.Score, Is.Zero);
        }

        [UnityTest]
        public IEnumerator ResultsMenuButtonReturnsToTitle()
        {
            Click("Play");
            float deadline = Time.realtimeSinceStartup + 10f;
            while (game.State == ForestRunnerGame.RunState.Running && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.GameOver));
            int best = game.HighScore;
            yield return null;
            Click("Results main menu");
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Ready));
            Assert.That(game.HighScore, Is.EqualTo(best));
            Assert.That(game.Score, Is.Zero);
            Assert.That(game.GetComponentInChildren<ForestMainMenu>(), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator MenuLayoutFitsTabletAndWideScreens()
        {
            AssertVisibleMenuLayout();
            yield return Capture("menu-tablet", 1024, 768);
            yield return Capture("menu-wide", 1920, 820);
            Click("Options");
            AssertVisibleMenuLayout();
            yield return Capture("menu-options-tablet", 1024, 768);
        }

        private void AssertVisibleMenuLayout()
        {
            ForestMainMenu menu = game.GetComponentInChildren<ForestMainMenu>();
            AssertUiLayout(menu.transform);
        }

        private static void AssertUiLayout(Transform root)
        {
            Canvas.ForceUpdateCanvases();
            foreach (Text text in root.GetComponentsInChildren<Text>())
            {
                Assert.That(text.font.fontNames, Does.Contain("Press Start 2P"), text.name + " uses the wrong font");
                Assert.That(text.preferredWidth, Is.LessThanOrEqualTo(text.rectTransform.rect.width + 1f), text.name + " overflows horizontally");
                Assert.That(text.preferredHeight, Is.LessThanOrEqualTo(text.rectTransform.rect.height + 1f), text.name + " overflows vertically");
                if (text.text.Contains("\n")) Assert.That(text.lineSpacing, Is.GreaterThanOrEqualTo(1.5f), text.name + " has overlapping pixel-font lines");
            }
            foreach (Button button in root.GetComponentsInChildren<Button>())
            {
                Image background = button.GetComponent<Image>();
                Assert.That(background.sprite, Is.Not.Null, button.name + " has missing art");
                Assert.That(background.type, Is.EqualTo(Image.Type.Sliced), button.name + " stretches its frame");
                Assert.That(background.sprite.border.sqrMagnitude, Is.GreaterThan(0));
                Transform icon = button.transform.Find("Icon");
                Transform label = button.GetComponentInChildren<Text>().transform;
                if (icon != null && label != null)
                {
                    Bounds a = RectTransformUtility.CalculateRelativeRectTransformBounds(button.transform, icon);
                    Bounds b = RectTransformUtility.CalculateRelativeRectTransformBounds(button.transform, label);
                    Assert.That(a.max.x, Is.LessThan(b.min.x), button.name + " icon overlaps its label");
                }
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
            Assert.That(hits.Count, Is.GreaterThan(0), name + " cannot be clicked");
            Assert.That(hits[0].gameObject, Is.EqualTo(button.gameObject), name + " is blocked by another UI graphic");
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }

        [UnityTest]
        public IEnumerator WellTimedJumpsClearMultipleObstacles()
        {
            game.StartRun();
            SpriteRenderer[] renderers = game.GetComponentsInChildren<SpriteRenderer>(true);
            float started = Time.time;
            float deadline = started + 16f;
            Transform dino = GameObject.Find("Blue dino player").transform;
            while (game.ObstaclesCleared < 3 && Time.time < deadline)
            {
                Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Running));
                float lead = RunnerRules.Speed(Time.time - started) * RunnerRules.JumpSpeed / RunnerRules.Gravity;
                foreach (SpriteRenderer renderer in renderers)
                {
                    if (!renderer.gameObject.activeInHierarchy || !renderer.name.StartsWith("Pooled obstacle")) continue;
                    float gap = renderer.transform.position.x - RunnerRules.PlayerX;
                    if (gap > 0 && gap <= lead && dino.position.y <= RunnerRules.GroundY + 0.01f)
                    {
                        game.Jump();
                        break;
                    }
                }
                yield return null;
            }
            Assert.That(game.ObstaclesCleared, Is.GreaterThanOrEqualTo(3));
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Running));
        }

        private static IEnumerator Capture(string name, int width = 1280, int height = 720)
        {
            // Optional screenshots for local CI, never written into the asset folder.
            string directory = System.Environment.GetEnvironmentVariable("DINO_RUSH_CAPTURE_DIR");
            if (string.IsNullOrEmpty(directory)) yield break;
            yield return null;
            Camera camera = Camera.main;
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            bool scalerEnabled = scaler.enabled;
            float previousScale = canvas.scaleFactor;
            RenderMode previousMode = canvas.renderMode;
            Camera previousCamera = canvas.worldCamera;
            RenderTexture target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previousActive = RenderTexture.active;
            Rect previousRect = camera.rect;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                float aspect = (float)width / height / (16f / 9f);
                camera.rect = aspect > 1f ? new Rect((1f - 1f / aspect) * 0.5f, 0, 1f / aspect, 1)
                    : new Rect(0, (1f - aspect) * 0.5f, 1, aspect);
                camera.targetTexture = target;
                scaler.enabled = false;
                canvas.scaleFactor = Mathf.Min(width / 1280f, height / 720f);
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                foreach (Text text in canvas.GetComponentsInChildren<Text>())
                    text.font.RequestCharactersInTexture(text.text, text.fontSize, text.fontStyle);
                Canvas.ForceUpdateCanvases();
                RenderTexture.active = target;
                GL.Clear(true, true, Color.black);
                // Explicit rendering works even when the batch runner does not draw a Game view.
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                File.WriteAllBytes(Path.Combine(directory, name + ".png"), image.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = previousMode;
                canvas.worldCamera = previousCamera;
                canvas.scaleFactor = previousScale;
                scaler.enabled = scalerEnabled;
                camera.targetTexture = null;
                camera.rect = previousRect;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
                Object.Destroy(image);
            }
            Canvas.ForceUpdateCanvases();
            yield return null;
        }
    }
}
