using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static DinoRush.RunnerShopPresentation;

namespace DinoRush
{
    /// <summary>Shared scene-authoring and responsive layout for the map gallery.</summary>
    public static class RunnerMapSelectionUi
    {
        private static readonly Color Cream = new Color32(240, 242, 207, 255);
        private static readonly Color Sage = new Color32(174, 203, 152, 255);
        private static readonly Color Gold = new Color32(255, 210, 117, 255);
        private static readonly Color Ink = new Color32(12, 35, 39, 255);

        private static List<Transform> Cards(Transform panel)
        {
            var cards = new List<Transform>();
            foreach (Transform child in panel)
                if (child.name.StartsWith("Choose ", System.StringComparison.Ordinal)) cards.Add(child);
            return cards;
        }

        public static void EnsureUi(Transform panel)
        {
            if (panel == null) return;
            Font font = panel.Find("Map heading").GetComponent<Text>().font;
            var cards = Cards(panel);
            Label("Map eyebrow", panel, "ENDLESS MODE / " + cards.Count + " TRAILS", font, 10, 0, 0, 560, 24, Gold);
            Label("Selection eyebrow", panel, "SELECTED TRAIL", font, 9, 0, 0, 400, 20, Sage);
            Art(Node("Map footer divider", panel), null, new Color32(75, 101, 83, 255), false);
            panel.Find("Map hint").GetComponent<Text>().text = "A NEW VIEW. THE SAME ADVENTURE.";
            for (int i = 0; i < cards.Count; i++)
            {
                Transform card = cards[i];
                Image preview = card.Find("Map preview").GetComponent<Image>();
                preview.preserveAspect = true;
                Art(Node("Trail number background", card), null, Ink, false);
                Label("Trail number", card, (i + 1).ToString("00"), font, 10, 0, 0, 32, 24, Cream);
                Art(Node("Selected tag background", card), null, Gold, false);
                Label("Selected tag", card, "SELECTED", font, 9, 0, 0, 104, 24, Ink);
                Art(Node("Selected edge", card), null, Gold, false);
                // The whole card is the hit target; scenery and badges never intercept taps.
                foreach (Graphic graphic in card.GetComponentsInChildren<Graphic>(true))
                    if (graphic.transform != card) graphic.raycastTarget = false;
            }
        }

        public static void SetSelected(Transform card, bool selected)
        {
            foreach (string name in new[] { "Selected tag background", "Selected tag", "Selected edge" })
                card.Find(name)?.gameObject.SetActive(selected);
            card.Find("Map name").GetComponent<Text>().color = selected ? Gold : Cream;
        }

        public static void Apply(Transform panel, bool portrait, Vector2 available)
        {
            if (panel == null) return;
            var cards = Cards(panel);
            int columns = portrait ? 2 : 3;
            int rows = Mathf.CeilToInt(cards.Count / (float)columns);
            float width = portrait ? 640 : 1120;
            float height = portrait ? 1120 + Mathf.Max(0, rows - 3) * 220 : 752 + Mathf.Max(0, rows - 2) * 256;
            float top = height * 0.5f, bottom = -top;
            Place(panel, 0, 0, width, height);
            float scale = Mathf.Min(1, (available.x - 48) / width, (available.y - 48) / height);
            RunnerUiReveal reveal = panel.GetComponent<RunnerUiReveal>();
            if (Application.isPlaying && reveal != null) reveal.SetRestLayout(Vector2.zero, Vector3.one * scale);
            else panel.localScale = Vector3.one * scale;

            TextLayout(panel.Find("Map eyebrow"), 0, top - (portrait ? 64 : 22), width - 80, 24, portrait ? 12 : 10, TextAnchor.MiddleCenter);
            TextLayout(panel.Find("Map heading"), 0, top - (portrait ? 104 : 58), width - 80, 44, portrait ? 24 : 28, TextAnchor.MiddleCenter);
            TextLayout(panel.Find("Map hint"), 0, top - (portrait ? 148 : 100), width - 80, 26, 12, TextAnchor.MiddleCenter);
            for (int i = 0; i < cards.Count; i++)
            {
                Transform card = cards[i];
                int row = i / columns;
                int countInRow = Mathf.Min(columns, cards.Count - row * columns);
                float cardWidth = portrait ? 268 : 328;
                float cardHeight = portrait ? 204 : 240;
                float previewWidth = cardWidth - 24;
                Place(card, (i % columns - (countInRow - 1) * 0.5f) * (portrait ? 288 : 344),
                    top - (portrait ? 300 : 246) - row * (portrait ? 220 : 256), cardWidth, cardHeight);
                Place(card.Find("Map preview"), 0, 22, previewWidth, previewWidth * 9 / 16);
                TextLayout(card.Find("Map name"), 0, portrait ? -62 : -80, previewWidth, 26, portrait ? 16 : 18, TextAnchor.MiddleLeft);
                TextLayout(card.Find("Map description"), 0, portrait ? -86 : -105, previewWidth, 20, portrait ? 11 : 10, TextAnchor.MiddleLeft);
                float badgeY = portrait ? 72 : 88;
                float left = -previewWidth * 0.5f + 22;
                float right = previewWidth * 0.5f - 58;
                Place(card.Find("Trail number background"), left, badgeY, 32, 24);
                TextLayout(card.Find("Trail number"), left, badgeY, 32, 24, portrait ? 12 : 10, TextAnchor.MiddleCenter);
                Place(card.Find("Selected tag background"), right, badgeY, 104, 24);
                TextLayout(card.Find("Selected tag"), right, badgeY, 104, 24, portrait ? 11 : 9, TextAnchor.MiddleCenter);
                Place(card.Find("Selected edge"), 0, -cardHeight * 0.5f + 2, cardWidth - 24, 3);
                card.GetComponent<RunnerUiButtonFeedback>()?.RefreshContentLayout();
            }

            Place(panel.Find("Map footer divider"), 0, bottom + (portrait ? 254 : 110), width - 80, 2);
            TextLayout(panel.Find("Selection eyebrow"), 0, bottom + (portrait ? 220 : 82), portrait ? 560 : 400, 20, portrait ? 11 : 9, TextAnchor.MiddleCenter);
            TextLayout(panel.Find("Selected map status"), 0, bottom + (portrait ? 188 : 50), portrait ? 560 : 400, 30, 16, TextAnchor.MiddleCenter);
            Control(panel.Find("Map selector back"), portrait ? -152 : -382, bottom + (portrait ? 96 : 56), portrait ? 256 : 260,
                portrait ? 104 : 64, portrait ? 18 : 20);
            Control(panel.Find("Start selected map"), portrait ? 140 : 372, bottom + (portrait ? 96 : 56), 280,
                portrait ? 104 : 64, portrait ? 18 : 20);
        }

        private static void Control(Transform node, float x, float y, float width, float height, int size)
        {
            Place(node, x, y, width, height);
            TextLayout(node.Find("Label"), 0, 0, width - 32, height - 16, size, TextAnchor.MiddleCenter);
            node.GetComponent<RunnerUiButtonFeedback>()?.RefreshContentLayout();
        }

        private static void TextLayout(Transform node, float x, float y, float width, float height, int size, TextAnchor alignment)
        {
            if (node == null) return;
            Place(node, x, y, width, height);
            Text text = node.GetComponent<Text>();
            text.fontSize = size;
            text.alignment = alignment;
            text.resizeTextForBestFit = false;
            text.lineSpacing = 1.2f;
        }

        private static void Place(Transform node, float x, float y, float width, float height)
        {
            if (node == null) return;
            Position(node, x, y, width, height);
            node.localScale = Vector3.one;
        }
    }
}
