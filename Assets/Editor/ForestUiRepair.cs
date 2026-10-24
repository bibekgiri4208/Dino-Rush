using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DinoRush.Editor
{
    /// <summary>Repairs the existing hierarchy in place; no scene objects or artwork are replaced.</summary>
    public static class ForestUiRepair
    {
        [MenuItem("Dino Rush/Compact Utility Menus")]
        public static void CompactUtilityMenus()
        {
            if (Application.isPlaying) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestRun.unity");
            ForestRunnerGame game = Object.FindAnyObjectByType<ForestRunnerGame>();
            ForestMainMenu menu = game.GetComponentInChildren<ForestMainMenu>(true);
            Transform panel = menu.transform.Find("Menu dialog/Moss framed dialog");
            var available = new Vector2(1280, 720);
            RunnerCompactMenus.ApplyDialog(panel, RunnerCompactMenus.DialogKind.Quit, false, available);
            menu.ApplyDialogLayout(false, available);
            RunnerCompactMenus.ApplyPause(game.transform.Find("Forest runner UI/16 by 9 safe content/Forest Pause Menu"), false, available);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Saved compact Quit, Options/SFX and Pause menus. Shop, hatch reveal, results and map artwork were preserved.");
        }

        [MenuItem("Dino Rush/Fix Main Menu Buttons")]
        public static void RepairButtonsScene()
        {
            if (Application.isPlaying) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestRun.unity");
            ForestMainMenu menu = Object.FindAnyObjectByType<ForestMainMenu>();
            if (menu == null) throw new System.InvalidOperationException("Forest main menu is missing.");
            PixelMenuArt.RebuildButtonFrames();
            RepairMainMenuButtons(menu.transform);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        [MenuItem("Dino Rush/Move Quit to Menu Corner")]
        public static void MoveQuitToCorner()
        {
            if (Application.isPlaying) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestRun.unity");
            var menu = Object.FindAnyObjectByType<ForestMainMenu>();
            if (menu == null) throw new System.InvalidOperationException("Forest main menu is missing.");
            menu.EnsureCornerQuitUi();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }

        [MenuItem("Dino Rush/Repair UI Layout")]
        public static void RepairScene()
        {
            if (Application.isPlaying) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestRun.unity");
            ForestRunnerGame game = Object.FindAnyObjectByType<ForestRunnerGame>();
            Repair(game);
            ForestUiMotionBuilder.Apply(game);
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
            SetBorder("ButtonPlay", new Vector4(24, 10, 24, 10));
            SetBorder("ButtonForest", new Vector4(24, 10, 24, 10));
            SetBorder("SoundFrame", new Vector4(6, 6, 6, 6));
            SetBorder("DialogFrame", new Vector4(14, 14, 14, 14));

            Transform root = menu.transform;
            Transform cornerBranches = root.Find("Forest framing - canopy, log, rocks and flowers");
            if (cornerBranches != null)
            {
                cornerBranches.gameObject.SetActive(false);
                EditorUtility.SetDirty(cornerBranches.gameObject);
            }
            Rect(root.Find("Dino Rush Logo/Mossy stone and amber logo"), 0, 188, 578, 282);
            root.Find("Dino Rush Logo/Mossy stone and amber logo").GetComponent<Image>().preserveAspect = true;
            Rect(root.Find("Dino Rush Logo/Forest subtitle plaque"), 0, 49, 388, 34);
            TextLayout(root.Find("Dino Rush Logo/Forest subtitle plaque/Forest subtitle"), 0, 0, 376, 30, 12);

            RepairMainMenuButtons(root);

            Transform sound = root.Find("SFX toggle");
            Rect(sound, 568, 296, 88, 88);
            Sliced(sound.GetComponent<Image>());
            Rect(sound.Find("Speaker icon"), 0, 14, 36, 36);
            sound.Find("Speaker icon").GetComponent<Image>().preserveAspect = true;
            TextLayout(sound.Find("SFX label"), 0, -23, 72, 28, 12);
            menu.EnsureCornerQuitUi();

            Transform card = root.Find("Menu dialog/Moss framed dialog");
            Rect(card, 0, 0, 560, 400);
            Sliced(card.GetComponent<Image>());
            TextLayout(card.Find("Dialog heading"), 0, 136, 500, 56, 24);
            RepairButton(card.Find("Back to main menu"), 0, -142, 320, 64, 24);

            Transform custom = card.Find("Customize page");
            Rect(custom, 0, 0, 500, 224);
            if (custom.Find("Dino choices") != null) ForestDinoSelectorBuilder.RepairLayout(custom);
            else
            {
                Rect(custom.Find("Selected blue dino"), -148, 24, 96, 96);
                custom.Find("Selected blue dino").GetComponent<Image>().preserveAspect = true;
                TextLayout(custom.Find("Dino selection"), 64, 58, 280, 44, 20);
                TextLayout(custom.Find("Map selection"), 64, 14, 280, 36, 16);
                TextLayout(custom.Find("Selected status"), 64, -26, 280, 34, 16);
                TextLayout(custom.Find("Single adventure"), 0, -78, 480, 50, 12);
            }

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
            ForestShopPresentationBuilder.Apply(menu);
            EditorUtility.SetDirty(menu);
            Canvas.ForceUpdateCanvases();
        }

        private static void RepairMainMenuButtons(Transform root)
        {
            bool missions = root.Find("Main menu buttons/Missions") != null;
            string[] names = missions ? new[] { "Play", "Shop", "Missions", "Options" } : new[] { "Play", "Shop", "Options" };
            for (int i = 0; i < names.Length; i++)
            {
                Transform button = root.Find("Main menu buttons/" + names[i]);
                if (button == null && names[i] == "Shop")
                {
                    button = root.Find("Main menu buttons/Customize");
                    button.name = "Shop";
                }
                if (names[i] == "Shop") button.Find("Label").GetComponent<Text>().text = "SHOP";
                RepairButton(button, 0, -12 - i * (missions ? 74 : 78), 360, 68, 24);
                Transform label = button.Find("Label");
                TextLayout(label, 32, 0, 240, 52, 24);
                Text text = label.GetComponent<Text>();
                text.resizeTextForBestFit = false;
                text.supportRichText = false;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Truncate;
                ButtonLetterSpacing tracking = label.GetComponent<ButtonLetterSpacing>();
                if (tracking == null) tracking = label.gameObject.AddComponent<ButtonLetterSpacing>();
                // Center the visible icon and word together, not in independent columns.
                // Texture padding and the font's trailing advance are not visible gaps.
                text.rectTransform.sizeDelta = new Vector2(Mathf.Ceil(tracking.PreferredWidth) + 2, 52);
                Vector2 caption = CaptionBounds(text, tracking.Spacing);
                Transform icon = button.Find("Icon");
                if (names[i] == "Missions")
                {
                    text.rectTransform.anchoredPosition = Vector2.zero;
                    EditorUtility.SetDirty(text.rectTransform); EditorUtility.SetDirty(text);
                    continue;
                }
                Vector2 artwork = IconBounds(icon.GetComponent<Image>());
                const float gap = 12;
                float left = -(artwork.y - artwork.x + gap + caption.y - caption.x) * 0.5f;
                ((RectTransform)icon).anchoredPosition = new Vector2(left - artwork.x, 0);
                text.rectTransform.anchoredPosition = new Vector2(left + artwork.y - artwork.x + gap - caption.x, 0);
                EditorUtility.SetDirty(icon);
                EditorUtility.SetDirty(text.rectTransform);
                EditorUtility.SetDirty(text);
            }
            root.GetComponent<ForestMainMenu>()?.EnsureCornerQuitUi();
            Canvas.ForceUpdateCanvases();
        }

        private static Vector2 CaptionBounds(Text text, float spacing)
        {
            using (var generator = new TextGenerator())
            {
                TextGenerationSettings settings = text.GetGenerationSettings(text.rectTransform.rect.size);
                settings.scaleFactor = 1;
                generator.Populate(text.text, settings);
                var bounds = new Vector2(float.PositiveInfinity, float.NegativeInfinity);
                var vertices = generator.verts;
                int count = vertices.Count / 4;
                for (int i = 0; i < vertices.Count; i++)
                {
                    float x = vertices[i].position.x + (i / 4 - (count - 1) * 0.5f) * spacing;
                    bounds.x = Mathf.Min(bounds.x, x);
                    bounds.y = Mathf.Max(bounds.y, x);
                }
                return bounds;
            }
        }

        private static Vector2 IconBounds(Image icon)
        {
            var texture = new Texture2D(2, 2);
            try
            {
                texture.LoadImage(System.IO.File.ReadAllBytes(AssetDatabase.GetAssetPath(icon.sprite.texture)));
                Rect sprite = icon.sprite.rect;
                int min = (int)sprite.xMax, max = (int)sprite.xMin;
                for (int y = (int)sprite.yMin; y < sprite.yMax; y++)
                for (int x = (int)sprite.xMin; x < sprite.xMax; x++)
                    if (texture.GetPixel(x, y).a > 0)
                    {
                        min = Mathf.Min(min, x);
                        max = Mathf.Max(max, x + 1);
                    }
                float width = icon.rectTransform.rect.width;
                return new Vector2((min - sprite.xMin) / sprite.width * width - width * 0.5f,
                    (max - sprite.xMin) / sprite.width * width - width * 0.5f);
            }
            finally { Object.DestroyImmediate(texture); }
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
