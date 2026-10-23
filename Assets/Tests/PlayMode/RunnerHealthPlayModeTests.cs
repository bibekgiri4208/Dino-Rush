using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DinoRush.Tests
{
    public sealed class RunnerHealthPlayModeTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private ForestRunnerGame game;
        private Transform root;
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
            root = game.transform.Find("Forest runner UI/16 by 9 safe content");
            Assert.That(root.Find("Life hearts").gameObject.activeSelf, Is.False);
            game.StartRun();
            Set("spawnTimer", float.MaxValue);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            if (game != null) Object.Destroy(game.gameObject);
            yield return null;
            if (hadBest) PlayerPrefs.SetInt("DinoRush.Forest.HighScore", oldBest);
            else PlayerPrefs.DeleteKey("DinoRush.Forest.HighScore");
            PlayerPrefs.Save();
        }

        [UnityTest]
        public IEnumerator ContactCostsOneHeartFlashesWhiteCriesAndFloatsOneHeartLoss()
        {
            AssertHearts(5);
            SpriteRenderer obstacle = Contact(0);
            Assert.That(game.Lives, Is.EqualTo(4));
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Running));
            Assert.That(game.IsInvulnerable, Is.True);
            AssertHearts(4);
            RectTransform popup = (RectTransform)root.Find("Damage popup");
            Assert.That(popup.gameObject.activeSelf, Is.True);
            Assert.That(popup.Find("Lost life").GetComponent<Text>().text, Is.EqualTo("-1"));
            Assert.That(popup.Find("Lost heart").GetComponent<RunnerHeartGraphic>().Filled, Is.True);
            Assert.That(popup.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
            Assert.That(popup.GetComponentInChildren<Text>().raycastTarget, Is.False);

            RunnerPlayer player = (RunnerPlayer)Get("player");
            Sprite[] frames = (Sprite[])typeof(RunnerPlayer).GetField("frames", Private).GetValue(player);
            SpriteRenderer dino = game.transform.Find("Player/Blue dino player").GetComponent<SpriteRenderer>();
            player.Render(0f, true, false, 1f);
            Assert.That(dino.sprite, Is.EqualTo(frames[15]), "Use the supplied white damage silhouette.");
            player.Render(0.09f, true, false, 1f);
            player.Render(0f, true, false, 1f);
            Assert.That(dino.sprite, Is.EqualTo(frames[14]), "Use the selected dino's crying pose.");
            Assert.That(player.Grounded, Is.True);
            RunnerHud hud = (RunnerHud)Get("hud");
            Vector2 initial = popup.anchoredPosition;
            hud.TickDamage(0.5f, Camera.main);
            Assert.That(popup.anchoredPosition.y, Is.GreaterThan(initial.y));
            Assert.That(popup.GetComponent<CanvasGroup>().alpha, Is.LessThan(1f));
            yield return ForestRunTests.Capture("damage-four-hearts");
            hud.TickDamage(1f, Camera.main);
            Assert.That(popup.gameObject.activeSelf, Is.False);

            // Keep touching the same object beyond the grace period: it must still cost only one heart.
            for (int i = 0; i < 180; i++)
            {
                obstacle.transform.position = new Vector3(RunnerRules.PlayerX, RunnerRules.GroundY, 0);
                Step();
            }
            Assert.That(game.Lives, Is.EqualTo(4));
            obstacle.transform.position = new Vector3(RunnerRules.PlayerX - 2f, RunnerRules.GroundY, 0);
            Step();
            Assert.That(game.ObstaclesCleared, Is.Zero);
            player.Render(1f, true, false, 1f);
            player.Render(0f, true, false, 1f);
            Assert.That(dino.sprite, Is.Not.EqualTo(frames[14]).And.Not.EqualTo(frames[15]));
        }

        [UnityTest]
        public IEnumerator ClusterGracePauseFinalHitAndRestartPreserveLifeRules()
        {
            Contact(0);
            Contact(1);
            Assert.That(game.Lives, Is.EqualTo(4), "A simultaneous cluster must not drain several hearts.");
            RunnerHealth health = (RunnerHealth)Get("health");
            float grace = health.InvulnerabilityRemaining;
            RunnerPlayer player = (RunnerPlayer)Get("player");
            player.Render(0f, true, false, 1f);
            SpriteRenderer dino = game.transform.Find("Player/Blue dino player").GetComponent<SpriteRenderer>();
            Sprite pose = dino.sprite;
            RectTransform popup = (RectTransform)root.Find("Damage popup");
            Vector2 position = popup.anchoredPosition;
            game.TogglePause();
            Assert.That(root.Find("Life hearts").gameObject.activeSelf, Is.False);
            Assert.That(popup.gameObject.activeSelf, Is.False);
            game.enabled = true;
            yield return new WaitForSeconds(0.2f);
            game.enabled = false;
            Assert.That(health.InvulnerabilityRemaining, Is.EqualTo(grace));
            Assert.That(dino.sprite, Is.EqualTo(pose));
            Assert.That(popup.anchoredPosition, Is.EqualTo(position));
            game.TogglePause();
            AssertHearts(4);
            Assert.That(popup.gameObject.activeSelf, Is.True);

            for (int remaining = 3; remaining >= 0; remaining--)
            {
                health.Step(RunnerHealth.InvulnerabilityDuration);
                Contact(4 - remaining);
                Assert.That(game.Lives, Is.EqualTo(remaining));
                Assert.That(game.State, Is.EqualTo(remaining == 0
                    ? ForestRunnerGame.RunState.GameOver : ForestRunnerGame.RunState.Running));
            }
            Assert.That(root.Find("Life hearts").gameObject.activeSelf, Is.False);
            Assert.That(popup.gameObject.activeSelf, Is.True, "The final hit must also show its heart loss.");
            game.RestartRun();
            AssertHearts(5);
            Assert.That(game.IsInvulnerable, Is.False);
            Assert.That(popup.gameObject.activeSelf, Is.False);
            Assert.That(dino.sprite.name, Does.Not.EndWith("frame 14").And.Not.EndWith("frame 15"));
            game.ReturnToMenu();
            Assert.That(root.Find("Life hearts").gameObject.activeSelf, Is.False);
            game.StartRun();
            AssertHearts(5);
        }

        private SpriteRenderer Contact(int index)
        {
            object obstacle = ((IList)Get("obstacles"))[index];
            var type = obstacle.GetType();
            foreach (string field in new[] { "active", "passed", "hitPlayer" })
                type.GetField(field).SetValue(obstacle, field == "active");
            type.GetField("width").SetValue(obstacle, 1f);
            type.GetField("height").SetValue(obstacle, 1f);
            var renderer = (SpriteRenderer)type.GetField("renderer").GetValue(obstacle);
            renderer.transform.position = new Vector3(RunnerRules.PlayerX, RunnerRules.GroundY, 0);
            renderer.gameObject.SetActive(true);
            Step();
            return renderer;
        }

        private void AssertHearts(int count)
        {
            Assert.That(game.Lives, Is.EqualTo(count));
            Assert.That(root.Find("Top left badge").gameObject.activeInHierarchy, Is.False);
            Assert.That(root.Find("Life hearts").gameObject.activeSelf, Is.True);
            RunnerHeartGraphic[] hearts = root.Find("Life hearts").GetComponentsInChildren<RunnerHeartGraphic>();
            Assert.That(hearts.Length, Is.EqualTo(5));
            Canvas.ForceUpdateCanvases();
            for (int i = 0; i < hearts.Length; i++)
            {
                Assert.That(hearts[i].Filled, Is.EqualTo(i < count));
                Assert.That(hearts[i].canvasRenderer, Is.Not.Null, "Each heart needs a canvas renderer.");
                Mesh mesh = hearts[i].canvasRenderer.GetMesh();
                Assert.That(mesh, Is.Not.Null);
                Assert.That(mesh.vertexCount, Is.GreaterThan(0), "Heart art must generate visible geometry.");
            }
        }

        private object Get(string name) => typeof(ForestRunnerGame).GetField(name, Private).GetValue(game);
        private void Set(string name, object value) => typeof(ForestRunnerGame).GetField(name, Private).SetValue(game, value);
        private void Step() => typeof(ForestRunnerGame).GetMethod("Step", Private).Invoke(game, new object[] { RunnerRules.SimulationStep });
    }
}
