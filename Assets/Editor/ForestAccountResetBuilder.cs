using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DinoRush.Editor
{
    public static class ForestAccountResetBuilder
    {
        [MenuItem("Dino Rush/Add Account Reset and Compact Shop Wallets")]
        public static void Build()
        {
            if (Application.isPlaying) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestRun.unity");
            var menu = Object.FindAnyObjectByType<ForestMainMenu>();
            if (menu == null) throw new System.InvalidOperationException("The authored menu is missing.");
            menu.EnsureAccountResetUi();
            menu.EnsureWalletUi();
            menu.ShopCollection.ApplyLayout(false);
            foreach (RunnerShopWallet wallet in menu.GetComponentsInChildren<RunnerShopWallet>(true))
                wallet.GetComponentInChildren<UnityEngine.UI.Text>(true).text = RunnerShopWallet.Caption(0);
            ForestUiMotionBuilder.Apply(menu.GetComponentInParent<ForestRunnerGame>());
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Saved account-reset confirmation and compact Shop egg indicators. No account data was reset.");
        }
    }
}
