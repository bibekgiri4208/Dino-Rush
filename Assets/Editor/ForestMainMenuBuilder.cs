using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DinoRush.Editor
{
    public static class ForestMainMenuBuilder
    {
        private static readonly Color Cream = PixelMenuArt.Hex(0xf0f2cf);
        private static readonly Color Sage = PixelMenuArt.Hex(0xaecb98);
        private static Font font;

        [MenuItem("Dino Rush/Refine Title Logo")]
        public static void RefineTitleLogo()
        {
            if (Application.isPlaying) return;
            PixelMenuArt.RebuildLogo();
            Debug.Log("Refined only the title logo's leafy edges; scene layout and other menu artwork were preserved.");
        }

        [MenuItem("Dino Rush/Apply Forest Main Menu")]
        public static void Build()
        {
            if (Application.isPlaying) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestRun.unity");
            ForestRunnerGame game = Object.FindAnyObjectByType<ForestRunnerGame>();
            if (!game.HasAuthoredScene) throw new System.InvalidOperationException("Bake the forest scene before adding its menu.");
            PixelMenuArt.Generate();
            ForestMainMenu existing = game.GetComponentInChildren<ForestMainMenu>(true);
            if (existing != null)
            {
                // Refresh framing without recreating or replacing any authored UI objects.
                ForestDinoSelectorBuilder.Apply(game, existing);
                if (game.MapCatalog != null) ForestMapSelectorBuilder.Apply(game);
                ForestMenuThoughtBuilder.Apply(game);
                ForestUiRepair.Repair(game);
                ForestUiMotionBuilder.Apply(game);
                game.AssignMainMenu(existing);
                EditorUtility.SetDirty(game);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("The forest menu already exists; its UI objects and artwork were preserved.");
                return;
            }
            font = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Fonts/PressStart2P-Regular.ttf");
            if (font == null) throw new System.InvalidOperationException("The licensed Press Start 2P font is missing.");
            Transform content = game.transform.Find("Forest runner UI/16 by 9 safe content");
            RectTransform menuRoot = Rect("Forest Main Menu", content, Vector2.zero, new Vector2(1280, 720));
            ForestMainMenu menu = menuRoot.gameObject.AddComponent<ForestMainMenu>();

            RectTransform logo = Rect("Dino Rush Logo", menuRoot, Vector2.zero, new Vector2(1280, 720));
            ImageArt("Mossy stone and amber logo", logo, "DinoRushLogo", new Vector2(0, 177), new Vector2(588, 287));
            Image plaque = ImageArt("Forest subtitle plaque", logo, null, new Vector2(0, 52), new Vector2(378, 32));
            plaque.color = PixelMenuArt.Hex(0x082d36);
            Label("Forest subtitle", plaque.transform, "- FOREST / ENDLESS RUNNER -", Vector2.zero, new Vector2(366, 24), 12, Sage);

            RectTransform choices = Rect("Main menu buttons", menuRoot, Vector2.zero, new Vector2(1280, 720));
            Button play = MenuButton("Play", choices, new Vector2(0, -12), "PLAY", "Play", true);
            Button customize = MenuButton("Customize", choices, new Vector2(0, -92), "CUSTOMIZE", "Dino");
            Button options = MenuButton("Options", choices, new Vector2(0, -166), "OPTIONS", "Gear");
            Button quit = MenuButton("Quit", choices, new Vector2(0, -240), "QUIT", "Quit");

            Image soundFrame = ImageArt("SFX toggle", menuRoot, "SoundFrame", new Vector2(576, 300), new Vector2(72, 72));
            Button sound = Interactive(soundFrame);
            Image speaker = ImageArt("Speaker icon", soundFrame.transform, "IconSpeaker", new Vector2(0, 8), new Vector2(34, 34));
            Text soundCaption = Label("SFX label", soundFrame.transform, "SFX", new Vector2(0, -21), new Vector2(64, 18), 11, Sage);

            RectTransform dialog = Rect("Menu dialog", menuRoot, Vector2.zero, new Vector2(1280, 720));
            Image dim = ImageArt("Dialog scrim", dialog, null, Vector2.zero, new Vector2(1280, 720));
            dim.color = new Color(0.015f, 0.06f, 0.065f, 0.6f);
            dim.raycastTarget = true;
            Image card = ImageArt("Moss framed dialog", dialog, "DialogFrame", Vector2.zero, new Vector2(520, 376));
            Text heading = Label("Dialog heading", card.transform, "OPTIONS", new Vector2(0, 134), new Vector2(460, 40), 19, Cream);
            Button back = MenuButton("Back to main menu", card.transform, new Vector2(0, -131), "BACK", null, false, new Vector2(280, 60));

            RectTransform customPage = Rect("Customize page", card.transform, Vector2.zero, new Vector2(480, 220));
            Sprite dino = game.transform.Find("Player/Blue dino player").GetComponent<SpriteRenderer>().sprite;
            Image portrait = ImageArt("Selected blue dino", customPage, null, new Vector2(-140, 20), new Vector2(108, 108));
            portrait.sprite = dino;
            portrait.color = Color.white;
            portrait.preserveAspect = true;
            Label("Dino selection", customPage, "BLUE DINO", new Vector2(57, 57), new Vector2(260, 30), 16, Cream);
            Label("Map selection", customPage, "MAP: FOREST", new Vector2(57, 20), new Vector2(270, 28), 14, Sage);
            Label("Selected status", customPage, "SELECTED", new Vector2(57, -17), new Vector2(260, 26), 12, PixelMenuArt.Hex(0xd1df8e));
            Label("Single adventure", customPage, "ONE DINO. ONE FOREST.\nENDLESS ADVENTURES.", new Vector2(0, -72), new Vector2(445, 42), 11, Sage);

            RectTransform optionsPage = Rect("Options page", card.transform, Vector2.zero, new Vector2(480, 220));
            Button optionsSound = MenuButton("Toggle sound effects", optionsPage, new Vector2(0, 45), "SFX: ON", "Speaker", false, new Vector2(330, 64));
            Text optionsSoundCaption = optionsSound.transform.Find("Label").GetComponent<Text>();
            Label("Controls help", optionsPage, "SPACE / UP / W : JUMP\nCLICK OR TAP : JUMP\nDOWN / S : DUCK / FAST DROP\nESC / P : PAUSE     M : SFX", new Vector2(0, -49), new Vector2(480, 90), 12, Sage);

            RectTransform quitPage = Rect("Quit confirmation page", card.transform, Vector2.zero, new Vector2(480, 220));
            Label("Quit question", quitPage, "LEAVING THE FOREST?\nYOUR BEST SCORE IS SAVED.", new Vector2(0, 45), new Vector2(460, 60), 12, Sage);
            Button confirmQuit = MenuButton("Confirm quit", quitPage, new Vector2(0, -35), "QUIT GAME", "Quit", false, new Vector2(330, 64));

            Set(menu, "play", play); Set(menu, "customize", customize); Set(menu, "options", options); Set(menu, "quit", quit);
            Set(menu, "sound", sound); Set(menu, "back", back); Set(menu, "confirmQuit", confirmQuit); Set(menu, "optionsSound", optionsSound);
            Set(menu, "choices", choices.gameObject); Set(menu, "logo", logo.gameObject); Set(menu, "dialog", dialog.gameObject);
            Set(menu, "customizePage", customPage.gameObject); Set(menu, "optionsPage", optionsPage.gameObject); Set(menu, "quitPage", quitPage.gameObject);
            Set(menu, "speaker", speaker); Set(menu, "speakerOn", PixelMenuArt.Load("IconSpeaker")); Set(menu, "speakerOff", PixelMenuArt.Load("IconMuted"));
            Set(menu, "soundCaption", soundCaption); Set(menu, "optionsSoundCaption", optionsSoundCaption); Set(menu, "dialogTitle", heading);
            var legacy = new List<GameObject>();
            foreach (Transform child in content) if (child != menuRoot) legacy.Add(child.gameObject);
            var serialized = new SerializedObject(menu);
            SerializedProperty chrome = serialized.FindProperty("gameplayChrome");
            chrome.arraySize = legacy.Count;
            for (int i = 0; i < legacy.Count; i++) chrome.GetArrayElementAtIndex(i).objectReferenceValue = legacy[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            menu.SetSound(false);
            ForestDinoSelectorBuilder.Apply(game, menu);
            if (game.MapCatalog != null) ForestMapSelectorBuilder.Apply(game);
            game.AssignMainMenu(menu);
            ForestUiRepair.Repair(game);
            ForestUiMotionBuilder.Apply(game);
            ForestMenuThoughtBuilder.Apply(game);
            EditorUtility.SetDirty(menu);
            EditorUtility.SetDirty(game);
            Canvas.ForceUpdateCanvases();
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Reference-inspired pixel-art forest menu saved in ForestRun, with working Play, Customize, Options, Quit and SFX controls.");
        }

        private static void Set(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Button MenuButton(string name, Transform parent, Vector2 position, string caption, string icon,
            bool primary = false, Vector2 size = default)
        {
            if (size == Vector2.zero) size = primary ? new Vector2(354, 76) : new Vector2(350, 68);
            Image background = ImageArt(name, parent, primary ? "ButtonPlay" : "ButtonForest", position, size);
            Button button = Interactive(background);
            if (icon != null)
                ImageArt("Icon", background.transform, "Icon" + icon, new Vector2(-size.x * 0.285f, 3), primary ? new Vector2(32, 36) : new Vector2(34, 34));
            Label("Label", background.transform, caption, new Vector2(icon == null ? 0 : 27, 2), new Vector2(size.x - 85, 42), primary ? 27 : 20, Cream);
            return button;
        }

        private static Button Interactive(Image image)
        {
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 0.83f);
            colors.pressedColor = new Color(0.74f, 0.84f, 0.7f);
            colors.fadeDuration = 0.07f;
            button.colors = colors;
            return button;
        }

        private static Image ImageArt(string name, Transform parent, string art, Vector2 position, Vector2 size)
        {
            Image image = Rect(name, parent, position, size).gameObject.AddComponent<Image>();
            if (art != null) image.sprite = PixelMenuArt.Load(art);
            image.raycastTarget = false;
            return image;
        }

        private static Text Label(string name, Transform parent, string caption, Vector2 position, Vector2 size, int pointSize, Color color)
        {
            Text label = Rect(name, parent, position, size).gameObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = pointSize;
            label.text = caption;
            label.color = color;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            Shadow shadow = label.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.03f, 0.12f, 0.13f, 0.6f);
            shadow.effectDistance = new Vector2(1, -2);
            return label;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }
    }
}
