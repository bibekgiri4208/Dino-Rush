using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using static DinoRush.RunnerShopPresentation;

namespace DinoRush
{
    /// <summary>A consistent, non-interactive currency badge for every Shop category.</summary>
    public sealed class RunnerShopWallet : MonoBehaviour
    {
        [SerializeField] private Text balance;
        [SerializeField] private Image icon;
        private readonly List<Object> generated = new List<Object>();

        public static RunnerShopWallet Ensure(Text label, Sprite eggSprite = null)
        {
            var wallet = label.GetComponentInParent<RunnerShopWallet>(true);
            if (wallet == null)
            {
                Transform node = Node(label.name + " wallet", label.transform.parent);
                node.SetSiblingIndex(label.transform.GetSiblingIndex());
                wallet = node.gameObject.AddComponent<RunnerShopWallet>();
                label.transform.SetParent(node, false);
                wallet.balance = label;
            }
            Art(wallet.transform, null, new Color32(11, 32, 32, 255), false);
            Transform edge = Node("Wallet gold accent", wallet.transform);
            Art(edge, null, new Color32(226, 185, 100, 255), false);
            if (wallet.icon == null) wallet.icon = Picture("Wallet egg icon", wallet.transform, 0, 0, 36);
            if (eggSprite != null) wallet.icon.sprite = eggSprite;
            if (wallet.icon.sprite == null && Application.isPlaying)
                wallet.icon.sprite = RunnerEggArt.CreateEgg(wallet.generated);
            label.transform.SetAsLastSibling();
            label.color = new Color32(255, 220, 139, 255);
            label.alignment = TextAnchor.MiddleRight;
            label.lineSpacing = 1;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 8;
            label.supportRichText = false;
            label.raycastTarget = false;
            return wallet;
        }

        public static void Layout(Text label, float x, float y, float width, bool portrait)
        {
            RunnerShopWallet wallet = Ensure(label);
            width = Mathf.Min(width, portrait ? 360 : 280);
            float height = portrait ? 44 : 36;
            Position(wallet.transform, x, y, width, height);
            Position(wallet.transform.Find("Wallet gold accent"), -width / 2 + 1, 0, 2, height);
            Position(wallet.icon.transform, -width / 2 + 19, 0, 20, 26);
            Position(label.transform, 14, 0, width - 52, 24);
            label.fontSize = portrait ? 18 : 16;
            label.resizeTextMaxSize = label.fontSize;
        }

        public static string Caption(int eggs) => "EGGS: " + Mathf.Max(0, eggs).ToString("N0", CultureInfo.InvariantCulture);

        private void OnDestroy()
        {
            foreach (Object asset in generated) if (asset != null) Destroy(asset);
            generated.Clear();
        }
    }
}
