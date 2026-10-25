using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DinoRush.Editor
{
    public static class ForestShopWalletBuilder
    {
        public const string IconPath = "Assets/Generated/ShopEggIcon.asset";

        [MenuItem("Dino Rush/Improve Shop Egg Wallets")]
        public static void Build()
        {
            if (Application.isPlaying) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestRun.unity");
            var menu = Object.FindAnyObjectByType<ForestMainMenu>();
            if (menu == null) throw new System.InvalidOperationException("The forest menu is missing.");
            Sprite icon = AssetDatabase.LoadAllAssetsAtPath(IconPath).OfType<Sprite>().FirstOrDefault();
            if (icon == null)
            {
                var assets = new List<Object>();
                icon = RunnerEggArt.CreateEgg(assets);
                AssetDatabase.CreateAsset(icon.texture, IconPath);
                AssetDatabase.AddObjectToAsset(icon, IconPath);
            }
            menu.EnsureWalletUi(icon);
            menu.ShopCollection.ApplyLayout(false);
            foreach (RunnerShopWallet wallet in menu.GetComponentsInChildren<RunnerShopWallet>(true))
                wallet.GetComponentInChildren<UnityEngine.UI.Text>(true).text = RunnerShopWallet.Caption(0);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Saved three readable Shop wallets with a shared currency icon.");
        }
    }
}
