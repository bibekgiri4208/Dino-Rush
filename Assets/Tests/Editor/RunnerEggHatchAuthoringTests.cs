using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DinoRush.Tests
{
    public sealed class RunnerEggHatchAuthoringTests
    {
        [Test]
        public void CatalogReferencesAllNineOriginalEggStripsWithMatchingDinoIds()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<RunnerEggHatchCatalog>("Assets/Generated/RunnerEggHatchCatalog.asset");
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Count, Is.EqualTo(9));
            string[] ids = { "red", "yellow", "green", "cole", "kira", "kuro", "loki", "mono", "sena" };
            string[] folders = { "egg - 2", "egg - 3", "egg- 4", "cole/egg", "kira/egg", "kuro/egg", "loki/egg", "mono/egg", "sena/egg" };
            for (int i = 0; i < ids.Length; i++)
            {
                var egg = catalog.Find(ids[i]);
                Assert.That(egg, Is.Not.Null);
                Texture2D[] sheets = { egg.move, egg.crack, egg.hatch };
                string[] stages = { "move", "crack", "hatch" };
                for (int j = 0; j < sheets.Length; j++)
                {
                    Assert.That(AssetDatabase.GetAssetPath(sheets[j]), Is.EqualTo("Assets/2D Sprites/dino/" + folders[i] + "/" + stages[j] + ".png"));
                    Assert.That(sheets[j].width, Is.EqualTo(96));
                    Assert.That(sheets[j].height, Is.EqualTo(24));
                }
            }
            Assert.That(catalog.Find("blue"), Is.Null);
        }

        [Test]
        public void SavedSceneContainsEditableEggShowcaseHatchOverlayAndCatalogBinding()
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath("Assets/Scenes/ForestRun.unity");
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestRun.unity", OpenSceneMode.Additive);
            try
            {
                ForestMainMenu menu = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                    if ((menu = root.GetComponentInChildren<ForestMainMenu>(true)) != null) break;
                Assert.That(menu.ShopPresentation, Is.Not.Null);
                Assert.That(menu.ShopPresentation.Catalog.Count, Is.EqualTo(9));
                Assert.That(menu.ShopPresentation.transform.Find("Mystery egg display/Showcase egg").GetComponent<UnityEngine.UI.Image>().sprite, Is.Not.Null);
                Assert.That(menu.transform.Find("Hatch reveal").gameObject.activeSelf, Is.False);
                foreach (RunnerHatchGlowGraphic glow in menu.GetComponentsInChildren<RunnerHatchGlowGraphic>(true))
                {
                    Assert.That(glow.GetComponent<CanvasRenderer>(), Is.Not.Null, "Glows must have a renderer, not just a graphic component.");
                    Assert.That(glow.raycastTarget, Is.False);
                }
                var data = new SerializedObject(menu.ShopPresentation);
                foreach (string field in new[] { "productEgg", "revealRoot", "revealFade", "revealEgg", "revealPortrait", "revealGlow", "heading", "amount", "progressFill", "rewardDetails", "continueButton" })
                    Assert.That(data.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }
    }
}
