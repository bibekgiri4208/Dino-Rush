using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DinoRush.Editor
{
    /// <summary>Keeps the focused pause/results layout in the game's existing forest pixel-art theme.</summary>
    public static class ForestMinimalUiBuilder
    {
        private static readonly Color Cream = PixelMenuArt.Hex(0xf0f2cf);
        private static readonly Color Sage = PixelMenuArt.Hex(0xaecb98);
        private static readonly Color Gold = PixelMenuArt.Hex(0xffd275);

        [MenuItem("Dino Rush/Match Minimal UI to Game Theme")]
        public static void MatchGameTheme() => Build();

        [MenuItem("Dino Rush/Apply Minimal Pause and Results UI")]
        public static void Build()
        {
            if (Application.isPlaying) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestRun.unity");
            ForestRunnerGame game = Object.FindAnyObjectByType<ForestRunnerGame>();
            if (game == null || !game.HasAuthoredScene) throw new System.InvalidOperationException("Open the authored runner scene first.");
            Apply(game);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Saved focused pause/results and touch controls in the forest pixel-art theme. Full controls help remains in Options.");
        }

        internal static bool IsApplied(ForestRunnerGame game) => new SerializedObject(game)
            .FindProperty("hud").FindPropertyRelative("minimalPresentation").boolValue;

        internal static void Apply(ForestRunnerGame game)
        {
            Transform root = game.transform.Find("Forest runner UI/16 by 9 safe content");
            ForestUiMotionBuilder.Apply(game);
            PixelMenuArt.Generate();
            ForestHudArt.Generate();
            Sprite panelArt = PixelMenuArt.Load("DialogFrame");
            Sprite primaryArt = PixelMenuArt.Load("ButtonPlay");
            Sprite secondaryArt = PixelMenuArt.Load("ButtonForest");
            Transform pause = root.Find("Forest Pause Menu");
            Transform results = root.Find("Menu card");
            Frame(pause, panelArt, 480, 404);
            Frame(results, panelArt, 480, 428);
            Position(pause, 0, 0, 480, 404);
            Position(results, 0, 0, 480, 428);

            Label(pause.Find("Pause eyebrow"), "FOREST", 0, 148, 384, 24, 12, Sage);
            Label(pause.Find("Pause title"), "PAUSED", 0, 106, 384, 48, 28, Cream);
            Hide(pause, "Pause description", "Pause shortcut");
            Text pausedScore = Stat(pause, "Paused score", "Score caption", "SCORE", -100, 32);
            Text pausedBest = Stat(pause, "Paused best", "Best caption", "BEST", 100, 32);
            Divider(pause, -18);
            Control(pause.Find("Resume run"), primaryArt, "RESUME", 0, -70, 384, 56, true);
            Control(pause.Find("Restart run"), secondaryArt, "RESTART", -100, -136, 184, 48);
            Control(pause.Find("Quit run"), secondaryArt, "MAIN MENU", 100, -136, 184, 48);

            Label(results.Find("Eyebrow"), "FOREST", 0, 158, 384, 24, 12, Gold);
            Label(results.Find("Title"), "GAME OVER", 0, 116, 384, 48, 28, Cream);
            Hide(results, "Description", "Result", "Shortcut", "Card accent");
            Text resultScore = Stat(results, "Results score", "Score caption", "SCORE", -100, 32);
            Text resultBest = Stat(results, "Results best", "Best caption", "BEST", 100, 32);
            Divider(results, -28);
            Control(results.Find("Primary action"), primaryArt, "PLAY AGAIN", 0, -80, 384, 56, true);
            Control(results.Find("Results main menu"), secondaryArt, "MAIN MENU", 0, -146, 384, 48);

            Image scrim = root.Find("Menu scrim").GetComponent<Image>();
            scrim.color = new Color(0.015f, 0.06f, 0.065f, 0.72f);
            scrim.raycastTarget = true;
            Hide(root, "Keyboard controls", "Controls");
            Hide(root.Find("Duck"), "Drop hint");
            // Match the HUD's pixel-cut green controls without restoring extra hints or decorations.
            Control(root.Find("Jump"), ForestHudArt.Load("HudJump"), "JUMP", 476, -276, 248, 88, true);
            Control(root.Find("Duck"), ForestHudArt.Load("HudJump"), "DUCK", 244, -276, 176, 88, true);

            var data = new SerializedObject(game);
            SerializedProperty hud = data.FindProperty("hud");
            hud.FindPropertyRelative("minimalPresentation").boolValue = true;
            hud.FindPropertyRelative("pausedScore").objectReferenceValue = pausedScore;
            hud.FindPropertyRelative("pausedBest").objectReferenceValue = pausedBest;
            hud.FindPropertyRelative("resultScore").objectReferenceValue = resultScore;
            hud.FindPropertyRelative("resultBest").objectReferenceValue = resultBest;
            data.ApplyModifiedPropertiesWithoutUndo();
            ForestUiMotionBuilder.Apply(game);
            game.GetComponentInChildren<ForestMainMenu>(true).Show();
            EditorUtility.SetDirty(game);
        }

        private static Text Stat(Transform parent, string valueName, string captionName, string caption, float x, float y)
        {
            Label(Node(captionName, parent), caption, x, y + 31, 174, 22, 12, Sage);
            Text value = Label(Node(valueName, parent), "0", x, y - 5, 174, 48, 28, Cream);
            value.resizeTextForBestFit = true;
            value.resizeTextMinSize = 12;
            value.resizeTextMaxSize = 28;
            return value;
        }

        private static void Divider(Transform parent, float y)
        {
            Transform node = Node("Section divider", parent);
            Position(node, 0, y, 384, 2);
            Image image = node.GetComponent<Image>() ?? node.gameObject.AddComponent<Image>();
            image.sprite = null;
            image.color = new Color(Sage.r, Sage.g, Sage.b, 0.18f);
            image.raycastTarget = false;
        }

        private static void Control(Transform node, Sprite sprite, string caption, float x, float y, float width, float height, bool primary = false)
        {
            Position(node, x, y, width, height);
            Frame(node, sprite, width, height);
            foreach (Transform child in node)
                if (child.GetComponent<Text>() == null) child.gameObject.SetActive(false);
            Text text = node.GetComponentInChildren<Text>(true);
            int size = node.name == "Jump" || node.name == "Duck" ? 20 : primary ? 18 : 14;
            Label(text.transform, caption, 0, 0, width - 32, height - 8, size, Cream);
            Button button = node.GetComponent<Button>();
            button.targetGraphic = node.GetComponent<Image>();
            button.targetGraphic.raycastTarget = true;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 0.88f);
            colors.pressedColor = new Color(0.74f, 0.84f, 0.7f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            var feedback = new SerializedObject(node.GetComponent<RunnerUiButtonFeedback>());
            feedback.FindProperty("hoverScale").floatValue = 1.015f;
            feedback.FindProperty("glowColor").colorValue = Sage;
            feedback.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Frame(Transform node, Sprite sprite, float width, float height)
        {
            Image image = node.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 0.5f;
            image.color = Color.white;
            image.raycastTarget = node.GetComponent<Button>() != null;
            foreach (Shadow effect in node.GetComponents<Shadow>()) effect.enabled = false;
            ((RectTransform)node).sizeDelta = new Vector2(width, height);
        }

        private static Text Label(Transform node, string caption, float x, float y, float width, float height, int size, Color color)
        {
            Position(node, x, y, width, height);
            Text text = node.GetComponent<Text>() ?? node.gameObject.AddComponent<Text>();
            text.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Fonts/PressStart2P-Regular.ttf");
            text.fontStyle = FontStyle.Normal;
            text.fontSize = size;
            text.text = caption;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.alignByGeometry = true;
            text.lineSpacing = 1.4f;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = false;
            text.raycastTarget = false;
            foreach (Shadow effect in node.GetComponents<Shadow>()) effect.enabled = false;
            node.gameObject.SetActive(true);
            return text;
        }

        private static void Hide(Transform parent, params string[] names)
        {
            foreach (string name in names) parent.Find(name)?.gameObject.SetActive(false);
        }

        private static Transform Node(string name, Transform parent)
        {
            Transform node = parent.Find(name);
            if (node != null) return node;
            node = new GameObject(name, typeof(RectTransform)).transform;
            node.SetParent(parent, false);
            return node;
        }

        private static void Position(Transform node, float x, float y, float width, float height)
        {
            var rect = (RectTransform)node;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

    }
}
