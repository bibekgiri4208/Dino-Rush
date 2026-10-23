using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DinoRush.Editor
{
    public static class RunnerMapLayoutEditing
    {
        public static void SaveToCatalog(this RunnerMapLayout layout, bool saveAssets = true)
        {
            if (Application.isPlaying) throw new InvalidOperationException("Edit map layouts outside Play mode.");
            if (layout.Map.id != layout.MapId || layout.Layers.Count != layout.Map.layers.Length)
                throw new InvalidOperationException("The catalog structure changed. Rebuild this layout before saving.");
            // Validate everything before changing any catalog entry.
            foreach (RunnerMapLayerLayout layer in layout.Layers)
            {
                if (layer.Spec.order >= 0) continue;
                float bottom = layer.transform.localPosition.y, height = layer.transform.localScale.y;
                if (float.IsNaN(bottom) || float.IsInfinity(bottom) || float.IsNaN(height) || float.IsInfinity(height) || height <= 0f)
                    throw new InvalidOperationException("Layer positions must be finite and heights must be positive.");
                if (layer.transform.localRotation != Quaternion.identity)
                    throw new InvalidOperationException("Map layers cannot rotate. Use Position Y and Height instead.");
            }
            Undo.RecordObject(layout.Catalog, "Save map layer layout");
            foreach (RunnerMapLayerLayout layer in layout.Layers)
            {
                RunnerMapCatalog.MapLayer spec = layer.Spec;
                if (spec.order >= 0) continue; // The shared gameplay surface is fixed.
                spec.bottom = layer.transform.localPosition.y;
                spec.height = layer.transform.localScale.y;
                if (spec.stationary) spec.x = layer.transform.localPosition.x;
            }
            EditorUtility.SetDirty(layout.Catalog);
            layout.ReloadFromCatalog();
            if (!saveAssets) return;
            layout.Map.preview = RunnerMapArt.Preview(layout.Map, true);
            EditorUtility.SetDirty(layout.Catalog);
            AssetDatabase.SaveAssetIfDirty(layout.Catalog);
            EditorSceneManager.SaveScene(layout.gameObject.scene);
            Debug.Log("Saved " + layout.Map.displayName + " layer positions/heights to the gameplay catalog.", layout);
        }

        public static void ReloadFromCatalog(this RunnerMapLayout layout)
        {
            foreach (RunnerMapLayerLayout layer in layout.Layers)
            {
                Undo.RecordObject(layer.transform, "Reload map layer layout");
                layer.ResetFraming();
            }
            EditorSceneManager.MarkSceneDirty(layout.gameObject.scene);
        }

        public static void FocusMap(this RunnerMapLayout layout)
        {
            Vector3 center = layout.transform.position;
            foreach (GameObject root in layout.gameObject.scene.GetRootGameObjects())
            {
                Camera camera = root.GetComponent<Camera>();
                if (camera == null) continue;
                Undo.RecordObject(camera.transform, "Preview map layout");
                Undo.RecordObject(camera, "Preview map layout");
                camera.transform.position = center + new Vector3(0f, 0f, -10f);
                camera.backgroundColor = layout.Map.skyColor;
            }
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView view = SceneView.lastActiveSceneView;
                view.in2DMode = true;
                view.LookAt(center, Quaternion.identity, 7f, true);
            }
            EditorSceneManager.MarkSceneDirty(layout.gameObject.scene);
        }
    }

    [CustomEditor(typeof(RunnerMapLayout))]
    internal sealed class RunnerMapLayoutInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var layout = (RunnerMapLayout)target;
            EditorGUILayout.LabelField(layout.Map.displayName + " scenery", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Expand this map in the Hierarchy, select a layer, and move it vertically with W or edit its Height. Click Save Layout to Game when finished. Ctrl+S alone saves only this preview scene. Ground is a fixed reference.", MessageType.Info);
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                if (GUILayout.Button("Preview This Map")) layout.FocusMap();
                if (GUILayout.Button("Save Layout to Game")) layout.SaveToCatalog();
                if (GUILayout.Button("Reload Saved Layout") && EditorUtility.DisplayDialog("Reload map layout?",
                    "Discard unsaved positioning in this map preview and reload its catalog values?", "Reload", "Cancel"))
                    layout.ReloadFromCatalog();
            }
        }
    }

    [CustomEditor(typeof(RunnerMapLayerLayout))]
    internal sealed class RunnerMapLayerLayoutInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var layer = (RunnerMapLayerLayout)target;
            EditorGUILayout.LabelField(layer.Layout.Map.displayName + " / " + layer.Spec.sprite.name, EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true)) EditorGUILayout.ObjectField("Source Artwork", layer.Spec.source, typeof(Texture2D), false);
            bool ground = layer.Spec.order >= 0;
            if (ground) EditorGUILayout.HelpBox("The ground stays at the shared gameplay collision height. Move scenery layers instead.", MessageType.Info);
            using (new EditorGUI.DisabledScope(ground || Application.isPlaying))
            {
                EditorGUI.BeginChangeCheck();
                float bottom = EditorGUILayout.FloatField("Bottom / Position Y", layer.transform.localPosition.y);
                float height = EditorGUILayout.FloatField("Height", layer.transform.localScale.y);
                float x = layer.Spec.stationary ? EditorGUILayout.FloatField("Position X", layer.transform.localPosition.x) : 0f;
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(layer.transform, "Adjust map layer");
                    layer.transform.localPosition = new Vector3(x, bottom, 0f);
                    layer.transform.localScale = new Vector3(Mathf.Max(0.01f, height), Mathf.Max(0.01f, height), 1f);
                }
            }
            EditorGUILayout.HelpBox("Moving this layer moves all three scrolling copies together. Save Layout to Game applies the whole map, including edits to other layers.", MessageType.None);
            using (new EditorGUI.DisabledScope(Application.isPlaying))
                if (GUILayout.Button("Save Layout to Game")) layer.Layout.SaveToCatalog();
        }
    }
}
