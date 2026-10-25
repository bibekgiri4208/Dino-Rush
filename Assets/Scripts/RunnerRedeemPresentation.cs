using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static DinoRush.RunnerShopPresentation;

namespace DinoRush
{
    /// <summary>Repeatable gift-code entry and an unscaled, presentation-only saved-wallet celebration.</summary>
    public sealed class RunnerRedeemPresentation : MonoBehaviour
    {
        public const float Duration = 1.5f;
        private static readonly Color Gold = new Color32(255, 215, 120, 255);
        private static readonly Color Cream = new Color32(240, 242, 207, 255);
        [SerializeField] private GameObject overlay, form, celebration;
        [SerializeField] private RectTransform panel;
        [SerializeField] private InputField input;
        [SerializeField] private Button redeem, cancel, continueButton;
        [SerializeField] private Text heading, hint, status, amount, sponsor, balance, continueLabel;
        [SerializeField] private RunnerGiftGraphic gift;
        [SerializeField] private RunnerHatchGlowGraphic glow;
        private RunnerShopProgress shop;
        private RunnerAudio audioCues;
        private Action walletChanged;
        private bool bound, portrait;
        private Vector2 available = new Vector2(1280, 720);
        private float elapsed;
        public bool IsShowing => overlay != null && overlay.activeSelf;
        public bool ShowingGift => IsShowing && celebration.activeSelf;
        public int DisplayedEggs { get; private set; }
        public InputField CodeInput => input;

        public void Initialize(RunnerShopProgress progress, RunnerAudio cues, Action onWalletChanged)
        {
            shop = progress;
            audioCues = cues;
            walletChanged = onWalletChanged;
            if (!bound)
            {
                bound = true;
                redeem.onClick.AddListener(Submit);
                input.onSubmit.AddListener(_ => Submit());
                input.onValueChanged.AddListener(_ => { if (!ShowingGift) status.text = ""; });
                cancel.onClick.AddListener(Cancel);
                continueButton.onClick.AddListener(() => HandleBack());
            }
            Cancel();
        }

        public void Open()
        {
            if (shop == null || IsShowing) return;
            input.SetTextWithoutNotify("");
            status.text = "";
            overlay.SetActive(true);
            overlay.transform.SetAsLastSibling();
            ShowForm();
            input.Select();
            input.ActivateInputField();
        }

        private void Submit()
        {
            if (!IsShowing || ShowingGift || shop == null) return;
            RunnerShopProgress.RedeemResult result = shop.Redeem(input.text);
            if (result != RunnerShopProgress.RedeemResult.Success)
            {
                status.color = new Color32(246, 165, 135, 255);
                status.text = result == RunnerShopProgress.RedeemResult.WalletFull ? "YOUR EGG WALLET IS FULL" : "CODE NOT RECOGNIZED. TRY AGAIN.";
                return;
            }
            // Commit first, then lock the form so click/Enter events cannot duplicate one submission.
            input.DeactivateInputField();
            EventSystem.current?.SetSelectedGameObject(null);
            form.SetActive(false);
            celebration.SetActive(true);
            elapsed = 0;
            DisplayedEggs = 0;
            walletChanged?.Invoke();
            audioCues?.Milestone();
            ApplyLayout(portrait, available);
            RenderGift();
        }

        private void ShowForm()
        {
            celebration.SetActive(false);
            form.SetActive(true);
            status.text = "";
            EventSystem.current?.SetSelectedGameObject(null);
            ApplyLayout(portrait, available);
        }

        public bool HandleBack()
        {
            if (!IsShowing) return false;
            if (ShowingGift && elapsed < Duration) { elapsed = Duration; RenderGift(); }
            else if (ShowingGift) ShowForm();
            else Cancel();
            return true;
        }

        public void Cancel()
        {
            if (input != null) input.DeactivateInputField();
            if (IsShowing) EventSystem.current?.SetSelectedGameObject(null);
            if (overlay != null) overlay.SetActive(false);
            elapsed = 0;
        }

        private void OnDisable() => Cancel();
        private void Update() { if (ShowingGift) Advance(Time.unscaledDeltaTime); }
        private void Advance(float dt) { elapsed += Mathf.Max(0, dt); RenderGift(); }

        private void RenderGift()
        {
            if (!ShowingGift) return;
            float reveal = Mathf.Clamp01((elapsed - 0.25f) / (Duration - 0.25f));
            float ease = 1 - Mathf.Pow(1 - reveal, 3);
            DisplayedEggs = Mathf.RoundToInt(RunnerShopProgress.GiftEggs * ease);
            amount.text = "+" + DisplayedEggs.ToString("N0") + " EGGS";
            amount.rectTransform.localScale = Vector3.one * (1 + Mathf.Sin(reveal * Mathf.PI) * 0.08f);
            balance.text = "EGG BALANCE: " + shop.Balance.ToString("N0");
            float opening = Mathf.Clamp01(elapsed / 0.65f);
            gift.Animate(opening);
            gift.rectTransform.localScale = Vector3.one * (1 + Mathf.Sin(opening * Mathf.PI) * 0.12f);
            gift.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(elapsed * 22) * (1 - opening) * 5);
            glow.Animate(elapsed, 0.85f + Mathf.Sin(opening * Mathf.PI) * 0.4f);
            continueLabel.text = elapsed >= Duration ? "CONTINUE" : "SKIP";
        }

        public void ApplyLayout(bool isPortrait, Vector2 space)
        {
            portrait = isPortrait;
            available = space;
            if (panel == null) return;
            Position(overlay.transform, 0, 0, space.x, space.y);
            bool showingGift = celebration.activeSelf;
            Vector2 size = showingGift ? (portrait ? new Vector2(640, 760) : new Vector2(560, 520)) :
                portrait ? new Vector2(600, 640) : new Vector2(520, 400);
            Position(panel, 0, 0, size.x, size.y);
            panel.localScale = Vector3.one * Mathf.Min(1, (space.x - 48) / size.x, (space.y - 48) / size.y);
            heading.text = showingGift ? "A GIFT FOR YOU!" : "REDEEM A GIFT";
            Place(heading, 0, showingGift ? (portrait ? 300 : 202) : portrait ? 252 : 144, size.x - 64, 44, portrait ? 24 : 22);
            Place(hint, 0, portrait ? 160 : 88, size.x - 64, 32, portrait ? 16 : 12);
            Position(input.transform, 0, portrait ? 74 : 28, portrait ? 512 : 432, portrait ? 100 : 64);
            Place(input.textComponent, 0, 0, portrait ? 468 : 396, portrait ? 72 : 44, portrait ? 20 : 18);
            var placeholder = (Text)input.placeholder;
            Place(placeholder, 0, 0, portrait ? 468 : 396, portrait ? 72 : 44, portrait ? 20 : 18);
            // Labels are geometry-centered elsewhere, but editable text must use the same
            // left-aligned font metrics as Unity's caret and selection calculations.
            input.textComponent.alignment = placeholder.alignment = TextAnchor.MiddleLeft;
            input.textComponent.alignByGeometry = placeholder.alignByGeometry = false;
            input.textComponent.lineSpacing = placeholder.lineSpacing = 1f;
            input.caretWidth = 2;
            input.ForceLabelUpdate();
            Place(status, 0, -36, size.x - 64, portrait ? 64 : 44, portrait ? 12 : 10);
            Control(redeem, portrait ? -136 : -112, portrait ? -204 : -124, portrait ? 248 : 208, portrait ? 100 : 56);
            Control(cancel, portrait ? 136 : 112, portrait ? -204 : -124, portrait ? 248 : 208, portrait ? 100 : 56);
            Position(glow.transform, 0, portrait ? 142 : 70, portrait ? 360 : 280, portrait ? 360 : 280);
            Position(gift.transform, 0, portrait ? 142 : 70, portrait ? 196 : 160, portrait ? 196 : 160);
            Place(sponsor, 0, portrait ? 12 : -48, size.x - 64, 72, portrait ? 18 : 14);
            Place(amount, 0, portrait ? -92 : -120, size.x - 80, 56, portrait ? 34 : 30);
            Place(balance, 0, portrait ? -160 : -170, size.x - 64, 30, portrait ? 14 : 12);
            Control(continueButton, 0, portrait ? -266 : -216, portrait ? 544 : 440, portrait ? 100 : 56);
            RenderGift();
        }

        private static void Place(Text label, float x, float y, float width, float height, int size)
        {
            Position(label.transform, x, y, width, height);
            label.fontSize = size;
        }

        private static void Control(Button button, float x, float y, float width, float height)
        {
            Position(button.transform, x, y, width, height);
            Text label = button.GetComponentInChildren<Text>(true);
            Place(label, 0, 0, width - 24, height - 16, height >= 100 ? 18 : 16);
            button.GetComponent<RunnerUiButtonFeedback>()?.RefreshContentLayout();
        }

        private static Button Button(string name, Transform parent, string caption, Font font, Sprite sprite)
        {
            Transform node = Node(name, parent);
            Image art = Art(node, sprite, Color.white, true);
            art.raycastTarget = true;
            Button button = node.GetComponent<Button>();
            if (button == null) button = node.gameObject.AddComponent<Button>();
            button.targetGraphic = art;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            Label("Label", node, caption, font, 16, 0, 0, 184, 40, Cream);
            return button;
        }

        public void EnsureUi(Transform menuRoot, Font font, Sprite panelSprite, Sprite buttonSprite)
        {
            Transform root = Node("Redeem dialog", menuRoot);
            overlay = root.gameObject;
            var group = root.GetComponent<CanvasGroup>();
            if (group == null) group = root.gameObject.AddComponent<CanvasGroup>();
            group.interactable = group.blocksRaycasts = true;
            Transform scrim = Node("Redeem scrim", root);
            Stretch(scrim);
            Art(scrim, null, new Color(0.015f, 0.04f, 0.045f, 0.95f), false).raycastTarget = true;
            panel = (RectTransform)Node("Redeem panel", root);
            Art(panel, panelSprite, Color.white, true);
            heading = Label("Redeem heading", panel, "REDEEM A GIFT", font, 22, 0, 144, 456, 44, Gold);
            form = Node("Code entry", panel).gameObject;
            Stretch(form.transform);
            hint = Label("Code hint", form.transform, "ENTER YOUR GIFT CODE", font, 12, 0, 88, 456, 32, Cream);
            Transform field = Node("Gift code input", form.transform);
            Image fieldArt = Art(field, panelSprite, new Color(0.6f, 0.78f, 0.65f), true);
            fieldArt.raycastTarget = true;
            input = field.GetComponent<InputField>();
            if (input == null) input = field.gameObject.AddComponent<InputField>();
            input.targetGraphic = fieldArt;
            input.textComponent = Label("Text", field, "", font, 18, 0, 0, 396, 44, Cream);
            input.placeholder = Label("Placeholder", field, "GIFT CODE", font, 16, 0, 0, 396, 44, new Color32(174, 203, 152, 255));
            input.contentType = InputField.ContentType.Standard;
            input.lineType = InputField.LineType.SingleLine;
            input.characterLimit = 64;
            input.keyboardType = TouchScreenKeyboardType.ASCIICapable;
            input.shouldHideMobileInput = false;
            input.customCaretColor = true;
            input.caretColor = Gold;
            input.selectionColor = new Color(1, 0.84f, 0.4f, 0.35f);
            input.navigation = new Navigation { mode = Navigation.Mode.None };
            input.textComponent.supportRichText = false;
            status = Label("Code status", form.transform, "", font, 10, 0, -36, 456, 44, Cream);
            redeem = Button("Submit gift code", form.transform, "REDEEM", font, buttonSprite);
            cancel = Button("Cancel gift code", form.transform, "BACK", font, buttonSprite);
            celebration = Node("Gift celebration", panel).gameObject;
            Stretch(celebration.transform);
            glow = Glow("Gift rays and sparkles", celebration.transform, 0, 70, 280);
            Transform giftArt = Node("Opening present", celebration.transform);
            gift = giftArt.GetComponent<RunnerGiftGraphic>();
            if (gift == null) gift = giftArt.gameObject.AddComponent<RunnerGiftGraphic>();
            gift.raycastTarget = false;
            sponsor = Label("Gift from production", celebration.transform, "Gift from\nBibek Giri Productions", font, 14, 0, -48, 496, 72, Cream);
            amount = Label("Gift egg amount", celebration.transform, "+0 EGGS", font, 30, 0, -120, 480, 56, Gold);
            balance = Label("Gift wallet balance", celebration.transform, "EGG BALANCE: 0", font, 12, 0, -170, 496, 30, Cream);
            continueButton = Button("Gift continue", celebration.transform, "SKIP", font, buttonSprite);
            continueLabel = continueButton.GetComponentInChildren<Text>(true);
            celebration.SetActive(false);
            form.SetActive(true);
            ApplyLayout(false, new Vector2(1280, 720));
            overlay.SetActive(false);
        }
    }
}
