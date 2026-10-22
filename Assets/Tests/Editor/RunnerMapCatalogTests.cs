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
        public void GeneratedLoopsHaveMatchingEdgesAndUncompressedImportSettings()
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
                    bool smooth = !path.EndsWith("_ground_loop.png")
                        && (Path.GetFileName(path).StartsWith("desert_") || Path.GetFileName(path).StartsWith("rural_"));
                    Assert.That(importer.filterMode, Is.EqualTo(smooth ? FilterMode.Bilinear : FilterMode.Point));
                    Assert.That(importer.mipmapEnabled, Is.False);
                    Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
                    Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(texture.height));
                }
                finally { Object.DestroyImmediate(texture); }
            }
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void NewSceneryRetainsNativeSourcePixelsAndImportsWithoutDownscaling(int mapIndex)
        {
            RunnerMapCatalog catalog = AssetDatabase.LoadAssetAtPath<RunnerMapCatalog>(Directory + "/MapCatalog.asset");
            RunnerMapCatalog.Map map = catalog[mapIndex];
            foreach (RunnerMapCatalog.MapLayer layer in map.layers)
            {
                if (layer.order >= 0) continue; // The shared pixel-art running surface is intentionally unchanged.
                string sourcePath = AssetDatabase.GetAssetPath(layer.source);
                string generatedPath = AssetDatabase.GetAssetPath(layer.sprite);
                var source = new Texture2D(2, 2);
                var generated = new Texture2D(2, 2);
                try
                {
                    Assert.That(source.LoadImage(File.ReadAllBytes(sourcePath)), Is.True);
                    Assert.That(generated.LoadImage(File.ReadAllBytes(generatedPath)), Is.True);
                    Color32[] original = source.GetPixels32();
                    Color32[] pixels = generated.GetPixels32();
                    int minX = 0, minY = 0;
                    if (layer.stationary)
                    {
                        minX = source.width;
                        minY = source.height;
                        int maxX = -1, maxY = -1;
                        for (int y = 0; y < source.height; y++)
                        for (int x = 0; x < source.width; x++)
                        {
                            if (original[y * source.width + x].a == 0) continue;
                            minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                            minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
                        }
                        Assert.That(generated.width, Is.EqualTo(maxX - minX + 5), generatedPath);
                        Assert.That(generated.height, Is.EqualTo(maxY - minY + 5), generatedPath);
                        minX -= 2;
                        minY -= 2;
                    }
                    else
                    {
                        Assert.That(generated.width, Is.EqualTo(source.width * 2), generatedPath);
                        Assert.That(generated.height, Is.EqualTo(source.height), generatedPath);
                    }
                    // Compare source samples, not imported source textures (which may have their own size caps).
                    int width = layer.stationary ? generated.width : source.width;
                    for (int y = 0; y < generated.height; y += Mathf.Max(1, generated.height / 64))
                    for (int x = 0; x < width; x += Mathf.Max(1, width / 64))
                    {
                        int sx = x + minX, sy = y + minY;
                        if (sx < 0 || sx >= source.width || sy < 0 || sy >= source.height) continue;
                        Color32 expected = original[sy * source.width + sx];
                        if (layer.order == -100 && expected.a == 0) continue; // Skies extend below the source horizon.
                        Assert.That(pixels[y * generated.width + x], Is.EqualTo(expected), generatedPath + " source pixel " + sx + "," + sy);
                    }
                    var importer = (TextureImporter)AssetImporter.GetAtPath(generatedPath);
                    Assert.That(importer.maxTextureSize, Is.GreaterThanOrEqualTo(Mathf.Max(generated.width, generated.height)), generatedPath);
                    Assert.That(layer.sprite.texture.width, Is.EqualTo(generated.width), generatedPath);
                    Assert.That(layer.sprite.texture.height, Is.EqualTo(generated.height), generatedPath);
                    Assert.That(layer.sprite.pixelsPerUnit, Is.EqualTo(generated.height), generatedPath);
                    Assert.That(importer.filterMode, Is.EqualTo(map.id == "jungle" ? FilterMode.Point : FilterMode.Bilinear));
                }
                finally
                {
                    Object.DestroyImmediate(source);
                    Object.DestroyImmediate(generated);
                }
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
