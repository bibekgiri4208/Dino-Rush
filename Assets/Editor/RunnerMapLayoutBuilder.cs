using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DinoRush.Editor
{
    /// <summary>A separate, saved authoring scene; never rebuilds or edits the gameplay scene.</summary>
    public static class RunnerMapLayoutBuilder
    {
        public const string ScenePath = "Assets/Scenes/MapLayouts.unity";
        private const string CatalogPath = "Assets/Generated/RunnerMaps/MapCatalog.asset";

        [MenuItem("Dino Rush/Edit Map Layers")]
        public static void OpenLayouts()
        {
            if (Application.isPlaying) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath))
            {
                Scene scene = EditorSceneManager.OpenScene(ScenePath);
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    RunnerMapLayout layout = root.GetComponent<RunnerMapLayout>();
                    if (layout == null) continue;
                    Selection.activeGameObject = root;
                    layout.FocusMap();
                    break;
                }
                return;
            }
            RunnerMapCatalog catalog = AssetDatabase.LoadAssetAtPath<RunnerMapCatalog>(CatalogPath);
            if (catalog == null) throw new System.InvalidOperationException("The map catalog is missing.");
            Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ForestSprites.mat");
            if (material == null) throw new System.InvalidOperationException("The shared sprite material is missing.");
            Scene layouts = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Map layout preview camera", typeof(Camera)).GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.625f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.allowHDR = false;
            camera.tag = "EditorOnly";
            RunnerMapLayout first = null;
            // Forest already has its original editable hierarchy in ForestRun.unity.
            for (int i = 1; i < catalog.Count; i++)
            {
                RunnerMapLayout layout = CreateMap(catalog, i, material, new Vector3((i - 1) * 240f, 0f, 0f));
                if (first == null) first = layout;
            }
            if (first != null)
            {
                Selection.activeGameObject = first.gameObject;
                first.FocusMap();
            }
            EditorSceneManager.SaveScene(layouts, ScenePath);
            Debug.Log("MapLayouts saved with editable Desert, Jungle, Rural, and Snow Mountain scenery. Select a map and Preview This Map; save layer edits with Save Layout to Game. Forest remains editable in ForestRun.");
        }

        public static RunnerMapLayout CreateMap(RunnerMapCatalog catalog, int index, Material material, Vector3 position)
        {
            var root = new GameObject(catalog[index].displayName + " - select to preview and save", typeof(RunnerMapLayout));
            root.tag = "EditorOnly";
            root.transform.position = position;
            RunnerMapLayout layout = root.GetComponent<RunnerMapLayout>();
            layout.Initialize(catalog, index);
            for (int i = 0; i < catalog[index].layers.Length; i++)
            {
                RunnerMapCatalog.MapLayer spec = catalog[index].layers[i];
                string label = spec.order >= 0 ? "Ground - fixed reference" : Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(spec.source));
                var group = new GameObject((i + 1).ToString("00") + " - " + label, typeof(RunnerMapLayerLayout));
                group.transform.SetParent(root.transform, false);
                RunnerMapLayerLayout layer = group.GetComponent<RunnerMapLayerLayout>();
                layer.Initialize(layout, i);
                layout.AddLayer(layer);
                float stride = (spec.sprite.rect.width - 1f) / spec.sprite.pixelsPerUnit;
                int count = spec.stationary ? 1 : 3;
                for (int tile = 0; tile < count; tile++)
                {
                    var renderer = new GameObject("Preview tile " + tile, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                    renderer.transform.SetParent(group.transform, false);
                    renderer.transform.localPosition = new Vector3(spec.stationary ? 0f : (tile - 1.25f) * stride, 0f, 0f);
                    renderer.sprite = spec.sprite;
                    renderer.sharedMaterial = material;
                    renderer.sortingOrder = spec.order;
                    renderer.gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.NotEditable;
                }
            }
            Sprite soil = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Generated/TrailSoil/" + layout.MapId + "_soil.png");
            if (soil != null)
            {
                var renderer = new GameObject("Soil - fixed reference", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                renderer.transform.SetParent(root.transform, false);
                renderer.transform.localPosition = new Vector3(0f, RunnerSoilArt.Top, 0f);
                renderer.sprite = soil;
                renderer.sharedMaterial = material;
                renderer.sortingOrder = -5;
                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.tileMode = SpriteTileMode.Continuous;
                renderer.size = new Vector2(120f, 8f);
                renderer.gameObject.hideFlags = HideFlags.NotEditable;
            }
            return layout;
        }
    }
}
