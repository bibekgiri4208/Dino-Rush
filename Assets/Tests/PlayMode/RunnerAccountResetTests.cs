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
using Object = UnityEngine.Object;

namespace DinoRush.Tests
{
    public sealed class RunnerAccountResetTests
    {
        private ForestRunnerGame game;
        private ForestMainMenu menu;
        private readonly Dictionary<string, int> ints = new Dictionary<string, int>();
        private readonly Dictionary<string, string> strings = new Dictionary<string, string>();
        private readonly HashSet<string> existed = new HashSet<string>();
        private const string Unrelated = "DinoRush.Tests.UnrelatedAccountPreference";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnitySetUp]
        public IEnumerator LoadProfile()
        {
            ints.Clear(); strings.Clear(); existed.Clear();
            var keys = new List<string>(RunnerAccountProgress.Keys(null)) { "DinoRush.Muted", Unrelated };
            foreach (string id in new[] { "blue", "red", "yellow", "green", "cole", "kira", "kuro", "loki", "mono", "sena" })
                keys.Add(RunnerShopProgress.FragmentKey(id));
            foreach (string key in keys)
            {
                if (PlayerPrefs.HasKey(key)) existed.Add(key);
                if (key == RunnerDinoRoster.SelectionKey || key == RunnerMapCatalog.SelectionKey || key == RunnerDailyMissions.SaveKey ||
                    key == RunnerStoryProgress.SaveKey || key == RunnerStoryProgress.TransactionKey)
                    strings[key] = PlayerPrefs.GetString(key);
                else ints[key] = PlayerPrefs.GetInt(key);
            }
            foreach (string key in keys) PlayerPrefs.DeleteKey(key);
            foreach (string id in new[] { "red", "yellow", "green", "cole", "kira", "kuro", "loki", "mono", "sena" })
                PlayerPrefs.SetInt(RunnerShopProgress.FragmentKey(id), 120);
            for (int i = 1; i <= RunnerTrailProgress.Count; i++) PlayerPrefs.SetInt(RunnerTrailProgress.OwnershipKey((RunnerTrailKind)i), 1);
            for (int i = 1; i <= 3; i++) PlayerPrefs.SetInt(RunnerSlashProgress.OwnershipKey((RunnerSlashKind)i), 1);
            PlayerPrefs.SetInt(RunnerTrailProgress.SelectionKey, (int)RunnerTrailKind.Cosmic);
            PlayerPrefs.SetInt(RunnerSlashProgress.SelectionKey, (int)RunnerSlashKind.Water);
            PlayerPrefs.SetInt(RunnerSlashProgress.ChargesKey, 3);
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 23456);
            PlayerPrefs.SetString(RunnerDinoRoster.SelectionKey, "red");
            PlayerPrefs.SetString(RunnerMapCatalog.SelectionKey, "jungle");
            PlayerPrefs.SetInt(RunnerAccountProgress.HighScoreKey, 3456);
            PlayerPrefs.SetString(RunnerDailyMissions.SaveKey,
                "{\"day\":" + DateTime.Today.Ticks + ",\"metres\":500,\"eggs\":30,\"cleared\":25,\"claimed\":3}");
            PlayerPrefs.SetInt("DinoRush.Muted", 1);
            PlayerPrefs.SetInt(Unrelated, 876);
            PlayerPrefs.Save();
            yield return Reload();
        }

        private IEnumerator Reload()
        {
            yield return SceneManager.LoadSceneAsync("ForestRun");
            game = Object.FindAnyObjectByType<ForestRunnerGame>(); game.enabled = false;
            menu = game.GetComponentInChildren<ForestMainMenu>();
            foreach (BaseInputModule input in EventSystem.current.GetComponents<BaseInputModule>()) input.enabled = false;
            yield return new WaitForSecondsRealtime(0.35f);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            if (game != null) Object.Destroy(game.gameObject);
            yield return null;
            foreach (var pair in ints)
                if (existed.Contains(pair.Key)) PlayerPrefs.SetInt(pair.Key, pair.Value); else PlayerPrefs.DeleteKey(pair.Key);
            foreach (var pair in strings)
                if (existed.Contains(pair.Key)) PlayerPrefs.SetString(pair.Key, pair.Value); else PlayerPrefs.DeleteKey(pair.Key);
            PlayerPrefs.Save();
        }

        private void AssertOwnedProfile()
        {
            Assert.That(game.Shop.Balance, Is.EqualTo(23456));
            Assert.That(game.SelectedDinoName, Is.EqualTo("Red"));
            for (int i = 1; i < game.DinoRoster.Count; i++) Assert.That(game.Shop.Fragments(i), Is.EqualTo(120));
            Assert.That(RunnerTrailProgress.Equipped, Is.EqualTo(RunnerTrailKind.Cosmic));
            Assert.That(RunnerSlashProgress.Equipped, Is.EqualTo(RunnerSlashKind.Water));
            Assert.That(RunnerSlashProgress.Charges, Is.EqualTo(3));
            Assert.That(game.DailyMissions.Claimed(0), Is.True);
            Assert.That(PlayerPrefs.GetInt(RunnerAccountProgress.HighScoreKey), Is.EqualTo(3456));
        }

        private void AssertFreshAccount(int eggs = 0)
        {
            Assert.That(game.Shop.Balance, Is.EqualTo(eggs));
            Assert.That(game.SelectedDinoIndex, Is.Zero);
            Assert.That(game.SelectedMapIndex, Is.EqualTo(game.MapCatalog.Find("forest")));
            Assert.That(game.Shop.IsUnlocked(0), Is.True);
            for (int i = 1; i < game.DinoRoster.Count; i++)
            {
                Assert.That(game.Shop.IsUnlocked(i), Is.False);
                Assert.That(game.Shop.Fragments(i), Is.Zero);
            }
            for (int i = 1; i <= RunnerTrailProgress.Count; i++) Assert.That(RunnerTrailProgress.Owned((RunnerTrailKind)i), Is.False);
            for (int i = 1; i <= 3; i++) Assert.That(RunnerSlashProgress.Owned((RunnerSlashKind)i), Is.False);
            Assert.That(RunnerTrailProgress.Equipped, Is.EqualTo(RunnerTrailKind.None));
            Assert.That(RunnerSlashProgress.Equipped, Is.EqualTo(RunnerSlashKind.None));
            Assert.That(RunnerSlashProgress.Charges, Is.Zero);
            for (int i = 0; i < RunnerDailyMissions.Count; i++)
            {
                Assert.That(game.DailyMissions.Progress(i), Is.Zero);
                Assert.That(game.DailyMissions.Claimed(i), Is.False);
            }
            Assert.That(PlayerPrefs.GetInt(RunnerAccountProgress.HighScoreKey), Is.Zero);
            Assert.That((int)typeof(ForestRunnerGame).GetField("best", Private).GetValue(game), Is.Zero);
            Assert.That(PlayerPrefs.GetInt("DinoRush.Muted"), Is.EqualTo(1));
            Assert.That(PlayerPrefs.GetInt(Unrelated), Is.EqualTo(876));
        }

        [UnityTest]
        public IEnumerator OpeningCancelAndEscapeNeverChangeTheAccountAndTheModalBlocksOptions()
        {
            Click("Options"); yield return new WaitForSecondsRealtime(0.3f);
            Click("Reset account");
            yield return null; // Native Canvas raycast depths refresh after the modal is activated.
            Assert.That(menu.AccountReset.IsShowing, Is.True);
            AssertOwnedProfile();
            game.PrimaryAction();
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Ready));
            var hits = Hits(GameObject.Find("Redeem gift").transform);
            Assert.That(hits[0].gameObject.name, Is.Not.EqualTo("Redeem gift"));
            yield return RunnerDisplayCapture.Capture("reset-account-confirm", 1280, 720);
            yield return RunnerDisplayCapture.Capture("reset-account-confirm-portrait", 360, 800);
            Click("Cancel account reset");
            Assert.That(menu.AccountReset.IsShowing, Is.False);
            Assert.That(menu.DialogOpen, Is.True);
            AssertOwnedProfile();
            Click("Reset account"); yield return null; menu.CloseDialog();
            Assert.That(menu.AccountReset.IsShowing, Is.False);
            Assert.That(menu.CanResetAccount, Is.True, "Escape returns to Options, not the title.");
            AssertOwnedProfile();
        }

        [UnityTest]
        public IEnumerator ConfirmationResetsLiveAndSavedProgressWithoutResurrectingItOnReloadOrRepeatEvents()
        {
            RunnerDailyMissions liveMissions = game.DailyMissions;
            liveMissions.RecordClear();
            Click("Options"); yield return new WaitForSecondsRealtime(0.3f);
            Click("Reset account");
            yield return null;
            Button confirm = GameObject.Find("Confirm account reset").GetComponent<Button>();
            Click("Confirm account reset");
            Assert.That(menu.DialogOpen, Is.False);
            Assert.That(menu.AccountReset.IsShowing, Is.False);
            AssertFreshAccount();
            liveMissions.Flush();
            typeof(ForestRunnerGame).GetMethod("OnApplicationFocus", Private).Invoke(game, new object[] { false });
            typeof(ForestRunnerGame).GetMethod("SaveBest", Private).Invoke(game, null);
            AssertFreshAccount();
            game.Shop.CollectEgg(); confirm.onClick.Invoke();
            AssertFreshAccount(1);
            yield return Reload();
            AssertFreshAccount(1);
            Click("Shop"); yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(GameObject.Find("Shop egg balance").GetComponent<Text>().text, Is.EqualTo("EGGS: 1"));
            Assert.That(GameObject.Find("Buy mystery egg").GetComponent<Button>().interactable, Is.False);
            yield return RunnerDisplayCapture.Capture("reset-account-fresh-shop", 1280, 720);
        }

        [UnityTest]
        public IEnumerator ResetConfirmationReflowsAndHiddenOrGameplayActionsCannotResetProgress()
        {
            menu.AccountReset.Open(); Assert.That(menu.AccountReset.IsShowing, Is.False);
            Click("Options"); yield return new WaitForSecondsRealtime(0.3f);
            yield return RunnerDisplayCapture.Capture("options-account-reset", 1280, 720);
            yield return RunnerDisplayCapture.Capture("options-account-reset-portrait", 360, 800);
            Click("Reset account");
            Transform panel = GameObject.Find("Reset account panel").transform;
            foreach (Vector2Int size in new[] { new Vector2Int(1280, 720), new Vector2Int(320, 568), new Vector2Int(360, 800), new Vector2Int(1024, 768) })
            {
                game.ApplyScreenLayout(RunnerScreenLayout.Calculate(size.x, size.y, new Rect(0, 24, size.x, size.y - 48)));
                Canvas.ForceUpdateCanvases();
                foreach (Text text in panel.GetComponentsInChildren<Text>())
                    Assert.That(text.cachedTextGenerator.characterCountVisible, Is.EqualTo(text.text.Replace("\n", "").Length).Or.EqualTo(text.text.Length), text.name);
                foreach (Button button in panel.GetComponentsInChildren<Button>())
                {
                    Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(panel, button.transform);
                    Assert.That(((RectTransform)panel).rect.Contains(bounds.min), Is.True);
                    Assert.That(((RectTransform)panel).rect.Contains(bounds.max), Is.True);
                }
                AssertOwnedProfile();
            }
            menu.CloseDialog(); menu.CloseDialog();
            game.StartRun();
            Assert.That(game.ResetAccount(), Is.False);
            menu.AccountReset.Open(); Assert.That(menu.AccountReset.IsShowing, Is.False);
            game.TogglePause(); Assert.That(game.ResetAccount(), Is.False);
            game.ReturnToMenu(); AssertOwnedProfile();
        }

        private static List<RaycastResult> Hits(Transform node)
        {
            Canvas.ForceUpdateCanvases();
            var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, node.position), button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits); return hits;
        }
        private static void Click(string name)
        {
            Button button = GameObject.Find(name).GetComponent<Button>();
            var hits = Hits(button.transform);
            Assert.That(hits.Count, Is.GreaterThan(0), name);
            Assert.That(hits[0].gameObject, Is.EqualTo(button.gameObject), name + " is blocked");
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }
    }
}
