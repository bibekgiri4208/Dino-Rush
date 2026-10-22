using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DinoRush.Editor
{
    /// <summary>Adds an editable pixel-art thought puff without rebuilding the existing menu.</summary>
    public static class ForestMenuThoughtBuilder
    {
        private const string ArtPath = "Assets/UI/ForestMenu/DinoThoughtPuff.png";

        [MenuItem("Dino Rush/Add Menu Dino Thoughts")]
        public static void Build()
        {
            if (Application.isPlaying) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestRun.unity");
            Apply(Object.FindAnyObjectByType<ForestRunnerGame>());
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Saved occasional title-dino thoughts. Submenus and gameplay remain silent.");
        }

        internal static void Apply(ForestRunnerGame game)
        {
            ForestMainMenu menu = game.GetComponentInChildren<ForestMainMenu>(true);
            Transform parent = menu.transform.Find("Main menu buttons");
            if (parent.Find("Dino thought puff") != null) return; // Preserve authored timing, lines and art.
            GenerateArt();
            RectTransform bubble = Rect("Dino thought puff", parent, new Vector2(272, 120));
            bubble.anchoredPosition = new Vector2(-364, -122);
            Image image = bubble.gameObject.AddComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath);
            image.raycastTarget = false;
            CanvasGroup group = bubble.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = group.blocksRaycasts = false;
            Text caption = Rect("Dino's thought", bubble, new Vector2(240, 72)).gameObject.AddComponent<Text>();
            caption.rectTransform.anchoredPosition = new Vector2(0, 12);
            caption.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Fonts/PressStart2P-Regular.ttf");
            caption.fontSize = 12;
            caption.lineSpacing = 1.6f;
            caption.text = "What a beautiful\nday!";
            caption.color = PixelMenuArt.Hex(0x193b36);
            caption.alignment = TextAnchor.MiddleCenter;
            caption.alignByGeometry = true;
            caption.horizontalOverflow = HorizontalWrapMode.Wrap;
            caption.verticalOverflow = VerticalWrapMode.Truncate;
            caption.raycastTarget = false;
            RunnerMenuThought thought = bubble.gameObject.AddComponent<RunnerMenuThought>();
            var data = new SerializedObject(thought);
            data.FindProperty("game").objectReferenceValue = game;
            data.FindProperty("dino").objectReferenceValue = game.transform.Find("Player/Blue dino player").GetComponent<SpriteRenderer>();
            data.FindProperty("worldCamera").objectReferenceValue = game.GetComponentInChildren<Camera>(true);
            data.FindProperty("caption").objectReferenceValue = caption;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(thought);
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            return rect;
        }

        private static void GenerateArt()
        {
            if (File.Exists(ArtPath)) return;
            var art = new PixelMenuArt.Pixels(136, 60);
            Color32 outline = PixelMenuArt.Hex(0x193b36);
            Color32 shade = PixelMenuArt.Hex(0xaecb98);
            Color32 cream = PixelMenuArt.Hex(0xf0f2cf);
            Cloud(art, 0, outline);
            Cloud(art, 2, shade);
            Cloud(art, 3, cream);
            art.Disc(64, 49, 4, outline);
            art.Disc(64, 49, 2, cream);
            art.Disc(68, 57, 2, outline);
            art.Disc(68, 57, 1, cream);
            var texture = new Texture2D(136, 60, TextureFormat.RGBA32, false);
            texture.SetPixels32(art.data);
            texture.Apply();
            File.WriteAllBytes(ArtPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(ArtPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(ArtPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        private static void Cloud(PixelMenuArt.Pixels art, int inset, Color32 color)
        {
            art.Rect(14, 3 + inset, 108, 40 - inset * 2, color);
            art.Rect(3 + inset, 14, 130 - inset * 2, 18, color);
            foreach (int x in new[] { 17, 37, 58, 79, 100, 118 })
            {
                art.Disc(x, 14, 13 - inset, color);
                art.Disc(x, 31, 13 - inset, color);
            }
        }
    }
}
