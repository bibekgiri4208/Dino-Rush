using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static DinoRush.RunnerShopPresentation;

namespace DinoRush
{
    /// <summary>Explicit, modal confirmation for a permanent local-account reset.</summary>
    public sealed class RunnerAccountReset : MonoBehaviour
    {
        private static readonly Color Cream = new Color32(240, 242, 207, 255);
        private static readonly Color Warning = new Color32(255, 169, 141, 255);
        [SerializeField] private GameObject overlay;
        [SerializeField] private RectTransform panel;
        [SerializeField] private Button open, cancel, confirm;
        [SerializeField] private Text heading, warning, details, note;
        private ForestMainMenu menu;
        private ForestRunnerGame game;
        private bool bound;
        public bool IsShowing => overlay != null && overlay.activeSelf;
        public Transform NavigationRoot => overlay != null ? overlay.transform : null;

        public void Initialize(ForestMainMenu owner, ForestRunnerGame runner)
        {
            menu = owner; game = runner;
            if (!bound)
            {
                bound = true;
                open.onClick.AddListener(Open);
                cancel.onClick.AddListener(Cancel);
                confirm.onClick.AddListener(Confirm);
            }
            Cancel();
        }

        public void Open()
        {
            if (IsShowing || menu == null || !menu.CanResetAccount || game.State != ForestRunnerGame.RunState.Ready) return;
            EventSystem.current?.SetSelectedGameObject(null);
            overlay.SetActive(true);
            overlay.transform.SetAsLastSibling();
        }

        private void Confirm()
        {
            if (!IsShowing || !menu.CanResetAccount || game.State != ForestRunnerGame.RunState.Ready) return;
            Cancel(); // Lock out duplicate button events before changing any account data.
            game.ResetAccount();
        }

        public bool HandleBack()
        {
            if (!IsShowing) return false;
            Cancel();
            return true;
        }

        public void Cancel()
        {
            if (IsShowing) EventSystem.current?.SetSelectedGameObject(null);
            if (overlay != null) overlay.SetActive(false);
        }
        private void OnDisable() => Cancel();

        public void ApplyLayout(bool portrait, Vector2 available)
        {
            if (panel == null) return;
            Position(overlay.transform, 0, 0, available.x, available.y);
            Vector2 size = portrait ? new Vector2(600, 720) : new Vector2(560, 440);
            Position(panel, 0, 0, size.x, size.y);
            panel.localScale = Vector3.one * Mathf.Min(1, (available.x - 48) / size.x, (available.y - 48) / size.y);
            Place(heading, 0, portrait ? 286 : 164, size.x - 64, 44, 22);
            Place(warning, 0, portrait ? 216 : 108, size.x - 64, 32, portrait ? 14 : 12);
            Place(details, 0, portrait ? 48 : -4, size.x - 64, portrait ? 224 : 152, portrait ? 16 : 12);
            Place(note, 0, portrait ? -132 : -102, size.x - 64, 52, portrait ? 14 : 11);
            float x = portrait ? 136 : 122, y = portrait ? -266 : -164;
            Control(cancel, -x, y, portrait ? 248 : 224, portrait ? 100 : 56);
            Control(confirm, x, y, portrait ? 248 : 224, portrait ? 100 : 56);
        }

        public void EnsureUi(Transform options, Font font, Sprite panelSprite, Sprite buttonSprite)
        {
            open = Control("Reset account", options, "RESET ACCOUNT", font, buttonSprite, Warning);
            overlay = Node("Reset account confirmation", transform).gameObject;
            Transform scrim = Node("Reset account scrim", overlay.transform);
            Stretch(scrim);
            Art(scrim, null, new Color(0.015f, 0.04f, 0.045f, 0.96f), false).raycastTarget = true;
            panel = (RectTransform)Node("Reset account panel", overlay.transform);
            Art(panel, panelSprite, Color.white, true);
            heading = Label("Reset account heading", panel, "RESET ACCOUNT?", font, 22, 0, 0, 496, 44, Warning);
            warning = Label("Reset account warning", panel, "THIS CANNOT BE UNDONE", font, 12, 0, 0, 496, 32, Warning);
            details = Label("Reset account details", panel,
                "ALL DINO FRAGMENTS + UNLOCKS\nALL TRAILS + SLASH STYLES\nSLASH CHARGES + EGGS\nRECORDS + DAILY MISSIONS", font, 12, 0, 0, 496, 152, Cream);
            note = Label("Reset account note", panel, "BLUE STAYS AVAILABLE.\nSOUND SETTINGS ARE KEPT.", font, 11, 0, 0, 496, 52, Cream);
            cancel = Control("Cancel account reset", panel, "CANCEL", font, buttonSprite, Cream);
            confirm = Control("Confirm account reset", panel, "RESET", font, buttonSprite, Warning);
            ApplyLayout(false, new Vector2(1280, 720));
            overlay.SetActive(false);
        }

        private static void Place(Text label, float x, float y, float width, float height, int size)
        {
            Position(label.transform, x, y, width, height);
            label.fontSize = size;
        }
        private static Button Control(string name, Transform parent, string caption, Font font, Sprite sprite, Color color)
        {
            Transform node = Node(name, parent);
            Image image = Art(node, sprite, Color.white, true); image.raycastTarget = true;
            Button button = node.GetComponent<Button>();
            if (button == null) button = node.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            Label("Label", node, caption, font, 16, 0, 0, 200, 40, color);
            return button;
        }
        private static void Control(Button button, float x, float y, float width, float height)
        {
            Position(button.transform, x, y, width, height);
            Place(button.GetComponentInChildren<Text>(true), 0, 0, width - 24, height - 16, height >= 100 ? 18 : 16);
            button.GetComponent<RunnerUiButtonFeedback>()?.RefreshContentLayout();
        }
    }
}
