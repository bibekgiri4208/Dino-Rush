using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DinoRush.Editor
{
    /// <summary>Adds four editable choice cards without rebuilding the menu or reslicing source art.</summary>
    public static class ForestDinoSelectorBuilder
    {
        private const string RosterPath = "Assets/Generated/ForestDinoRoster.asset";
        private static readonly string[] Names = { "Blue", "Red", "Yellow", "Green" };
        private static readonly Color Cream = PixelMenuArt.Hex(0xf0f2cf);
        private static readonly Color Sage = PixelMenuArt.Hex(0xaecb98);

        [MenuItem("Dino Rush/Add Dino Selector")]
        public static void Build()
        {
            if (Application.isPlaying) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestRun.unity");
            ForestRunnerGame game = Object.FindAnyObjectByType<ForestRunnerGame>();
            ForestMainMenu menu = game != null ? game.GetComponentInChildren<ForestMainMenu>(true) : null;
            if (menu == null || !game.HasAuthoredScene) throw new System.InvalidOperationException("The authored forest menu is missing.");
            Apply(game, menu);
            ForestUiMotionBuilder.Apply(game);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Saved Blue / Red / Yellow / Green dino choices. Source sprite sheets and slices were preserved.");
        }

        internal static void Apply(ForestRunnerGame game, ForestMainMenu menu)
        {
            RunnerDinoRoster roster = Roster();
            ForestHudArt.Generate();
            Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Fonts/PressStart2P-Regular.ttf");
            if (font == null) throw new System.InvalidOperationException("The forest pixel font is missing.");
            Transform custom = menu.transform.Find("Menu dialog/Moss framed dialog/Customize page");
            Transform choices = Node("Dino choices", custom);
            var buttons = new Button[Names.Length];
            var portraits = new Image[Names.Length];
            var captions = new Text[Names.Length];
            for (int i = 0; i < Names.Length; i++)
            {
                Transform card = Node("Select " + Names[i] + " dino", choices);
                Image frame = Image(card);
                frame.sprite = ForestHudArt.Load(i == 0 ? "HudJump" : "HudPanel");
                frame.type = UnityEngine.UI.Image.Type.Sliced;
                frame.pixelsPerUnitMultiplier = 0.5f;
                frame.raycastTarget = true;
                Button button = card.GetComponent<Button>();
                if (button == null) button = card.gameObject.AddComponent<Button>();
                button.targetGraphic = frame;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                ColorBlock colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1f, 1f, 0.83f);
                colors.pressedColor = new Color(0.74f, 0.84f, 0.7f);
                colors.fadeDuration = 0.07f;
                button.colors = colors;
                buttons[i] = button;
                portraits[i] = Image(Node("Dino portrait", card));
                portraits[i].sprite = roster[i].Portrait;
                portraits[i].preserveAspect = true;
                portraits[i].raycastTarget = false;
                captions[i] = Label(Node("Dino name", card), roster[i].DisplayName.ToUpperInvariant(), font, 12, Cream);
            }
            Text selected = Label(custom.Find("Selected status"), roster[0].DisplayName.ToUpperInvariant() + " DINO SELECTED", font, 12, Cream);
            Label(custom.Find("Map selection"), "MAP: FOREST", font, 12, Sage);
            RepairLayout(custom);

            var gameData = new SerializedObject(game);
            gameData.FindProperty("dinoRoster").objectReferenceValue = roster;
            gameData.ApplyModifiedPropertiesWithoutUndo();
            var menuData = new SerializedObject(menu);
            References(menuData.FindProperty("dinoChoices"), buttons);
            References(menuData.FindProperty("dinoPortraits"), portraits);
            References(menuData.FindProperty("dinoCaptions"), captions);
            menuData.FindProperty("selectedDinoLabel").objectReferenceValue = selected;
            menuData.FindProperty("selectedDinoFrame").objectReferenceValue = ForestHudArt.Load("HudJump");
            menuData.FindProperty("unselectedDinoFrame").objectReferenceValue = ForestHudArt.Load("HudPanel");
            menuData.ApplyModifiedPropertiesWithoutUndo();
            menu.EnsureShopUi();
            EditorUtility.SetDirty(game);
            EditorUtility.SetDirty(menu);
        }

        internal static void RepairLayout(Transform custom)
        {
            foreach (string legacy in new[] { "Selected blue dino", "Dino selection", "Single adventure" })
                custom.Find(legacy).gameObject.SetActive(false); // Retain old authored artwork, but not the blue-only placeholder.
            Transform choices = custom.Find("Dino choices");
            Position(choices, 0, 0, 500, 224);
            for (int i = 0; i < Names.Length; i++)
            {
                Transform card = choices.Find("Select " + Names[i] + " dino");
                Position(card, -180 + i * 120, 20, 108, 124);
                Position(card.Find("Dino portrait"), 0, 14, 72, 80);
                Position(card.Find("Dino name"), 0, -40, 96, 22);
            }
            Position(custom.Find("Selected status"), 0, -64, 480, 24);
            Position(custom.Find("Map selection"), 0, -88, 480, 20);
        }

        private static RunnerDinoRoster Roster()
        {
            RunnerDinoRoster existing = AssetDatabase.LoadAssetAtPath<RunnerDinoRoster>(RosterPath);
            if (existing != null)
            {
                if (existing.Count != Names.Length) throw new System.InvalidOperationException("The edited dino roster must contain four choices.");
                return existing;
            }
            var roster = ScriptableObject.CreateInstance<RunnerDinoRoster>();
            roster.name = "Forest Dino Roster";
            var data = new SerializedObject(roster);
            SerializedProperty entries = data.FindProperty("dinos");
            entries.arraySize = Names.Length;
            for (int i = 0; i < Names.Length; i++)
            {
                string path = $"Assets/2D Sprites/dino/DinoSprites - {i + 1}.png";
                Texture2D sheet = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Sprite portrait = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault(sprite => sprite.name.EndsWith("_0"));
                if (sheet == null || portrait == null || sheet.width != sheet.height * 24)
                    throw new System.InvalidOperationException("Expected a complete 24-frame dino sheet: " + path);
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("id").stringValue = Names[i].ToLowerInvariant();
                entry.FindPropertyRelative("displayName").stringValue = Names[i];
                entry.FindPropertyRelative("sheet").objectReferenceValue = sheet;
                entry.FindPropertyRelative("portrait").objectReferenceValue = portrait;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(roster, RosterPath);
            return roster;
        }

        private static void References(SerializedProperty property, Object[] values)
        {
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static Image Image(Transform node)
        {
            Image image = node.GetComponent<Image>();
            if (image == null) image = node.gameObject.AddComponent<Image>();
            image.color = Color.white;
            return image;
        }

        private static Text Label(Transform node, string caption, Font font, int size, Color color)
        {
            Text text = node.GetComponent<Text>();
            if (text == null) text = node.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Normal;
            text.text = caption;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.alignByGeometry = true;
            text.lineSpacing = 1.65f;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
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
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
