using UnityEngine;
using UnityEngine.UI;
using static DinoRush.RunnerShopPresentation;

namespace DinoRush
{
    public sealed class RunnerSlashHud : MonoBehaviour
    {
        [SerializeField] private Text label, stock, hint;
        [SerializeField] private Image[] pips = new Image[RunnerSlashProgress.MaxUsesPerRound];
        [SerializeField] private Button button;
        private ForestRunnerGame game;
        private bool portrait;
        private Image compactFrame;

        private void Start()
        {
            game = GetComponentInParent<ForestRunnerGame>();
            // Older authored scenes get the same details without rebuilding the rest of their UI.
            if (stock == null) EnsureDetails(label.font);
            ApplyLayout(portrait);
            button.onClick.AddListener(() => game.Attack());
        }

        private void Update()
        {
            bool running = game != null && game.State == ForestRunnerGame.RunState.Running;
            button.gameObject.SetActive(running);
            if (!running) return;
            RunnerSlashKind kind = RunnerSlashProgress.Equipped;
            int remaining = game.SlashesRemainingThisRound, charges = RunnerSlashProgress.Charges;
            bool equipped = kind != RunnerSlashKind.None;
            Color accent = kind == RunnerSlashKind.Fire ? new Color32(255, 181, 108, 255) :
                kind == RunnerSlashKind.Lightning ? new Color32(255, 231, 126, 255) : new Color32(140, 224, 255, 255);
            string name = kind == RunnerSlashKind.Lightning ? "VOLT" : equipped ? kind.ToString().ToUpperInvariant() : "SLASH";
            label.text = name + " " + remaining + "/" + RunnerSlashProgress.MaxUsesPerRound;
            label.color = accent;
            stock.text = "x" + CompactCount(charges);
            hint.text = !equipped ? "EQUIP A SLASH IN SHOP" : remaining == 0 ? "ROUND LIMIT REACHED" :
                charges == 0 ? "BUY CHARGES IN SHOP" : game.SlashAttack.Active ? "SLASHING..." : game.UsingGamepad ? "X / RB TO SLASH" : portrait ? "TAP TO SLASH" : "ENTER / TAP TO SLASH";
            for (int i = 0; i < pips.Length; i++)
                pips[i].color = equipped && i < Mathf.Min(remaining, charges) ? accent : new Color32(64, 77, 79, 255);
            button.interactable = equipped && remaining > 0 && charges > 0 && !game.SlashAttack.Active;
        }

        public void EnsureUi(Font font, Sprite sprite)
        {
            Transform node = Node("Slash indicator", transform);
            var rect = (RectTransform)node; rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0); rect.anchoredPosition = new Vector2(0, 26);
            Image image = Art(node, sprite, Color.white, true); image.raycastTarget = true;
            button = node.GetComponent<Button>(); if (button == null) button = node.gameObject.AddComponent<Button>();
            button.targetGraphic = image; button.navigation = new Navigation { mode = Navigation.Mode.None };
            label = Label("Slash charges", node, "SLASH 3/3", font, 12, 0, 0, 128, 24, Color.white);
            EnsureDetails(font);
            ApplyLayout(false);
            node.gameObject.SetActive(false);
        }

        private void EnsureDetails(Font font)
        {
            stock = Label("Slash stock", button.transform, "0 IN STOCK", font, 12, 0, -13, 240, 22, Color.white);
            hint = Label("Slash hint", button.transform, "ENTER / TAP TO SLASH", font, 10, 0, -36, 240, 20, new Color32(215, 220, 206, 255));
            pips = new Image[RunnerSlashProgress.MaxUsesPerRound];
            for (int i = 0; i < pips.Length; i++)
                pips[i] = Art(Node("Round use " + (i + 1), button.transform), null, new Color32(64, 77, 79, 255), false);
        }

        public void ApplyLayout(bool isPortrait)
        {
            portrait = isPortrait;
            if (button == null) return;
            if (stock == null) EnsureDetails(label.font);
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0);
            rect.pivot = Vector2.one * 0.5f;
            // The visible strip matches Pace; the larger invisible portrait hit area stays tappable.
            rect.sizeDelta = portrait ? new Vector2(340, 100) : new Vector2(300, 44);
            var pace = transform.Find("Trail stats frame") as RectTransform;
            float y = pace != null ? pace.anchoredPosition.y + ((RectTransform)transform).rect.height * 0.5f : 66;
            rect.anchoredPosition = portrait ? new Vector2(0, 116) :
                new Vector2(pace != null ? pace.anchoredPosition.x + pace.rect.width * 0.5f + 166 : -94, y);
            if (compactFrame == null)
            {
                Image original = button.GetComponent<Image>();
                Image paceArt = pace != null ? pace.GetComponent<Image>() : null;
                compactFrame = Art(Node("Compact frame", button.transform), paceArt != null ? paceArt.sprite : original.sprite, Color.white, true);
                compactFrame.transform.SetAsFirstSibling();
                original.color = Color.clear;
                original.raycastTarget = true;
                button.targetGraphic = compactFrame;
            }
            Position(compactFrame.transform, 0, 0, rect.sizeDelta.x, portrait ? 48 : 42);
            Position(label.transform, portrait ? -92 : -82, 0, portrait ? 144 : 128, 28);
            label.fontSize = portrait ? 16 : 12;
            Position(stock.transform, portrait ? 116 : 104, 0, portrait ? 84 : 76, 28);
            stock.fontSize = portrait ? 16 : 12;
            // Retain status text for state checks, without another line of permanent HUD instructions.
            hint.gameObject.SetActive(false);
            for (int i = 0; i < pips.Length; i++)
                Position(pips[i].transform, i * (portrait ? 26 : 24), 0, portrait ? 20 : 18, portrait ? 10 : 8);
            button.GetComponent<RunnerUiButtonFeedback>()?.RefreshContentLayout();
        }

        private static string CompactCount(int count) => count >= 1000000000 ? (count / 1000000000f).ToString("0.#") + "B" :
            count >= 1000000 ? (count / 1000000f).ToString(count >= 100000000 ? "0" : "0.#") + "M" :
            count >= 1000 ? (count / 1000f).ToString(count >= 100000 ? "0" : "0.#") + "K" : count.ToString();
    }
}
