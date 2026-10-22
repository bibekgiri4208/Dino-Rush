using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DinoRush.Tests
{
    public sealed class ForestRunTests
    {
        private const string ScoreKey = "DinoRush.Forest.HighScore";
        private int previousBest;
        private bool hadBest;
        private ForestRunnerGame game;

        [UnitySetUp]
        public IEnumerator LoadForest()
        {
            hadBest = PlayerPrefs.HasKey(ScoreKey);
            previousBest = PlayerPrefs.GetInt(ScoreKey);
            PlayerPrefs.DeleteKey(ScoreKey);
            yield return SceneManager.LoadSceneAsync("ForestRun");
            game = Object.FindAnyObjectByType<ForestRunnerGame>();
            Assert.That(game, Is.Not.Null);
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Ready));
        }

        [UnityTearDown]
        public IEnumerator RestorePrefs()
        {
            if (game != null) Object.Destroy(game.gameObject);
            yield return null;
            if (hadBest) PlayerPrefs.SetInt(ScoreKey, previousBest);
            else PlayerPrefs.DeleteKey(ScoreKey);
            PlayerPrefs.Save();
        }

        [UnityTest]
        public IEnumerator StartPauseJumpCrashAndRetry()
        {
            yield return Capture("forest-ready");
            game.PrimaryAction();
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
            game.PrimaryAction();
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Running));
            Assert.That(game.Score, Is.Zero);
            Assert.That(game.ObstaclesCleared, Is.Zero);
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

        private static IEnumerator Capture(string name)
        {
            // Optional screenshots for local CI, never written into the asset folder.
            string directory = System.Environment.GetEnvironmentVariable("DINO_RUSH_CAPTURE_DIR");
            if (string.IsNullOrEmpty(directory)) yield break;
            yield return null;
            Camera camera = Camera.main;
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            RenderTexture target = RenderTexture.GetTemporary(1280, 720, 24, RenderTextureFormat.ARGB32);
            RenderTexture previousActive = RenderTexture.active;
            Rect previousRect = camera.rect;
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.rect = new Rect(0, 0, 1, 1);
                camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                Canvas.ForceUpdateCanvases();
                // Explicit rendering works even when the batch runner does not draw a Game view.
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply();
                File.WriteAllBytes(Path.Combine(directory, name + ".png"), image.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                camera.targetTexture = null;
                camera.rect = previousRect;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
                Object.Destroy(image);
            }
        }
    }
}
