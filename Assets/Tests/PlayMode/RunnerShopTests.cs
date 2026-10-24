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
            var glow = game.GetComponentInChildren<RunnerHatchGlowGraphic>();
            Canvas.ForceUpdateCanvases();
            Mesh glowMesh = glow.canvasRenderer.GetMesh();
            Assert.That(glowMesh, Is.Not.Null);
            Assert.That(glowMesh.vertexCount, Is.GreaterThan(0), "The egg showcase's light/glints must actually render.");
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
            RunnerShopPresentation reveal = game.GetComponentInChildren<ForestMainMenu>().ShopPresentation;
            Assert.That(reveal.IsShowing, Is.True);
            Assert.That(reveal.Stage, Is.EqualTo(RunnerShopPresentation.HatchStage.Wobble));
            // A second click/event cannot pay again while the reward is being presented.
            GameObject.Find("Buy mystery egg").GetComponent<Button>().onClick.Invoke();
            Assert.That(game.Shop.Balance, Is.EqualTo(50));
            Advance(reveal, RunnerShopPresentation.Duration);
            Assert.That(GameObject.Find("Fragment reward amount").GetComponent<Text>().text,
                Is.EqualTo("+" + (game.Shop.Fragments(unlocked) - 99)));
            yield return RunnerDisplayCapture.Capture("shop-hatch-unlocked", 1280, 720);
            Click("Hatch continue");
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

        [UnityTest]
        public IEnumerator EverySuppliedEggPlaysMoveCrackAndHatchThenCountsTheCorrectDinosFragments()
        {
            Click("Shop");
            yield return new WaitForSecondsRealtime(0.3f);
            RunnerShopPresentation reveal = game.GetComponentInChildren<ForestMainMenu>().ShopPresentation;
            Assert.That(reveal.Catalog.Count, Is.EqualTo(3));
            int balance = game.Shop.Balance;
            for (int index = 1; index < game.DinoRoster.Count; index++)
            {
                RunnerDinoRoster.Dino dino = game.DinoRoster[index];
                RunnerEggHatchCatalog.Egg art = reveal.Catalog.Find(dino.Id);
                reveal.Play(dino, 17, 46, false);
                Image eggImage = GameObject.Find("Hatching egg").GetComponent<Image>();
                Advance(reveal, 0.3f);
                Assert.That(eggImage.sprite.texture, Is.SameAs(art.move));
                Assert.That(eggImage.sprite.rect.size, Is.EqualTo(Vector2.one * 24));
                Advance(reveal, 0.9f);
                Assert.That(reveal.Stage, Is.EqualTo(RunnerShopPresentation.HatchStage.Crack));
                Assert.That(eggImage.sprite.texture, Is.SameAs(art.crack));
                Advance(reveal, 0.7f);
                Assert.That(reveal.Stage, Is.EqualTo(RunnerShopPresentation.HatchStage.Hatch));
                Assert.That(eggImage.sprite.texture, Is.SameAs(art.hatch));
                if (index == 1) yield return RunnerDisplayCapture.Capture("shop-hatch-shell", 1280, 720);
                Advance(reveal, 0.7f);
                Assert.That(reveal.Stage, Is.EqualTo(RunnerShopPresentation.HatchStage.Reward));
                Assert.That(reveal.DisplayedFragments, Is.InRange(1, 16), "The count should animate, not jump straight to 17.");
                Advance(reveal, 1f);
                Assert.That(reveal.DisplayedFragments, Is.EqualTo(17));
                Assert.That(GameObject.Find("Fragment reward species").GetComponent<Text>().text, Is.EqualTo(dino.DisplayName.ToUpperInvariant() + " DINO FRAGMENTS"));
                Assert.That(GameObject.Find("Fragment reward progress").GetComponent<Text>().text, Is.EqualTo("46 / 100 FRAGMENTS"));
                Assert.That(GameObject.Find("Hatched dino").GetComponent<Image>().sprite, Is.SameAs(dino.Portrait));
                yield return RunnerDisplayCapture.Capture("shop-hatch-" + dino.Id, 1280, 720);
                if (index == 2) yield return RunnerDisplayCapture.Capture("shop-hatch-portrait", 360, 800);
                Click("Hatch continue");
                Assert.That(reveal.IsShowing, Is.False);
            }
            Assert.That(game.Shop.Balance, Is.EqualTo(balance), "Presentation must not award or charge anything itself.");
        }

        [UnityTest]
        public IEnumerator HatchRunsWithScaledTimePausedBlocksUnderlyingButtonsAndEscapeSkipsWithoutSpendingAgain()
        {
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 100);
            Click("Shop");
            yield return new WaitForSecondsRealtime(0.3f);
            Click("Buy mystery egg");
            ForestMainMenu menu = game.GetComponentInChildren<ForestMainMenu>();
            RunnerShopPresentation reveal = menu.ShopPresentation;
            Sprite first = GameObject.Find("Hatching egg").GetComponent<Image>().sprite;
            float scale = Time.timeScale;
            try
            {
                Time.timeScale = 0;
                yield return new WaitForSecondsRealtime(0.45f);
                Assert.That(GameObject.Find("Hatching egg").GetComponent<Image>().sprite, Is.Not.SameAs(first));
                Assert.That(game.Shop.Balance, Is.EqualTo(50));
                var pointer = new PointerEventData(EventSystem.current) { position =
                    RectTransformUtility.WorldToScreenPoint(null, GameObject.Find("Buy mystery egg").transform.position) };
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointer, hits);
                Assert.That(hits[0].gameObject.name, Is.Not.EqualTo("Buy mystery egg"));
                menu.CloseDialog(); // Escape skips to the saved result, not out of the shop.
                Assert.That(reveal.Stage, Is.EqualTo(RunnerShopPresentation.HatchStage.Reward));
                Assert.That(reveal.DisplayedFragments, Is.InRange(1, 20));
                Assert.That(game.Shop.Balance, Is.EqualTo(50));
                menu.CloseDialog();
                Assert.That(reveal.IsShowing, Is.False);
                Assert.That(menu.DialogOpen, Is.True);
                menu.CloseDialog();
                Assert.That(menu.DialogOpen, Is.False);
            }
            finally { Time.timeScale = scale; }
        }

        [UnityTest]
        public IEnumerator InterruptingTheHatchKeepsTheCommittedRewardAndCleansUpItsCachedSprites()
        {
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 50);
            Click("Shop");
            yield return new WaitForSecondsRealtime(0.3f);
            Click("Buy mystery egg");
            ForestMainMenu menu = game.GetComponentInChildren<ForestMainMenu>();
            Sprite animationFrame = GameObject.Find("Hatching egg").GetComponent<Image>().sprite;
            Texture2D original = animationFrame.texture;
            int[] fragments = { game.Shop.Fragments(1), game.Shop.Fragments(2), game.Shop.Fragments(3) };
            menu.Hide();
            Assert.That(menu.ShopPresentation.IsShowing, Is.False);
            yield return Reload();
            Assert.That(game.Shop.Balance, Is.Zero);
            for (int i = 1; i < 4; i++) Assert.That(game.Shop.Fragments(i), Is.EqualTo(fragments[i - 1]));
            Assert.That(animationFrame == null, Is.True, "Generated presentation frames should be disposed on scene exit.");
            Assert.That(original != null, Is.True, "Never destroy the supplied sprite sheet.");
        }

        private static void Advance(RunnerShopPresentation reveal, float dt) => typeof(RunnerShopPresentation)
            .GetMethod("Advance", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(reveal, new object[] { dt });

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
