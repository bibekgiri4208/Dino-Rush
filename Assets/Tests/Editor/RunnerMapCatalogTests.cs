using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DinoRush.Tests
{
    public sealed class RunnerMapCatalogTests
    {
        private const string Directory = "Assets/Generated/RunnerMaps";

        [Test]
        public void FourMapsReferenceTheirSuppliedArtAndShareTheSameRunningSurface()
        {
            RunnerMapCatalog catalog = AssetDatabase.LoadAssetAtPath<RunnerMapCatalog>(Directory + "/MapCatalog.asset");
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Count, Is.EqualTo(4));
            string[] ids = { "forest", "desert", "jungle", "rural" };
            string[] folders = { "forest bg", "desert bg", "jungle bg", "rural area bg" };
            for (int i = 0; i < catalog.Count; i++)
            {
                RunnerMapCatalog.Map map = catalog[i];
                Assert.That(map.id, Is.EqualTo(ids[i]));
                Assert.That(catalog.Find(ids[i]), Is.EqualTo(i));
                Assert.That(map.preview, Is.Not.Null);
                Assert.That(map.preview.rect.size, Is.EqualTo(new Vector2(256, 144)));
                Assert.That(map.layers.Length, Is.GreaterThanOrEqualTo(6));
                int themedSources = 0;
                foreach (RunnerMapCatalog.MapLayer layer in map.layers)
                {
                    Assert.That(layer.source, Is.Not.Null, map.id);
                    Assert.That(layer.sprite, Is.Not.Null, map.id);
                    if (AssetDatabase.GetAssetPath(layer.source).StartsWith("Assets/2D Sprites/" + folders[i] + "/")) themedSources++;
                    if (layer.stationary) continue;
                    float stride = (layer.sprite.rect.width - 1) / layer.sprite.pixelsPerUnit * layer.height;
                    Assert.That(stride, Is.GreaterThan(20.2f), layer.sprite.name + " cannot cover the viewport through a wrap");
                }
                Assert.That(themedSources, Is.GreaterThanOrEqualTo(5));
                RunnerMapCatalog.MapLayer ground = map.layers[map.layers.Length - 1];
                Assert.That(ground.height, Is.EqualTo(1f));
                Assert.That(ground.bottom, Is.EqualTo(-3.96f));
                Assert.That(ground.speed, Is.EqualTo(1f));
                Assert.That(ground.order, Is.Zero);
            }
            Assert.That(catalog.Find("missing-map"), Is.Zero);
            Assert.That(catalog.Find(null), Is.Zero);
        }

        [Test]
        public void GeneratedLoopsHaveMatchingEdgesAndPixelPerfectImportSettings()
        {
            foreach (string path in System.IO.Directory.GetFiles(Directory, "*_loop.png"))
            {
                var texture = new Texture2D(2, 2);
                try
                {
                    Assert.That(texture.LoadImage(File.ReadAllBytes(path)), Is.True);
                    Color32[] pixels = texture.GetPixels32();
                    for (int y = 0; y < texture.height; y++)
                        Assert.That(pixels[y * texture.width], Is.EqualTo(pixels[(y + 1) * texture.width - 1]), path + " row " + y);
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
                    Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
                    Assert.That(importer.mipmapEnabled, Is.False);
                    Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
                    Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(texture.height));
                }
                finally { Object.DestroyImmediate(texture); }
            }
        }

        [Test]
        public void SceneSavesTheCatalogFourCardsAndStartBackBindings()
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath("Assets/Scenes/ForestRun.unity");
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestRun.unity", OpenSceneMode.Additive);
            try
            {
                ForestRunnerGame game = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                    if ((game = root.GetComponentInChildren<ForestRunnerGame>()) != null) break;
                Assert.That(game.MapCatalog, Is.Not.Null);
                var data = new SerializedObject(game.GetComponentInChildren<ForestMainMenu>(true));
                foreach (string name in new[] { "mapChoices", "mapPreviews", "mapCaptions" })
                {
                    SerializedProperty list = data.FindProperty(name);
                    Assert.That(list.arraySize, Is.EqualTo(4));
                    for (int i = 0; i < 4; i++) Assert.That(list.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null);
                }
                foreach (string name in new[] { "mapPage", "mapStatus", "customizeMapLabel", "startMap", "mapBack" })
                    Assert.That(data.FindProperty(name).objectReferenceValue, Is.Not.Null);
                Assert.That(((GameObject)data.FindProperty("mapPage").objectReferenceValue).activeSelf, Is.False);
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }
    }
}
