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
    public sealed class RunnerShopTests
    {
        private ForestRunnerGame game;
        private readonly Dictionary<string, int> saved = new Dictionary<string, int>();
        private readonly HashSet<string> existing = new HashSet<string>();
        private string previousDino;
        private bool hadDino;

        [UnitySetUp]
        public IEnumerator FreshProfile()
        {
            saved.Clear();
            existing.Clear();
            foreach (string key in new[] { RunnerShopProgress.WalletKey, RunnerShopProgress.FragmentKey("red"),
                RunnerShopProgress.FragmentKey("yellow"), RunnerShopProgress.FragmentKey("green") })
            {
                if (PlayerPrefs.HasKey(key)) existing.Add(key);
                saved[key] = PlayerPrefs.GetInt(key);
                PlayerPrefs.DeleteKey(key);
            }
            hadDino = PlayerPrefs.HasKey(RunnerDinoRoster.SelectionKey);
            previousDino = PlayerPrefs.GetString(RunnerDinoRoster.SelectionKey);
            PlayerPrefs.SetString(RunnerDinoRoster.SelectionKey, "blue");
            yield return Reload();
        }

        private IEnumerator Reload()
        {
            yield return SceneManager.LoadSceneAsync("ForestRun");
            game = Object.FindAnyObjectByType<ForestRunnerGame>();
            game.enabled = false;
            foreach (BaseInputModule input in EventSystem.current.GetComponents<BaseInputModule>()) input.enabled = false;
            yield return new WaitForSecondsRealtime(0.35f);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            if (game != null) Object.Destroy(game.gameObject);
            yield return null;
            foreach (var pair in saved)
                if (existing.Contains(pair.Key)) PlayerPrefs.SetInt(pair.Key, pair.Value);
                else PlayerPrefs.DeleteKey(pair.Key);
            if (hadDino) PlayerPrefs.SetString(RunnerDinoRoster.SelectionKey, previousDino);
            else PlayerPrefs.DeleteKey(RunnerDinoRoster.SelectionKey);
            PlayerPrefs.Save();
        }

        [UnityTest]
        public IEnumerator ShopReplacesCustomizeShowsLockedProgressAndBlocksUnaffordablePurchases()
        {
            Assert.That(GameObject.Find("Customize"), Is.Null);
            Assert.That(GameObject.Find("Shop").GetComponentInChildren<Text>().text, Is.EqualTo("SHOP"));
            Click("Shop");
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(GameObject.Find("Dialog heading").GetComponent<Text>().text, Is.EqualTo("SHOP"));
            Assert.That(GameObject.Find("Shop egg balance").GetComponent<Text>().text, Is.EqualTo("EGGS: 0"));
            Assert.That(GameObject.Find("Buy mystery egg").GetComponent<Button>().interactable, Is.False);
            for (int i = 1; i < game.DinoRoster.Count; i++)
            {
                Button card = GameObject.Find("Select " + game.DinoRoster[i].DisplayName + " dino").GetComponent<Button>();
                Assert.That(card.interactable, Is.False);
                Assert.That(card.transform.Find("Fragments").GetComponent<Text>().text, Is.EqualTo("0 / 100\nLOCKED"));
                game.SelectDino(i); // Also enforce ownership outside UI.
                Assert.That(game.SelectedDinoIndex, Is.Zero);
            }
            game.PrimaryAction();
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Ready));
            yield return RunnerDisplayCapture.Capture("shop-locked", 1280, 720);
            yield return RunnerDisplayCapture.Capture("shop-portrait", 360, 800);
        }

        [UnityTest]
        public IEnumerator PurchaseRevealsRewardUnlocksItsCardAndPersistsOwnershipWalletAndSelection()
        {
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 100);
            for (int i = 1; i < game.DinoRoster.Count; i++)
                PlayerPrefs.SetInt(RunnerShopProgress.FragmentKey(game.DinoRoster[i].Id), 99);
            Click("Shop");
            yield return new WaitForSecondsRealtime(0.3f);
            Click("Buy mystery egg");
            Assert.That(game.Shop.Balance, Is.EqualTo(50));
            Assert.That(GameObject.Find("Shop egg balance").GetComponent<Text>().text, Is.EqualTo("EGGS: 50"));
            int unlocked = -1;
            for (int i = 1; i < game.DinoRoster.Count; i++)
                if (game.Shop.IsUnlocked(i)) unlocked = i;
            Assert.That(unlocked, Is.GreaterThan(0));
            Assert.That(game.Shop.Fragments(unlocked), Is.InRange(100, 119));
            Assert.That(GameObject.Find("Mystery egg result").GetComponent<Text>().text, Does.Contain("FRAGMENTS!").And.Contain("DINO UNLOCKED!"));
            Button card = GameObject.Find("Select " + game.DinoRoster[unlocked].DisplayName + " dino").GetComponent<Button>();
            Assert.That(card.interactable, Is.True);
            Click(card.name);
            Assert.That(game.SelectedDinoIndex, Is.EqualTo(unlocked));
            yield return RunnerDisplayCapture.Capture("shop-unlocked", 1280, 720);
            yield return Reload();
            Assert.That(game.SelectedDinoIndex, Is.EqualTo(unlocked));
            Assert.That(game.Shop.Balance, Is.EqualTo(50));
            Assert.That(game.Shop.IsUnlocked(unlocked), Is.True);
        }

        [UnityTest]
        public IEnumerator PickupsAreBankedOnceAndSurviveRestartMenuAndSceneReload()
        {
            game.StartRun();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(ForestRunnerGame).GetField("spawnTimer", flags).SetValue(game, float.MaxValue);
            MethodInfo step = typeof(ForestRunnerGame).GetMethod("Step", flags);
            for (int i = 0; i < 195; i++) step.Invoke(game, new object[] { RunnerRules.SimulationStep });
            Assert.That(game.EggsCollected, Is.EqualTo(4));
            Assert.That(game.Shop.Balance, Is.EqualTo(4));
            game.TogglePause();
            Assert.That(game.Shop.Balance, Is.EqualTo(4));
            game.RestartRun();
            Assert.That(game.EggsCollected, Is.Zero);
            Assert.That(game.Shop.Balance, Is.EqualTo(4));
            game.ReturnToMenu();
            Assert.That(game.Shop.Balance, Is.EqualTo(4));
            PlayerPrefs.SetString(RunnerDinoRoster.SelectionKey, "red");
            yield return Reload();
            Assert.That(game.Shop.Balance, Is.EqualTo(4));
            Assert.That(game.SelectedDinoIndex, Is.Zero, "A saved but locked choice must fall back to Blue.");
        }

        private static void Click(string name)
        {
            Canvas.ForceUpdateCanvases();
            Button button = GameObject.Find(name).GetComponent<Button>();
            var rect = (RectTransform)button.transform;
            var pointer = new PointerEventData(EventSystem.current) {
                position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)),
                button = PointerEventData.InputButton.Left
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits.Count, Is.GreaterThan(0), name);
            Assert.That(hits[0].gameObject, Is.EqualTo(button.gameObject), name + " is blocked");
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }
    }
}
