using System;
using UnityEngine;
using UnityEngine.UI;
using static DinoRush.RunnerShopPresentation;

namespace DinoRush
{
    public sealed class RunnerSlashShop : MonoBehaviour
    {
        [SerializeField] private GameObject content;
        [SerializeField] private Text wallet, status;
        [SerializeField] private Button[] styles = new Button[3];
        [SerializeField] private Button charge;
        private Action changed;
        private bool bound;
        public void Initialize(Action refresh)
        {
            changed = refresh;
            if (bound) return;
            bound = true;
            for (int i = 0; i < styles.Length; i++)
            {
                var kind = (RunnerSlashKind)(i + 1);
                styles[i].onClick.AddListener(() =>
                {
                    if (!content.activeInHierarchy) return;
                    if (RunnerSlashProgress.Owned(kind)) RunnerSlashProgress.Equip(kind); else RunnerSlashProgress.Buy(kind);
                    UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
                    changed?.Invoke(); Refresh();
                });
            }
            charge.onClick.AddListener(() => { if (!content.activeInHierarchy) return; RunnerSlashProgress.BuyCharge(); changed?.Invoke(); Refresh(); });
        }
        public void Show(bool visible) { if (content != null) content.SetActive(visible); Refresh(); }
        public void Refresh()
        {
            if (content == null) return;
            int balance = Mathf.Max(0, PlayerPrefs.GetInt(RunnerShopProgress.WalletKey));
            wallet.text = "EGGS: " + balance.ToString("N0");
            status.text = "LOADED " + RunnerSlashProgress.Charges + "/3 / " + RunnerSlashProgress.Equipped.ToString().ToUpperInvariant();
            for (int i = 0; i < 3; i++)
            {
                var kind = (RunnerSlashKind)(i + 1);
                bool owned = RunnerSlashProgress.Owned(kind), selected = RunnerSlashProgress.Equipped == kind;
                styles[i].GetComponentInChildren<Text>(true).text = selected ? "EQUIPPED" : owned ? "EQUIP" : "UNLOCK / 5,000";
                styles[i].interactable = !selected && (owned || balance >= RunnerSlashProgress.UnlockCost);
                styles[i].transform.parent.Find("Ownership").GetComponent<Text>().text = owned ? "PERMANENTLY OWNED" : "5,000 EGGS / FOREVER";
            }
            charge.interactable = RunnerSlashProgress.Equipped != RunnerSlashKind.None && RunnerSlashProgress.Charges < 3 && balance >= RunnerSlashProgress.ChargeCost;
            charge.GetComponentInChildren<Text>(true).text = RunnerSlashProgress.Charges == 3 ? "FULL / 3 CHARGES" : "+1 CHARGE / 1,000 EGGS";
        }
        public void EnsureUi(Font font, Sprite panel, Sprite button, RunnerSlashCatalog catalog)
        {
            content = Node("Slashes section", transform).gameObject; Stretch(content.transform);
            wallet = Label("Slash egg balance", content.transform, "EGGS: 0", font, 16, -252, 226, 340, 32, Color.white);
            status = Label("Slash loadout", content.transform, "LOADED 0/3", font, 12, 210, 226, 420, 32, Color.white);
            string[] names = { "FIRE", "LIGHTNING", "WATER" };
            for (int i = 0; i < 3; i++)
            {
                Transform card = Node("Slash card " + (i + 1), content.transform); Art(card, panel, Color.white, true);
                Label("Name", card, names[i], font, 22, 0, 174, 250, 32, new Color32(255, 210, 117, 255));
                Art(Node("Preview background", card), null, new Color32(8, 23, 31, 255), false);
                var image = Art(Node("Slash illustration", card), catalog.Frame((RunnerSlashKind)(i + 1), 0.2f), Color.white, false);
                image.preserveAspect = true;
                var preview = image.GetComponent<RunnerSlashPreview>(); if (preview == null) preview = image.gameObject.AddComponent<RunnerSlashPreview>();
                preview.Configure(catalog, (RunnerSlashKind)(i + 1));
                Label("Description", card, "SHATTER OBSTACLES\nSame power / 3 styles", font, 10, 0, -74, 248, 44, Color.white);
                Label("Ownership", card, "5,000 EGGS / FOREVER", font, 10, 0, -120, 248, 28, new Color32(255, 210, 117, 255));
                styles[i] = Control("Unlock slash " + (i + 1), card, font, button);
            }
            Label("Charge rules", content.transform, "ENTER TO SLASH / MAX 3 PER MATCH\nUnused charges carry over / shared across styles", font, 10, 0, -260, 840, 48, Color.white);
            charge = Control("Buy slash charge", content.transform, font, button);
            Show(false); ApplyLayout(false);
        }
        public void ApplyLayout(bool portrait)
        {
            if (content == null) return;
            Position(wallet.transform, portrait ? -142 : -252, portrait ? 492 : 226, portrait ? 280 : 340, 32);
            Position(status.transform, portrait ? 142 : 210, portrait ? 492 : 226, portrait ? 280 : 420, 32);
            for (int i = 0; i < 3; i++)
            {
                Transform card = styles[i].transform.parent;
                Position(card, portrait ? 0 : (i - 1) * 294, portrait ? 300 - i * 300 : -8, portrait ? 576 : 278, portrait ? 280 : 416);
                Position(card.Find("Name"), portrait ? 130 : 0, portrait ? 95 : 174, portrait ? 270 : 248, 32);
                Position(card.Find("Preview background"), portrait ? -155 : 0, portrait ? 0 : 48, portrait ? 232 : 250, portrait ? 240 : 190);
                Position(card.Find("Slash illustration"), portrait ? -155 : 0, portrait ? 0 : 48, portrait ? 224 : 244, portrait ? 224 : 186);
                Position(card.Find("Description"), portrait ? 130 : 0, portrait ? 40 : -76, portrait ? 270 : 248, 44);
                card.Find("Description").GetComponent<Text>().fontSize = portrait ? 12 : 10;
                Position(card.Find("Ownership"), portrait ? 130 : 0, portrait ? -10 : -120, portrait ? 270 : 248, 28);
                card.Find("Ownership").GetComponent<Text>().fontSize = portrait ? 12 : 10;
                Place(styles[i].transform, portrait ? 130 : 0, portrait ? -86 : -170, portrait ? 260 : 244, portrait ? 84 : 52);
                styles[i].GetComponentInChildren<Text>(true).fontSize = portrait ? 14 : 12;
            }
            Position(content.transform.Find("Charge rules"), 0, portrait ? -484 : -260, portrait ? 576 : 840, portrait ? 60 : 48);
            Place(charge.transform, portrait ? 146 : 240, portrait ? -614 : -330, portrait ? 276 : 360, portrait ? 88 : 52);
            if (content.activeSelf)
            {
                Transform back = transform.parent.Find("Back to main menu");
                Place(back, portrait ? -146 : -240, portrait ? -614 : -330, portrait ? 276 : 360, portrait ? 88 : 52);
            }
        }
        private static Button Control(string name, Transform parent, Font font, Sprite sprite)
        {
            Transform node = Node(name, parent); Image image = Art(node, sprite, Color.white, true); image.raycastTarget = true;
            Button result = node.GetComponent<Button>(); if (result == null) result = node.gameObject.AddComponent<Button>();
            result.targetGraphic = image; result.navigation = new Navigation { mode = Navigation.Mode.None };
            Label("Label", node, "", font, 12, 0, 0, 240, 40, Color.white); return result;
        }
        private static void Place(Transform node, float x, float y, float w, float h)
        {
            Position(node, x, y, w, h); Position(node.Find("Label"), 0, 0, w - 16, h - 12);
            node.GetComponent<RunnerUiButtonFeedback>()?.RefreshContentLayout();
        }
    }
}
