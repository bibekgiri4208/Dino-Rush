using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DinoRush
{
    /// <summary>Four-card collection pages keep a growing roster readable and tappable on every screen.</summary>
    public sealed class RunnerShopCollection : MonoBehaviour
    {
        public const int PageSize = 4;
        [SerializeField] private Button[] cards = new Button[0];
        [SerializeField] private Button previous;
        [SerializeField] private Button next;
        [SerializeField] private Text pageLabel;
        [SerializeField] private Text collectionLabel;
        private RunnerShopProgress shop;
        private int selected;
        private bool bound;
        public int Page { get; private set; }
        public int PageCount => Mathf.Max(1, Mathf.CeilToInt(cards.Length / (float)PageSize));

        public void Initialize(RunnerShopProgress progress)
        {
            shop = progress;
            if (!bound)
            {
                bound = true;
                previous.onClick.AddListener(() => MovePage(-1));
                next.onClick.AddListener(() => MovePage(1));
            }
            Refresh(selected);
        }

        public void ShowIndex(int index)
        {
            Page = Mathf.Clamp(index / PageSize, 0, PageCount - 1);
            Refresh(selected);
        }

        public void MovePage(int direction)
        {
            EventSystem.current?.SetSelectedGameObject(null);
            Page = Mathf.Clamp(Page + direction, 0, PageCount - 1);
            Refresh(selected);
        }

        public void Refresh(int selectedIndex)
        {
            selected = selectedIndex;
            int owned = 0;
            for (int i = 0; i < cards.Length; i++)
            {
                bool unlocked = shop == null ? i == 0 : shop.IsUnlocked(i);
                if (unlocked) owned++;
                cards[i].gameObject.SetActive(i / PageSize == Page);
                Transform fill = cards[i].transform.Find("Collection progress/Fill");
                var rect = (RectTransform)fill;
                rect.anchorMax = new Vector2(unlocked ? 1 : Mathf.Clamp01((shop != null ? shop.Fragments(i) : 0) / 100f), 1);
                fill.GetComponent<Image>().color = unlocked ? new Color32(167, 209, 112, 255) : new Color32(255, 210, 117, 255);
                Text status = cards[i].transform.Find("Collection status").GetComponent<Text>();
                status.text = i == selected ? "EQUIPPED" : unlocked ? "TAP TO EQUIP" : "LOCKED";
                status.color = i == selected ? new Color32(255, 210, 117, 255) : new Color32(174, 203, 152, 255);
            }
            collectionLabel.text = "COLLECTION  " + owned + "/" + cards.Length;
            pageLabel.text = "PAGE " + (Page + 1) + " / " + PageCount;
            previous.interactable = Page > 0;
            next.interactable = Page + 1 < PageCount;
        }

        public void EnsureUi(Button[] choices, Font font, Sprite buttonSprite)
        {
            cards = choices;
            collectionLabel = Label("Collection heading", transform, font, "COLLECTION  1/" + cards.Length);
            Label("Collection hint", transform, font, "100 FRAGMENTS TO UNLOCK EACH DINO");
            previous = Control("Previous dinos", transform, font, buttonSprite, "< PREV");
            next = Control("Next dinos", transform, font, buttonSprite, "NEXT >");
            pageLabel = Label("Collection page", transform, font, "PAGE 1 / " + PageCount);
            foreach (Button card in cards)
            {
                Transform track = Node("Collection progress", card.transform);
                Picture(track, new Color32(13, 37, 35, 255));
                Transform fill = Node("Fill", track);
                Picture(fill, new Color32(255, 210, 117, 255));
                var rect = (RectTransform)fill;
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                Label("Collection status", card.transform, font, "LOCKED");
            }
            ApplyLayout(false);
            Refresh(0);
        }

        public void ApplyLayout(bool portrait)
        {
            float productX = portrait ? 0 : -272;
            Rect("Mystery egg display", productX, portrait ? 370 : 54, portrait ? 560 : 340, portrait ? 220 : 312);
            const string product = "Mystery egg display/";
            Rect(product + "Egg display glow", portrait ? -184 : 0, portrait ? 0 : 64, 184, 184);
            Rect(product + "Showcase egg", portrait ? -184 : 0, portrait ? 0 : 64, 144, 144);
            Rect(product + "Companion egg left", portrait ? -232 : -62, portrait ? -12 : 52, 80, 80);
            Rect(product + "Companion egg right", portrait ? -136 : 62, portrait ? -12 : 52, 80, 80);
            TextLayout(product + "Egg product title", portrait ? 86 : 0, portrait ? 54 : -28, portrait ? 336 : 312, 30, portrait ? 20 : 18);
            TextLayout(product + "Egg product description", portrait ? 86 : 0, portrait ? 0 : -78, portrait ? 336 : 312, 48, portrait ? 14 : 11);
            TextLayout(product + "Egg unlock requirement", portrait ? 86 : 0, portrait ? -60 : -126, portrait ? 336 : 312, 24, portrait ? 12 : 10);
            TextLayout("Shop egg balance", productX, portrait ? 516 : 226, portrait ? 560 : 340, 30, 16);
            Rect("Buy mystery egg", productX, portrait ? 196 : -146, portrait ? 560 : 340, portrait ? 100 : 60);
            TextLayout("Buy mystery egg/Label", 0, 0, portrait ? 520 : 316, portrait ? 72 : 44, portrait ? 20 : 14);
            transform.Find("Buy mystery egg").GetComponent<RunnerUiButtonFeedback>()?.RefreshContentLayout();
            TextLayout("Mystery egg result", productX, portrait ? 104 : -212, portrait ? 560 : 340, 52, portrait ? 12 : 10);
            TextLayout("Collection heading", portrait ? 0 : 184, portrait ? 34 : 224, portrait ? 560 : 512, 30, portrait ? 22 : 16);
            TextLayout("Collection hint", portrait ? 0 : 184, portrait ? 0 : 192, portrait ? 560 : 512, 24, portrait ? 12 : 10);
            Rect("Dino choices", portrait ? 0 : 184, 0, portrait ? 560 : 512, portrait ? 420 : 352);
            for (int i = 0; i < cards.Length; i++)
            {
                Transform card = cards[i].transform;
                Position(card, i % 2 == 0 ? (portrait ? -144 : -130) : (portrait ? 144 : 130),
                    i % 4 < 2 ? (portrait ? -112 : 90) : (portrait ? -300 : -86), portrait ? 264 : 244, portrait ? 176 : 160);
                Position(card.Find("Dino portrait"), -74, 18, 72, 80);
                TextLayout(card.Find("Dino name"), 36, portrait ? 44 : 38, portrait ? 152 : 144, 26, portrait ? 18 : 12);
                TextLayout(card.Find("Fragments"), 36, portrait ? 8 : 2, portrait ? 152 : 144, 40, portrait ? 14 : 10);
                Position(card.Find("Collection progress"), 0, portrait ? -48 : -44, portrait ? 224 : 204, 6);
                TextLayout(card.Find("Collection status"), 0, portrait ? -70 : -64, portrait ? 232 : 212, 22, portrait ? 12 : 9);
                card.GetComponent<RunnerUiButtonFeedback>()?.RefreshContentLayout();
            }
            float center = portrait ? 0 : 184, y = portrait ? -450 : -210;
            Rect("Previous dinos", center - (portrait ? 198 : 174), y, portrait ? 152 : 144, portrait ? 104 : 52);
            Rect("Next dinos", center + (portrait ? 198 : 174), y, portrait ? 152 : 144, portrait ? 104 : 52);
            TextLayout("Previous dinos/Label", 0, 0, portrait ? 136 : 128, portrait ? 80 : 40, portrait ? 16 : 12);
            TextLayout("Next dinos/Label", 0, 0, portrait ? 136 : 128, portrait ? 80 : 40, portrait ? 16 : 12);
            previous.GetComponent<RunnerUiButtonFeedback>()?.RefreshContentLayout();
            next.GetComponent<RunnerUiButtonFeedback>()?.RefreshContentLayout();
            TextLayout("Collection page", center, y, portrait ? 236 : 196, 28, portrait ? 14 : 12);
            Transform status = transform.Find("Selected status"), map = transform.Find("Map selection");
            status.gameObject.SetActive(!portrait);
            map.gameObject.SetActive(!portrait);
            TextLayout(status, 184, -266, 512, 26, 12);
            TextLayout(map, 184, -294, 512, 22, 10);
            if (collectionLabel != null) Refresh(selected);
        }

        private void Rect(string path, float x, float y, float width, float height) => Position(transform.Find(path), x, y, width, height);
        private void TextLayout(string path, float x, float y, float width, float height, int size) => TextLayout(transform.Find(path), x, y, width, height, size);
        private static void TextLayout(Transform node, float x, float y, float width, float height, int size)
        {
            if (node == null) return;
            Position(node, x, y, width, height);
            Text text = node.GetComponent<Text>();
            text.fontSize = size;
            text.resizeTextForBestFit = false;
        }

        private static void Position(Transform node, float x, float y, float width, float height)
        {
            if (node == null) return;
            var rect = (RectTransform)node;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = Vector3.one;
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

        private static Image Picture(Transform node, Color color)
        {
            Image image = node.GetComponent<Image>();
            if (image == null) image = node.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text Label(string name, Transform parent, Font font, string caption)
        {
            Transform node = Node(name, parent);
            Text text = node.GetComponent<Text>();
            if (text == null) text = node.gameObject.AddComponent<Text>();
            text.font = font;
            text.text = caption;
            text.color = new Color32(240, 242, 207, 255);
            text.alignment = TextAnchor.MiddleCenter;
            text.alignByGeometry = true;
            text.lineSpacing = 1.65f;
            text.raycastTarget = false;
            text.supportRichText = false;
            return text;
        }

        private static Button Control(string name, Transform parent, Font font, Sprite sprite, string caption)
        {
            Transform node = Node(name, parent);
            Image image = Picture(node, Color.white);
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 0.5f;
            image.raycastTarget = true;
            Button button = node.GetComponent<Button>();
            if (button == null) button = node.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            Label("Label", node, font, caption);
            return button;
        }
    }
}
