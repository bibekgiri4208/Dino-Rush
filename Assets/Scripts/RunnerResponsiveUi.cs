using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DinoRush
{
    /// <summary>Reflows the existing authored widgets; never rebuilds menus or their event bindings.</summary>
    public sealed class RunnerResponsiveUi
    {
        private sealed class Placement
        {
            public RectTransform rect;
            public Vector2 position, size, anchorMin, anchorMax, pivot;
            public Vector3 scale;
            public RunnerUiReveal reveal;
            public Text text;
            public int fontSize;

            public void Restore()
            {
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
                rect.pivot = pivot;
                rect.sizeDelta = size;
                SetPosition(position);
                if (text != null) text.fontSize = fontSize;
            }

            public void SetPosition(Vector2 value)
            {
                if (reveal != null) reveal.SetRestLayout(value, scale);
                else { rect.anchoredPosition = value; rect.localScale = scale; }
            }
        }

        private readonly RectTransform root;
        private readonly Canvas canvas;
        private readonly CanvasScaler scaler;
        private readonly Dictionary<string, Placement> placements = new Dictionary<string, Placement>();
        private readonly RectTransform[] mattes = new RectTransform[4];

        public RunnerResponsiveUi(RectTransform root)
        {
            this.root = root;
            canvas = root.GetComponentInParent<Canvas>();
            canvas.gameObject.layer = 5;
            scaler = canvas.GetComponent<CanvasScaler>();
            foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
            {
                rect.gameObject.layer = 5; // Also supports rendering the UI independently for resolution tests.
                if (rect == root) continue;
                RunnerUiReveal reveal = rect.GetComponent<RunnerUiReveal>();
                Text text = rect.GetComponent<Text>();
                placements[Path(rect)] = new Placement { rect = rect, reveal = reveal,
                    position = reveal != null ? reveal.RestPosition : rect.anchoredPosition,
                    scale = reveal != null ? reveal.RestScale : rect.localScale,
                    size = rect.sizeDelta, anchorMin = rect.anchorMin, anchorMax = rect.anchorMax,
                    pivot = rect.pivot, text = text, fontSize = text != null ? text.fontSize : 0 };
            }
            var holder = new GameObject("Viewport borders", typeof(RectTransform));
            holder.transform.SetParent(canvas.transform, false);
            holder.transform.SetSiblingIndex(0);
            var holderRect = (RectTransform)holder.transform;
            holderRect.anchorMin = Vector2.zero;
            holderRect.anchorMax = Vector2.one;
            holderRect.offsetMin = holderRect.offsetMax = Vector2.zero;
            for (int i = 0; i < mattes.Length; i++)
            {
                var image = new GameObject("Viewport border " + i, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                image.transform.SetParent(holder.transform, false);
                image.gameObject.layer = 5;
                image.color = new Color32(12, 35, 39, 255);
                image.raycastTarget = false;
                mattes[i] = image.rectTransform;
                mattes[i].anchorMin = mattes[i].anchorMax = Vector2.one * 0.5f;
            }
        }

        private string Path(Transform node)
        {
            string path = node.name;
            while (node.parent != root) { node = node.parent; path = node.name + "/" + path; }
            return path;
        }

        public void Apply(RunnerScreenLayout layout)
        {
            // Explicit scaling follows the safe rectangle, not the full physical display behind a notch.
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = layout.UiScale;
            }
            canvas.scaleFactor = layout.UiScale;
            root.anchorMin = root.anchorMax = root.pivot = Vector2.one * 0.5f;
            root.sizeDelta = layout.UiSize;
            root.anchoredPosition = (layout.UiPixels.center - layout.ScreenSize * 0.5f) / layout.UiScale;
            foreach (Placement placement in placements.Values) placement.Restore();

            Size("Menu scrim", layout.UiSize);
            Size("Forest Main Menu", layout.UiSize);
            Size("Forest Main Menu/Menu dialog", layout.UiSize);
            Size("Forest Main Menu/Menu dialog/Dialog scrim", layout.UiSize);
            Size("Forest Main Menu/Map selector", layout.UiSize);
            Size("Forest Main Menu/Map selector/Map selector scrim", layout.UiSize);
            Size("Forest Main Menu/Hatch reveal", layout.UiSize);
            if (layout.Portrait) Portrait(layout.UiSize);
            else
            {
                float extra = (layout.UiSize.y - 720f) * 0.5f;
                foreach (string path in new[] { "Life hearts", "Egg counter", "Stage progress", "Scoreboard", "Sound", "Pause", "Milestone toast", "Forest Main Menu/SFX toggle" })
                    ShiftVertical(path, extra);
                foreach (string path in new[] { "Jump", "Duck", "Trail stats frame", "Gameplay bottom fade", "Controls", "Keyboard controls" })
                    ShiftVertical(path, -extra);
                float extraWidth = (layout.UiSize.x - 1280f) * 0.5f;
                foreach (string path in new[] { "Life hearts", "Egg counter", "Trail stats frame" })
                    ShiftHorizontal(path, -extraWidth);
                foreach (string path in new[] { "Scoreboard", "Sound", "Pause", "Forest Main Menu/SFX toggle" })
                    ShiftHorizontal(path, extraWidth);
                if (placements.TryGetValue("Gameplay bottom fade", out Placement fade))
                {
                    fade.rect.sizeDelta = new Vector2(layout.FullBleedLandscape ? layout.ScreenSize.x / layout.UiScale : layout.UiSize.x,
                        fade.rect.sizeDelta.y);
                    if (layout.FullBleedLandscape)
                        fade.SetPosition(new Vector2((layout.ScreenSize.x * 0.5f - layout.UiPixels.center.x) / layout.UiScale,
                            -layout.UiSize.y * 0.5f - layout.UiPixels.yMin / layout.UiScale + fade.rect.sizeDelta.y * 0.5f));
                }
            }
            MirrorMenuCorners();
            Fit("Forest Main Menu/Hatch reveal/Hatch reward panel", layout.UiSize);
            root.Find("Forest Main Menu")?.GetComponent<ForestMainMenu>()?.ApplyDialogLayout(layout.Portrait, layout.UiSize);
            RunnerCompactMenus.ApplyPause(root.Find("Forest Pause Menu"), layout.Portrait, layout.UiSize);
            root.Find("Forest Main Menu")?.GetComponent<ForestMainMenu>()?.ShopPresentation?.RefreshLayout();

            Rect world = layout.WorldPixels;
            Matte(0, Rect.MinMaxRect(0, 0, world.xMin, layout.ScreenSize.y), layout);
            Matte(1, Rect.MinMaxRect(world.xMax, 0, layout.ScreenSize.x, layout.ScreenSize.y), layout);
            Matte(2, Rect.MinMaxRect(world.xMin, 0, world.xMax, world.yMin), layout);
            Matte(3, Rect.MinMaxRect(world.xMin, world.yMax, world.xMax, layout.ScreenSize.y), layout);
        }

        private void Matte(int index, Rect pixels, RunnerScreenLayout layout)
        {
            mattes[index].anchoredPosition = (pixels.center - layout.ScreenSize * 0.5f) / layout.UiScale;
            mattes[index].sizeDelta = pixels.size / layout.UiScale;
            mattes[index].gameObject.SetActive(pixels.width > 0.1f && pixels.height > 0.1f);
        }

        private void ShiftVertical(string path, float offset)
        {
            if (placements.TryGetValue(path, out Placement placement))
                placement.SetPosition(placement.position + Vector2.up * offset);
        }

        private void ShiftHorizontal(string path, float offset)
        {
            if (placements.TryGetValue(path, out Placement placement))
                placement.SetPosition(placement.rect.anchoredPosition + Vector2.right * offset);
        }
        private void MirrorMenuCorners()
        {
            if (!placements.TryGetValue("Forest Main Menu/Quit", out Placement quit) ||
                !placements.TryGetValue("Forest Main Menu/SFX toggle", out Placement sound)) return;
            quit.rect.sizeDelta = sound.rect.sizeDelta;
            Vector2 position = sound.rect.anchoredPosition;
            quit.SetPosition(new Vector2(-position.x, position.y));
        }

        private void Portrait(Vector2 size)
        {
            float top = size.y * 0.5f, bottom = -top;
            Place("Life hearts", -220, top - 51);
            Place("Stage progress", -146, top - 126);
            Scale("Stage progress", 0.82f);
            Place("Egg counter", -220, top - 195);
            Place("Scoreboard", 202, top - 102);
            Place("Sound", 222, top - 212, 68, 68);
            Place("Pause", 306, top - 212, 68, 68);
            Place("Jump", 170, bottom + 84, 304, 104);
            Place("Duck", -170, bottom + 84, 304, 104);
            ButtonText("Jump", 28);
            ButtonText("Duck", 28);
            Place("Trail stats frame", 0, bottom + 192);
            Place("Milestone toast", 0, 258, 656, 56);
            FontSize("Milestone toast", 18);
            Place("Gameplay bottom fade", 0, bottom + 140, 720, 280);
            Place("Forest Main Menu/SFX toggle", 302, top - 66);
            float logoScale = Mathf.Clamp((size.y - 576f) / 287f, 0.5f, 1f);
            Place("Forest Main Menu/Dino Rush Logo", 0, top - 32f - 320.5f * logoScale);
            Scale("Forest Main Menu/Dino Rush Logo", logoScale);
            Place("Forest Main Menu/Main menu buttons", 0, bottom + 360);
            bool missions = root.Find("Forest Main Menu/Main menu buttons/Missions") != null;
            bool cornerQuit = placements.ContainsKey("Forest Main Menu/Quit");
            string[] menuButtons = missions ? (cornerQuit ? new[] { "Play", "Shop", "Missions", "Options" } : new[] { "Play", "Shop", "Missions", "Options", "Quit" }) :
                cornerQuit ? new[] { "Play", "Shop", "Options" } : new[] { "Play", "Shop", "Options", "Quit" };
            for (int i = 0; i < menuButtons.Length; i++)
                Place("Forest Main Menu/Main menu buttons/" + menuButtons[i], 0, 84 + (menuButtons.Length - 4) * 120 - i * 120, 520, 104);

            const string hatch = "Forest Main Menu/Hatch reveal/Hatch reward panel";
            Place(hatch, 0, 0, 620, 820);
            Place(hatch + "/Hatch eyebrow", 0, 364);
            Place(hatch + "/Hatch heading", 0, 308);
            Place(hatch + "/Hatch light and sparkles", 0, 126);
            Place(hatch + "/Hatching egg", 0, 132);
            Place(hatch + "/Hatched dino", 0, 132);
            const string reward = hatch + "/Hatch reward details";
            Place(reward + "/Fragment reward amount", 0, -26);
            Place(reward + "/Fragment reward species", 0, -82);
            Place(reward + "/Fragment reward progress", 0, -130);
            Place(reward + "/Fragment progress track", 0, -156);
            Place(reward + "/Hatch unlock message", 0, -204);
            Place(hatch + "/Hatch continue", 0, -316, 500, 88);

            Place("Menu card", 0, 0, 600, 560);
            Place("Menu card/Eyebrow", 0, 220);
            Place("Menu card/Title", 0, 168);
            Place("Menu card/Primary action", 0, -100, 504, 88);
            Place("Menu card/Results main menu", 0, -204, 504, 80);
            Fit("Menu card", size);
        }

        private void Place(string path, float x, float y, float width = -1f, float height = -1f)
        {
            if (!placements.TryGetValue(path, out Placement placement)) return;
            placement.SetPosition(new Vector2(x, y));
            if (width >= 0f && height >= 0f) placement.rect.sizeDelta = new Vector2(width, height);
        }

        private void Size(string path, Vector2 size)
        {
            if (placements.TryGetValue(path, out Placement placement)) placement.rect.sizeDelta = size;
        }

        private void FontSize(string path, int size)
        {
            if (placements.TryGetValue(path, out Placement placement) && placement.text != null) placement.text.fontSize = size;
        }

        private void Scale(string path, float factor)
        {
            if (!placements.TryGetValue(path, out Placement placement)) return;
            if (placement.reveal != null)
                placement.reveal.SetRestLayout(placement.reveal.RestPosition, placement.scale * factor);
            else placement.rect.localScale = placement.scale * factor;
        }

        private void Fit(string path, Vector2 available)
        {
            if (!placements.TryGetValue(path, out Placement placement)) return;
            Vector2 size = placement.rect.sizeDelta;
            Scale(path, Mathf.Min(1f, (available.x - 48f) / size.x, (available.y - 48f) / size.y));
        }

        private void ButtonText(string path, int size)
        {
            if (!placements.TryGetValue(path, out Placement placement)) return;
            Text text = placement.rect.GetComponentInChildren<Text>(true);
            if (text != null) text.fontSize = size;
        }
    }
}
