using System;
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
    public sealed class RunnerDailyMissionPlayTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private ForestRunnerGame game;
        private ForestMainMenu menu;
        private string oldSave;
        private bool hadSave;
        private readonly Dictionary<string, int> old = new Dictionary<string, int>();
        private readonly HashSet<string> existing = new HashSet<string>();
        [UnitySetUp] public IEnumerator Load()
        {
            hadSave = PlayerPrefs.HasKey(RunnerDailyMissions.SaveKey); oldSave = PlayerPrefs.GetString(RunnerDailyMissions.SaveKey);
            PlayerPrefs.DeleteKey(RunnerDailyMissions.SaveKey);
            foreach (string key in new[] { RunnerShopProgress.WalletKey, "DinoRush.Forest.HighScore", "DinoRush.Muted" })
            {
                if (PlayerPrefs.HasKey(key)) existing.Add(key); old[key] = PlayerPrefs.GetInt(key); PlayerPrefs.DeleteKey(key);
            }
            yield return Reload();
        }
        private IEnumerator Reload()
        {
            yield return SceneManager.LoadSceneAsync("ForestRun");
            game = UnityEngine.Object.FindAnyObjectByType<ForestRunnerGame>(); game.enabled = false;
            menu = game.GetComponentInChildren<ForestMainMenu>(true);
            foreach (BaseInputModule module in EventSystem.current.GetComponents<BaseInputModule>()) module.enabled = false;
            yield return new WaitForSecondsRealtime(0.35f);
        }
        [UnityTearDown] public IEnumerator Restore()
        {
            if (game != null) UnityEngine.Object.Destroy(game.gameObject); yield return null;
            if (hadSave) PlayerPrefs.SetString(RunnerDailyMissions.SaveKey, oldSave); else PlayerPrefs.DeleteKey(RunnerDailyMissions.SaveKey);
            foreach (var pair in old) if (existing.Contains(pair.Key)) PlayerPrefs.SetInt(pair.Key, pair.Value); else PlayerPrefs.DeleteKey(pair.Key);
            old.Clear(); existing.Clear(); PlayerPrefs.Save();
        }
        private void Invoke(string method, params object[] args) => typeof(ForestRunnerGame).GetMethod(method, Private).Invoke(game, args);
        private void Set(string field, object value) => typeof(ForestRunnerGame).GetField(field, Private).SetValue(game, value);
        private void Step(int frames) { for (int i = 0; i < frames; i++) Invoke("Step", RunnerRules.SimulationStep); }
        private void Begin() { game.StartRun(); Set("spawnTimer", float.MaxValue); }
        private static void Click(string name)
        {
            Canvas.ForceUpdateCanvases(); Button button = GameObject.Find(name).GetComponent<Button>();
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, button.transform.position) };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits.Count, Is.GreaterThan(0), name);
            Assert.That(hits[0].gameObject, Is.EqualTo(button.gameObject), name + " is blocked");
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }
        [UnityTest] public IEnumerator ActualDistanceAndPickupsAccumulateAcrossRunsButNotPauseOrWaiting()
        {
            game.PrepareRun(); Step(195);
            Assert.That(game.DailyMissions.Progress(0), Is.Zero); Assert.That(game.DailyMissions.Progress(1), Is.Zero);
            game.PrimaryAction(); Set("spawnTimer", float.MaxValue); Step(195);
            Assert.That(game.EggsCollected, Is.EqualTo(4)); Assert.That(game.DailyMissions.Progress(1), Is.EqualTo(4));
            Assert.That(game.DailyMissions.Progress(0), Is.EqualTo(game.Score));
            int metres = game.DailyMissions.Progress(0);
            game.TogglePause(); Step(195);
            Assert.That(game.DailyMissions.Progress(0), Is.EqualTo(metres));
            Assert.That(game.DailyMissions.Progress(1), Is.EqualTo(4));
            Assert.That(new RunnerDailyMissions().Progress(0), Is.EqualTo(metres), "Pause flushes fractional distance as well as pickups.");
            game.RestartRun(); Set("spawnTimer", float.MaxValue); Step(195);
            Assert.That(game.DailyMissions.Progress(1), Is.EqualTo(8));
            Assert.That(game.DailyMissions.Progress(0), Is.GreaterThan(metres));
            game.ReturnToMenu();
            int total = game.DailyMissions.Progress(0);
            Step(120); Assert.That(game.DailyMissions.Progress(0), Is.EqualTo(total));
            yield return Reload(); Assert.That(game.DailyMissions.Progress(0), Is.EqualTo(total));
            Assert.That(game.DailyMissions.Progress(1), Is.EqualTo(8));
        }
        [UnityTest] public IEnumerator SafelyPassedObstaclesCountExactlyOnceAndHitObstaclesDoNotCount()
        {
            Begin();
            var pool = (IList)typeof(ForestRunnerGame).GetField("obstacles", Private).GetValue(game);
            for (int i = 0; i < 2; i++)
            {
                object obstacle = pool[i]; var type = obstacle.GetType();
                type.GetField("active").SetValue(obstacle, true); type.GetField("passed").SetValue(obstacle, false);
                type.GetField("hitPlayer").SetValue(obstacle, i == 1); type.GetField("overhead").SetValue(obstacle, false);
                type.GetField("width").SetValue(obstacle, 0.6f); type.GetField("height").SetValue(obstacle, 1f);
                var art = (SpriteRenderer)type.GetField("renderer").GetValue(obstacle);
                art.transform.position = new Vector3(RunnerRules.PlayerX - 2, RunnerRules.GroundY, 0); art.gameObject.SetActive(true);
            }
            Step(1); Assert.That(game.DailyMissions.Progress(2), Is.EqualTo(1));
            Step(120); Assert.That(game.DailyMissions.Progress(2), Is.EqualTo(1));
            Assert.That(game.ObstaclesCleared, Is.EqualTo(1));
            yield return null;
        }
        [UnityTest] public IEnumerator MenuClaimsPersistWithoutDoublePayAndRewardsNeverCountAsPickups()
        {
            Click("Missions"); yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(menu.DialogOpen, Is.True); Assert.That(menu.MissionsPage.gameObject.activeInHierarchy, Is.True);
            Assert.That(GameObject.Find("Claim daily mission 1").GetComponent<Button>().interactable, Is.False);
            game.DailyMissions.RecordDistance(500); menu.MissionsPage.Refresh();
            Click("Claim daily mission 1");
            Assert.That(game.Shop.Balance, Is.EqualTo(100));
            Assert.That(game.DailyMissions.Progress(1), Is.Zero); Assert.That(game.EggsCollected, Is.Zero);
            GameObject.Find("Claim daily mission 1").GetComponent<Button>().onClick.Invoke();
            Assert.That(game.Shop.Balance, Is.EqualTo(100));
            for (int i = 0; i < 30; i++) game.DailyMissions.RecordEgg();
            for (int i = 0; i < 25; i++) game.DailyMissions.RecordClear();
            menu.MissionsPage.Refresh(); Click("Claim daily mission 2"); Click("Claim daily mission 3");
            Assert.That(game.Shop.Balance, Is.EqualTo(600));
            yield return ForestRunTests.Capture("missions-claimed");
            Click("Back to main menu"); yield return null; Click("Shop");
            Assert.That(GameObject.Find("Shop egg balance").GetComponent<Text>().text, Is.EqualTo("EGGS: 600"));
            menu.CloseDialog(); yield return Reload();
            Assert.That(game.Shop.Balance, Is.EqualTo(600));
            for (int i = 0; i < 3; i++) Assert.That(game.DailyMissions.Claimed(i), Is.True);
        }
        [UnityTest] public IEnumerator MidnightWhileRunningStartsFreshGoalsAndUpdatesTheOpenCountdown()
        {
            DateTime now = new DateTime(2030, 10, 24, 23, 59, 59);
            var daily = new RunnerDailyMissions(() => now);
            typeof(ForestRunnerGame).GetField("<DailyMissions>k__BackingField", Private).SetValue(game, daily);
            menu.InitializeDailyMissions(daily);
            Begin(); Step(120); Assert.That(daily.Progress(0), Is.GreaterThan(0));
            now = now.AddSeconds(2); Step(1); Assert.That(daily.Progress(0), Is.Zero);
            game.ReturnToMenu(); Click("Missions"); yield return null;
            Assert.That(GameObject.Find("Daily reset countdown").GetComponent<Text>().text, Is.EqualTo("RESETS IN 23:59:59"));
            Assert.That(GameObject.Find("Claim daily mission 1").GetComponent<Button>().interactable, Is.False);
        }
        [UnityTest] public IEnumerator MissionsBackStaysCenteredAfterLeavingSlashesAndRotating()
        {
            Click("Shop"); yield return null;
            Click("Slashes category");
            Transform panel = menu.MissionsPage.transform.parent;
            var back = (RectTransform)panel.Find("Back to main menu");
            var label = (RectTransform)back.Find("Label");
            var slashes = panel.Find("Customize page/Slashes section");
            Assert.That(slashes.gameObject.activeInHierarchy, Is.True);
            menu.CloseDialog(); yield return null;

            // Slashes retains its category selection under the inactive Shop page.
            // Reflowing it must not overwrite another dialog's shared footer.
            foreach (Vector2Int size in new[] { new Vector2Int(1280, 720), new Vector2Int(320, 568),
                new Vector2Int(360, 800), new Vector2Int(1024, 768), new Vector2Int(1280, 720) })
            {
                Click("Missions");
                var layout = RunnerScreenLayout.Calculate(size.x, size.y, new Rect(0, 24, size.x, size.y - 48));
                game.ApplyScreenLayout(layout); Canvas.ForceUpdateCanvases();
                Assert.That(slashes.gameObject.activeSelf, Is.True);
                Assert.That(slashes.gameObject.activeInHierarchy, Is.False);
                Vector2 expectedPosition = new Vector2(0, layout.Portrait ? -490 : -280);
                Vector2 expectedSize = new Vector2(layout.Portrait ? 560 : 360, layout.Portrait ? 100 : 52);
                Assert.That(back.anchoredPosition, Is.EqualTo(expectedPosition));
                Assert.That(back.sizeDelta, Is.EqualTo(expectedSize));
                Assert.That(label.anchoredPosition, Is.EqualTo(Vector2.zero));
                Assert.That(label.sizeDelta, Is.EqualTo(expectedSize - new Vector2(24, 8)));
                yield return new WaitForSecondsRealtime(0.3f);
                Assert.That(back.anchoredPosition, Is.EqualTo(expectedPosition), "The entrance must not move the footer.");
                Assert.That(label.anchoredPosition, Is.EqualTo(Vector2.zero), "Button feedback must retain the centered label.");
                // Synthetic layouts don't resize the physical batch-mode window used for raycasts.
                game.ApplyScreenLayout(RunnerScreenLayout.Calculate(Screen.width, Screen.height, Screen.safeArea));
                Click("Back to main menu");
                Assert.That(menu.DialogOpen, Is.False);
                yield return null;
            }
        }
        [UnityTest] public IEnumerator MissionsFitPhonesAndDesktopAndDoNotLeakIntoOtherDialogs()
        {
            game.DailyMissions.RecordDistance(250);
            for (int i = 0; i < 8; i++) game.DailyMissions.RecordEgg();
            for (int i = 0; i < 25; i++) game.DailyMissions.RecordClear();
            yield return ForestRunTests.Capture("missions-main-menu");
            yield return ForestRunTests.Capture("missions-main-menu-portrait", 720, 1280);
            Click("Missions"); yield return new WaitForSecondsRealtime(0.3f);
            Transform panel = menu.MissionsPage.transform.parent;
            foreach (Vector2Int size in new[] { new Vector2Int(1280, 720), new Vector2Int(320, 568), new Vector2Int(360, 800), new Vector2Int(1024, 768) })
            {
                var layout = RunnerScreenLayout.Calculate(size.x, size.y, new Rect(0, 24, size.x, size.y - 48));
                game.ApplyScreenLayout(layout); Canvas.ForceUpdateCanvases();
                foreach (Text text in panel.GetComponentsInChildren<Text>())
                {
                    Assert.That(text.preferredWidth, Is.LessThanOrEqualTo(text.rectTransform.rect.width + 1), text.name);
                    Assert.That(text.preferredHeight, Is.LessThanOrEqualTo(text.rectTransform.rect.height + 1), text.name);
                }
                foreach (Button button in panel.GetComponentsInChildren<Button>())
                {
                    Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(panel, button.transform);
                    Rect rect = ((RectTransform)panel).rect;
                    Assert.That(bounds.min.x, Is.GreaterThanOrEqualTo(rect.xMin)); Assert.That(bounds.max.x, Is.LessThanOrEqualTo(rect.xMax));
                    Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(rect.yMin)); Assert.That(bounds.max.y, Is.LessThanOrEqualTo(rect.yMax));
                    if (layout.Portrait) Assert.That(((RectTransform)button.transform).rect.height * panel.localScale.y * layout.UiScale, Is.GreaterThanOrEqualTo(44));
                }
                var fill = GameObject.Find("Daily mission 1/Mission progress track/Mission progress fill").GetComponent<Image>();
                Assert.That(fill.rectTransform.anchorMax.x, Is.EqualTo(0.5f).Within(0.001f));
                Assert.That(fill.rectTransform.rect.width, Is.GreaterThan(0));
            }
            game.ApplyScreenLayout(RunnerScreenLayout.Calculate(Screen.width, Screen.height, Screen.safeArea));
            yield return ForestRunTests.Capture("daily-missions-landscape");
            yield return ForestRunTests.Capture("daily-missions-portrait", 720, 1280);
            menu.CloseDialog(); yield return null; Click("Options");
            Assert.That(menu.MissionsPage.gameObject.activeSelf, Is.False);
            Assert.That(((RectTransform)panel).sizeDelta, Is.EqualTo(new Vector2(480, 432)));
            menu.CloseDialog(); yield return null; Click("Shop");
            Assert.That(((RectTransform)panel).sizeDelta, Is.EqualTo(new Vector2(960, 744)));
            menu.CloseDialog(); Begin(); Assert.That(menu.MissionsPage.gameObject.activeInHierarchy, Is.False);
        }
    }
}
