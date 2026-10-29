using System;
using UnityEngine;
using UnityEngine.UI;

namespace DinoRush
{
    /// <summary>References the actual menu skin, never a second approximation of the game's UI.</summary>
    public sealed class RunnerStoryTheme : MonoBehaviour
    {
        public Font Font { get; private set; }
        public Sprite Panel { get; private set; }
        public Sprite Primary { get; private set; }
        public Sprite Secondary { get; private set; }
        public void Initialize(ForestRunnerGame game)
        {
            var menu = game.GetComponentInChildren<ForestMainMenu>(true);
            Transform dialog = menu != null ? menu.transform.Find("Menu dialog/Moss framed dialog") : null;
            Panel = dialog != null ? dialog.GetComponent<Image>().sprite : null;
            Font = dialog != null ? dialog.GetComponentInChildren<Text>(true).font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Transform root = game.transform.Find("Forest runner UI/16 by 9 safe content");
            Primary = root?.Find("Forest Pause Menu/Resume run")?.GetComponent<Image>().sprite;
            Secondary = root?.Find("Forest Pause Menu/Restart run")?.GetComponent<Image>().sprite;
            if (Primary == null) Primary = menu?.transform.Find("Main menu buttons/Play")?.GetComponent<Image>().sprite;
            if (Secondary == null) Secondary = Primary;
        }
    }

    internal static class RunnerStoryUi
    {
        internal static readonly Color Ink = new Color32(15, 35, 39, 255);
        internal static readonly Color Cream = new Color32(240, 242, 207, 255);
        internal static readonly Color Mint = new Color32(174, 203, 152, 255);
        internal static readonly Color Gold = new Color32(255, 210, 117, 255);
        internal static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.gameObject.layer = 5;
            RunnerShopPresentation.Position(rect, x, y, w, h); return rect;
        }
        internal static Image Box(string name, Transform parent, float x, float y, float w, float h, Color color)
        {
            var image = Rect(name, parent, x, y, w, h).gameObject.AddComponent<Image>();
            image.color = color; image.raycastTarget = false; return image;
        }
        internal static Image Panel(string name, Transform parent, float x, float y, float w, float h)
        {
            Image image = Box(name, parent, x, y, w, h, Color.white);
            Skin(image, parent.GetComponentInParent<RunnerStoryTheme>(true)?.Panel); return image;
        }
        internal static void Skin(Image image, Sprite sprite)
        {
            image.sprite = sprite; image.type = Image.Type.Sliced; image.pixelsPerUnitMultiplier = 0.5f;
            image.color = sprite != null ? Color.white : Ink;
        }
        internal static Text Label(string name, Transform parent, string caption, float x, float y, float w, float h,
            int size = 22, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var text = Rect(name, parent, x, y, w, h).gameObject.AddComponent<Text>();
            var theme = parent.GetComponentInParent<RunnerStoryTheme>(true);
            text.font = theme != null ? theme.Font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = Mathf.Max(12, Mathf.RoundToInt(size * 0.72f));
            text.resizeTextForBestFit = true; text.resizeTextMinSize = Mathf.Max(10, text.fontSize - 5); text.resizeTextMaxSize = text.fontSize;
            text.text = caption; text.color = Cream; text.alignment = alignment; text.raycastTarget = false;
            text.supportRichText = false; text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate; text.lineSpacing = 1.5f;
            return text;
        }
        internal static Button Button(string name, Transform parent, string caption, float x, float y, float w, float h, Action click)
        {
            Image image = Box(name, parent, x, y, w, h, Color.white); image.raycastTarget = true;
            Skin(image, parent.GetComponentInParent<RunnerStoryTheme>(true)?.Secondary);
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock colors = button.colors; colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1, 1, 0.88f); colors.pressedColor = new Color(0.74f, 0.84f, 0.7f);
            colors.disabledColor = new Color(0.48f, 0.56f, 0.48f); colors.fadeDuration = 0.08f; button.colors = colors;
            Label("Label", image.transform, caption, 0, 0, w - 32, h - 12, 21);
            button.onClick.AddListener(() => click()); return button;
        }
        internal static void Primary(Button button, bool primary = true)
        {
            var theme = button.GetComponentInParent<RunnerStoryTheme>(true);
            Skin(button.GetComponent<Image>(), primary ? theme?.Primary : theme?.Secondary);
        }
        internal static void Caption(Button button, string caption) => button.GetComponentInChildren<Text>().text = caption;
        internal static void Fit(RectTransform panel, Vector2 available, Vector2 design)
        {
            panel.sizeDelta = design;
            panel.localScale = Vector3.one * Mathf.Min(1.15f, Mathf.Min((available.x - 32) / design.x, (available.y - 48) / design.y));
        }
    }
}
