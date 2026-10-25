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
        private bool hadMute;
        private int previousMute;

        [UnitySetUp]
        public IEnumerator FreshProfile()
        {
            saved.Clear();
            existing.Clear();
            foreach (string key in RunnerMapPreferenceScope.ShopKeys)
            {
                if (PlayerPrefs.HasKey(key)) existing.Add(key);
                saved[key] = PlayerPrefs.GetInt(key);
                PlayerPrefs.DeleteKey(key);
            }
            hadDino = PlayerPrefs.HasKey(RunnerDinoRoster.SelectionKey);
            previousDino = PlayerPrefs.GetString(RunnerDinoRoster.SelectionKey);
            PlayerPrefs.SetString(RunnerDinoRoster.SelectionKey, "blue");
            hadMute = PlayerPrefs.HasKey("DinoRush.Muted");
            previousMute = PlayerPrefs.GetInt("DinoRush.Muted");
            PlayerPrefs.SetInt("DinoRush.Muted", 0);
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
            if (hadMute) PlayerPrefs.SetInt("DinoRush.Muted", previousMute);
            else PlayerPrefs.DeleteKey("DinoRush.Muted");
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
            Assert.That(GameObject.Find("Buy ten mystery eggs").GetComponent<Button>().interactable, Is.False);
            for (int i = 1; i < game.DinoRoster.Count; i++)
            {
                game.GetComponentInChildren<ForestMainMenu>().ShopCollection.ShowIndex(i);
                Button card = GameObject.Find("Select " + game.DinoRoster[i].DisplayName + " dino").GetComponent<Button>();
                Assert.That(card.interactable, Is.False);
                Assert.That(card.transform.Find("Fragments").GetComponent<Text>().text, Is.EqualTo("0 / 100"));
                game.SelectDino(i); // Also enforce ownership outside UI.
                Assert.That(game.SelectedDinoIndex, Is.Zero);
            }
            game.PrimaryAction();
            game.GetComponentInChildren<ForestMainMenu>().ShopCollection.ShowIndex(0);
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
            yield return null;
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
            Assert.That(reveal.Catalog.Count, Is.EqualTo(game.DinoRoster.Count - 1));
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
                Assert.That(((RectTransform)GameObject.Find("Fragment progress fill").transform).anchorMax.x,
                    Is.EqualTo(0.46f).Within(0.001f), "Resizing/capturing must preserve the actual reward progress bar.");
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
            var fragments = new int[game.DinoRoster.Count];
            for (int i = 1; i < fragments.Length; i++) fragments[i] = game.Shop.Fragments(i);
            menu.Hide();
            Assert.That(menu.ShopPresentation.IsShowing, Is.False);
            yield return Reload();
            Assert.That(game.Shop.Balance, Is.Zero);
            for (int i = 1; i < fragments.Length; i++) Assert.That(game.Shop.Fragments(i), Is.EqualTo(fragments[i]));
            Assert.That(animationFrame == null, Is.True, "Generated presentation frames should be disposed on scene exit.");
            Assert.That(original != null, Is.True, "Never destroy the supplied sprite sheet.");
        }

        private static void Advance(RunnerShopPresentation reveal, float dt) => typeof(RunnerShopPresentation)
            .GetMethod("Advance", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(reveal, new object[] { dt });

        [UnityTest]
        public IEnumerator CollectionPagesShowAllTenDinosWithProgressAndNeverSpendOnNavigation()
        {
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 100);
            PlayerPrefs.SetInt(RunnerShopProgress.FragmentKey("cole"), 42);
            Click("Shop");
            yield return new WaitForSecondsRealtime(0.3f);
            var collection = game.GetComponentInChildren<ForestMainMenu>().ShopCollection;
            Assert.That(collection.PageCount, Is.EqualTo(3));
            for (int page = 0; page < 3; page++)
            {
                Assert.That(collection.Page, Is.EqualTo(page));
                Assert.That(GameObject.Find("Collection page").GetComponent<Text>().text, Is.EqualTo("PAGE " + (page + 1) + " / 3"));
                Assert.That(GameObject.Find("Dino choices").GetComponentsInChildren<Button>().Length, Is.EqualTo(page < 2 ? 4 : 2));
                if (page == 1)
                {
                    Transform cole = GameObject.Find("Select Cole dino").transform;
                    Assert.That(cole.Find("Fragments").GetComponent<Text>().text, Is.EqualTo("42 / 100"));
                    Assert.That(((RectTransform)cole.Find("Collection progress/Fill")).anchorMax.x, Is.EqualTo(0.42f).Within(0.001f));
                }
                yield return RunnerDisplayCapture.Capture("shop-collection-page-" + (page + 1), 1280, 720);
                if (page == 1) yield return RunnerDisplayCapture.Capture("shop-collection-new-portrait", 320, 568);
                if (page < 2) { Click("Next dinos"); yield return null; }
            }
            Assert.That(GameObject.Find("Next dinos").GetComponent<Button>().interactable, Is.False);
            Click("Previous dinos");
            yield return null;
            Assert.That(collection.Page, Is.EqualTo(1));
            Assert.That(game.Shop.Balance, Is.EqualTo(100));
            Assert.That(game.SelectedDinoIndex, Is.Zero);
        }

        [UnityTest]
        public IEnumerator TenPullRequiresFiveHundredSavesEveryRewardAndShowsAnAccurateSkippableResultGrid()
        {
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 499);
            Click("Shop");
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(GameObject.Find("Buy mystery egg").GetComponent<Button>().interactable, Is.True);
            Assert.That(GameObject.Find("Buy ten mystery eggs").GetComponent<Button>().interactable, Is.False);
            GameObject.Find("Buy ten mystery eggs").GetComponent<Button>().onClick.Invoke();
            Assert.That(game.Shop.Balance, Is.EqualTo(499));
            var menu = game.GetComponentInChildren<ForestMainMenu>();
            menu.CloseDialog();
            yield return new WaitForSecondsRealtime(0.3f);
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 1000);
            for (int i = 1; i < game.DinoRoster.Count; i++) PlayerPrefs.SetInt(RunnerShopProgress.FragmentKey(game.DinoRoster[i].Id), 99);
            Click("Shop");
            yield return new WaitForSecondsRealtime(0.3f);
            yield return RunnerDisplayCapture.Capture("shop-ten-pull-option", 1280, 720);
            yield return RunnerDisplayCapture.Capture("shop-ten-pull-option-portrait", 360, 800);
            Click("Buy ten mystery eggs");
            RunnerShopBatchPresentation batch = menu.ShopPresentation.Batch;
            Assert.That(menu.ShopPresentation.IsShowing, Is.True);
            Assert.That(menu.ShopPresentation.Stage, Is.EqualTo(RunnerShopPresentation.HatchStage.Batch));
            Assert.That(batch.RevealedCount, Is.Zero);
            Assert.That(game.Shop.Balance, Is.EqualTo(500));
            Assert.That(GameObject.Find("Buy mystery egg").GetComponent<Button>().interactable, Is.False);
            Assert.That(GameObject.Find("Buy ten mystery eggs").GetComponent<Button>().interactable, Is.False);
            var rewards = (RunnerShopProgress.Reward[])typeof(RunnerShopBatchPresentation).GetField("rewards", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(batch);
            Assert.That(rewards.Length, Is.EqualTo(10));
            var totals = new int[game.DinoRoster.Count];
            int sum = 0, unlocks = 0;
            foreach (var reward in rewards)
            {
                totals[reward.DinoIndex] += reward.Fragments;
                sum += reward.Fragments;
                if (reward.NewlyUnlocked) unlocks++;
            }
            for (int i = 1; i < totals.Length; i++) Assert.That(game.Shop.Fragments(i), Is.EqualTo(99 + totals[i]));
            GameObject.Find("Buy ten mystery eggs").GetComponent<Button>().onClick.Invoke();
            GameObject.Find("Buy mystery egg").GetComponent<Button>().onClick.Invoke();
            Assert.That(game.Shop.Balance, Is.EqualTo(500));
            AdvanceBatch(batch, 0.45f);
            yield return RunnerDisplayCapture.Capture("shop-ten-pull-summon", 1280, 720);
            AdvanceBatch(batch, 2.1f);
            Assert.That(batch.RevealedCount, Is.InRange(1, 5));
            yield return RunnerDisplayCapture.Capture("shop-ten-pull-hatching", 1280, 720);
            Click("Ten pull continue"); // Reveal all; do not dismiss results or make another purchase.
            Assert.That(batch.Complete, Is.True);
            Assert.That(batch.RevealedCount, Is.EqualTo(10));
            for (int i = 0; i < rewards.Length; i++)
            {
                Transform card = GameObject.Find("Ten pull reward " + (i + 1)).transform;
                var reward = rewards[i];
                Assert.That(card.Find("Reward dino").GetComponent<Image>().sprite, Is.SameAs(game.DinoRoster[reward.DinoIndex].Portrait));
                Assert.That(card.Find("Reward labels/Reward name").GetComponent<Text>().text, Is.EqualTo(game.DinoRoster[reward.DinoIndex].DisplayName.ToUpperInvariant()));
                Assert.That(card.Find("Reward labels/Reward fragments").GetComponent<Text>().text, Is.EqualTo("+" + reward.Fragments + " FRAGS"));
                Assert.That(card.Find("Reward labels/Reward badge").GetComponent<Text>().text, Is.EqualTo(reward.NewlyUnlocked ? "NEW DINO!" : "FRAGMENTS"));
            }
            Assert.That(GameObject.Find("Ten pull summary").GetComponent<Text>().text, Is.EqualTo("+" + sum + " FRAGMENTS / " + unlocks + " NEW DINOS"));
            yield return RunnerDisplayCapture.Capture("shop-ten-pull-results", 1280, 720);
            yield return RunnerDisplayCapture.Capture("shop-ten-pull-results-portrait", 360, 800);
            foreach (Vector2Int size in new[] { new Vector2Int(1280, 720), new Vector2Int(1024, 768), new Vector2Int(320, 568) })
            {
                game.ApplyScreenLayout(RunnerScreenLayout.Calculate(size.x, size.y, new Rect(0, 16, size.x, size.y - 32)));
                Canvas.ForceUpdateCanvases();
                Transform panel = GameObject.Find("Ten pull panel").transform;
                var bounds = new List<Bounds>();
                for (int i = 1; i <= 10; i++)
                {
                    Transform card = panel.Find("Ten pull reward " + i);
                    Bounds current = RectTransformUtility.CalculateRelativeRectTransformBounds(panel, card);
                    foreach (Bounds other in bounds) Assert.That(current.Intersects(other), Is.False, "Reward cards must not overlap at " + size);
                    bounds.Add(current);
                    foreach (Text label in card.GetComponentsInChildren<Text>())
                        Assert.That(label.cachedTextGenerator.characterCountVisible, Is.EqualTo(label.text.Length), label.name + " clipped at " + size);
                }
            }
            game.ApplyScreenLayout(RunnerScreenLayout.Calculate(Screen.width, Screen.height, Screen.safeArea));
            Click("Ten pull continue");
            Assert.That(menu.ShopPresentation.IsShowing, Is.False);
            Assert.That(menu.DialogOpen, Is.True);
            Assert.That(game.Shop.Balance, Is.EqualTo(500));
        }

        [UnityTest]
        public IEnumerator TenPullUsesMatchingEggStagesAndRunsUnscaledBehindAModalBlocker()
        {
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 500);
            Click("Shop");
            yield return new WaitForSecondsRealtime(0.3f);
            var menu = game.GetComponentInChildren<ForestMainMenu>();
            var batch = menu.ShopPresentation.Batch;
            Click("Buy ten mystery eggs");
            Image egg = GameObject.Find("Ten pull reward 1").transform.Find("Sealed egg").GetComponent<Image>();
            Vector3 before = egg.transform.parent.localPosition;
            float scale = Time.timeScale;
            try
            {
                Time.timeScale = 0;
                yield return new WaitForSecondsRealtime(0.35f);
                Assert.That(egg.transform.parent.localPosition, Is.Not.EqualTo(before));
                var rewards = (RunnerShopProgress.Reward[])typeof(RunnerShopBatchPresentation).GetField("rewards", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(batch);
                var art = menu.ShopPresentation.Catalog.Find(game.DinoRoster[rewards[0].DinoIndex].Id);
                Assert.That(egg.sprite.texture, Is.SameAs(art.move));
                AdvanceBatch(batch, 1.0f);
                Assert.That(egg.sprite.texture, Is.SameAs(art.crack));
                AdvanceBatch(batch, 0.6f);
                Assert.That(egg.sprite.texture, Is.SameAs(art.hatch));
                var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, GameObject.Find("Buy ten mystery eggs").transform.position) };
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointer, hits);
                Assert.That(hits[0].gameObject.name, Is.Not.EqualTo("Buy ten mystery eggs"));
                menu.CloseDialog();
                Assert.That(batch.Complete, Is.True);
                menu.CloseDialog();
                Assert.That(batch.IsShowing, Is.False);
                Assert.That(menu.DialogOpen, Is.True);
                Assert.That(game.Shop.Balance, Is.Zero);
            }
            finally { Time.timeScale = scale; }
        }

        [UnityTest]
        public IEnumerator InterruptedTenPullPersistsAllTenRewardsAndCleansUpSharedAnimationFrames()
        {
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 500);
            Click("Shop");
            yield return new WaitForSecondsRealtime(0.3f);
            Click("Buy ten mystery eggs");
            var menu = game.GetComponentInChildren<ForestMainMenu>();
            Sprite frame = GameObject.Find("Ten pull reward 1").transform.Find("Sealed egg").GetComponent<Image>().sprite;
            Texture2D source = frame.texture;
            var totals = new int[game.DinoRoster.Count];
            int sum = 0;
            for (int i = 1; i < totals.Length; i++) { totals[i] = game.Shop.Fragments(i); sum += totals[i]; }
            Assert.That(sum, Is.InRange(10, 200));
            menu.Hide();
            Assert.That(menu.ShopPresentation.IsShowing, Is.False);
            yield return Reload();
            Assert.That(game.Shop.Balance, Is.Zero);
            for (int i = 1; i < totals.Length; i++) Assert.That(game.Shop.Fragments(i), Is.EqualTo(totals[i]));
            Assert.That(frame == null, Is.True);
            Assert.That(source != null, Is.True);
        }

        private static void AdvanceBatch(RunnerShopBatchPresentation batch, float dt) => typeof(RunnerShopBatchPresentation)
            .GetMethod("Advance", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(batch, new object[] { dt });

        [UnityTest]
        public IEnumerator TenPullFinishesNaturallyAndAnotherPurchaseStartsAFreshSequenceWithoutReawarding()
        {
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 1000);
            Click("Shop");
            yield return new WaitForSecondsRealtime(0.3f);
            Click("Buy ten mystery eggs");
            var menu = game.GetComponentInChildren<ForestMainMenu>();
            var batch = menu.ShopPresentation.Batch;
            AdvanceBatch(batch, RunnerShopBatchPresentation.Duration + 0.1f);
            Assert.That(batch.Complete, Is.True);
            Assert.That(batch.RevealedCount, Is.EqualTo(10));
            Assert.That(game.Shop.Balance, Is.EqualTo(500));
            yield return RunnerDisplayCapture.Capture("shop-ten-pull-fragment-results", 1280, 720);
            Click("Ten pull continue");
            yield return null;
            Click("Buy ten mystery eggs");
            Assert.That(game.Shop.Balance, Is.Zero);
            Assert.That(batch.RevealedCount, Is.Zero);
            Assert.That(batch.Complete, Is.False);
            Assert.That(GameObject.Find("Ten pull reward 1").transform.Find("Sealed egg").GetComponent<Image>().enabled, Is.True);
            int before = 0;
            for (int i = 1; i < game.DinoRoster.Count; i++) before += game.Shop.Fragments(i);
            AdvanceBatch(batch, RunnerShopBatchPresentation.Duration + 1);
            AdvanceBatch(batch, RunnerShopBatchPresentation.Duration);
            int after = 0;
            for (int i = 1; i < game.DinoRoster.Count; i++) after += game.Shop.Fragments(i);
            Assert.That(after, Is.EqualTo(before), "Finishing or continuing a reveal never writes rewards again.");
            Assert.That(game.Shop.Balance, Is.Zero);
            menu.CloseDialog();
            for (int i = 1; i < game.DinoRoster.Count; i++) PlayerPrefs.SetInt(RunnerShopProgress.FragmentKey(game.DinoRoster[i].Id), 100);
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 1000);
            menu.CloseDialog();
            yield return new WaitForSecondsRealtime(0.3f);
            Click("Shop");
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(GameObject.Find("Buy mystery egg").GetComponent<Button>().interactable, Is.False);
            Assert.That(GameObject.Find("Buy ten mystery eggs").GetComponent<Button>().interactable, Is.False);
        }

        [UnityTest]
        public IEnumerator EggWalletsStayReadableAccurateAndNonBlockingInEveryCategoryAndOrientation()
        {
            Click("Shop");
            yield return new WaitForSecondsRealtime(0.3f);
            var menu = game.GetComponentInChildren<ForestMainMenu>();
            foreach (int balance in new[] { 0, 49, 50, 499, 500, 2000, 5000, int.MaxValue })
            {
                PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, balance);
                typeof(ForestMainMenu).GetMethod("RefreshShop", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(menu, null);
                foreach (bool portrait in new[] { false, true })
                {
                    Vector2Int size = portrait ? new Vector2Int(320, 568) : new Vector2Int(1280, 720);
                    game.ApplyScreenLayout(RunnerScreenLayout.Calculate(size.x, size.y, new Rect(0, 24, size.x, size.y - 48)));
                    for (int category = 0; category < 3; category++)
                    {
                        if (category == 0) menu.TrailShop.ShowTrails(false);
                        else if (category == 1) menu.TrailShop.ShowTrails(true);
                        else menu.TrailShop.ShowSlashes();
                        Canvas.ForceUpdateCanvases();
                        Assert.That(menu.ShopCollection.transform.Find("Dino selection").gameObject.activeSelf, Is.False,
                            "The retired selection caption must not overlap the phone collection heading.");
                        var wallets = menu.GetComponentsInChildren<RunnerShopWallet>();
                        Assert.That(wallets.Length, Is.EqualTo(1), "Only the selected category's wallet should be visible.");
                        Text label = wallets[0].GetComponentInChildren<Text>();
                        Assert.That(label.text, Is.EqualTo(RunnerShopWallet.Caption(balance)));
                        Assert.That(label.fontSize, Is.InRange(16, 18));
                        Assert.That(((RectTransform)wallets[0].transform).rect.height, Is.LessThanOrEqualTo(44));
                        Assert.That(((RectTransform)wallets[0].transform).rect.width, Is.LessThanOrEqualTo(360));
                        Assert.That(label.cachedTextGenerator.characterCountVisible, Is.EqualTo(label.text.Length), "The full balance must fit, including the maximum saved wallet.");
                        Assert.That(wallets[0].GetComponentInChildren<Image>().raycastTarget, Is.False);
                        Assert.That(wallets[0].transform.Find("Wallet egg icon").GetComponent<Image>().sprite, Is.Not.Null);
                        foreach (Graphic art in wallets[0].GetComponentsInChildren<Graphic>()) Assert.That(art.raycastTarget, Is.False);
                    }
                }
                Assert.That(game.Shop.Balance, Is.EqualTo(balance), "Switching categories and resizing must never spend eggs.");
            }
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 12345);
            typeof(ForestMainMenu).GetMethod("RefreshShop", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(menu, null);
            for (int category = 0; category < 3; category++)
            {
                if (category == 0) menu.TrailShop.ShowTrails(false);
                else if (category == 1) menu.TrailShop.ShowTrails(true);
                else menu.TrailShop.ShowSlashes();
                yield return RunnerDisplayCapture.Capture("shop-wallet-" + category, 1280, 720);
                yield return RunnerDisplayCapture.Capture("shop-wallet-portrait-" + category, 360, 800);
            }
        }

        private RunnerAudio Audio => (RunnerAudio)typeof(ForestRunnerGame).GetField("audioCues", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);

        [UnityTest]
        public IEnumerator SinglePullPlaysStageSoundsOnceUnlocksCelebrateAndSkipDoesNotStackShellSounds()
        {
            Click("Shop");
            yield return new WaitForSecondsRealtime(0.3f);
            var reveal = game.GetComponentInChildren<ForestMainMenu>().ShopPresentation;
            reveal.enabled = false;
            var cues = new List<RunnerAudio.EggCue>();
            Audio.EggCuePlayed += cues.Add;
            reveal.Play(game.DinoRoster[1], 5, 100, true);
            Assert.That(cues, Is.EqualTo(new[] { RunnerAudio.EggCue.Pull }));
            Advance(reveal, RunnerShopPresentation.CrackTime + 0.01f);
            Advance(reveal, RunnerShopPresentation.HatchTime - RunnerShopPresentation.CrackTime);
            Advance(reveal, RunnerShopPresentation.RewardTime - RunnerShopPresentation.HatchTime);
            Assert.That(cues.GetRange(0, 4), Is.EqualTo(new[] { RunnerAudio.EggCue.Pull, RunnerAudio.EggCue.Crack, RunnerAudio.EggCue.Charge, RunnerAudio.EggCue.Unlock }));
            Advance(reveal, 1);
            int count = cues.Count;
            reveal.RefreshLayout(); Advance(reveal, 1);
            Assert.That(cues.Count, Is.EqualTo(count));
            reveal.HandleBack();
            cues.Clear();
            reveal.Play(game.DinoRoster[1], 5, 15, false);
            reveal.HandleBack();
            Assert.That(cues, Is.EqualTo(new[] { RunnerAudio.EggCue.Pull, RunnerAudio.EggCue.Reveal }));
            Assert.That(reveal.DisplayedFragments, Is.EqualTo(5));
            reveal.HandleBack();
            Audio.EggCuePlayed -= cues.Add;
            yield return null;
        }

        [UnityTest]
        public IEnumerator TenPullChimesEachCardMuteSuppressesCuesAndSlowFramesOnlyPlayOneReveal()
        {
            Click("Shop");
            yield return new WaitForSecondsRealtime(0.3f);
            var reveal = game.GetComponentInChildren<ForestMainMenu>().ShopPresentation;
            var batch = reveal.Batch;
            batch.enabled = false;
            var rewards = new RunnerShopProgress.Reward[10];
            for (int i = 0; i < rewards.Length; i++) rewards[i] = new RunnerShopProgress.Reward(1, 5, 50 + i * 5, i == 9);
            var cues = new List<RunnerAudio.EggCue>();
            Audio.EggCuePlayed += cues.Add;
            batch.Play(game.DinoRoster, rewards);
            AdvanceBatch(batch, RunnerShopBatchPresentation.GatherTime + 0.01f);
            AdvanceBatch(batch, RunnerShopBatchPresentation.RevealTime - RunnerShopBatchPresentation.GatherTime);
            for (int i = 1; i < 10; i++) AdvanceBatch(batch, RunnerShopBatchPresentation.RevealSpacing);
            Assert.That(cues.FindAll(c => c == RunnerAudio.EggCue.Reveal).Count, Is.EqualTo(9));
            Assert.That(cues.FindAll(c => c == RunnerAudio.EggCue.Unlock).Count, Is.EqualTo(1));
            Assert.That(cues[0], Is.EqualTo(RunnerAudio.EggCue.Pull));
            Assert.That(cues[1], Is.EqualTo(RunnerAudio.EggCue.Crack));
            batch.Cancel(); cues.Clear();
            batch.Play(game.DinoRoster, rewards);
            AdvanceBatch(batch, RunnerShopBatchPresentation.Duration + 1);
            Assert.That(cues, Is.EqualTo(new[] { RunnerAudio.EggCue.Pull, RunnerAudio.EggCue.Unlock }));
            batch.Cancel(); cues.Clear();
            Audio.ToggleMute();
            batch.Play(game.DinoRoster, rewards);
            AdvanceBatch(batch, RunnerShopBatchPresentation.Duration + 1);
            Assert.That(cues, Is.Empty);
            foreach (AudioSource source in game.GetComponents<AudioSource>()) Assert.That(source.mute, Is.True);
            batch.Cancel();
            Audio.ToggleMute();
            batch.Play(game.DinoRoster, rewards); batch.HandleBack();
            Assert.That(cues, Is.EqualTo(new[] { RunnerAudio.EggCue.Pull, RunnerAudio.EggCue.Unlock }));
            batch.Cancel();
            Audio.EggCuePlayed -= cues.Add;
            Assert.That(game.Shop.Balance, Is.Zero, "Audio and presentation do not write wallet progress.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator GeneratedCuesAreDisposedAndSharedElementSamplesRemainAudibleAcrossAudioInstances()
        {
            var owner = new GameObject("Shop audio lifecycle test");
            Random.State state = Random.state;
            float expected = Random.value;
            Random.state = state;
            var audio = new RunnerAudio(owner);
            Assert.That(Random.value, Is.EqualTo(expected), "Sound synthesis must not consume gameplay randomness.");
            Random.state = state;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var clips = new List<AudioClip>((AudioClip[])typeof(RunnerAudio).GetField("eggCues", flags).GetValue(audio));
            clips.AddRange((AudioClip[])typeof(RunnerAudio).GetField("revealNotes", flags).GetValue(audio));
            var click = (AudioClip)typeof(RunnerAudio).GetField("buttonClick", flags).GetValue(audio);
            var slashes = (AudioClip[])typeof(RunnerAudio).GetField("slashClips", flags).GetValue(audio);
            clips.Add(click);
            Assert.That(click.length, Is.InRange(0.04f, 0.09f));
            Assert.That(slashes.Length, Is.EqualTo(3));
            for (int i = 0; i < slashes.Length; i++)
            {
                Assert.That(slashes[i].name, Does.Contain(((RunnerSlashKind)(i + 1)).ToString()));
                Assert.That(slashes[i], Is.SameAs(Resources.Load<AudioClip>("Audio/Slashes/" + (RunnerSlashKind)(i + 1) + " slash")),
                    "Use the authored elemental samples, not a regenerated approximation.");
                Assert.That(slashes[i].channels, Is.EqualTo(1));
                Assert.That(slashes[i].length, i == (int)RunnerSlashKind.Lightning - 1 ? Is.InRange(0.7f, 0.85f) : Is.InRange(0.25f, 0.8f));
                for (int j = 0; j < i; j++) Assert.That(slashes[i].samples, Is.Not.EqualTo(slashes[j].samples), "Each element has a different envelope.");
                var body = new float[slashes[i].samples]; slashes[i].GetData(body, 0);
                double energy = 0;
                foreach (float sample in body) energy += sample * sample;
                Assert.That(System.Math.Sqrt(energy / body.Length), Is.InRange(0.08, 0.3),
                    "The body of every element must remain audible, not just a single normalized spike.");
                if (i == (int)RunnerSlashKind.Lightning - 1)
                {
                    double attack = 0, tail = 0;
                    int window = Mathf.RoundToInt(slashes[i].frequency * 0.04f);
                    for (int n = 0; n < window; n++)
                    {
                        attack += body[n] * body[n];
                        tail += body[body.Length - 1 - n] * body[body.Length - 1 - n];
                    }
                    Assert.That(attack, Is.GreaterThan(tail * 8), "Lightning should snap immediately and decay, not swell like a spell.");
                    int sustainStart = Mathf.RoundToInt(slashes[i].frequency * 0.2f);
                    int sustainEnd = Mathf.RoundToInt(slashes[i].frequency * 0.55f);
                    double sustain = 0;
                    for (int n = sustainStart; n < sustainEnd; n++) sustain += body[n] * body[n];
                    Assert.That(System.Math.Sqrt(sustain / (sustainEnd - sustainStart)), Is.GreaterThan(0.06),
                        "The longer zap must contain audible electricity through the slash, not added silence.");
                }
            }
            var allClips = new List<AudioClip>(clips); allClips.AddRange(slashes);
            foreach (AudioClip clip in allClips)
            {
                var samples = new float[clip.samples];
                Assert.That(clip.GetData(samples, 0), Is.True);
                float peak = 0;
                foreach (float sample in samples)
                {
                    Assert.That(float.IsNaN(sample) || float.IsInfinity(sample), Is.False);
                    peak = Mathf.Max(peak, Mathf.Abs(sample));
                }
                Assert.That(peak, Is.InRange(0.1f, 0.91f), clip.name);
                Assert.That(Mathf.Abs(samples[0]), Is.LessThan(0.01f));
                Assert.That(Mathf.Abs(samples[samples.Length - 1]), Is.LessThan(0.01f));
            }
            AudioSource[] sources = owner.GetComponents<AudioSource>();
            audio.Dispose();
            yield return null;
            foreach (AudioClip clip in clips) Assert.That(clip == null, Is.True);
            foreach (AudioSource source in sources) Assert.That(source == null, Is.True);
            var nextAudio = new RunnerAudio(owner);
            var nextSlashes = (AudioClip[])typeof(RunnerAudio).GetField("slashClips", flags).GetValue(nextAudio);
            for (int i = 0; i < slashes.Length; i++)
            {
                Assert.That(slashes[i] != null, Is.True, "Disposing an audio owner must not destroy imported assets.");
                Assert.That(nextSlashes[i], Is.SameAs(slashes[i]));
            }
            nextAudio.Dispose();
            Object.Destroy(owner);
            yield return null;
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
