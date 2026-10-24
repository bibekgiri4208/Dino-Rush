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
        private static readonly string[] Names = { "WIND", "INFERNO", "VOLTAGE" };
        private static readonly string[] Descriptions = { "Drifting blades\nand wind swirls", "Rising fire embers\nand warm particles", "Electric sparks\nand current flickers" };
        [SerializeField] private GameObject content;
        [SerializeField] private GameObject[] dinoContent;
        [SerializeField] private Button dinosTab, trailsTab, unequip;
        [SerializeField] private Button[] purchases = new Button[3];
        [SerializeField] private Text[] states = new Text[3];
        [SerializeField] private Text wallet, hint;
        private RunnerShopProgress shop;
        private RunnerAudio audioCues;
        private RunnerShopPresentation hatch;
        private Action changed;
        private bool bound, portrait;
        public bool IsTrails { get; private set; }

        public void Initialize(RunnerShopProgress progress, RunnerShopPresentation presentation, RunnerAudio audio, Action refresh)
        {
            shop = progress; hatch = presentation; audioCues = audio; changed = refresh;
            if (!bound)
            {
                bound = true;
                dinosTab.onClick.AddListener(() => ShowTrails(false));
                trailsTab.onClick.AddListener(() => ShowTrails(true));
                unequip.onClick.AddListener(() =>
                {
                    if (!CanInteract) return;
                    RunnerTrailProgress.Equip(RunnerTrailKind.None);
                    hint.text = "TRAIL REMOVED / YOUR COLLECTION IS SAVED";
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
        public void ShowTrails(bool show)
        {
            if (hatch != null && hatch.IsShowing) return;
            EventSystem.current?.SetSelectedGameObject(null);
            IsTrails = show;
            Refresh();
            ApplyLayout(portrait);
        }
        private void PurchaseOrEquip(int index)
        {
            if (!CanInteract || !IsTrails) return;
            var kind = (RunnerTrailKind)(index + 1);
            bool purchased = false;
            if (!RunnerTrailProgress.Owned(kind))
            {
                if (!RunnerTrailProgress.Buy(kind)) { hint.text = "YOU NEED 2,000 EGGS TO BUY THIS TRAIL"; Refresh(); return; }
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
            foreach (GameObject node in dinoContent)
                if (node != null) node.SetActive(!IsTrails && node.name != "Mystery egg rules" &&
                    (!portrait || node.name != "Selected status" && node.name != "Map selection"));
            wallet.text = "EGGS: " + (shop != null ? shop.Balance : Mathf.Max(0, PlayerPrefs.GetInt(RunnerShopProgress.WalletKey))).ToString("N0");
            RunnerTrailKind equipped = RunnerTrailProgress.Equipped;
            for (int i = 0; i < 3; i++)
            {
                var kind = (RunnerTrailKind)(i + 1);
                bool owned = RunnerTrailProgress.Owned(kind), selected = equipped == kind;
                states[i].text = selected ? "EQUIPPED" : owned ? "OWNED / FOREVER" : "2,000 EGGS / PERMANENT";
                states[i].color = selected ? Gold : Cream;
                purchases[i].GetComponentInChildren<Text>(true).text = selected ? "EQUIPPED" : owned ? "EQUIP" : "BUY - 2,000 EGGS";
                purchases[i].interactable = !selected && (owned || shop != null && shop.Balance >= RunnerTrailProgress.Cost);
                Transform edge = purchases[i].transform.parent.Find("Equipped accent");
                edge.gameObject.SetActive(selected);
            }
            unequip.interactable = equipped != RunnerTrailKind.None;
            dinosTab.GetComponentInChildren<Text>(true).color = IsTrails ? Cream : Gold;
            trailsTab.GetComponentInChildren<Text>(true).color = IsTrails ? Gold : Cream;
        }
        public void EnsureUi(Font font, Sprite panelSprite, Sprite buttonSprite, Sprite dinoPortrait)
        {
            // Record only the existing immediate children. No hierarchy names or Shop bindings change.
            var nodes = new List<GameObject>();
            foreach (Transform child in transform)
                if (child.name != "Trails section" && child.name != "Dinos category" && child.name != "Trails category") nodes.Add(child.gameObject);
            // Retain intentionally hidden legacy labels and per-layout elements by restoring them from layout below.
            dinoContent = nodes.ToArray();
            dinosTab = Control("Dinos category", transform, "DINOS", font, buttonSprite);
            trailsTab = Control("Trails category", transform, "TRAILS", font, buttonSprite);
            content = Node("Trails section", transform).gameObject;
            Stretch(content.transform);
            wallet = Label("Trail egg balance", content.transform, "EGGS: 0", font, 16, 0, 226, 840, 32, Gold);
            hint = Label("Trail shop hint", content.transform, "PREVIEW LIVE / BUY ONCE / EQUIP ON ANY DINO", font, 11, 0, -224, 840, 40, Cream);
            unequip = Control("Remove trail", content.transform, "NO TRAIL", font, buttonSprite);
            for (int i = 0; i < 3; i++)
            {
                Transform card = Node("Trail card " + (i + 1), content.transform);
                Art(card, panelSprite, Color.white, true);
                Transform accent = Node("Equipped accent", card);
                Art(accent, null, Gold, false);
                Label("Trail name", card, Names[i], font, 18, 0, 140, 248, 30, Gold);
                Transform backdrop = Node("Preview backdrop", card);
                Art(backdrop, null, new Color32(4, 13, 27, 255), false);
                Transform preview = Node("Live trail preview", card);
                var effect = preview.GetComponent<RunnerTrailPreview>();
                if (effect == null) effect = preview.gameObject.AddComponent<RunnerTrailPreview>();
                effect.Kind = (RunnerTrailKind)(i + 1);
                effect.raycastTarget = false;
                Image dino = Art(Node("Preview dino", card), dinoPortrait, Color.white, false);
                dino.preserveAspect = true;
                Transform front = Node("Live aura foreground", card);
                var frontEffect = front.GetComponent<RunnerTrailPreview>();
                if (frontEffect == null) frontEffect = front.gameObject.AddComponent<RunnerTrailPreview>();
                frontEffect.Kind = (RunnerTrailKind)(i + 1);
                frontEffect.Foreground = true;
                frontEffect.raycastTarget = false;
                front.SetSiblingIndex(dino.transform.GetSiblingIndex() + 1);
                front.gameObject.SetActive(false); // Old saved binding only; particles do not need a foreground shell.
                Label("Trail description", card, Descriptions[i], font, 11, 0, -40, 244, 52, Cream);
                states[i] = Label("Trail ownership", card, "2,000 EGGS / PERMANENT", font, 10, 0, -94, 244, 28, Cream);
                purchases[i] = Control("Buy or equip trail " + (i + 1), card, "BUY - 2,000 EGGS", font, buttonSprite);
            }
            IsTrails = false;
            ApplyLayout(false);
            Refresh();
        }
        public void ApplyLayout(bool isPortrait)
        {
            portrait = isPortrait;
            if (content == null) return;
            Place(dinosTab.transform, portrait ? -146 : -174, portrait ? 566 : 280, portrait ? 268 : 328, portrait ? 76 : 52);
            Place(trailsTab.transform, portrait ? 146 : 174, portrait ? 566 : 280, portrait ? 268 : 328, portrait ? 76 : 52);
            TextSize(dinosTab.transform.Find("Label"), portrait ? 20 : 18);
            TextSize(trailsTab.transform.Find("Label"), portrait ? 20 : 18);
            Position(wallet.transform, 0, portrait ? 492 : 226, portrait ? 560 : 840, 32);
            for (int i = 0; i < 3; i++)
            {
                Transform card = purchases[i].transform.parent;
                Position(card, portrait ? 0 : (i - 1) * 294, portrait ? 310 - i * 300 : 0, portrait ? 560 : 278, portrait ? 270 : 400);
                Position(card.Find("Equipped accent"), 0, portrait ? 128 : 192, portrait ? 512 : 238, 3);
                Position(card.Find("Trail name"), portrait ? 100 : 0, portrait ? 82 : 164, portrait ? 320 : 248, 30);
                Position(card.Find("Preview backdrop"), portrait ? -158 : 0, portrait ? 10 : 66, portrait ? 204 : 244, portrait ? 186 : 150);
                Vector2 previewSize = new Vector2(portrait ? 196 : 244, portrait ? 156 : 150);
                Vector2 previewCenter = new Vector2(portrait ? -158 : 0, portrait ? 10 : 66);
                Position(card.Find("Live trail preview"), previewCenter.x, previewCenter.y, previewSize.x, previewSize.y);
                Transform foreground = card.Find("Live aura foreground");
                if (foreground != null) Position(foreground, previewCenter.x, previewCenter.y, previewSize.x, previewSize.y);
                float worldScale = RunnerTrailPreview.WorldScale(previewSize);
                Vector2 dinoCenter = previewCenter + RunnerTrailPreview.FeetOffset(previewSize) + Vector2.up * worldScale * (1 - 2f / 12f);
                Position(card.Find("Preview dino"), dinoCenter.x, dinoCenter.y, worldScale * 2, worldScale * 2);
                card.Find("Live trail preview").GetComponent<RunnerTrailPreview>().SetDinoLayout((RectTransform)card.Find("Preview dino"), dinoCenter);
                Position(card.Find("Trail description"), portrait ? 108 : 0, portrait ? 36 : -54, portrait ? 304 : 244, 56);
                TextSize(card.Find("Trail description"), portrait ? 12 : 11);
                Position(states[i].transform, portrait ? 108 : 0, portrait ? -16 : -108, portrait ? 304 : 244, 28);
                Place(purchases[i].transform, portrait ? 108 : 0, portrait ? -80 : -156, portrait ? 288 : 244, portrait ? 96 : 52);
            }
            Position(hint.transform, 0, portrait ? -466 : -224, portrait ? 560 : 840, 40);
            hint.fontSize = portrait ? 11 : 12;
            Place(unequip.transform, portrait ? 146 : 184, portrait ? -560 : -272, portrait ? 268 : 340, portrait ? 100 : 52);
            Transform back = transform.parent.Find("Back to main menu");
            if (gameObject.activeSelf && back != null) Place(back, portrait ? (IsTrails ? -146 : 0) : -272, portrait ? -560 : -272,
                portrait ? (IsTrails ? 268 : 560) : 340, portrait ? 100 : 52);
            // Refresh may be called by dino selection/layout; the category always owns its visibility.
            Refresh();
            if (!IsTrails)
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
