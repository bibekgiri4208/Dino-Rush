using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static DinoRush.RunnerShopPresentation;

namespace DinoRush
{
    /// <summary>Independent Trails category; previews are free and purchases never roll random rewards.</summary>
    public sealed class RunnerTrailShop : MonoBehaviour
    {
        private static readonly Color Cream = new Color32(240, 242, 207, 255);
        private static readonly Color Gold = new Color32(255, 210, 117, 255);
        private static readonly string[] Names = { "WIND", "INFERNO", "VOLTAGE", "FROST", "BLOSSOM", "COSMIC" };
        private static readonly string[] Descriptions = { "Drifting wind\nblades & swirls", "Rising embers\n& fire particles", "Electric sparks\n& current flickers",
            "Tumbling ice\n& snowflakes", "Floating petals\non a soft breeze", "Twinkling stars\n& violet stardust" };
        private static readonly Color[] Tints = { new Color32(179, 225, 230, 255), new Color32(255, 168, 99, 255),
            new Color32(139, 203, 255, 255), new Color32(168, 230, 255, 255), new Color32(255, 170, 199, 255), new Color32(216, 180, 255, 255) };
        private const int PageSize = 3;
        [SerializeField] private GameObject content;
        [SerializeField] private GameObject[] dinoContent;
        [SerializeField] private Button dinosTab, trailsTab, unequip;
        [SerializeField] private Button previousPage, nextPage;
        [SerializeField] private Button[] purchases = new Button[3];
        [SerializeField] private Text[] states = new Text[3];
        [SerializeField] private Text wallet, hint;
        [SerializeField] private Text equippedLabel, pageLabel;
        private RunnerShopProgress shop;
        private RunnerAudio audioCues;
        private RunnerShopPresentation hatch;
        private Action changed;
        private bool bound, portrait;
        private int page;
        public int Page => page;
        public int PageCount => (RunnerTrailProgress.Count + PageSize - 1) / PageSize;
        public bool IsTrails { get; private set; }
        [SerializeField] private RunnerSlashShop slashShop;
        [SerializeField] private Button slashesTab;
        public bool IsSlashes { get; private set; }
        public bool IsEffects => IsTrails || IsSlashes;

        public void Initialize(RunnerShopProgress progress, RunnerShopPresentation presentation, RunnerAudio audio, Action refresh)
        {
            shop = progress; hatch = presentation; audioCues = audio; changed = refresh;
            slashShop?.Initialize(refresh, audio);
            if (!bound)
            {
                bound = true;
                previousPage.onClick.AddListener(() => ChangePage(-1));
                nextPage.onClick.AddListener(() => ChangePage(1));
                dinosTab.onClick.AddListener(() => ShowTrails(false));
                trailsTab.onClick.AddListener(() => ShowTrails(true));
                if (slashesTab != null) slashesTab.onClick.AddListener(ShowSlashes);
                unequip.onClick.AddListener(() =>
                {
                    if (!CanInteract) return;
                    RunnerTrailProgress.Equip(RunnerTrailKind.None);
                    hint.text = "TRAIL REMOVED / OWNED TRAILS KEPT";
                    Refresh();
                });
                for (int i = 0; i < purchases.Length; i++)
                {
                    int index = i;
                    purchases[i].onClick.AddListener(() => PurchaseOrEquip(index));
                }
            }
            ShowTrails(false);
        }
        private bool CanInteract => shop != null && gameObject.activeInHierarchy && (hatch == null || !hatch.IsShowing);
        private void ChangePage(int direction)
        {
            if (!CanInteract || !IsTrails) return;
            page = Mathf.Clamp(page + direction, 0, PageCount - 1);
            hint.text = "PERMANENT UNLOCKS / FREE SWAPS";
            EventSystem.current?.SetSelectedGameObject(null);
            Refresh();
        }
        public void ShowTrails(bool show)
        {
            if (hatch != null && hatch.IsShowing) return;
            EventSystem.current?.SetSelectedGameObject(null);
            IsTrails = show;
            IsSlashes = false;
            Refresh();
            ApplyLayout(portrait);
        }
        public void ShowSlashes()
        {
            if (!CanInteract) return;
            IsTrails = false; IsSlashes = true;
            EventSystem.current?.SetSelectedGameObject(null);
            Refresh(); ApplyLayout(portrait);
        }
        private void PurchaseOrEquip(int index)
        {
            if (!CanInteract || !IsTrails || index < 0 || index >= purchases.Length || index / PageSize != page) return;
            var kind = (RunnerTrailKind)(index + 1);
            bool purchased = false;
            if (!RunnerTrailProgress.Owned(kind))
            {
                if (!RunnerTrailProgress.Buy(kind)) { hint.text = "NOT ENOUGH EGGS / NEED 2,000"; Refresh(); return; }
                purchased = true;
            }
            RunnerTrailProgress.Equip(kind);
            hint.text = Names[index] + (purchased ? " UNLOCKED + EQUIPPED!" : " EQUIPPED!");
            if (purchased) audioCues?.Milestone();
            EventSystem.current?.SetSelectedGameObject(null);
            changed?.Invoke();
            Refresh();
        }
        public void Refresh()
        {
            if (content == null) return;
            content.SetActive(IsTrails);
            slashShop?.Show(IsSlashes);
            foreach (GameObject node in dinoContent)
                if (node != null) node.SetActive(!IsEffects && node.name != "Mystery egg rules" && node.name != "Single adventure" && node.name != "Dino selection" &&
                    (!portrait || node.name != "Selected status" && node.name != "Map selection"));
            transform.Find("Shop egg balance wallet")?.gameObject.SetActive(!IsEffects);
            wallet.text = RunnerShopWallet.Caption(shop != null ? shop.Balance : PlayerPrefs.GetInt(RunnerShopProgress.WalletKey));
            RunnerTrailKind equipped = RunnerTrailProgress.Equipped;
            equippedLabel.text = "USING: " + (RunnerTrailProgress.Valid(equipped) ? Names[(int)equipped - 1] : "NONE");
            pageLabel.text = "PAGE " + (page + 1) + " / " + PageCount;
            for (int i = 0; i < purchases.Length; i++)
            {
                purchases[i].transform.parent.gameObject.SetActive(i / PageSize == page);
                var kind = (RunnerTrailKind)(i + 1);
                bool owned = RunnerTrailProgress.Owned(kind), selected = equipped == kind;
                states[i].text = selected ? "IN USE" : owned ? "OWNED" : "2,000 EGGS";
                states[i].color = owned && !selected ? new Color32(183, 224, 151, 255) : Gold;
                purchases[i].GetComponentInChildren<Text>(true).text = selected ? "EQUIPPED" : owned ? "EQUIP" : "BUY TRAIL";
                purchases[i].interactable = !selected && (owned || shop != null && shop.Balance >= RunnerTrailProgress.Cost);
                Transform edge = purchases[i].transform.parent.Find("Equipped accent");
                edge.gameObject.SetActive(selected);
            }
            unequip.interactable = equipped != RunnerTrailKind.None;
            unequip.GetComponentInChildren<Text>(true).text = equipped == RunnerTrailKind.None ? "NO TRAIL ACTIVE" : "REMOVE TRAIL";
            previousPage.interactable = page > 0;
            nextPage.interactable = page < PageCount - 1;
            dinosTab.GetComponentInChildren<Text>(true).color = IsEffects ? Cream : Gold;
            trailsTab.GetComponentInChildren<Text>(true).color = IsTrails ? Gold : Cream;
            if (slashesTab != null) slashesTab.GetComponentInChildren<Text>(true).color = IsSlashes ? Gold : Cream;
        }
        public void EnsureUi(Font font, Sprite panelSprite, Sprite buttonSprite, Sprite dinoPortrait)
        {
            // Record only the existing immediate children. No hierarchy names or Shop bindings change.
            var nodes = new List<GameObject>();
            foreach (Transform child in transform)
                if (child.name != "Trails section" && child.name != "Dinos category" && child.name != "Trails category" && child.name != "Slashes section" && child.name != "Slashes category") nodes.Add(child.gameObject);
            // Retain intentionally hidden legacy labels and per-layout elements by restoring them from layout below.
            dinoContent = nodes.ToArray();
            dinosTab = Control("Dinos category", transform, "DINOS", font, buttonSprite);
            trailsTab = Control("Trails category", transform, "TRAILS", font, buttonSprite);
            content = Node("Trails section", transform).gameObject;
            Stretch(content.transform);
            wallet = content.transform.Find("Trail egg balance wallet/Trail egg balance")?.GetComponent<Text>() ??
                Label("Trail egg balance", content.transform, "EGGS: 0", font, 16, 0, 226, 840, 32, Gold);
            hint = Label("Trail shop hint", content.transform, "PERMANENT UNLOCKS / FREE SWAPS", font, 11, 0, -278, 840, 28, Cream);
            equippedLabel = Label("Equipped trail summary", content.transform, "USING: NONE", font, 12, 220, 226, 400, 32, Cream);
            pageLabel = Label("Trail page indicator", content.transform, "PAGE 1 / 2", font, 14, 0, -236, 280, 32, Cream);
            unequip = Control("Remove trail", content.transform, "NO TRAIL", font, buttonSprite);
            previousPage = Control("Previous trails page", content.transform, "< PREV", font, buttonSprite);
            nextPage = Control("Next trails page", content.transform, "NEXT >", font, buttonSprite);
            Array.Resize(ref purchases, RunnerTrailProgress.Count);
            Array.Resize(ref states, RunnerTrailProgress.Count);
            for (int i = 0; i < RunnerTrailProgress.Count; i++)
            {
                Transform card = Node("Trail card " + (i + 1), content.transform);
                Art(card, panelSprite, Color.white, true);
                Transform accent = Node("Equipped accent", card);
                Art(accent, null, Gold, false);
                Transform theme = Node("Trail theme accent", card);
                Art(theme, null, Tints[i] * new Color(1, 1, 1, 0.6f), false);
                Label("Trail name", card, Names[i], font, 22, 0, 174, 248, 32, Tints[i]);
                Transform backdrop = Node("Preview backdrop", card);
                Art(backdrop, null, new Color32(8, 23, 31, 255), false);
                Transform preview = Node("Live trail preview", card);
                var effect = preview.GetComponent<RunnerTrailPreview>();
                if (effect == null) effect = preview.gameObject.AddComponent<RunnerTrailPreview>();
                effect.Kind = (RunnerTrailKind)(i + 1);
                effect.raycastTarget = false;
                Image dino = Art(Node("Preview dino", card), dinoPortrait, Color.white, false);
                dino.preserveAspect = true;
                Transform front = card.Find("Live aura foreground");
                if (front != null) front.gameObject.SetActive(false); // Retain old bindings, never create more shells.
                Label("Trail description", card, Descriptions[i], font, 11, 0, -40, 244, 52, Cream);
                Transform badge = Node("Ownership badge", card);
                Art(badge, null, new Color32(17, 41, 39, 255), false);
                states[i] = Label("Trail ownership", card, "2,000 EGGS", font, 12, 0, -112, 244, 28, Gold);
                badge.SetSiblingIndex(states[i].transform.GetSiblingIndex());
                purchases[i] = Control("Buy or equip trail " + (i + 1), card, "BUY - 2,000 EGGS", font, buttonSprite);
            }
            IsTrails = false;
            ApplyLayout(false);
            Refresh();
        }
        public void EnsureSlashUi(Font font, Sprite panel, Sprite button, RunnerSlashCatalog catalog)
        {
            slashShop = GetComponent<RunnerSlashShop>();
            if (slashShop == null) slashShop = gameObject.AddComponent<RunnerSlashShop>();
            slashShop.EnsureUi(font, panel, button, catalog);
            slashesTab = Control("Slashes category", transform, "SLASHES", font, button);
            ApplyLayout(portrait);
        }
        public void EnsureWalletUi(Sprite eggSprite = null)
        {
            RunnerShopWallet.Ensure(wallet, eggSprite);
            slashShop?.EnsureWalletUi(eggSprite);
        }
        public void ApplyLayout(bool isPortrait)
        {
            portrait = isPortrait;
            if (content == null) return;
            Place(dinosTab.transform, portrait ? -146 : -174, portrait ? 566 : 280, portrait ? 268 : 328, portrait ? 76 : 52);
            Place(trailsTab.transform, portrait ? 146 : 174, portrait ? 566 : 280, portrait ? 268 : 328, portrait ? 76 : 52);
            TextSize(dinosTab.transform.Find("Label"), portrait ? 20 : 18);
            TextSize(trailsTab.transform.Find("Label"), portrait ? 20 : 18);
            if (slashesTab != null)
            {
                Place(dinosTab.transform, portrait ? -194 : -294, portrait ? 566 : 280, portrait ? 180 : 278, portrait ? 76 : 52);
                Place(trailsTab.transform, 0, portrait ? 566 : 280, portrait ? 180 : 278, portrait ? 76 : 52);
                Place(slashesTab.transform, portrait ? 194 : 294, portrait ? 566 : 280, portrait ? 180 : 278, portrait ? 76 : 52);
                TextSize(dinosTab.transform.Find("Label"), portrait ? 16 : 18);
                TextSize(trailsTab.transform.Find("Label"), portrait ? 16 : 18);
                TextSize(slashesTab.transform.Find("Label"), portrait ? 16 : 18);
            }
            RunnerShopWallet.Layout(wallet, portrait ? -146 : -254, portrait ? 492 : 226, portrait ? 280 : 340, portrait);
            Position(equippedLabel.transform, portrait ? 142 : 216, portrait ? 492 : 226, portrait ? 280 : 408, 32);
            equippedLabel.fontSize = portrait ? 14 : 12;
            Place(previousPage.transform, portrait ? -205 : -304, portrait ? -482 : -244, portrait ? 146 : 184, portrait ? 60 : 48);
            Place(nextPage.transform, portrait ? 205 : 304, portrait ? -482 : -244, portrait ? 146 : 184, portrait ? 60 : 48);
            TextSize(previousPage.transform.Find("Label"), portrait ? 16 : 12);
            TextSize(nextPage.transform.Find("Label"), portrait ? 16 : 12);
            Position(pageLabel.transform, 0, portrait ? -482 : -244, portrait ? 240 : 280, 32);
            for (int i = 0; i < purchases.Length; i++)
            {
                Transform card = purchases[i].transform.parent;
                int slot = i % PageSize;
                Position(card, portrait ? 0 : (slot - 1) * 294, portrait ? 300 - slot * 300 : -8, portrait ? 576 : 278, portrait ? 280 : 416);
                Position(card.Find("Equipped accent"), 0, portrait ? 134 : 202, portrait ? 544 : 246, 4);
                Position(card.Find("Trail theme accent"), portrait ? -155 : 0, portrait ? -126 : 150, portrait ? 224 : 246, 2);
                Position(card.Find("Trail name"), portrait ? 130 : 0, portrait ? 95 : 174, portrait ? 270 : 248, 32);
                TextSize(card.Find("Trail name"), portrait ? 24 : 22);
                Position(card.Find("Preview backdrop"), portrait ? -155 : 0, portrait ? 0 : 48, portrait ? 232 : 250, portrait ? 240 : 190);
                Vector2 previewSize = new Vector2(portrait ? 224 : 250, portrait ? 216 : 186);
                Vector2 previewCenter = new Vector2(portrait ? -155 : 0, portrait ? 0 : 48);
                Position(card.Find("Live trail preview"), previewCenter.x, previewCenter.y, previewSize.x, previewSize.y);
                Transform foreground = card.Find("Live aura foreground");
                if (foreground != null) Position(foreground, previewCenter.x, previewCenter.y, previewSize.x, previewSize.y);
                float worldScale = RunnerTrailPreview.WorldScale(previewSize);
                Vector2 dinoCenter = previewCenter + RunnerTrailPreview.FeetOffset(previewSize) + Vector2.up * worldScale * (1 - 2f / 12f);
                Position(card.Find("Preview dino"), dinoCenter.x, dinoCenter.y, worldScale * 2, worldScale * 2);
                card.Find("Live trail preview").GetComponent<RunnerTrailPreview>().SetDinoLayout((RectTransform)card.Find("Preview dino"), dinoCenter);
                Position(card.Find("Trail description"), portrait ? 130 : 0, portrait ? 40 : -76, portrait ? 270 : 244, portrait ? 48 : 40);
                TextSize(card.Find("Trail description"), portrait ? 14 : 11);
                Position(card.Find("Ownership badge"), portrait ? 130 : 0, portrait ? -10 : -120, portrait ? 200 : 176, 32);
                Position(states[i].transform, portrait ? 130 : 0, portrait ? -10 : -120, portrait ? 200 : 176, 28);
                states[i].fontSize = portrait ? 16 : 12;
                Place(purchases[i].transform, portrait ? 130 : 0, portrait ? -86 : -170, portrait ? 260 : 244, portrait ? 84 : 52);
                TextSize(purchases[i].transform.Find("Label"), portrait ? 18 : 14);
            }
            Position(hint.transform, 0, portrait ? -541 : -284, portrait ? 576 : 840, portrait ? 28 : 20);
            hint.fontSize = portrait ? 12 : 10;
            Place(unequip.transform, portrait ? 146 : 240, portrait ? -614 : -330, portrait ? 276 : 360, portrait ? 88 : 52);
            TextSize(unequip.transform.Find("Label"), portrait ? 16 : 12);
            Transform back = transform.parent.Find("Back to main menu");
            if (gameObject.activeSelf && back != null) Place(back, portrait ? (IsTrails ? -146 : 0) : IsTrails ? -240 : -272,
                portrait ? (IsTrails ? -614 : -560) : IsTrails ? -330 : -272,
                portrait ? (IsTrails ? 276 : 560) : IsTrails ? 360 : 340, portrait ? (IsTrails ? 88 : 100) : 52);
            // Refresh may be called by dino selection/layout; the category always owns its visibility.
            Refresh();
            slashShop?.ApplyLayout(portrait);
            if (!IsEffects)
            {
                transform.Find("Mystery egg rules")?.gameObject.SetActive(false);
                transform.Find("Selected status")?.gameObject.SetActive(!portrait);
                transform.Find("Map selection")?.gameObject.SetActive(!portrait);
            }
        }
        private static void TextSize(Transform node, int size) => node.GetComponent<Text>().fontSize = size;
        private static void Place(Transform node, float x, float y, float width, float height)
        {
            Position(node, x, y, width, height);
            Transform label = node.Find("Label");
            if (label != null) Position(label, 0, 0, width - 20, height - 12);
            node.GetComponent<RunnerUiButtonFeedback>()?.RefreshContentLayout();
        }
        private static Button Control(string name, Transform parent, string caption, Font font, Sprite sprite)
        {
            Transform node = Node(name, parent);
            Image art = Art(node, sprite, Color.white, true); art.raycastTarget = true;
            Button button = node.GetComponent<Button>();
            if (button == null) button = node.gameObject.AddComponent<Button>();
            button.targetGraphic = art;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            Label("Label", node, caption, font, 12, 0, 0, 240, 40, Cream);
            return button;
        }
    }
}
