using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DinoRush
{
    /// <summary>Unscaled, presentation-only hatch sequence. Purchases are saved before this starts.</summary>
    public sealed class RunnerShopPresentation : MonoBehaviour
    {
        public enum HatchStage { Idle, Wobble, Crack, Hatch, Reward }
        public const float CrackTime = 1.1f;
        public const float HatchTime = 1.8f;
        public const float RewardTime = 2.45f;
        public const float Duration = 3.3f;
        private static readonly Color Gold = new Color32(255, 215, 120, 255);
        private static readonly Color Cream = new Color32(240, 242, 207, 255);

        [SerializeField] private RunnerEggHatchCatalog catalog;
        [SerializeField] private Image productEgg;
        [SerializeField] private Image[] companionEggs;
        [SerializeField] private RunnerHatchGlowGraphic productGlow;
        [SerializeField] private GameObject revealRoot;
        [SerializeField] private CanvasGroup revealFade;
        [SerializeField] private Image revealEgg;
        [SerializeField] private Image revealPortrait;
        [SerializeField] private Image flash;
        [SerializeField] private RunnerHatchGlowGraphic revealGlow;
        [SerializeField] private Text heading;
        [SerializeField] private Text amount;
        [SerializeField] private Text species;
        [SerializeField] private Text progress;
        [SerializeField] private Image progressFill;
        [SerializeField] private Text unlockNote;
        [SerializeField] private CanvasGroup rewardDetails;
        [SerializeField] private Button continueButton;
        [SerializeField] private Text continueLabel;
        private readonly Dictionary<Texture2D, Sprite[]> frames = new Dictionary<Texture2D, Sprite[]>();
        private RunnerDinoRoster.Dino recipient;
        private RunnerEggHatchCatalog.Egg egg;
        private RunnerAudio audioCues;
        private Action closed;
        private int reward, total;
        private bool unlocked, bound, crackPlayed, hatchPlayed;
        private float elapsed, idleTime, lastTick;

        public RunnerEggHatchCatalog Catalog => catalog;
        public bool IsShowing => revealRoot != null && revealRoot.activeSelf;
        public int DisplayedFragments { get; private set; }
        public HatchStage Stage => !IsShowing ? HatchStage.Idle : elapsed < CrackTime ? HatchStage.Wobble :
            elapsed < HatchTime ? HatchStage.Crack : elapsed < RewardTime ? HatchStage.Hatch : HatchStage.Reward;

        public void SetCatalog(RunnerEggHatchCatalog value) => catalog = value;
        public void RefreshLayout() { if (IsShowing) RenderReveal(); }
        public void SetPreview(Sprite[] sprites)
        {
            if (sprites.Length == 0) return;
            productEgg.sprite = sprites[0];
            for (int i = 0; i < companionEggs.Length; i++) companionEggs[i].sprite = sprites[(i + 1) % sprites.Length];
        }

        public void Initialize(RunnerAudio cues, Action onClosed)
        {
            audioCues = cues;
            closed = onClosed;
            if (!bound && continueButton != null)
            {
                bound = true;
                continueButton.onClick.AddListener(() =>
                {
                    UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
                    HandleBack();
                });
            }
            if (catalog != null)
                for (int i = 0; i < catalog.Count; i++)
                {
                    Frames(catalog[i].move);
                    Frames(catalog[i].crack);
                    Frames(catalog[i].hatch);
                }
            CancelReveal();
        }

        public void Play(RunnerDinoRoster.Dino dino, int fragments, int fragmentTotal, bool newlyUnlocked)
        {
            if (IsShowing) return;
            recipient = dino;
            egg = catalog != null ? catalog.Find(dino.Id) : null;
            reward = fragments;
            total = fragmentTotal;
            unlocked = newlyUnlocked;
            elapsed = 0;
            lastTick = -1;
            DisplayedFragments = 0;
            crackPlayed = hatchPlayed = false;
            revealRoot.SetActive(true);
            revealRoot.transform.SetAsLastSibling();
            revealPortrait.sprite = dino.Portrait;
            revealGlow.color = Color.Lerp(Gold, egg != null ? egg.accent : Gold, 0.2f);
            RenderReveal();
        }

        public bool HandleBack()
        {
            if (!IsShowing) return false;
            if (elapsed < Duration) { elapsed = Duration; Advance(0); return true; }
            CancelReveal();
            closed?.Invoke();
            return true;
        }

        public void CancelReveal()
        {
            if (revealRoot != null) revealRoot.SetActive(false);
            if (revealFade != null) revealFade.alpha = 1;
            elapsed = 0;
        }

        private void OnDisable() => CancelReveal();

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            idleTime += dt;
            if (catalog != null && catalog.Count > 0 && productEgg != null)
            {
                int index = (int)(idleTime / 3f) % catalog.Count;
                Sprite[] move = Frames(catalog[index].move);
                if (move.Length > 0) productEgg.sprite = move[(int)(idleTime * 5) % move.Length];
                productEgg.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(idleTime * 2) * 0.035f);
                productEgg.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(idleTime * 1.6f) * 3f);
                for (int i = 0; i < companionEggs.Length; i++)
                {
                    Sprite[] side = Frames(catalog[(index + i + 1) % catalog.Count].move);
                    if (side.Length > 0) companionEggs[i].sprite = side[0];
                }
                productGlow.Animate(idleTime, 0.85f);
            }
            if (IsShowing) Advance(dt);
        }

        private void Advance(float dt)
        {
            if (!IsShowing) return;
            elapsed += Mathf.Max(0, dt);
            if (elapsed >= CrackTime && !crackPlayed) { crackPlayed = true; audioCues?.EggCrack(); }
            if (elapsed >= RewardTime && !hatchPlayed) { hatchPlayed = true; audioCues?.EggHatch(); }
            RenderReveal();
        }

        private void RenderReveal()
        {
            if (revealFade != null) revealFade.alpha = Mathf.Clamp01(elapsed / 0.18f);
            bool revealed = elapsed >= RewardTime;
            revealEgg.enabled = !revealed;
            revealPortrait.enabled = revealed;
            float reveal = Mathf.Clamp01((elapsed - RewardTime) / (Duration - RewardTime));
            float ease = 1f - Mathf.Pow(1f - reveal, 3);
            rewardDetails.alpha = Mathf.Clamp01(reveal * 6);
            heading.text = revealed ? (unlocked ? "NEW DINO UNLOCKED!" : "LOOK WHAT HATCHED!") :
                elapsed >= HatchTime ? "HERE IT COMES..." : elapsed >= CrackTime ? "IT'S CRACKING..." : "A LITTLE MYSTERY...";
            heading.color = revealed ? Gold : Cream;
            continueLabel.text = elapsed >= Duration ? "CONTINUE" : "SKIP REVEAL";
            revealGlow.Animate(elapsed, revealed ? 1.5f - reveal * 0.45f : 0.65f + elapsed * 0.25f);
            float burst = Mathf.Clamp01((elapsed - HatchTime) / 0.3f);
            Color flashColor = Gold; flashColor.a = elapsed >= HatchTime && elapsed < RewardTime ? Mathf.Sin(burst * Mathf.PI) * 0.12f : 0;
            flash.color = flashColor;
            if (!revealed)
            {
                Texture2D sheet = egg == null ? null : elapsed < CrackTime ? egg.move : elapsed < HatchTime ? egg.crack : egg.hatch;
                Sprite[] strip = Frames(sheet);
                float localTime = elapsed < CrackTime ? elapsed : elapsed < HatchTime ? elapsed - CrackTime : elapsed - HatchTime;
                int frame = elapsed < CrackTime ? (int)(localTime * 8) % Mathf.Max(1, strip.Length) :
                    Mathf.Min(strip.Length - 1, (int)(localTime * strip.Length / (elapsed < HatchTime ? HatchTime - CrackTime : RewardTime - HatchTime)));
                revealEgg.sprite = strip.Length > 0 ? strip[frame] : recipient.Portrait;
                float shake = elapsed < CrackTime ? Mathf.Sin(elapsed * 22) * (2 + elapsed * 3) : elapsed < HatchTime ? Mathf.Sin(elapsed * 60) * 4 : 0;
                revealEgg.rectTransform.localRotation = Quaternion.Euler(0, 0, shake);
                revealEgg.rectTransform.localScale = Vector3.one * (1 + Mathf.Sin(elapsed * 14) * 0.025f);
                return;
            }
            float pop = Mathf.Sin(Mathf.Clamp01(reveal * 2) * Mathf.PI) * 0.16f;
            revealPortrait.rectTransform.localScale = Vector3.one * (1 + pop);
            int count = Mathf.RoundToInt(reward * ease);
            if (count != DisplayedFragments && elapsed - lastTick > 0.065f && elapsed < Duration)
            {
                lastTick = elapsed;
                audioCues?.Pickup();
            }
            DisplayedFragments = count;
            amount.text = "+" + count;
            amount.rectTransform.localScale = Vector3.one * (1 + Mathf.Sin(reveal * Mathf.PI) * 0.12f);
            species.text = recipient.DisplayName.ToUpperInvariant() + " DINO FRAGMENTS";
            int credited = Mathf.Clamp(total - reward + count, 0, RunnerShopProgress.RequiredFragments);
            progress.text = credited + " / " + RunnerShopProgress.RequiredFragments + " FRAGMENTS";
            progressFill.rectTransform.anchorMax = new Vector2((float)credited / RunnerShopProgress.RequiredFragments, 1);
            unlockNote.text = unlocked ? "YOURS TO RUN WITH. SELECT IT IN THE SHOP!" : "A LITTLE CLOSER TO YOUR NEXT DINO";
            unlockNote.color = unlocked ? Gold : Cream;
        }

        private Sprite[] Frames(Texture2D sheet)
        {
            if (sheet == null) return Array.Empty<Sprite>();
            if (frames.TryGetValue(sheet, out Sprite[] cached)) return cached;
            sheet.filterMode = FilterMode.Point; // Runtime only: source import settings and slices stay untouched.
            int size = RunnerEggHatchCatalog.CellSize;
            var sprites = new Sprite[sheet.width / size];
            for (int i = 0; i < sprites.Length; i++)
            {
                sprites[i] = Sprite.Create(sheet, new Rect(i * size, 0, size, size), Vector2.one * 0.5f, size, 0, SpriteMeshType.FullRect);
                sprites[i].name = sheet.name + " hatch presentation frame " + i;
            }
            frames.Add(sheet, sprites);
            return sprites;
        }

        private void OnDestroy()
        {
            foreach (Sprite[] strip in frames.Values) foreach (Sprite sprite in strip) Destroy(sprite);
            frames.Clear();
        }

        // Idempotent authoring, also used to upgrade an older saved menu at runtime.
        public void EnsureUi(Transform menuRoot, Font font, Sprite panelSprite, Sprite buttonSprite)
        {
            Transform display = Node("Mystery egg display", transform);
            Position(display, 0, 140, 620, 140);
            Art(display, panelSprite, new Color(0.72f, 0.88f, 0.82f, 1), true);
            productGlow = Glow("Egg display glow", display, -214, 0, 152);
            companionEggs = new[] { Picture("Companion egg left", display, -260, -10, 72), Picture("Companion egg right", display, -168, -10, 72) };
            foreach (Image side in companionEggs) side.color = new Color(1, 1, 1, 0.48f);
            productEgg = Picture("Showcase egg", display, -214, 0, 120);
            Label("Egg product title", display, "MYSTERY EGG", font, 18, 76, 42, 386, 30, Gold);
            Label("Egg product description", display, "1-20 RANDOM FRAGMENTS\nA NEW DINO AWAITS.", font, 11, 76, 0, 386, 44, Cream);
            Label("Egg unlock requirement", display, "100 FRAGMENTS TO UNLOCK", font, 10, 76, -44, 386, 24, Cream);

            Transform overlay = Node("Hatch reveal", menuRoot);
            revealRoot = overlay.gameObject;
            Position(overlay, 0, 0, 1280, 720);
            revealFade = overlay.GetComponent<CanvasGroup>();
            if (revealFade == null) revealFade = overlay.gameObject.AddComponent<CanvasGroup>();
            revealFade.alpha = 1;
            revealFade.interactable = revealFade.blocksRaycasts = true;
            Transform scrim = Node("Hatch scrim", overlay);
            Stretch(scrim);
            Art(scrim, null, new Color(0.015f, 0.04f, 0.045f, 0.94f), false).raycastTarget = true;
            Transform panel = Node("Hatch reward panel", overlay);
            Position(panel, 0, 0, 620, 680);
            Art(panel, panelSprite, Color.white, true);
            Label("Hatch eyebrow", panel, "M Y S T E R Y   E G G", font, 12, 0, 274, 560, 26, Gold);
            heading = Label("Hatch heading", panel, "A LITTLE MYSTERY...", font, 21, 0, 220, 568, 48, Cream);
            revealGlow = Glow("Hatch light and sparkles", panel, 0, 64, 360);
            revealEgg = Picture("Hatching egg", panel, 0, 70, 240);
            revealPortrait = Picture("Hatched dino", panel, 0, 70, 184);
            Transform details = Node("Hatch reward details", panel);
            Position(details, 0, 0, 560, 440);
            rewardDetails = details.GetComponent<CanvasGroup>();
            if (rewardDetails == null) rewardDetails = details.gameObject.AddComponent<CanvasGroup>();
            rewardDetails.blocksRaycasts = false;
            amount = Label("Fragment reward amount", details, "+0", font, 48, 0, -68, 500, 70, Gold);
            species = Label("Fragment reward species", details, "DINO FRAGMENTS", font, 14, 0, -124, 540, 30, Cream);
            progress = Label("Fragment reward progress", details, "0 / 100 FRAGMENTS", font, 12, 0, -166, 500, 28, Cream);
            Transform track = Node("Fragment progress track", details);
            Position(track, 0, -192, 420, 8);
            Art(track, null, new Color32(16, 35, 33, 255), false);
            Transform fill = Node("Fragment progress fill", track);
            Stretch(fill);
            progressFill = Art(fill, null, Gold, false);
            unlockNote = Label("Hatch unlock message", details, "A LITTLE CLOSER TO YOUR NEXT DINO", font, 11, 0, -224, 560, 32, Cream);
            Transform button = Node("Hatch continue", panel);
            Position(button, 0, -282, 440, 64);
            Image buttonArt = Art(button, buttonSprite, Color.white, true);
            buttonArt.raycastTarget = true;
            continueButton = button.GetComponent<Button>();
            if (continueButton == null) continueButton = button.gameObject.AddComponent<Button>();
            continueButton.targetGraphic = buttonArt;
            continueButton.navigation = new Navigation { mode = Navigation.Mode.None };
            continueLabel = Label("Label", button, "SKIP REVEAL", font, 18, 0, 0, 408, 44, Cream);
            Transform light = Node("Hatch flash", overlay);
            Stretch(light);
            flash = Art(light, null, Color.clear, false);
            overlay.gameObject.SetActive(false);
        }

        private static Transform Node(string name, Transform parent)
        {
            Transform node = parent.Find(name);
            if (node != null) return node;
            node = new GameObject(name, typeof(RectTransform)).transform;
            node.SetParent(parent, false);
            node.gameObject.layer = 5;
            return node;
        }

        private static void Position(Transform node, float x, float y, float width, float height)
        {
            var rect = (RectTransform)node;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Stretch(Transform node)
        {
            var rect = (RectTransform)node;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static Image Art(Transform node, Sprite sprite, Color color, bool sliced)
        {
            Image image = node.GetComponent<Image>();
            if (image == null) image = node.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.pixelsPerUnitMultiplier = 0.5f;
            image.raycastTarget = false;
            return image;
        }

        private static Image Picture(string name, Transform parent, float x, float y, float size)
        {
            Transform node = Node(name, parent);
            Position(node, x, y, size, size);
            Image image = Art(node, null, Color.white, false);
            image.preserveAspect = true;
            return image;
        }

        private static RunnerHatchGlowGraphic Glow(string name, Transform parent, float x, float y, float size)
        {
            Transform node = Node(name, parent);
            Position(node, x, y, size, size);
            if (node.GetComponent<CanvasRenderer>() == null) node.gameObject.AddComponent<CanvasRenderer>();
            RunnerHatchGlowGraphic glow = node.GetComponent<RunnerHatchGlowGraphic>();
            if (glow == null) glow = node.gameObject.AddComponent<RunnerHatchGlowGraphic>();
            glow.color = Gold;
            glow.raycastTarget = false;
            return glow;
        }

        private static Text Label(string name, Transform parent, string caption, Font font, int size,
            float x, float y, float width, float height, Color color)
        {
            Transform node = Node(name, parent);
            Position(node, x, y, width, height);
            Text label = node.GetComponent<Text>();
            if (label == null) label = node.gameObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = size;
            label.text = caption;
            label.color = color;
            label.alignment = TextAnchor.MiddleCenter;
            label.alignByGeometry = true;
            label.lineSpacing = 1.65f;
            label.supportRichText = false;
            label.raycastTarget = false;
            return label;
        }
    }
}
