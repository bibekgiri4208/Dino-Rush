using UnityEngine;
using UnityEngine.UI;
using static DinoRush.RunnerShopPresentation;

namespace DinoRush
{
    /// <summary>A themed, pointer/touch-draggable volume control shared by Options and Pause.</summary>
    public static class RunnerMusicUi
    {
        public static Slider EnsureSlider(Transform parent, Button template)
        {
            Transform node = Node("Music volume", parent);
            Slider slider = node.GetComponent<Slider>();
            if (slider != null) return slider;
            var original = template.GetComponent<Image>();
            Image background = Art(node, original.sprite, Color.white, original.type == Image.Type.Sliced);
            background.pixelsPerUnitMultiplier = original.pixelsPerUnitMultiplier;
            background.raycastTarget = true;
            slider = node.gameObject.AddComponent<RunnerMusicSlider>();
            slider.minValue = 0;
            slider.maxValue = 100;
            slider.wholeNumbers = true;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            var text = template.GetComponentInChildren<Text>(true);
            Label("Label", node, "MUSIC 35%", text.font, 14, 0, 12, 304, 24, text.color);
            var track = Art(Node("Volume track", node), null, new Color32(20, 42, 39, 255), false);
            var fill = Art(Node("Volume fill", track.transform), null, new Color32(184, 217, 87, 255), false);
            Stretch(fill.transform);
            var handleArea = (RectTransform)Node("Volume handle area", node);
            var handle = Art(Node("Volume handle", handleArea), null, new Color32(255, 224, 133, 255), false);
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.onValueChanged.AddListener(value => SetVolume(slider, value / 100f));
            Layout(slider, 328, 52, 14);
            return slider;
        }

        public static void SetVolume(Slider slider, float volume)
        {
            if (slider == null) return;
            slider.SetValueWithoutNotify(Mathf.RoundToInt(Mathf.Clamp01(volume) * 100));
            slider.transform.Find("Label").GetComponent<Text>().text = "MUSIC " + slider.value.ToString("0") + "%";
        }

        public static void Layout(Slider slider, float width, float height, int fontSize)
        {
            if (slider == null) return;
            bool large = height >= 80;
            bool compact = width < 260;
            float inset = large ? 30 : compact ? 16 : 22;
            float handleWidth = large ? 24 : compact ? 12 : 14;
            float labelWidth = large ? 192 : compact ? 104 : 136;
            float gap = large ? 18 : compact ? 8 : 12;
            float left = -width * 0.5f + inset;
            float trackStart = left + labelWidth + gap + handleWidth * 0.5f;
            float trackEnd = width * 0.5f - inset - handleWidth * 0.5f;
            float trackWidth = trackEnd - trackStart;
            float trackCenter = (trackStart + trackEnd) * 0.5f;
            // A single row stays inside the moss border even in the 48-unit Pause slot.
            Position(slider.transform.Find("Label"), left + labelWidth * 0.5f, 0, labelWidth, height - (large ? 48 : 28));
            Text label = slider.transform.Find("Label").GetComponent<Text>();
            label.fontSize = Mathf.Min(fontSize, large ? 18 : compact ? 10 : 12);
            label.lineSpacing = 1f;
            label.alignment = TextAnchor.MiddleLeft;
            Position(slider.transform.Find("Volume track"), trackCenter, 0, trackWidth, large ? 10 : 6);
            Position(slider.transform.Find("Volume handle area"), trackCenter, 0, trackWidth, large ? 28 : 18);
            // The Slider owns the handle's anchors along the track.
            slider.handleRect.anchorMin = new Vector2(slider.handleRect.anchorMin.x, 0);
            slider.handleRect.anchorMax = new Vector2(slider.handleRect.anchorMax.x, 1);
            slider.handleRect.sizeDelta = new Vector2(handleWidth, 0);
        }
    }
}
