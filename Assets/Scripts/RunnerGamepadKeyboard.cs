using System;
using UnityEngine;
using UnityEngine.UI;
using static DinoRush.RunnerShopPresentation;

namespace DinoRush
{
    /// <summary>A small controller-operated code keyboard; never submits or spends currency on closing.</summary>
    public sealed class RunnerGamepadKeyboard : IDisposable
    {
        private readonly RectTransform root, panel;
        private readonly Text preview;
        private readonly Font font;
        private InputField input;
        public bool IsOpen => root.gameObject.activeSelf;
        public Transform Root => root;
        public InputField Target => input;

        public RunnerGamepadKeyboard(RectTransform parent, RunnerAudio audio)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            root = (RectTransform)Node("Gamepad code keyboard", parent);
            Stretch(root);
            Art(root, null, new Color(0.02f, 0.06f, 0.07f, 1f), false).raycastTarget = true;
            panel = (RectTransform)Node("Keyboard panel", root);
            Position(panel, 0, 0, 680, 400);
            Label("Keyboard heading", panel, "ENTER GIFT CODE", font, 24, 0, 166, 640, 40, Color.white);
            preview = Label("Keyboard text", panel, "", font, 20, 0, 112, 624, 44, new Color32(255, 224, 133, 255));
            preview.supportRichText = false;
            preview.horizontalOverflow = HorizontalWrapMode.Wrap;
            const string letters = "1234567890QWERTYUIOPASDFGHJKL-ZXCVBNM._/";
            for (int i = 0; i < letters.Length; i++)
            {
                string letter = letters[i].ToString();
                Key("Key " + letter, letter, -288 + i % 10 * 64, 52 - i / 10 * 48, 58, () => Append(letter));
            }
            Key("Keyboard space", "SPACE", -228, -162, 136, () => Append(" "));
            Key("Keyboard delete", "DELETE", -76, -162, 136, Backspace);
            Key("Keyboard done", "DONE", 76, -162, 136, Close);
            Key("Keyboard close", "CLOSE", 228, -162, 136, Close);
            RunnerUiEffects.Install(root, audio);
            root.gameObject.SetActive(false);
        }

        private void Key(string name, string caption, float x, float y, float width, UnityEngine.Events.UnityAction action)
        {
            Transform node = Node(name, panel);
            Position(node, x, y, width, 40);
            Image image = Art(node, null, new Color32(40, 76, 77, 255), false);
            image.raycastTarget = true;
            Button button = node.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(action);
            Label("Label", node, caption, font, 16, 0, 0, width - 4, 36, Color.white).raycastTarget = false;
        }

        public void Open(InputField target)
        {
            input = target;
            input.DeactivateInputField();
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            Refresh();
        }

        public void Append(string value)
        {
            if (!IsOpen || input == null) return;
            string text = input.text + value;
            if (input.characterLimit > 0 && text.Length > input.characterLimit) text = text.Substring(0, input.characterLimit);
            input.text = text;
            Refresh();
        }

        public void Backspace()
        {
            if (!IsOpen || input == null || input.text.Length == 0) return;
            input.text = input.text.Substring(0, input.text.Length - 1);
            Refresh();
        }

        public void Validate()
        {
            if (IsOpen && (input == null || !input.IsActive())) Close();
            if (IsOpen) Refresh();
        }

        private void Refresh()
        {
            preview.text = input.text.Length > 42 ? "…" + input.text.Substring(input.text.Length - 42) : input.text + "_";
            var parent = (RectTransform)root.parent;
            panel.localScale = Vector3.one * Mathf.Min(1f, (parent.rect.width - 24) / 680f, (parent.rect.height - 60) / 400f);
        }

        public void Close() => root.gameObject.SetActive(false);
        public void Dispose() => UnityEngine.Object.Destroy(root.gameObject);
    }
}
