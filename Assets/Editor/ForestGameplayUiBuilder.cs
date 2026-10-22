using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DinoRush.Editor
{
    /// <summary>Styles the saved HUD in place and authors an editable forest pause menu.</summary>
    public static class ForestGameplayUiBuilder
    {
        private static readonly Color Cream = PixelMenuArt.Hex(0xf0f2cf);
        private static readonly Color Sage = PixelMenuArt.Hex(0xaecb98);
        private static Font font;

        [MenuItem("Dino Rush/Apply Gameplay UI Theme")]
        public static void Build()
        {
            if (Application.isPlaying) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestRun.unity");
            ForestRunnerGame game = Object.FindAnyObjectByType<ForestRunnerGame>();
            if (game == null || !game.HasAuthoredScene) throw new System.InvalidOperationException("Open the authored forest scene first.");
            PixelMenuArt.Generate();
            ForestHudArt.Generate();
            font = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Fonts/PressStart2P-Regular.ttf");
            if (font == null) throw new System.InvalidOperationException("The forest pixel font is missing.");
            Transform root = game.transform.Find("Forest runner UI/16 by 9 safe content");
            ForestMainMenu main = game.GetComponentInChildren<ForestMainMenu>(true);
            if (main == null) throw new System.InvalidOperationException("Apply the forest main menu first.");

            Transform badge = root.Find("Top left badge");
            Position(badge, -446, 292, 316, 80);
            HudFrame(badge, "HudPanel");
            badge.Find("Badge accent").gameObject.SetActive(false);
            HudArt("Forest pine", badge, "HudPine", -116, 0, 48, 66);
            Label(badge.Find("Brand"), "FOREST RUN", 35, 13, 216, 30, 20, Cream, TextAnchor.MiddleLeft);
            Label(badge.Find("Map"), "ENDLESS", 35, -17, 216, 26, 16, Sage, TextAnchor.MiddleLeft);
            Moss(badge, 316, 80, true);

            Transform scores = root.Find("Scoreboard");
            Position(scores, 398, 254, 260, 148);
            scores.GetComponent<Image>().enabled = false;
            ScoreCard(scores, "Score card", "Score", "SCORE", "HudTrophy", 40);
            ScoreCard(scores, "Best card", "Best", "BEST", "HudCrown", -40);
            scores.Find("Divider").gameObject.SetActive(false);

            Transform sound = root.Find("Sound");
            Position(sound, 584, 294, 60, 60);
            HudFrame(sound, "HudControl");
            sound.Find("Sound label").gameObject.SetActive(false);
            Image speaker = HudArt("Sound icon", sound, "HudSpeaker", 0, 0, 36, 36);
            Transform pause = root.Find("Pause");
            Position(pause, 584, 214, 60, 60);
            HudFrame(pause, "HudControl");
            pause.Find("Pause label").gameObject.SetActive(false);
            HudArt("Pause icon", pause, "HudPause", 0, 0, 36, 36);

            Transform jump = root.Find("Jump");
            Position(jump, 476, -276, 248, 88);
            HudFrame(jump, "HudJump");
            HudArt("Icon", jump, "HudJumpArrow", -66, 0, 44, 44);
            Text jumpCaption = jump.GetComponentInChildren<Text>(true);
            Label(jumpCaption.transform, "JUMP", 28, 0, 136, 48, 28);
            Moss(jump, 248, 88, true);
            Transform statsFrame = Node("Trail stats frame", root, -430, -294, 340, 42);
            HudFrame(statsFrame, "HudPace");
            HudArt("Running dino", statsFrame, "HudDino", -144, 0, 28, 30);
            Transform meter = Node("Pace meter", statsFrame, -28, 0, 180, 14);
            HudFrame(meter, "HudMeter");
            var segments = new Image[10];
            for (int i = 0; i < segments.Length; i++)
            {
                Transform segment = Node("Pace segment " + (i + 1), meter, -72 + i * 16, 0, 14, 8);
                Image fill = segment.GetComponent<Image>();
                if (fill == null) fill = segment.gameObject.AddComponent<Image>();
                fill.raycastTarget = false;
                fill.color = i < 6 ? PixelMenuArt.Hex(0xb8d957) : PixelMenuArt.Hex(0x1a352e);
                segments[i] = fill;
            }
            Transform stats = root.Find("Run stats") ?? statsFrame.Find("Run stats");
            stats.SetParent(statsFrame, false);
            Label(stats, "1.0x", 120, 0, 72, 28, 16);
            Moss(statsFrame, 340, 42);
            statsFrame.SetSiblingIndex(root.Find("Menu scrim").GetSiblingIndex());
            Transform guide = KeyboardGuide(root);
            Image bottomFade = HudArt("Gameplay bottom fade", root, "HudBottomFade", 0, -280, 1280, 160);
            bottomFade.preserveAspect = false;
            bottomFade.transform.SetSiblingIndex(0);
            Label(root.Find("Controls"), "", 0, -334, 960, 24, 10, Sage);
            Label(root.Find("Milestone toast"), "", 0, 172, 1000, 48, 24);

            Image scrim = root.Find("Menu scrim").GetComponent<Image>();
            scrim.color = new Color(0.015f, 0.06f, 0.065f, 0.68f);
            scrim.raycastTarget = true;
            Transform results = root.Find("Menu card");
            Position(results, 0, 0, 584, 456);
            Frame(results, "DialogFrame");
            Outline outline = results.GetComponent<Outline>();
            if (outline != null) outline.enabled = false;
            results.Find("Card accent").gameObject.SetActive(false);
            Label(results.Find("Eyebrow"), "A NEW PERSONAL BEST!", 0, 174, 520, 30, 12, Sage);
            Label(results.Find("Title"), "NICE RUN!", 0, 120, 520, 56, 32);
            Label(results.Find("Description"), "SCORE 000000 / BEST 000000\nONE MORE JUMP. ONE MORE TRY.", 0, 48, 520, 76, 16, Sage);
            Label(results.Find("Result"), "YOUR FURTHEST ADVENTURE YET!", 0, -14, 520, 30, 12, Sage);
            Transform retry = results.Find("Primary action");
            Position(retry, 0, -78, 360, 76);
            StyleButton(retry.GetComponent<Button>(), "RUN AGAIN", "IconRestart", true, 24);
            Button resultsMenu = NewButton("Results main menu", results, "MAIN MENU", "IconQuit", -158);
            results.Find("Shortcut").gameObject.SetActive(false);

            Transform panel = Node("Forest Pause Menu", root, 0, 0, 584, 508);
            Frame(panel, "DialogFrame");
            panel.SetSiblingIndex(main.transform.GetSiblingIndex());
            Label(Node("Pause eyebrow", panel), "FOREST / ENDLESS RUNNER", 0, 202, 520, 30, 12, Sage);
            Label(Node("Pause title", panel), "PAUSED", 0, 154, 520, 56, 32);
            Label(Node("Pause description", panel), "TAKE A BREATHER", 0, 106, 520, 30, 16, Sage);
            Text pausedScore = Label(Node("Paused score", panel), "SCORE 000000 / BEST 000000", 0, 64, 520, 32, 16);
            Button resume = NewButton("Resume run", panel, "RESUME", "IconPlay", -4, true);
            Button restart = NewButton("Restart run", panel, "RESTART", "IconRestart", -86);
            Button quit = NewButton("Quit run", panel, "QUIT TO MENU", "IconQuit", -164, false, 20);
            Label(Node("Pause shortcut", panel), "ESC / P TO RESUME", 0, -220, 520, 28, 12, Sage);
            panel.gameObject.SetActive(false);

            SetHud(game, "font", font);
            SetHud(game, "pausePanel", panel.gameObject);
            SetHud(game, "resumeControl", resume);
            SetHud(game, "restartControl", restart);
            SetHud(game, "quitRunControl", quit);
            SetHud(game, "resultsMenuControl", resultsMenu);
            SetHud(game, "pausedScore", pausedScore);
            SetHud(game, "statsFrame", statsFrame.gameObject);
            SetHud(game, "soundIcon", speaker);
            SetHud(game, "soundOn", ForestHudArt.Load("HudSpeaker"));
            SetHud(game, "soundOff", ForestHudArt.Load("HudMuted"));
            SetHud(game, "controlsGuide", guide.gameObject);
            SetHud(game, "gameplayBackdrop", bottomFade.gameObject);
            var segmentData = new SerializedObject(game);
            SerializedProperty savedSegments = segmentData.FindProperty("hud").FindPropertyRelative("paceSegments");
            savedSegments.arraySize = segments.Length;
            for (int i = 0; i < segments.Length; i++)
                savedSegments.GetArrayElementAtIndex(i).objectReferenceValue = segments[i];
            segmentData.ApplyModifiedPropertiesWithoutUndo();

            var serialized = new SerializedObject(game);
            SerializedProperty buttons = serialized.FindProperty("hud").FindPropertyRelative("buttons");
            var controls = new List<Button>();
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                if (button.GetComponentInParent<ForestMainMenu>(true) != null) continue;
                controls.Add(button);
                ColorBlock colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1, 1, 0.83f);
                colors.pressedColor = new Color(0.74f, 0.84f, 0.7f);
                colors.fadeDuration = 0.07f;
                button.colors = colors;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
            }
            buttons.arraySize = controls.Count;
            for (int i = 0; i < controls.Count; i++) buttons.GetArrayElementAtIndex(i).objectReferenceValue = controls[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var mainData = new SerializedObject(main);
            SerializedProperty chrome = mainData.FindProperty("gameplayChrome");
            chrome.arraySize = root.childCount - 1;
            int index = 0;
            foreach (Transform child in root)
                if (child != main.transform) chrome.GetArrayElementAtIndex(index++).objectReferenceValue = child.gameObject;
            mainData.ApplyModifiedPropertiesWithoutUndo();

            game.AssignMainMenu(main);
            // Resetting the preview above hides the newly authored gameplay objects too.
            EditorUtility.SetDirty(game);
            EditorUtility.SetDirty(main);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Saved themed gameplay HUD, results screen and Resume / Restart / Main Menu pause controls.");
        }

        private static void SetHud(ForestRunnerGame game, string name, Object value)
        {
            var data = new SerializedObject(game);
            data.FindProperty("hud").FindPropertyRelative(name).objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ScoreCard(Transform scores, string name, string valueName, string caption, string icon, float y)
        {
            Transform heading = scores.Find(valueName + " caption") ?? scores.Find(name + "/" + valueName + " caption");
            Transform value = scores.Find(valueName) ?? scores.Find(name + "/" + valueName);
            Transform card = Node(name, scores, 0, y, 260, 68);
            HudFrame(card, "HudPanel");
            heading.SetParent(card, false);
            value.SetParent(card, false);
            Label(heading, caption, 22, 19, 184, 24, 16, Sage, TextAnchor.MiddleLeft);
            Label(value, "000000", 22, -11, 184, 36, 28, Cream, TextAnchor.MiddleLeft);
            HudArt("Medal icon", card, icon, -94, 0, 38, 38);
            Moss(card, 260, 68);
        }

        private static Transform KeyboardGuide(Transform root)
        {
            Transform guide = Node("Keyboard controls", root, 64, -306, 520, 30);
            Color hint = PixelMenuArt.Hex(0x91a39f);
            Key(guide, "Space key", "SPACE", -218, 68);
            Label(Node("First separator", guide), "/", -174, 0, 12, 26, 12, hint);
            Transform up = Key(guide, "Up key", "", -150, 24);
            HudArt("Up arrow", up, "HudJumpArrow", 0, 0, 18, 20);
            Label(Node("Second separator", guide), "/", -126, 0, 12, 26, 12, hint);
            Key(guide, "W key", "W", -102, 24);
            Label(Node("Jump hint", guide), ": JUMP", -47, 0, 72, 26, 12, hint);
            Image divider = Node("Hint divider", guide, 16, 0, 1, 26).GetComponent<Image>();
            if (divider == null) divider = guide.Find("Hint divider").gameObject.AddComponent<Image>();
            divider.color = PixelMenuArt.Hex(0x40565a);
            divider.raycastTarget = false;
            Key(guide, "Escape key", "ESC", 64, 48);
            Label(Node("Pause hint", guide), ": PAUSE", 134, 0, 94, 26, 12, hint);
            return guide;
        }

        private static Transform Key(Transform guide, string name, string caption, float x, float width)
        {
            Transform key = Node(name, guide, x, 0, width, 26);
            HudFrame(key, "HudKey");
            Label(Node("Key caption", key), caption, 0, 0, width - 8, 22, 12, PixelMenuArt.Hex(0xaab6b3));
            return key;
        }

        private static void Moss(Transform parent, float width, float height, bool both = false)
        {
            HudArt("Right moss sprig", parent, "HudLeaves", width * 0.5f - 7, height * 0.5f - 5, 28, 28);
            if (both)
            {
                Image left = HudArt("Left moss sprig", parent, "HudLeaves", -width * 0.5f + 8, -height * 0.5f + 7, 28, 28);
                left.rectTransform.localRotation = Quaternion.Euler(0, 0, 180);
            }
        }

        private static Image HudArt(string name, Transform parent, string sprite, float x, float y, float width, float height)
        {
            Transform node = Node(name, parent, x, y, width, height);
            Image image = node.GetComponent<Image>();
            if (image == null) image = node.gameObject.AddComponent<Image>();
            image.sprite = ForestHudArt.Load(sprite);
            image.color = Color.white;
            image.raycastTarget = false;
            image.preserveAspect = true;
            return image;
        }

        private static void HudFrame(Transform node, string sprite)
        {
            Image image = node.GetComponent<Image>();
            if (image == null) image = node.gameObject.AddComponent<Image>();
            image.sprite = ForestHudArt.Load(sprite);
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 0.5f;
            image.color = Color.white;
            image.raycastTarget = node.GetComponent<Button>() != null;
        }

        private static Button NewButton(string name, Transform parent, string caption, string icon, float y, bool primary = false, int size = 24)
        {
            Transform node = Node(name, parent, 0, y, 360, primary ? 76 : 68);
            Button button = node.GetComponent<Button>();
            if (button == null) button = node.gameObject.AddComponent<Button>();
            StyleButton(button, caption, icon, primary, size);
            return button;
        }

        private static void StyleButton(Button button, string caption, string icon, bool primary, int size)
        {
            Frame(button.transform, primary ? "ButtonPlay" : "ButtonForest");
            button.targetGraphic = button.GetComponent<Image>();
            button.targetGraphic.raycastTarget = true;
            float width = ((RectTransform)button.transform).sizeDelta.x;
            Art("Icon", button.transform, icon, -width * 0.5f + 58, 0, 28, 28);
            Text text = button.GetComponentInChildren<Text>(true);
            if (text == null) text = Node("Label", button.transform).gameObject.AddComponent<Text>();
            Label(text.transform, caption, 28, 0, width - 116, 48, size);
        }

        private static Image Art(string name, Transform parent, string sprite, float x, float y, float width, float height)
        {
            Transform node = Node(name, parent, x, y, width, height);
            Image image = node.GetComponent<Image>();
            if (image == null) image = node.gameObject.AddComponent<Image>();
            image.sprite = PixelMenuArt.Load(sprite);
            image.color = Color.white;
            image.raycastTarget = false;
            image.preserveAspect = true;
            return image;
        }

        private static void Frame(Transform node, string sprite)
        {
            Image image = node.GetComponent<Image>();
            if (image == null) image = node.gameObject.AddComponent<Image>();
            image.sprite = PixelMenuArt.Load(sprite);
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 0.5f;
            image.color = Color.white;
            image.raycastTarget = node.GetComponent<Button>() != null;
        }

        private static Text Label(Transform node, string caption, float x, float y, float width, float height, int size, Color? color = null,
            TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            Position(node, x, y, width, height);
            Text text = node.GetComponent<Text>();
            if (text == null) text = node.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontStyle = FontStyle.Normal;
            text.fontSize = size;
            text.text = caption;
            text.color = color ?? Cream;
            text.alignment = alignment;
            text.alignByGeometry = true;
            text.lineSpacing = 1.65f;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = false;
            text.raycastTarget = false;
            Shadow shadow = node.GetComponent<Shadow>();
            if (shadow == null) shadow = node.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.03f, 0.12f, 0.13f, 0.6f);
            shadow.effectDistance = new Vector2(1, -1);
            return text;
        }

        private static Transform Node(string name, Transform parent, float x = 0, float y = 0, float width = 520, float height = 40)
        {
            Transform node = parent.Find(name);
            if (node == null)
            {
                node = new GameObject(name, typeof(RectTransform)).transform;
                node.SetParent(parent, false);
            }
            Position(node, x, y, width, height);
            return node;
        }

        private static void Position(Transform node, float x, float y, float width, float height)
        {
            var rect = (RectTransform)node;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
