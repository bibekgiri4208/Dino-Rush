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
            foreach (string key in RunnerMapPreferenceScope.ShopKeys)
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
