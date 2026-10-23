using System.Linq;
using DinoRush.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DinoRush.Tests
{
    public sealed class RunnerMapLayoutTests
    {
        [Test]
        public void SavedLayoutSceneContainsAllOtherMapsAndNeverEntersTheGameBuild()
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(RunnerMapLayoutBuilder.ScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(RunnerMapLayoutBuilder.ScenePath, OpenSceneMode.Additive);
            try
            {
                RunnerMapLayout[] maps = scene.GetRootGameObjects().Select(root => root.GetComponent<RunnerMapLayout>())
                    .Where(layout => layout != null).ToArray();
                Assert.That(maps.Select(map => map.MapId), Is.EquivalentTo(new[] { "desert", "jungle", "rural", "snow" }));
                foreach (RunnerMapLayout layout in maps)
                {
                    Assert.That(layout.CompareTag("EditorOnly"), Is.True);
                    Assert.That(layout.Layers.Count, Is.EqualTo(layout.Map.layers.Length));
                    foreach (RunnerMapLayerLayout layer in layout.Layers)
                    {
                        Assert.That(layer.transform.localPosition.y, Is.EqualTo(layer.Spec.bottom));
                        Assert.That(layer.transform.localScale.y, Is.EqualTo(layer.Spec.height));
                        SpriteRenderer[] tiles = layer.GetComponentsInChildren<SpriteRenderer>();
                        Assert.That(tiles.Length, Is.EqualTo(layer.Spec.stationary ? 1 : 3));
                        foreach (SpriteRenderer tile in tiles)
                        {
                            Assert.That(tile.sprite, Is.EqualTo(layer.Spec.sprite));
                            Assert.That(EditorUtility.IsPersistent(tile.sprite), Is.True);
                            Assert.That(tile.sharedMaterial, Is.Not.Null);
                            Assert.That(tile.sortingOrder, Is.EqualTo(layer.Spec.order));
                        }
                    }
                }
                Assert.That(scene.GetRootGameObjects().Any(root => root.GetComponent<ForestRunnerGame>() != null), Is.False);
                Assert.That(EditorBuildSettings.scenes.Any(entry => entry.path == RunnerMapLayoutBuilder.ScenePath), Is.False);
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        [Test]
        public void MovingAndResizingLayersOnlyAffectsTheChosenMapAfterExplicitSaveAndSupportsUndo()
        {
            RunnerMapCatalog catalog = Object.Instantiate(AssetDatabase.LoadAssetAtPath<RunnerMapCatalog>("Assets/Generated/RunnerMaps/MapCatalog.asset"));
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ForestSprites.mat");
                RunnerMapLayout layout = RunnerMapLayoutBuilder.CreateMap(catalog, catalog.Find("snow"), material, new Vector3(240f, 0f, 0f));
                SceneManager.MoveGameObjectToScene(layout.gameObject, scene);
                RunnerMapLayerLayout cloud = layout.Layers.First(layer => layer.Spec.source.name == "sky_clouds");
                RunnerMapLayerLayout ground = layout.Layers.Last();
                RunnerMapLayerLayout moon = layout.Layers.First(layer => layer.Spec.stationary);
                float bottom = cloud.Spec.bottom, height = cloud.Spec.height, speed = cloud.Spec.speed;
                float groundBottom = ground.Spec.bottom, groundHeight = ground.Spec.height;
                float moonX = moon.Spec.x;
                float desertBottom = catalog[catalog.Find("desert")].layers[0].bottom;
                cloud.transform.localPosition += Vector3.up * 0.75f;
                cloud.transform.localScale = new Vector3(height + 1f, height + 1f, 1f);
                ground.transform.localPosition += Vector3.up;
                moon.transform.localPosition += Vector3.right * 0.4f;
                Assert.That(cloud.Spec.bottom, Is.EqualTo(bottom), "Dragging the preview must not silently overwrite catalog edits.");
                layout.SaveToCatalog(false);
                Assert.That(cloud.Spec.bottom, Is.EqualTo(bottom + 0.75f));
                Assert.That(cloud.Spec.height, Is.EqualTo(height + 1f));
                Assert.That(cloud.Spec.speed, Is.EqualTo(speed));
                Assert.That(moon.Spec.x, Is.EqualTo(moonX + 0.4f).Within(0.0001f));
                Assert.That(ground.Spec.bottom, Is.EqualTo(groundBottom));
                Assert.That(ground.Spec.height, Is.EqualTo(groundHeight));
                Assert.That(ground.transform.localPosition.y, Is.EqualTo(groundBottom));
                Assert.That(catalog[catalog.Find("desert")].layers[0].bottom, Is.EqualTo(desertBottom));
                Undo.FlushUndoRecordObjects();
                Undo.PerformUndo();
                Assert.That(cloud.Spec.bottom, Is.EqualTo(bottom));
                Assert.That(cloud.Spec.height, Is.EqualTo(height));
                layout.ReloadFromCatalog();
                Assert.That(cloud.transform.localPosition.y, Is.EqualTo(bottom));
                Assert.That(cloud.transform.localScale.y, Is.EqualTo(height));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                Object.DestroyImmediate(catalog);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }
    }
}
