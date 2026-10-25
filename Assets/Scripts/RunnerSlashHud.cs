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
            label.text = (equipped ? kind.ToString().ToUpperInvariant() : "SLASH") + " / ROUND " + remaining + "/" + RunnerSlashProgress.MaxUsesPerRound;
            label.color = accent;
            stock.text = charges.ToString("N0") + " IN STOCK";
            hint.text = !equipped ? "EQUIP A SLASH IN SHOP" : remaining == 0 ? "ROUND LIMIT REACHED" :
                charges == 0 ? "BUY CHARGES IN SHOP" : game.SlashAttack.Active ? "SLASHING..." : portrait ? "TAP TO SLASH" : "ENTER / TAP TO SLASH";
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
            label = Label("Slash charges", node, "SLASH / ROUND 3/3", font, 12, 0, 34, 240, 24, Color.white);
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
            rect.sizeDelta = portrait ? new Vector2(440, 164) : new Vector2(280, 110);
            Position(label.transform, 0, portrait ? 52 : 34, rect.sizeDelta.x - 20, portrait ? 32 : 24);
            label.fontSize = portrait ? 18 : 12;
            Position(stock.transform, 0, portrait ? -19 : -13, rect.sizeDelta.x - 20, portrait ? 30 : 22);
            stock.fontSize = portrait ? 18 : 12;
            Position(hint.transform, 0, portrait ? -53 : -36, rect.sizeDelta.x - 20, portrait ? 28 : 20);
            hint.fontSize = portrait ? 14 : 10;
            for (int i = 0; i < pips.Length; i++)
                Position(pips[i].transform, (i - 1) * (portrait ? 126 : 80), portrait ? 14 : 9, portrait ? 112 : 70, portrait ? 10 : 7);
            button.GetComponent<RunnerUiButtonFeedback>()?.RefreshContentLayout();
        }
    }
}
