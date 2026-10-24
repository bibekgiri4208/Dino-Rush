using UnityEngine;
using UnityEngine.UI;
using static DinoRush.RunnerShopPresentation;

namespace DinoRush
{
    public sealed class RunnerSlashHud : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private Button button;
        private ForestRunnerGame game;
        private void Start()
        {
            game = GetComponentInParent<ForestRunnerGame>();
            button.onClick.AddListener(() => game.Attack());
        }
        private void Update()
        {
            bool running = game != null && game.State == ForestRunnerGame.RunState.Running;
            button.gameObject.SetActive(running);
            if (!running) return;
            int remaining = Mathf.Min(RunnerSlashProgress.Charges, 3 - game.SlashesUsed);
            label.text = "SLASH " + remaining + "/3  [ENTER]\n" + RunnerSlashProgress.Equipped.ToString().ToUpperInvariant();
            button.interactable = remaining > 0 && !game.SlashAttack.Active;
        }
        public void EnsureUi(Font font, Sprite sprite)
        {
            Transform node = Node("Slash indicator", transform);
            var rect = (RectTransform)node; rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0); rect.anchoredPosition = new Vector2(0, 26); rect.sizeDelta = new Vector2(218, 76);
            Image image = Art(node, sprite, Color.white, true); image.raycastTarget = true;
            button = node.GetComponent<Button>(); if (button == null) button = node.gameObject.AddComponent<Button>();
            button.targetGraphic = image; button.navigation = new Navigation { mode = Navigation.Mode.None };
            label = Label("Slash charges", node, "SLASH 0/3  [ENTER]", font, 11, 0, 0, 202, 64, Color.white);
            node.gameObject.SetActive(false);
        }
        public void ApplyLayout(bool portrait)
        {
            if (button == null) return;
            var rect = (RectTransform)button.transform;
            rect.sizeDelta = portrait ? new Vector2(340, 112) : new Vector2(218, 76);
            label.rectTransform.sizeDelta = rect.sizeDelta - new Vector2(16, 12);
            label.fontSize = portrait ? 16 : 11;
            button.GetComponent<RunnerUiButtonFeedback>()?.RefreshContentLayout();
        }
    }
}
