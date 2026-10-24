using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace DinoRush
{
    /// <summary>The scene-authored title screen, mystery-egg shop, selectors, and settings dialogs.</summary>
    public sealed class ForestMainMenu : MonoBehaviour
    {
        [SerializeField] private Button play;
        [SerializeField] private Button customize;
        [SerializeField] private Button options;
        [SerializeField] private Button quit;
        [SerializeField] private Button sound;
        [SerializeField] private Button back;
        [SerializeField] private Button confirmQuit;
        [SerializeField] private Button optionsSound;
        [SerializeField] private GameObject choices;
        [SerializeField] private GameObject logo;
        [SerializeField] private GameObject dialog;
        [SerializeField] private GameObject customizePage;
        [SerializeField] private GameObject optionsPage;
        [SerializeField] private GameObject quitPage;
        [SerializeField] private GameObject modePage;
        [SerializeField] private Button endlessMode;
        [SerializeField] private GameObject[] gameplayChrome;
        [SerializeField] private Image speaker;
        [SerializeField] private Sprite speakerOn;
        [SerializeField] private Sprite speakerOff;
        [SerializeField] private Text soundCaption;
        [SerializeField] private Text optionsSoundCaption;
        [SerializeField] private Text dialogTitle;
        [SerializeField] private Button[] dinoChoices = new Button[0];
        [SerializeField] private Image[] dinoPortraits = new Image[0];
        [SerializeField] private Text[] dinoCaptions = new Text[0];
        [SerializeField] private Text selectedDinoLabel;
        [SerializeField] private Sprite selectedDinoFrame;
        [SerializeField] private Sprite unselectedDinoFrame;
        [SerializeField] private GameObject mapPage;
        [SerializeField] private Button[] mapChoices = new Button[0];
        [SerializeField] private Image[] mapPreviews = new Image[0];
        [SerializeField] private Text[] mapCaptions = new Text[0];
        [SerializeField] private Text mapStatus;
        [SerializeField] private Text customizeMapLabel;
        [SerializeField] private Button startMap;
        [SerializeField] private Button mapBack;
        [SerializeField] private Button buyMysteryEgg;
        [SerializeField] private Button buyTenMysteryEggs;
        [SerializeField] private Text shopBalance;
        [SerializeField] private Text shopResult;
        [SerializeField] private Text[] dinoFragments = new Text[0];
        [SerializeField] private RunnerShopPresentation shopPresentation;
        [SerializeField] private RunnerShopCollection shopCollection;
        [SerializeField] private Button redeemGift;
        [SerializeField] private RunnerRedeemPresentation redeemPresentation;
        [SerializeField] private RunnerTrailShop trailShop;
        private int pendingRewardIndex = -1;
        private RunnerShopProgress shop;
        private int selectedDino;
        private RunnerMapCatalog mapCatalog;
        private bool mapsBound;
        private RunnerDinoRoster dinoRoster;
        private bool dinosBound;
        private bool bound;
        private bool dialogPortrait;
        private Vector2 dialogAvailable = new Vector2(1280, 720);

        public bool DialogOpen => (dialog != null && dialog.activeInHierarchy) || (mapPage != null && mapPage.activeInHierarchy);
        public bool HasMapSelector => mapPage != null;
        public bool HasModeSelector => modePage != null;
        public RunnerShopPresentation ShopPresentation => shopPresentation;
        public RunnerShopCollection ShopCollection => shopCollection;
        public RunnerRedeemPresentation RedeemPresentation => redeemPresentation;
        public RunnerTrailShop TrailShop => trailShop;

        public void ApplyDialogLayout(bool portrait, Vector2 available)
        {
            dialogPortrait = portrait;
            dialogAvailable = available;
            if (dialogTitle == null) return;
            var kind = quitPage != null && quitPage.activeSelf ? RunnerCompactMenus.DialogKind.Quit :
                customizePage != null && customizePage.activeSelf ? RunnerCompactMenus.DialogKind.Shop :
                modePage != null && modePage.activeSelf ? RunnerCompactMenus.DialogKind.Mode : RunnerCompactMenus.DialogKind.Options;
            RunnerCompactMenus.ApplyDialog(dialogTitle.transform.parent, kind, portrait, available);
            shopPresentation?.ApplyBatchLayout(portrait, available);
            redeemPresentation?.ApplyLayout(portrait, available);
            trailShop?.ApplyLayout(portrait);
        }

        public void OpenModeSelector()
        {
            if (modePage == null) { OpenMapSelector(); return; }
            OpenDialog("CHOOSE YOUR MODE", modePage);
        }

        public void OpenMapSelector()
        {
            if (mapPage == null) return;
            choices.SetActive(false);
            logo.SetActive(false);
            dialog.SetActive(false);
            mapPage.SetActive(true);
        }

        public void InitializeMaps(RunnerMapCatalog catalog, int selected, Action<int> select, Action start)
        {
            mapCatalog = catalog;
            if (catalog == null || catalog.Count == 0 || mapChoices.Length != catalog.Count) return;
            Text startLabel = startMap != null ? startMap.GetComponentInChildren<Text>(true) : null;
            if (startLabel != null) startLabel.text = "CONTINUE";
            if (!mapsBound)
            {
                mapsBound = true;
                for (int i = 0; i < mapChoices.Length; i++)
                {
                    int index = i;
                    Bind(mapChoices[i], () => select(index));
                }
                Bind(startMap, start);
                Bind(mapBack, () =>
                {
                    if (HasModeSelector) OpenModeSelector();
                    else CloseDialog();
                });
            }
            SetMapSelection(selected);
        }

        public void SetMapSelection(int selected)
        {
            if (mapCatalog == null || selected < 0 || selected >= mapCatalog.Count || mapChoices.Length != mapCatalog.Count) return;
            for (int i = 0; i < mapChoices.Length; i++)
            {
                mapPreviews[i].sprite = mapCatalog[i].preview;
                mapCaptions[i].text = mapCatalog[i].displayName.ToUpperInvariant();
                mapChoices[i].GetComponent<Image>().sprite = i == selected ? selectedDinoFrame : unselectedDinoFrame;
            }
            string name = mapCatalog[selected].displayName.ToUpperInvariant();
            mapStatus.text = name + " SELECTED / READY TO RUN";
            if (customizeMapLabel != null) customizeMapLabel.text = "MAP: " + name;
        }

        public void Initialize(Action start, Action toggleSound)
        {
            if (bound) return;
            bound = true;
            Bind(play, start);
            if (endlessMode != null) Bind(endlessMode, OpenMapSelector);
            Bind(customize, () =>
            {
                RefreshShop();
                shopCollection?.ShowIndex(selectedDino);
                OpenDialog("SHOP", customizePage);
                trailShop?.ShowTrails(false);
            });
            Bind(options, () => OpenDialog("OPTIONS", optionsPage));
            Bind(quit, () => OpenDialog("QUIT GAME?", quitPage));
            Bind(back, CloseDialog);
            Bind(sound, toggleSound);
            Bind(optionsSound, toggleSound);
            Bind(confirmQuit, QuitGame);
            if (buyMysteryEgg != null) Bind(buyMysteryEgg, BuyMysteryEgg);
            if (buyTenMysteryEggs != null) Bind(buyTenMysteryEggs, BuyTenMysteryEggs);
            if (redeemGift != null) Bind(redeemGift, () =>
            {
                if (optionsPage.activeInHierarchy) redeemPresentation.Open();
            });
        }

        public void InitializeShop(RunnerShopProgress progress, RunnerAudio audio = null)
        {
            shop = progress;
            EnsureShopUi();
            EnsureRedeemUi();
            EnsureTrailUi();
            trailShop?.Initialize(shop, shopPresentation, audio, RefreshShop);
            redeemPresentation?.Initialize(shop, audio, RefreshShop);
            shopCollection?.Initialize(shop);
            shopPresentation?.Initialize(audio, () =>
            {
                if (pendingRewardIndex >= 0) shopCollection?.ShowIndex(pendingRewardIndex);
                pendingRewardIndex = -1;
                RefreshShop();
            });
        }

        public void EnsureTrailUi()
        {
            if (customizePage == null || selectedDinoLabel == null || back == null) return;
            if (Application.isPlaying && trailShop != null) return;
            trailShop = customizePage.GetComponent<RunnerTrailShop>();
            if (trailShop == null) trailShop = customizePage.AddComponent<RunnerTrailShop>();
            trailShop.EnsureUi(selectedDinoLabel.font, dialogTitle.transform.parent.GetComponent<Image>().sprite,
                back.GetComponent<Image>().sprite, dinoPortraits[0].sprite);
            ApplyDialogLayout(dialogPortrait, dialogAvailable);
        }

        public void EnsureRedeemUi()
        {
            if (optionsPage == null || back == null || selectedDinoLabel == null) return;
            if (Application.isPlaying && redeemGift != null && redeemPresentation != null && redeemGift.transform.Find("Icon") != null) return;
            redeemGift = PurchaseButton(optionsPage.transform, "Redeem gift");
            ShopLabel(redeemGift.transform, "Label", "REDEEM", selectedDinoLabel.font, 14, 0, 44, 132);
            Transform icon = RunnerShopPresentation.Node("Icon", redeemGift.transform);
            RunnerGiftGraphic giftIcon = icon.GetComponent<RunnerGiftGraphic>();
            if (giftIcon == null) giftIcon = icon.gameObject.AddComponent<RunnerGiftGraphic>();
            giftIcon.raycastTarget = false;
            redeemPresentation = GetComponent<RunnerRedeemPresentation>();
            if (redeemPresentation == null) redeemPresentation = gameObject.AddComponent<RunnerRedeemPresentation>();
            redeemPresentation.EnsureUi(transform, selectedDinoLabel.font, dialogTitle.transform.parent.GetComponent<Image>().sprite,
                back.GetComponent<Image>().sprite);
            ApplyDialogLayout(dialogPortrait, dialogAvailable);
        }

        // Shared by scene authoring and runtime fallback for older scenes. Existing bindings are retained.
        public void EnsureShopUi()
        {
            if (customize == null || customizePage == null || dinoChoices.Length == 0) return;
            if (Application.isPlaying && buyMysteryEgg != null && shopBalance != null && shopResult != null &&
                dinoFragments.Length == dinoChoices.Length && shopPresentation != null && shopCollection != null &&
                buyTenMysteryEggs != null && shopPresentation.Batch != null) return; // Preserve editable saved layouts.
            customize.name = "Shop";
            customize.GetComponentInChildren<Text>(true).text = "SHOP";
            Font font = selectedDinoLabel.font;
            Transform page = customizePage.transform;
            shopBalance = ShopLabel(page, "Shop egg balance", "EGGS: 0", font, 14, 238, 30);
            // Retain the legacy text object for old scene references, but replace it with a visual product card.
            if (page.Find("Mystery egg rules") != null) page.Find("Mystery egg rules").gameObject.SetActive(false);
            buyMysteryEgg = PurchaseButton(page, "Buy mystery egg");
            Transform purchase = buyMysteryEgg.transform;
            ShopPosition(purchase, 0, 32, 440, 56);
            ShopLabel(purchase, "Label", "HATCH EGG - " + RunnerShopProgress.MysteryEggCost + " EGGS", font, 16, 0, 40, 416);
            buyTenMysteryEggs = PurchaseButton(page, "Buy ten mystery eggs");
            ShopLabel(buyTenMysteryEggs.transform, "Label", "HATCH 10 - " + RunnerShopProgress.TenPullCost + " EGGS", font, 14, 0, 44, 316);
            shopResult = ShopLabel(page, "Mystery egg result", "COLLECT EGGS ON YOUR RUNS TO HATCH", font, 10, -20, 36);
            dinoFragments = new Text[dinoChoices.Length];
            ShopPosition(page.Find("Dino choices"), 0, 0, 620, 340);
            for (int i = 0; i < dinoChoices.Length; i++)
            {
                Transform card = dinoChoices[i].transform;
                ShopPosition(card, -234 + i * 156, -124, 140, 144);
                ShopPosition(dinoPortraits[i].transform, 0, 20, 72, 72);
                ShopPosition(dinoCaptions[i].transform, 0, -26, 130, 24);
                dinoFragments[i] = ShopLabel(card, "Fragments", i == 0 ? "UNLOCKED" : "0 / 100", font, 10, -52, 28, 130);
            }
            ShopPosition(selectedDinoLabel.transform, 0, -218, 620, 26);
            if (customizeMapLabel != null) ShopPosition(customizeMapLabel.transform, 0, -242, 620, 24);
            ShopPosition(dialogTitle.transform.parent, 0, 0, 680, 680);
            ShopPosition(dialogTitle.transform, 0, 284, 620, 48);
            ShopPosition(back.transform, 0, -288, 400, 64);
            shopPresentation = page.GetComponent<RunnerShopPresentation>();
            if (shopPresentation == null) shopPresentation = page.gameObject.AddComponent<RunnerShopPresentation>();
            shopPresentation.EnsureUi(transform, font, dialogTitle.transform.parent.GetComponent<Image>().sprite, back.GetComponent<Image>().sprite);
            shopCollection = page.GetComponent<RunnerShopCollection>();
            if (shopCollection == null) shopCollection = page.gameObject.AddComponent<RunnerShopCollection>();
            shopCollection.EnsureUi(dinoChoices, font, back.GetComponent<Image>().sprite);
            ApplyDialogLayout(dialogPortrait, dialogAvailable);
        }

        private Button PurchaseButton(Transform page, string name)
        {
            Transform purchase = page.Find(name);
            if (purchase == null)
            {
                purchase = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)).transform;
                purchase.SetParent(page, false);
                purchase.gameObject.layer = 5;
                Image image = purchase.GetComponent<Image>();
                image.sprite = back.GetComponent<Image>().sprite;
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 0.5f;
                Button button = purchase.GetComponent<Button>();
                button.targetGraphic = image;
                button.colors = back.colors;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
            }
            return purchase.GetComponent<Button>();
        }

        private static Text ShopLabel(Transform parent, string name, string caption, Font font, int size,
            float y, float height, float width = 620)
        {
            Transform node = parent.Find(name);
            if (node == null)
            {
                node = new GameObject(name, typeof(RectTransform), typeof(Text)).transform;
                node.SetParent(parent, false);
            }
            Text text = node.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.text = caption;
            text.color = new Color32(240, 242, 207, 255);
            text.alignment = TextAnchor.MiddleCenter;
            text.alignByGeometry = true;
            text.lineSpacing = 1.65f;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            ShopPosition(node, 0, y, width, height);
            return text;
        }

        private static void ShopPosition(Transform node, float x, float y, float width, float height)
        {
            if (node == null) return;
            var rect = (RectTransform)node;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private void BuyMysteryEgg()
        {
            if (shop == null || !customizePage.activeInHierarchy || (trailShop != null && trailShop.IsTrails) || (shopPresentation != null && shopPresentation.IsShowing)) return;
            if (shop.TryBuy(out int index, out int fragments, out bool unlocked))
            {
                shopResult.text = "+" + fragments + " " + dinoRoster[index].DisplayName.ToUpperInvariant() + " FRAGMENTS!\n" +
                    (unlocked ? "DINO UNLOCKED! TAP ITS CARD TO SELECT" : shop.Fragments(index) + " / 100 TO UNLOCK");
                pendingRewardIndex = index;
                shopPresentation?.Play(dinoRoster[index], fragments, shop.Fragments(index), unlocked);
            }
            RefreshShop();
        }

        private void RefreshShop()
        {
            if (shop == null || shopBalance == null) return;
            shopBalance.text = "EGGS: " + shop.Balance;
            buyMysteryEgg.interactable = shop.CanBuy && (shopPresentation == null || !shopPresentation.IsShowing);
            if (buyTenMysteryEggs != null) buyTenMysteryEggs.interactable = shop.CanBuyTen && (shopPresentation == null || !shopPresentation.IsShowing);
            if (shop.LockedCount == 0 && !shopResult.text.StartsWith("+", StringComparison.Ordinal)) shopResult.text = "ALL DINOS UNLOCKED!";
            else if (shop.Balance < RunnerShopProgress.MysteryEggCost && !shopResult.text.StartsWith("+", StringComparison.Ordinal))
                shopResult.text = "COLLECT EGGS ON YOUR RUNS TO HATCH";
            SetDinoSelection(selectedDino);
            trailShop?.Refresh();
        }

        private void BuyTenMysteryEggs()
        {
            if (shop == null || !customizePage.activeInHierarchy || (trailShop != null && trailShop.IsTrails) || shopPresentation == null || shopPresentation.IsShowing) return;
            if (shop.TryBuyTen(out RunnerShopProgress.Reward[] rewards))
            {
                int total = 0, unlocks = 0;
                pendingRewardIndex = rewards[0].DinoIndex;
                foreach (var reward in rewards)
                {
                    total += reward.Fragments;
                    if (reward.NewlyUnlocked) { unlocks++; pendingRewardIndex = reward.DinoIndex; }
                }
                shopResult.text = "+" + total + " FRAGMENTS / " + unlocks + " NEW DINOS";
                shopPresentation.Batch.Play(dinoRoster, rewards);
            }
            RefreshShop();
        }

        public void InitializeDinos(RunnerDinoRoster roster, int selected, Action<int> select)
        {
            dinoRoster = roster;
            if (roster == null || roster.Count == 0 || dinoChoices.Length != roster.Count) return;
            if (!dinosBound)
            {
                dinosBound = true;
                for (int i = 0; i < dinoChoices.Length; i++)
                {
                    int index = i;
                    Bind(dinoChoices[i], () => select(index));
                }
            }
            SetDinoSelection(selected);
        }

        public void SetDinoSelection(int selected)
        {
            if (dinoRoster == null || selected < 0 || selected >= dinoRoster.Count || dinoChoices.Length != dinoRoster.Count) return;
            selectedDino = selected;
            for (int i = 0; i < dinoChoices.Length; i++)
            {
                bool unlocked = shop == null || shop.IsUnlocked(i);
                dinoPortraits[i].sprite = dinoRoster[i].Portrait;
                dinoPortraits[i].color = unlocked ? Color.white : new Color(0.75f, 0.8f, 0.78f, 1f);
                dinoChoices[i].interactable = unlocked;
                if (i < dinoFragments.Length)
                    dinoFragments[i].text = unlocked ? "OWNED" : shop.Fragments(i) + " / 100";
                dinoCaptions[i].text = dinoRoster[i].DisplayName.ToUpperInvariant();
                dinoChoices[i].GetComponent<Image>().sprite = i == selected ? selectedDinoFrame : unselectedDinoFrame;
            }
            selectedDinoLabel.text = dinoRoster[selected].DisplayName.ToUpperInvariant() + " DINO SELECTED";
            shopCollection?.Refresh(selected);
        }

        private static void Bind(Button button, Action action)
        {
            button.onClick.AddListener(() =>
            {
                EventSystem.current?.SetSelectedGameObject(null);
                action();
            });
        }

        public void Show()
        {
            shopPresentation?.CancelReveal();
            redeemPresentation?.Cancel();
            gameObject.SetActive(true);
            foreach (GameObject item in gameplayChrome) if (item != null) item.SetActive(false);
            CloseDialog();
        }

        public void Hide()
        {
            shopPresentation?.CancelReveal();
            redeemPresentation?.Cancel();
            gameObject.SetActive(false);
            foreach (GameObject item in gameplayChrome) if (item != null) item.SetActive(true);
        }

        public void SetSound(bool muted)
        {
            speaker.sprite = muted ? speakerOff : speakerOn;
            soundCaption.text = muted ? "OFF" : "SFX";
            optionsSoundCaption.text = muted ? "SFX: OFF" : "SFX: ON";
        }

        private void OpenDialog(string heading, GameObject page)
        {
            if (mapPage != null) mapPage.SetActive(false);
            choices.SetActive(false);
            logo.SetActive(false);
            dialog.SetActive(true);
            customizePage.SetActive(page == customizePage);
            optionsPage.SetActive(page == optionsPage);
            quitPage.SetActive(page == quitPage);
            if (modePage != null) modePage.SetActive(page == modePage);
            dialogTitle.text = heading;
            ApplyDialogLayout(dialogPortrait, dialogAvailable);
        }

        public void CloseDialog()
        {
            if (redeemPresentation != null && redeemPresentation.HandleBack()) return;
            if (shopPresentation != null && shopPresentation.HandleBack()) return;
            if (mapPage != null) mapPage.SetActive(false);
            dialog.SetActive(false);
            choices.SetActive(true);
            logo.SetActive(true);
            customizePage.SetActive(false);
            optionsPage.SetActive(true);
            quitPage.SetActive(false);
            if (modePage != null) modePage.SetActive(false);
            dialogTitle.text = "OPTIONS";
            ApplyDialogLayout(dialogPortrait, dialogAvailable);
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
