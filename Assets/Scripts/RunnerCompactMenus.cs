using UnityEngine;
using UnityEngine.UI;

namespace DinoRush
{
    /// <summary>Content-sized utility dialogs, independent of the larger Shop and reward layouts.</summary>
    public static class RunnerCompactMenus
    {
        public enum DialogKind { Shop, Options, Quit, Mode }

        public static void ApplyDialog(Transform panel, DialogKind kind, bool portrait, Vector2 available)
        {
            if (panel == null) return;
            Transform heading = panel.Find("Dialog heading");
            Transform back = panel.Find("Back to main menu");
            if (kind == DialogKind.Options)
            {
                Panel(panel, portrait ? new Vector2(560, 440) : new Vector2(480, 352), available);
                Label(heading, 0, portrait ? 172 : 126, portrait ? 488 : 416, 40, portrait ? 24 : 22);
                Transform options = panel.Find("Options page");
                Control(options.Find("Toggle sound effects"), 0, portrait ? 94 : 66, portrait ? 472 : 328,
                    portrait ? 100 : 52, portrait ? 22 : 18, null, true);
                Label(options.Find("Controls help"), 0, portrait ? -20 : -22, portrait ? 488 : 420,
                    portrait ? 112 : 100, portrait ? 12 : 10);
                Control(options.Find("Redeem gift"), portrait ? -124 : -90, portrait ? -146 : -116,
                    portrait ? 224 : 156, portrait ? 100 : 52, portrait ? 18 : 14, "REDEEM");
                Control(back, portrait ? 124 : 90, portrait ? -146 : -116,
                    portrait ? 224 : 156, portrait ? 100 : 52, 18, "BACK");
            }
            else if (kind == DialogKind.Quit)
            {
                Panel(panel, portrait ? new Vector2(560, 320) : new Vector2(448, 272), available);
                Label(heading, 0, portrait ? 108 : 86, portrait ? 488 : 400, 40, 22);
                Transform quit = panel.Find("Quit confirmation page");
                Label(quit.Find("Quit question"), 0, portrait ? 42 : 24, portrait ? 488 : 384, portrait ? 62 : 52, portrait ? 12 : 11);
                float x = portrait ? 124 : 98, y = portrait ? -84 : -70;
                float width = portrait ? 224 : 180, height = portrait ? 100 : 52;
                Control(back, -x, y, width, height, 14, "CANCEL");
                Control(quit.Find("Confirm quit"), x, y, width, height, portrait ? 14 : 12, "QUIT GAME");
            }
            else if (kind == DialogKind.Mode)
            {
                Panel(panel, portrait ? new Vector2(600, 640) : new Vector2(560, 400), available);
                Label(heading, 0, portrait ? 264 : 154, portrait ? 536 : 504, 40, portrait ? 24 : 20);
                Transform modes = panel.Find("Mode selection page");
                ModeCard(modes.Find("Story mode"), true, portrait);
                ModeCard(modes.Find("Endless mode"), false, portrait);
                Control(back, 0, portrait ? -252 : -152, portrait ? 512 : 320, portrait ? 100 : 52, 18, "BACK");
            }
            else
            {
                // The Shop keeps its roomy layout, even after visiting a compact dialog.
                Panel(panel, portrait ? new Vector2(640, 1240) : new Vector2(960, 644), available);
                Label(heading, 0, portrait ? 566 : 280, portrait ? 560 : 900, 48, 24);
                Control(back, portrait ? 0 : -272, portrait ? -560 : -272, portrait ? 560 : 340, portrait ? 100 : 52, 18, "BACK");
                panel.Find("Customize page")?.GetComponent<RunnerShopCollection>()?.ApplyLayout(portrait);
            }
        }

        private static void ModeCard(Transform card, bool story, bool portrait)
        {
            if (card == null) return;
            Position(card, portrait ? 0 : story ? -126 : 126, portrait ? (story ? 126 : -86) : 0,
                portrait ? 528 : 236, portrait ? 196 : 244);
            Transform art = card.Find("Mode illustration");
            Position(art, portrait ? -152 : 0, portrait ? 0 : 50, portrait ? 184 : 208, 112);
            Position(art != null ? art.Find("Trail dino") : null, -52, -22, 40, 44);
            Label(card.Find("Label"), portrait ? 96 : 0, portrait ? 48 : -28, portrait ? 272 : 212, 32, portrait ? 18 : 14);
            Label(card.Find("Mode description"), portrait ? 96 : 0, portrait ? 0 : -60, portrait ? 264 : 212, 40, portrait ? 12 : 10);
            Label(card.Find("Mode status"), portrait ? 96 : 0, portrait ? -52 : -96, portrait ? 264 : 212, 24, portrait ? 12 : 10);
            card.GetComponent<RunnerUiButtonFeedback>()?.RefreshContentLayout();
        }

        public static void ApplyPause(Transform panel, bool portrait, Vector2 available)
        {
            if (panel == null) return;
            Panel(panel, portrait ? new Vector2(520, 448) : new Vector2(420, 336), available);
            float width = portrait ? 432 : 344;
            Label(panel.Find("Pause eyebrow"), 0, portrait ? 184 : 134, width, 22, portrait ? 11 : 10);
            Label(panel.Find("Pause title"), 0, portrait ? 144 : 96, width, portrait ? 44 : 40, portrait ? 26 : 24);
            float x = portrait ? 112 : 90;
            Label(panel.Find("Score caption"), -x, portrait ? 94 : 54, portrait ? 192 : 164, 22, 11);
            Label(panel.Find("Best caption"), x, portrait ? 94 : 54, portrait ? 192 : 164, 22, 11);
            Stat(panel.Find("Paused score"), -x, portrait ? 60 : 20, portrait ? 192 : 164, portrait ? 26 : 24);
            Stat(panel.Find("Paused best"), x, portrait ? 60 : 20, portrait ? 192 : 164, portrait ? 26 : 24);
            Position(panel.Find("Section divider"), 0, portrait ? 26 : -18, width, 2);
            Control(panel.Find("Resume run"), 0, portrait ? -40 : -58, width, portrait ? 100 : 52, portrait ? 20 : 16);
            Control(panel.Find("Restart run"), -x, portrait ? -148 : -116, portrait ? 208 : 164, portrait ? 100 : 48, portrait ? 16 : 12);
            Control(panel.Find("Quit run"), x, portrait ? -148 : -116, portrait ? 208 : 164, portrait ? 100 : 48, portrait ? 16 : 12);
        }

        private static void Stat(Transform node, float x, float y, float width, int size)
        {
            Label(node, x, y, width, 44, size);
            if (node == null) return;
            Text text = node.GetComponent<Text>();
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = size;
        }

        private static void Panel(Transform node, Vector2 size, Vector2 available)
        {
            Position(node, 0, 0, size.x, size.y);
            float scale = Mathf.Min(1, (available.x - 48) / size.x, (available.y - 48) / size.y);
            RunnerUiReveal reveal = node.GetComponent<RunnerUiReveal>();
            if (Application.isPlaying && reveal != null) reveal.SetRestLayout(Vector2.zero, Vector3.one * scale);
            else node.localScale = Vector3.one * scale;
        }

        private static void Control(Transform node, float x, float y, float width, float height, int size,
            string caption = null, bool showIcon = false)
        {
            if (node == null) return;
            Position(node, x, y, width, height);
            Transform icon = node.Find("Icon");
            if (icon != null)
            {
                icon.gameObject.SetActive(showIcon);
                Position(icon, -width * 0.5f + 44, 0, height >= 80 ? 32 : 24, height >= 80 ? 32 : 24);
            }
            Transform label = node.Find("Label");
            if (label == null) label = node.GetComponentInChildren<Text>(true)?.transform;
            Label(label, showIcon ? 20 : 0, 0, width - (showIcon ? 96 : 24), height - 8, size);
            if (caption != null && label != null) label.GetComponent<Text>().text = caption;
            node.GetComponent<RunnerUiButtonFeedback>()?.RefreshContentLayout();
        }

        private static void Label(Transform node, float x, float y, float width, float height, int size)
        {
            if (node == null) return;
            Position(node, x, y, width, height);
            Text text = node.GetComponent<Text>();
            text.fontSize = size;
            text.resizeTextForBestFit = false;
            text.lineSpacing = text.text.Contains("\n") ? 1.65f : 1.4f;
        }

        private static void Position(Transform node, float x, float y, float width, float height)
        {
            if (node == null) return;
            var rect = (RectTransform)node;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = Vector3.one;
        }
    }
}
