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
    public sealed class ForestGameplayHudTests
    {
        private ForestRunnerGame game;
        private bool hadBest, hadSound;
        private int previousBest, previousSound;

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
        public IEnumerator ReferenceHudHasLiveReadoutsPaceMeterAndWorkingControls()
        {
            Transform root = game.transform.Find("Forest runner UI/16 by 9 safe content");
            Assert.That(root.Find("Top left badge/Brand").GetComponent<Text>().text, Is.EqualTo("FOREST RUN"));
            Assert.That(root.Find("Top left badge/Map").GetComponent<Text>().text, Is.EqualTo("ENDLESS"));
            Transform scoreCard = root.Find("Scoreboard/Score card");
            Transform bestCard = root.Find("Scoreboard/Best card");
            Assert.That(((RectTransform)scoreCard).anchoredPosition.y, Is.GreaterThan(((RectTransform)bestCard).anchoredPosition.y));
            Assert.That(root.Find("Sound").GetComponent<RectTransform>().anchoredPosition.x,
                Is.EqualTo(root.Find("Pause").GetComponent<RectTransform>().anchoredPosition.x));
            Assert.That(root.Find("Jump").GetComponent<RectTransform>().sizeDelta, Is.EqualTo(new Vector2(248, 88)));
            Assert.That(root.Find("Duck").gameObject.activeInHierarchy, Is.True);
            Assert.That(root.Find("Keyboard controls/Down key"), Is.Not.Null);
            Assert.That(root.Find("Keyboard controls/S key"), Is.Not.Null);
            Bounds duckBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(root, root.Find("Duck"));
            Bounds jumpBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(root, root.Find("Jump"));
            Bounds guideBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(root, root.Find("Keyboard controls"));
            Assert.That(guideBounds.max.x, Is.LessThan(duckBounds.min.x));
            Assert.That(duckBounds.max.x, Is.LessThan(jumpBounds.min.x));
            Assert.That(root.Find("Keyboard controls").gameObject.activeInHierarchy, Is.True);
            Assert.That(root.Find("Gameplay bottom fade").gameObject.activeInHierarchy, Is.True);
            Assert.That(root.Find("Controls").gameObject.activeSelf, Is.False, "Do not draw the old text over the keycaps.");
            Canvas.ForceUpdateCanvases();
            foreach (Text text in root.GetComponentsInChildren<Text>())
            {
                Assert.That(text.preferredWidth, Is.LessThanOrEqualTo(text.rectTransform.rect.width + 1), text.name);
                Assert.That(text.preferredHeight, Is.LessThanOrEqualTo(text.rectTransform.rect.height + 1), text.name);
            }
            foreach (Button button in root.GetComponentsInChildren<Button>())
            {
                Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(root, button.transform);
                Assert.That(bounds.min.x, Is.GreaterThanOrEqualTo(-640));
                Assert.That(bounds.max.x, Is.LessThanOrEqualTo(640));
                Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(-360));
                Assert.That(bounds.max.y, Is.LessThanOrEqualTo(360));
            }

            var hud = (RunnerHud)typeof(ForestRunnerGame).GetField("hud", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(game);
            hud.UpdateScore(16, 529, 3, RunnerRules.Speed(0));
            Assert.That(scoreCard.Find("Score").GetComponent<Text>().text, Is.EqualTo("000016"));
            Assert.That(bestCard.Find("Best").GetComponent<Text>().text, Is.EqualTo("000529"));
            Transform meter = root.Find("Trail stats frame/Pace meter");
            Assert.That(meter.childCount, Is.EqualTo(10));
            Assert.That(LitSegments(meter), Is.EqualTo(6));
            Assert.That(root.Find("Trail stats frame/Run stats").GetComponent<Text>().text, Is.EqualTo("1.0x"));
            hud.UpdateScore(72, 529, 8, RunnerRules.Speed(100));
            Assert.That(LitSegments(meter), Is.EqualTo(10));
            Assert.That(root.Find("Trail stats frame/Run stats").GetComponent<Text>().text, Is.EqualTo("1.9x"));
            game.RestartRun();
            yield return ForestRunTests.Capture("forest-hud");
            yield return ForestRunTests.Capture("forest-hud-tablet", 1024, 768);
            yield return ForestRunTests.Capture("forest-hud-wide", 1920, 820);

            int muted = PlayerPrefs.GetInt("DinoRush.Muted", 0);
            Click("Sound");
            Assert.That(PlayerPrefs.GetInt("DinoRush.Muted"), Is.EqualTo(1 - muted));
            Assert.That(root.Find("Sound/Sound icon").GetComponent<Image>().sprite.name,
                Is.EqualTo(muted == 0 ? "HudMuted" : "HudSpeaker"));
            float ground = game.transform.Find("Player/Blue dino player").position.y;
            Click("Jump");
            yield return new WaitForSeconds(0.1f);
            Assert.That(game.transform.Find("Player/Blue dino player").position.y, Is.GreaterThan(ground));
            Click("Pause");
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Paused));
            Assert.That(root.Find("Duck").gameObject.activeSelf, Is.False);
            Assert.That(root.Find("Keyboard controls").gameObject.activeSelf, Is.False);
            Assert.That(root.Find("Gameplay bottom fade").gameObject.activeSelf, Is.False);
            yield return null; // Allow the newly enabled modal controls to register with the UI raycaster.
            Click("Resume run");
            Assert.That(root.Find("Keyboard controls").gameObject.activeInHierarchy, Is.True);
            game.ReturnToMenu();
            Assert.That(root.Find("Duck").gameObject.activeSelf, Is.False);
            Assert.That(root.Find("Keyboard controls").gameObject.activeSelf, Is.False);
            Assert.That(root.Find("Gameplay bottom fade").gameObject.activeSelf, Is.False);
        }

        private static int LitSegments(Transform meter)
        {
            int count = 0;
            foreach (Transform segment in meter)
                if (segment.GetComponent<Image>().color.g > 0.5f) count++;
            return count;
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
            Assert.That(hits.Count, Is.GreaterThan(0));
            Assert.That(hits[0].gameObject, Is.EqualTo(button.gameObject), name + " is blocked");
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }
    }
}
