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
        public void FiveMapsReferenceTheirSuppliedArtAndShareTheSameRunningSurface()
        {
            RunnerMapCatalog catalog = AssetDatabase.LoadAssetAtPath<RunnerMapCatalog>(Directory + "/MapCatalog.asset");
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Count, Is.EqualTo(5));
            string[] ids = { "forest", "desert", "jungle", "rural", "snow" };
            string[] folders = { "forest bg", "desert bg", "jungle bg", "rural area bg", "skies bg" };
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
                        && (Path.GetFileName(path).StartsWith("desert_") || Path.GetFileName(path).StartsWith("rural_")
                            || Path.GetFileName(path).StartsWith("snow_"));
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
        [TestCase(4)]
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
        public void SceneSavesTheCatalogFiveCardsAndStartBackBindings()
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
                    Assert.That(list.arraySize, Is.EqualTo(game.MapCatalog.Count));
                    for (int i = 0; i < list.arraySize; i++) Assert.That(list.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null);
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

        [TestCase(1, "desert_dunemid.png")]
        [TestCase(2, "plx-5.png")]
        [TestCase(3, "ruralparallaxriverfront.png")]
        [TestCase(4, "sky_cloud_floor_2.png")]
        public void ForegroundIsAnchoredToTheTrailWithoutTransparentGapsAndSkyCoversTheView(int index, string foregroundSource)
        {
            RunnerMapCatalog catalog = AssetDatabase.LoadAssetAtPath<RunnerMapCatalog>(Directory + "/MapCatalog.asset");
            RunnerMapCatalog.Map map = catalog[index];
            RunnerMapCatalog.MapLayer sky = map.layers[0];
            Assert.That(sky.bottom, Is.LessThanOrEqualTo(RunnerRules.GroundY));
            Assert.That(sky.bottom + sky.height, Is.GreaterThanOrEqualTo(5.625f));
            RunnerMapCatalog.MapLayer foreground = FindLayer(map, foregroundSource);
            Assert.That(foreground.bottom, Is.InRange(RunnerRules.GroundY - 1f, RunnerRules.GroundY));
            Assert.That(foreground.height, Is.InRange(8f, 10f), "Use the map's scenic frame, not a one-size-fits-all full-screen crop.");
            var texture = new Texture2D(2, 2);
            try
            {
                texture.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(foreground.sprite)));
                Color32[] pixels = texture.GetPixels32();
                int row = Mathf.FloorToInt((RunnerRules.GroundY + 0.08f - foreground.bottom) / foreground.height * texture.height);
                Assert.That(row, Is.InRange(0, texture.height - 1));
                // A continuous opaque row immediately above the trail prevents sky
                // leaks for every horizontal phase, not just the initial screenshot.
                for (int x = 0; x < texture.width; x++)
                    Assert.That(pixels[row * texture.width + x].a, Is.EqualTo(255), map.id + " trail gap at texture x=" + x);
            }
            finally { Object.DestroyImmediate(texture); }
        }

        [Test]
        public void RuralVillageWaterAndBankShareTheirSourceCanvasAndJungleLayersRemainRegistered()
        {
            RunnerMapCatalog catalog = AssetDatabase.LoadAssetAtPath<RunnerMapCatalog>(Directory + "/MapCatalog.asset");
            RunnerMapCatalog.Map rural = catalog[3];
            RunnerMapCatalog.MapLayer river = FindLayer(rural, "ruralparallaxriver.png");
            RunnerMapCatalog.MapLayer reflection = FindLayer(rural, "ruralparallaxriverskyredlex.png");
            Assert.That(reflection.speed, Is.EqualTo(river.speed), "The reflection must not drift out of the water.");
            foreach (string source in new[] { "ruralparallaxvillage.png", "ruralparallaxriverskyredlex.png", "ruralparallaxriverfront.png" })
            {
                RunnerMapCatalog.MapLayer layer = FindLayer(rural, source);
                Assert.That(layer.height, Is.EqualTo(river.height), source);
                Assert.That(layer.bottom, Is.EqualTo(river.bottom), source);
            }
            RunnerMapCatalog.MapLayer mountain = FindLayer(rural, "ruralparallaxmountain.png");
            // The source summit is 202px from the top of a 1200px canvas.
            float summit = mountain.bottom + mountain.height * (1f - 202f / 1200f);
            Assert.That(summit, Is.InRange(1.5f, 3f), "The distant mountain should not fill the upper sky/HUD.");
            foreach (RunnerMapCatalog.MapLayer layer in catalog[2].layers)
            {
                if (layer.order >= 0) continue;
                Assert.That(layer.height, Is.EqualTo(catalog[2].layers[0].height));
                Assert.That(layer.bottom, Is.EqualTo(catalog[2].layers[0].bottom));
            }
            foreach (int index in new[] { 1, 3 })
            foreach (RunnerMapCatalog.MapLayer layer in catalog[index].layers)
            {
                if (!layer.stationary) continue;
                float halfWidth = layer.sprite.rect.width / layer.sprite.pixelsPerUnit * layer.height * 0.5f;
                Assert.That(Mathf.Abs(layer.x) + halfWidth, Is.LessThan(3.5f), "Moon should sit between the HUD cards, not behind them.");
                Assert.That(layer.bottom, Is.GreaterThan(3.3f));
                Assert.That(layer.bottom + layer.height, Is.LessThan(5.2f));
            }
        }

        private static RunnerMapCatalog.MapLayer FindLayer(RunnerMapCatalog.Map map, string sourceName)
        {
            foreach (RunnerMapCatalog.MapLayer layer in map.layers)
                if (Path.GetFileName(AssetDatabase.GetAssetPath(layer.source)) == sourceName) return layer;
            throw new AssertionException("Missing map layer: " + sourceName);
        }
    }
}
