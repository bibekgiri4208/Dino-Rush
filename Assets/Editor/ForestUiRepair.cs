using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DinoRush.Editor
{
    /// <summary>Repairs the existing hierarchy in place; no scene objects or artwork are replaced.</summary>
    public static class ForestUiRepair
    {
        [MenuItem("Dino Rush/Repair UI Layout")]
        public static void RepairScene()
        {
            if (Application.isPlaying) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestRun.unity");
            ForestRunnerGame game = Object.FindAnyObjectByType<ForestRunnerGame>();
            Repair(game);
            game.AssignMainMenu(game.GetComponentInChildren<ForestMainMenu>(true));
            EditorUtility.SetDirty(game);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Repaired forest UI: sliced frames, bounded labels, separated icons, modal layout and editor preview states.");
        }

        public static void Repair(ForestRunnerGame game)
        {
            ForestMainMenu menu = game.GetComponentInChildren<ForestMainMenu>(true);
            if (menu == null) throw new System.InvalidOperationException("Forest main menu is missing.");
            Canvas canvas = game.GetComponentInChildren<Canvas>(true);
            canvas.pixelPerfect = true;
            EditorUtility.SetDirty(canvas);
            SetBorder("ButtonPlay", new Vector4(24, 10, 24, 12));
            SetBorder("ButtonForest", new Vector4(24, 10, 24, 12));
            SetBorder("SoundFrame", new Vector4(6, 6, 6, 6));
            SetBorder("DialogFrame", new Vector4(14, 14, 14, 14));

            Transform root = menu.transform;
            Rect(root.Find("Dino Rush Logo/Mossy stone and amber logo"), 0, 188, 578, 282);
            root.Find("Dino Rush Logo/Mossy stone and amber logo").GetComponent<Image>().preserveAspect = true;
            Rect(root.Find("Dino Rush Logo/Forest subtitle plaque"), 0, 49, 388, 34);
            TextLayout(root.Find("Dino Rush Logo/Forest subtitle plaque/Forest subtitle"), 0, 0, 376, 30, 12);

            RepairButton(root.Find("Main menu buttons/Play"), 0, -12, 360, 76, 32);
            RepairButton(root.Find("Main menu buttons/Customize"), 0, -90, 360, 68, 24);
            RepairButton(root.Find("Main menu buttons/Options"), 0, -164, 360, 68, 24);
            RepairButton(root.Find("Main menu buttons/Quit"), 0, -238, 360, 68, 24);

            Transform sound = root.Find("SFX toggle");
            Rect(sound, 568, 296, 88, 88);
            Sliced(sound.GetComponent<Image>());
            Rect(sound.Find("Speaker icon"), 0, 14, 36, 36);
            sound.Find("Speaker icon").GetComponent<Image>().preserveAspect = true;
            TextLayout(sound.Find("SFX label"), 0, -23, 72, 28, 12);

            Transform card = root.Find("Menu dialog/Moss framed dialog");
            Rect(card, 0, 0, 560, 400);
            Sliced(card.GetComponent<Image>());
            TextLayout(card.Find("Dialog heading"), 0, 136, 500, 56, 24);
            RepairButton(card.Find("Back to main menu"), 0, -142, 320, 64, 24);

            Transform custom = card.Find("Customize page");
            Rect(custom, 0, 0, 500, 224);
            Rect(custom.Find("Selected blue dino"), -148, 24, 96, 96);
            custom.Find("Selected blue dino").GetComponent<Image>().preserveAspect = true;
            TextLayout(custom.Find("Dino selection"), 64, 58, 280, 44, 20);
            TextLayout(custom.Find("Map selection"), 64, 14, 280, 36, 16);
            TextLayout(custom.Find("Selected status"), 64, -26, 280, 34, 16);
            TextLayout(custom.Find("Single adventure"), 0, -78, 480, 50, 12);

            Transform options = card.Find("Options page");
            Rect(options, 0, 0, 500, 224);
            RepairButton(options.Find("Toggle sound effects"), 0, 44, 360, 68, 24);
            TextLayout(options.Find("Controls help"), 0, -49, 480, 90, 12);

            Transform quit = card.Find("Quit confirmation page");
            Rect(quit, 0, 0, 500, 224);
            TextLayout(quit.Find("Quit question"), 0, 48, 480, 76, 16);
            RepairButton(quit.Find("Confirm quit"), 0, -36, 360, 68, 24);

            // An inspector preview must never display all three pages on top of one another.
            custom.gameObject.SetActive(false);
            options.gameObject.SetActive(true);
            quit.gameObject.SetActive(false);

            foreach (Image image in menu.GetComponentsInChildren<Image>(true))
            {
                image.raycastTarget = image.GetComponent<Button>() != null || image.name == "Dialog scrim";
                EditorUtility.SetDirty(image);
            }
            foreach (Text text in menu.GetComponentsInChildren<Text>(true))
            {
                text.raycastTarget = false;
                text.resizeTextForBestFit = false;
                text.supportRichText = false;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Truncate;
                // This pixel font's line advance is smaller than its visible capitals.
                // Extra leading prevents lines from drawing through each other.
                text.lineSpacing = text.text.Contains("\n") ? 1.65f : 1f;
                Shadow shadow = text.GetComponent<Shadow>();
                if (shadow != null) shadow.effectDistance = new Vector2(1, -1);
                EditorUtility.SetDirty(text);
            }
            Canvas.ForceUpdateCanvases();
        }

        private static void RepairButton(Transform button, float x, float y, float width, float height, int fontSize)
        {
            Rect(button, x, y, width, height);
            Sliced(button.GetComponent<Image>());
            Transform icon = button.Find("Icon");
            if (icon != null)
            {
                Rect(icon, -width * 0.5f + 62, 0, 28, 28);
                icon.GetComponent<Image>().preserveAspect = true;
            }
            float labelWidth = icon == null ? width - 64 : width - 116;
            TextLayout(button.Find("Label"), icon == null ? 0 : 30, 0, labelWidth, height - 16, fontSize);
        }

        private static void Sliced(Image image)
        {
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 0.5f;
            image.fillCenter = true;
            image.preserveAspect = false;
        }

        private static void SetBorder(string asset, Vector4 border)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath($"{PixelMenuArt.Directory}/{asset}.png");
            if (importer == null) throw new System.InvalidOperationException("Missing menu frame: " + asset);
            if (importer.spriteBorder == border) return;
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }

        private static void TextLayout(Transform transform, float x, float y, float width, float height, int fontSize)
        {
            Rect(transform, x, y, width, height);
            Text text = transform.GetComponent<Text>();
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.lineSpacing = 1f;
            text.alignByGeometry = true;
        }

        private static void Rect(Transform transform, float x, float y, float width, float height)
        {
            if (transform == null) throw new System.InvalidOperationException("An expected UI object is missing; repair stopped instead of replacing it.");
            var rect = (RectTransform)transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            EditorUtility.SetDirty(rect);
        }
    }
}
