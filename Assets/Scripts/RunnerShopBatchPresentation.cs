using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static DinoRush.RunnerShopPresentation;

namespace DinoRush
{
    /// <summary>Presentation-only ten-pull: orbit, charge, staggered hatches, then a persistent result grid.</summary>
    public sealed class RunnerShopBatchPresentation : MonoBehaviour
    {
        public const float GatherTime = 1.25f;
        public const float RevealTime = 2.1f;
        public const float RevealSpacing = 0.24f;
        public const float Duration = RevealTime + 9 * RevealSpacing + 0.65f;
        private static readonly Color Gold = new Color32(255, 215, 120, 255);
        private static readonly Color Cream = new Color32(240, 242, 207, 255);
        private static readonly Color Sage = new Color32(174, 203, 152, 255);

        [Serializable]
        private sealed class Card
        {
            public RectTransform root;
            public Image frame, egg, portrait;
            public RunnerHatchGlowGraphic glow;
            public CanvasGroup details;
            public Text name, amount, progress, badge;
        }

        [SerializeField] private GameObject overlay;
        [SerializeField] private RectTransform panel;
        [SerializeField] private CanvasGroup fade;
        [SerializeField] private Text heading, subtitle, summary, continueLabel;
        [SerializeField] private Button continueButton;
        [SerializeField] private Image flash;
        [SerializeField] private RunnerHatchGlowGraphic summonGlow;
        [SerializeField] private Card[] cards = new Card[0];
        private RunnerShopPresentation owner;
        private RunnerAudio audioCues;
        private Action closed;
        private RunnerDinoRoster roster;
        private RunnerShopProgress.Reward[] rewards;
        private readonly Vector2[] slots = new Vector2[RunnerShopProgress.TenPullCount];
        private bool bound, charged, portraitLayout;
        private float elapsed;
        private int lastRevealed;
        public bool IsShowing => overlay != null && overlay.activeSelf;
        public bool Complete => IsShowing && elapsed >= Duration;
        public int RevealedCount => !IsShowing || elapsed < RevealTime ? 0 :
            Mathf.Clamp(Mathf.FloorToInt((elapsed - RevealTime) / RevealSpacing) + 1, 0, cards.Length);

        public void Initialize(RunnerShopPresentation presentation, RunnerAudio cues, Action onClosed)
        {
            owner = presentation;
            audioCues = cues;
            closed = onClosed;
            if (!bound && continueButton != null)
            {
                bound = true;
                continueButton.onClick.AddListener(() =>
                {
                    EventSystem.current?.SetSelectedGameObject(null);
                    HandleBack();
                });
            }
            Cancel();
        }

        public void Play(RunnerDinoRoster dinos, RunnerShopProgress.Reward[] results)
        {
            if (IsShowing || owner == null || owner.IsShowing || results == null || results.Length != cards.Length) return;
            roster = dinos;
            rewards = (RunnerShopProgress.Reward[])results.Clone();
            elapsed = 0;
            lastRevealed = 0;
            charged = false;
            audioCues?.EggPull();
            for (int i = 0; i < cards.Length; i++)
            {
                var reward = rewards[i];
                var dino = roster[reward.DinoIndex];
                cards[i].portrait.sprite = dino.Portrait;
                cards[i].name.text = dino.DisplayName.ToUpperInvariant();
                cards[i].amount.text = "+" + reward.Fragments + " FRAGS";
                cards[i].progress.text = Mathf.Min(reward.Total, RunnerShopProgress.RequiredFragments) + " / 100";
                cards[i].badge.text = reward.NewlyUnlocked ? "NEW DINO!" : "FRAGMENTS";
                cards[i].badge.color = reward.NewlyUnlocked ? Gold : Sage;
                cards[i].frame.color = reward.NewlyUnlocked ? new Color(1, 0.92f, 0.65f) : Color.white;
            }
            overlay.SetActive(true);
            overlay.transform.SetAsLastSibling();
            Render();
        }

        public bool HandleBack()
        {
            if (!IsShowing) return false;
            if (!Complete)
            {
                audioCues?.StopEggSounds();
                PlayRevealSound(cards.Length);
                elapsed = Duration;
                charged = true;
                lastRevealed = cards.Length;
                Render();
                return true;
            }
            Cancel();
            closed?.Invoke();
            return true;
        }

        public void Cancel()
        {
            audioCues?.StopEggSounds();
            if (overlay != null) overlay.SetActive(false);
            rewards = null;
            elapsed = 0;
        }

        private void OnDisable() => Cancel();
        private void Update() { if (IsShowing) Advance(Time.unscaledDeltaTime); }

        private void Advance(float dt)
        {
            if (!IsShowing) return;
            elapsed += Mathf.Max(0, dt);
            int count = RevealedCount;
            if (!charged && elapsed >= GatherTime && count == 0) audioCues?.EggCrack();
            charged |= elapsed >= GatherTime;
            if (count > lastRevealed)
            {
                // At most one cue per frame, including when a slow frame crosses several reveals.
                PlayRevealSound(count);
                lastRevealed = count;
            }
            Render();
        }

        private void PlayRevealSound(int count)
        {
            if (count <= lastRevealed || rewards == null) return;
            bool unlocked = false;
            for (int i = lastRevealed; i < count; i++) unlocked |= rewards[i].NewlyUnlocked;
            audioCues?.EggHatch(unlocked, count - 1);
        }

        public void RefreshLayout() { if (IsShowing) Render(); }

        private void Render()
        {
            if (rewards == null) return;
            fade.alpha = Mathf.Clamp01(elapsed / 0.2f);
            int count = RevealedCount;
            heading.text = Complete ? "YOUR TEN-PULL RESULTS" : elapsed < GatherTime ? "TEN LITTLE MYSTERIES..." : "LET'S SEE WHAT HATCHES!";
            subtitle.text = count == 0 ? "10 EGGS / 10 SURPRISES" : count + " / 10 REVEALED";
            continueLabel.text = Complete ? "CONTINUE" : "REVEAL ALL";
            summonGlow.gameObject.SetActive(!Complete);
            summonGlow.Animate(elapsed, elapsed < GatherTime ? 0.75f : 1.15f);
            float light = Mathf.Clamp01((elapsed - RevealTime) / 0.25f);
            flash.color = new Color(1, 0.87f, 0.52f, elapsed >= RevealTime && light < 1 ? Mathf.Sin(light * Mathf.PI) * 0.14f : 0);
            int total = 0, unlocks = 0;
            for (int i = 0; i < cards.Length; i++)
            {
                Card card = cards[i];
                var reward = rewards[i];
                total += reward.Fragments;
                if (reward.NewlyUnlocked) unlocks++;
                float revealAt = RevealTime + i * RevealSpacing;
                bool revealed = elapsed >= revealAt;
                float age = Mathf.Clamp01((elapsed - revealAt) / 0.5f);
                float gather = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / GatherTime));
                float angle = i * Mathf.PI * 2 / cards.Length + elapsed * 1.6f;
                Vector2 orbit = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (portraitLayout ? 130 : 140);
                card.root.anchoredPosition = Vector2.Lerp(orbit, slots[i], gather);
                Color frameColor = card.frame.color;
                frameColor.a = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.8f, 1, gather));
                card.frame.color = frameColor;
                float flip = revealed ? Mathf.Clamp01(age / 0.22f) : 1;
                card.root.localScale = new Vector3(revealed ? Mathf.Lerp(0.12f, 1, flip) : Mathf.Lerp(0.55f, 1, gather),
                    revealed ? 1 + Mathf.Sin(age * Mathf.PI) * 0.08f : Mathf.Lerp(0.55f, 1, gather), 1);
                card.egg.enabled = !revealed;
                card.portrait.enabled = revealed;
                card.details.alpha = revealed ? Mathf.Clamp01(age * 6) : 0;
                card.glow.gameObject.SetActive(revealed);
                if (revealed) card.glow.Animate(elapsed + i, reward.NewlyUnlocked ? 1.3f : 0.55f);
                else
                {
                    RunnerEggHatchCatalog.Egg art = owner.Catalog != null ? owner.Catalog.Find(roster[reward.DinoIndex].Id) : null;
                    bool cracking = elapsed >= GatherTime;
                    bool hatching = elapsed >= revealAt - 0.24f;
                    Texture2D sheet = art == null ? null : hatching ? art.hatch : cracking ? art.crack : art.move;
                    Sprite[] strip = owner.Frames(sheet);
                    float localTime = hatching ? elapsed - (revealAt - 0.24f) : cracking ? elapsed - GatherTime : elapsed;
                    int frame = strip.Length == 0 ? 0 : cracking || hatching ? Mathf.Min(strip.Length - 1, (int)(localTime * 12)) : (int)(localTime * 6 + i) % strip.Length;
                    card.egg.sprite = strip.Length > 0 ? strip[frame] : roster[reward.DinoIndex].Portrait;
                    card.egg.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(elapsed * (cracking ? 42 : 16) + i) * (cracking ? 6 : 4));
                }
            }
            summary.text = Complete ? "+" + total + " FRAGMENTS / " + unlocks + " NEW DINOS" : "REWARDS SAVED / DUPLICATES STACK";
        }

        public void ApplyLayout(bool portrait, Vector2 available)
        {
            if (panel == null) return;
            portraitLayout = portrait;
            Position(overlay.transform, 0, 0, available.x, available.y);
            Vector2 size = portrait ? new Vector2(640, 1240) : new Vector2(1040, 680);
            Position(panel, 0, 0, size.x, size.y);
            panel.localScale = Vector3.one * Mathf.Min(1, (available.x - 32) / size.x, (available.y - 32) / size.y);
            PlaceText(heading, 0, portrait ? 552 : 286, size.x - 64, 48, portrait ? 22 : 23);
            PlaceText(subtitle, 0, portrait ? 496 : 238, size.x - 64, 32, 14);
            Position(summonGlow.transform, 0, portrait ? 40 : 20, portrait ? 540 : 500, portrait ? 540 : 500);
            for (int i = 0; i < cards.Length; i++)
            {
                Card card = cards[i];
                slots[i] = portrait ? new Vector2(i % 2 == 0 ? -144 : 144, 360 - i / 2 * 174) :
                    new Vector2(-392 + i % 5 * 196, i < 5 ? 118 : -100);
                Position(card.root, slots[i].x, slots[i].y, portrait ? 264 : 184, portrait ? 164 : 200);
                Position(card.egg.transform, 0, 0, 100, 100);
                Position(card.portrait.transform, portrait ? -76 : 0, portrait ? 12 : 28, 80, 80);
                Position(card.glow.transform, portrait ? -76 : 0, portrait ? 12 : 28, 126, 126);
                PlaceText(card.badge, 0, portrait ? 60 : 78, portrait ? 232 : 164, 24, portrait ? 12 : 10);
                PlaceText(card.name, portrait ? 42 : 0, portrait ? 20 : -26, portrait ? 156 : 168, 26, portrait ? 14 : 12);
                PlaceText(card.amount, portrait ? 42 : 0, portrait ? -12 : -54, portrait ? 156 : 168, 26, portrait ? 12 : 11);
                PlaceText(card.progress, 0, portrait ? -58 : -80, portrait ? 232 : 164, 24, 10);
            }
            PlaceText(summary, 0, portrait ? -462 : -230, size.x - 64, 40, portrait ? 12 : 14);
            Position(continueButton.transform, 0, portrait ? -550 : -292, portrait ? 560 : 440, portrait ? 100 : 64);
            PlaceText(continueLabel, 0, 0, portrait ? 520 : 408, portrait ? 76 : 44, 18);
            continueButton.GetComponent<RunnerUiButtonFeedback>()?.RefreshContentLayout();
            RefreshLayout();
        }

        private static void PlaceText(Text text, float x, float y, float width, float height, int size)
        {
            Position(text.transform, x, y, width, height);
            text.fontSize = size;
        }

        public void EnsureUi(Transform menuRoot, Font font, Sprite panelSprite, Sprite buttonSprite)
        {
            Transform root = Node("Ten egg reveal", menuRoot);
            overlay = root.gameObject;
            fade = root.GetComponent<CanvasGroup>();
            if (fade == null) fade = root.gameObject.AddComponent<CanvasGroup>();
            fade.interactable = fade.blocksRaycasts = true;
            Transform scrim = Node("Ten pull scrim", root);
            Stretch(scrim);
            Art(scrim, null, new Color(0.015f, 0.04f, 0.045f, 0.96f), false).raycastTarget = true;
            panel = (RectTransform)Node("Ten pull panel", root);
            Art(panel, panelSprite, Color.white, true);
            summonGlow = Glow("Ten pull summoning light", panel, 0, 0, 500);
            heading = Label("Ten pull heading", panel, "TEN LITTLE MYSTERIES...", font, 23, 0, 286, 960, 48, Gold);
            subtitle = Label("Ten pull subtitle", panel, "10 EGGS / 10 SURPRISES", font, 14, 0, 238, 960, 32, Cream);
            cards = new Card[RunnerShopProgress.TenPullCount];
            for (int i = 0; i < cards.Length; i++)
            {
                var card = new Card { root = (RectTransform)Node("Ten pull reward " + (i + 1), panel) };
                card.frame = Art(card.root, panelSprite, Color.white, true);
                card.glow = Glow("Reward light", card.root, 0, 28, 126);
                card.egg = Picture("Sealed egg", card.root, 0, 0, 100);
                card.portrait = Picture("Reward dino", card.root, 0, 28, 80);
                Transform details = Node("Reward labels", card.root);
                Stretch(details);
                card.details = details.GetComponent<CanvasGroup>();
                if (card.details == null) card.details = details.gameObject.AddComponent<CanvasGroup>();
                card.details.blocksRaycasts = false;
                card.badge = Label("Reward badge", details, "FRAGMENTS", font, 10, 0, 78, 164, 24, Sage);
                card.name = Label("Reward name", details, "DINO", font, 12, 0, -26, 168, 26, Cream);
                card.amount = Label("Reward fragments", details, "+0 FRAGS", font, 11, 0, -54, 168, 26, Gold);
                card.progress = Label("Reward progress", details, "0 / 100", font, 10, 0, -80, 164, 24, Sage);
                cards[i] = card;
            }
            summary = Label("Ten pull summary", panel, "REWARDS SAVED / DUPLICATES STACK", font, 14, 0, -230, 960, 40, Gold);
            Transform button = Node("Ten pull continue", panel);
            Image buttonArt = Art(button, buttonSprite, Color.white, true);
            buttonArt.raycastTarget = true;
            continueButton = button.GetComponent<Button>();
            if (continueButton == null) continueButton = button.gameObject.AddComponent<Button>();
            continueButton.targetGraphic = buttonArt;
            continueButton.navigation = new Navigation { mode = Navigation.Mode.None };
            continueLabel = Label("Label", button, "REVEAL ALL", font, 18, 0, 0, 408, 44, Cream);
            Transform light = Node("Ten pull flash", root);
            Stretch(light);
            flash = Art(light, null, Color.clear, false);
            ApplyLayout(false, new Vector2(1280, 720));
            overlay.SetActive(false);
        }
    }
}
