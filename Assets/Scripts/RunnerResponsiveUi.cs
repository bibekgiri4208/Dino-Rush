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
        private readonly List<string> mapCards = new List<string>();

        public RunnerResponsiveUi(RectTransform root)
        {
            this.root = root;
            canvas = root.GetComponentInParent<Canvas>();
            canvas.gameObject.layer = 5;
            scaler = canvas.GetComponent<CanvasScaler>();
            Transform maps = root.Find("Forest Main Menu/Map selector/Map selection panel");
            if (maps != null)
                foreach (Transform child in maps)
                    if (child.name.StartsWith("Choose ", System.StringComparison.Ordinal)) mapCards.Add(child.name);
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
            }
            Fit("Forest Main Menu/Hatch reveal/Hatch reward panel", layout.UiSize);
            root.Find("Forest Main Menu")?.GetComponent<ForestMainMenu>()?.ApplyDialogLayout(layout.Portrait, layout.UiSize);
            RunnerCompactMenus.ApplyPause(root.Find("Forest Pause Menu"), layout.Portrait, layout.UiSize);

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
            string[] menuButtons = { "Play", "Shop", "Options", "Quit" };
            for (int i = 0; i < menuButtons.Length; i++)
                Place("Forest Main Menu/Main menu buttons/" + menuButtons[i], 0, 84 - i * 120, 520, 104);

            const string dialog = "Forest Main Menu/Menu dialog/Moss framed dialog";
            const string custom = dialog + "/Customize page";
            Place(custom + "/Shop egg balance", 0, 428, 560, 30);
            const string product = custom + "/Mystery egg display";
            Place(product, 0, 320, 560, 176);
            Place(product + "/Egg display glow", -178, 0, 144, 144);
            Place(product + "/Showcase egg", -178, 0, 120, 120);
            Place(product + "/Companion egg left", -220, -10, 64, 64);
            Place(product + "/Companion egg right", -136, -10, 64, 64);
            Place(product + "/Egg product title", 80, 50, 320, 30);
            FontSize(product + "/Egg product title", 16);
            Place(product + "/Egg product description", 80, 0, 320, 44);
            Place(product + "/Egg unlock requirement", 80, -52, 320, 24);
            Place(custom + "/Buy mystery egg", 0, 172, 520, 88);
            Place(custom + "/Buy mystery egg/Label", 0, 0, 496, 64);
            FontSize(custom + "/Buy mystery egg/Label", 14);
            Place(custom + "/Mystery egg result", 0, 92, 560, 48);
            string[] dinos = { "Blue", "Red", "Yellow", "Green" };
            for (int i = 0; i < dinos.Length; i++)
            {
                string card = custom + "/Dino choices/Select " + dinos[i] + " dino";
                Place(card, i % 2 == 0 ? -142 : 142, i < 2 ? -44 : -240, 236, 176);
                Place(card + "/Dino name", 0, -26, 200, 28);
                Place(card + "/Fragments", 0, -60, 212, 36);
                FontSize(card + "/Fragments", 12);
                FontSize(card + "/Dino name", 18);
            }
            Place(custom + "/Selected status", 0, -366, 560, 28);
            Place(custom + "/Map selection", 0, -402, 560, 24);
            FontSize(custom + "/Selected status", 14);
            FontSize(custom + "/Map selection", 14);

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

            const string maps = "Forest Main Menu/Map selector/Map selection panel";
            int rows = Mathf.CeilToInt(mapCards.Count / 2f);
            float extra = Mathf.Max(0, rows - 2) * 228f;
            Place(maps, 0, 0, 620, 720 + extra);
            Place(maps + "/Map heading", 0, 306 + extra * 0.5f, 560, 48);
            Place(maps + "/Map hint", 0, 262 + extra * 0.5f, 560, 30);
            for (int i = 0; i < mapCards.Count; i++)
            {
                bool lastSingle = i == mapCards.Count - 1 && mapCards.Count % 2 == 1;
                Place(maps + "/" + mapCards[i], lastSingle ? 0 : (i % 2 == 0 ? -142 : 142),
                    124 + extra * 0.5f - i / 2 * 228, 248, 208);
            }
            Place(maps + "/Selected map status", 0, -238 - extra * 0.5f, 560, 30);
            Place(maps + "/Map selector back", -146, -306 - extra * 0.5f, 244, 72);
            Place(maps + "/Map selector back/Label", 0, 0, 220, 40);
            Place(maps + "/Start selected map", 126, -306 - extra * 0.5f, 268, 72);
            Place(maps + "/Start selected map/Label", 0, 0, 244, 40);

            Place("Menu card", 0, 0, 600, 560);
            Place("Menu card/Eyebrow", 0, 220);
            Place("Menu card/Title", 0, 168);
            Place("Menu card/Primary action", 0, -100, 504, 88);
            Place("Menu card/Results main menu", 0, -204, 504, 80);
            Fit(maps, size);
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
