using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DinoRush.Editor
{
    public static class ForestMapSelectorBuilder
    {
        private const string CatalogPath = RunnerMapArt.Directory + "/MapCatalog.asset";
        private static readonly Color Cream = PixelMenuArt.Hex(0xf0f2cf);
        private static readonly Color Sage = PixelMenuArt.Hex(0xaecb98);

        [MenuItem("Dino Rush/Restore Map Background Source Resolution")]
        public static void RestoreSourceResolution()
        {
            if (Application.isPlaying) return;
            RunnerMapCatalog catalog = AssetDatabase.LoadAssetAtPath<RunnerMapCatalog>(CatalogPath);
            if (catalog == null) throw new System.InvalidOperationException("Add the map selector first.");
            for (int i = 1; i < catalog.Count; i++)
            {
                RunnerMapCatalog.Map map = catalog[i];
                foreach (RunnerMapCatalog.MapLayer layer in map.layers)
                {
                    string path = AssetDatabase.GetAssetPath(layer.sprite);
                    // Only regenerate derived scenery, never originals, ground, or user-assigned art.
                    if (!path.StartsWith(RunnerMapArt.Directory + "/", System.StringComparison.Ordinal)
                        || path.EndsWith("_ground_loop.png", System.StringComparison.Ordinal)) continue;
                    string source = AssetDatabase.GetAssetPath(layer.source);
                    string name = Path.GetFileNameWithoutExtension(path);
                    if (name.EndsWith("_loop", System.StringComparison.Ordinal))
                        layer.sprite = RunnerMapArt.Loop(source, name.Substring(0, name.Length - 5), layer.order == -100,
                            map.id == "jungle" ? FilterMode.Point : FilterMode.Bilinear, true);
                    else if (name.EndsWith("_moon", System.StringComparison.Ordinal))
                        layer.sprite = RunnerMapArt.Moon(source, name.Substring(0, name.Length - 5), true);
                }
                map.preview = RunnerMapArt.Preview(map, true);
            }
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("Restored native-resolution map scenery: smooth Desert/Rural, crisp Jungle pixel art. Source art, GUIDs, ground, and map framing preserved.");
        }

        [MenuItem("Dino Rush/Refresh Map Previews")]
        public static void RefreshPreviews()
        {
            if (Application.isPlaying) return;
            RunnerMapCatalog catalog = AssetDatabase.LoadAssetAtPath<RunnerMapCatalog>(CatalogPath);
            if (catalog == null) throw new System.InvalidOperationException("Add the map selector first.");
            for (int i = 0; i < catalog.Count; i++) catalog[i].preview = RunnerMapArt.Preview(catalog[i], true);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Dino Rush/Add Map Selector and Maps")]
        public static void Build()
        {
            if (Application.isPlaying) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestRun.unity");
            ForestRunnerGame game = Object.FindAnyObjectByType<ForestRunnerGame>();
            if (game == null || !game.HasAuthoredScene) throw new System.InvalidOperationException("Open the authored forest scene first.");
            Apply(game);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Saved Forest / Desert / Jungle / Rural maps and the Play map-selector submenu. Source artwork was preserved.");
        }

        internal static void Apply(ForestRunnerGame game)
        {
            if (!AssetDatabase.IsValidFolder(RunnerMapArt.Directory)) AssetDatabase.CreateFolder("Assets/Generated", "RunnerMaps");
            RunnerMapCatalog catalog = Catalog(game);
            ForestMainMenu menu = game.GetComponentInChildren<ForestMainMenu>(true);
            Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Fonts/PressStart2P-Regular.ttf");
            menu.transform.Find("Dino Rush Logo/Forest subtitle plaque/Forest subtitle").GetComponent<Text>().text = "4 MAPS / ENDLESS RUNNER";
            Transform page = Node("Map selector", menu.transform, 0, 0, 1280, 720);
            Image scrim = Art(Node("Map selector scrim", page, 0, 0, 1280, 720), null, true);
            scrim.color = new Color(0.015f, 0.06f, 0.065f, 0.65f);
            Transform panel = Node("Map selection panel", page, 0, 0, 1000, 512);
            Art(panel, PixelMenuArt.Load("DialogFrame"));
            Label(Node("Map heading", panel, 0, 198, 920, 56), "CHOOSE YOUR MAP", font, 28, Cream);
            Label(Node("Map hint", panel, 0, 150, 920, 26), "PICK A TRAIL. SAME DINO. NEW ADVENTURE.", font, 12, Sage);
            var buttons = new Button[catalog.Count];
            var previews = new Image[catalog.Count];
            var captions = new Text[catalog.Count];
            for (int i = 0; i < catalog.Count; i++)
            {
                RunnerMapCatalog.Map map = catalog[i];
                Transform card = Node("Choose " + map.displayName + " map", panel, -351 + i * 234, 12, 216, 220);
                buttons[i] = Button(card, ForestHudArt.Load(i == 0 ? "HudJump" : "HudPanel"));
                previews[i] = Art(Node("Map preview", card, 0, 28, 192, 108), map.preview);
                captions[i] = Label(Node("Map name", card, 0, -46, 200, 30), map.displayName.ToUpperInvariant(), font, 20, Cream);
                Label(Node("Map description", card, 0, -78, 200, 24), map.description, font, 10, Sage);
            }
            Text status = Label(Node("Selected map status", panel, 0, -126, 940, 30), "FOREST SELECTED / READY TO RUN", font, 12, Cream);
            Transform back = Node("Map selector back", panel, -182, -194, 300, 64);
            Transform start = Node("Start selected map", panel, 182, -194, 340, 64);
            Button backControl = Button(back, PixelMenuArt.Load("ButtonForest"));
            Button startControl = Button(start, PixelMenuArt.Load("ButtonPlay"));
            Label(Node("Label", back, 0, 0, 276, 40), "BACK", font, 24, Cream);
            Label(Node("Label", start, 0, 0, 316, 40), "START RUN", font, 24, Cream);
            page.gameObject.SetActive(false);

            var gameData = new SerializedObject(game);
            gameData.FindProperty("mapCatalog").objectReferenceValue = catalog;
            gameData.ApplyModifiedPropertiesWithoutUndo();
            var menuData = new SerializedObject(menu);
            menuData.FindProperty("mapPage").objectReferenceValue = page.gameObject;
            menuData.FindProperty("mapStatus").objectReferenceValue = status;
            menuData.FindProperty("customizeMapLabel").objectReferenceValue = menu.transform.Find("Menu dialog/Moss framed dialog/Customize page/Map selection").GetComponent<Text>();
            menuData.FindProperty("startMap").objectReferenceValue = startControl;
            menuData.FindProperty("mapBack").objectReferenceValue = backControl;
            References(menuData.FindProperty("mapChoices"), buttons);
            References(menuData.FindProperty("mapPreviews"), previews);
            References(menuData.FindProperty("mapCaptions"), captions);
            menuData.ApplyModifiedPropertiesWithoutUndo();
            ForestUiMotionBuilder.Apply(game);
            EditorUtility.SetDirty(game);
            EditorUtility.SetDirty(menu);
        }

        private static RunnerMapCatalog Catalog(ForestRunnerGame game)
        {
            RunnerMapCatalog catalog = AssetDatabase.LoadAssetAtPath<RunnerMapCatalog>(CatalogPath);
            if (catalog != null) return catalog; // Preserve authored catalog edits on reapplication.
            var maps = new List<RunnerMapCatalog.Map>();
            var data = new SerializedObject(game);
            SerializedProperty forestLayers = data.FindProperty("layers");
            var forest = new RunnerMapCatalog.Map { id = "forest", displayName = "Forest", description = "THE ORIGINAL TRAIL",
                skyColor = PixelMenuArt.Hex(0x76b8c9), soilColor = PixelMenuArt.Hex(0x133031), layers = new RunnerMapCatalog.MapLayer[forestLayers.arraySize] };
            string[] sourceNames = { "sky", "mountains", "distantForest", "middleForest", "tallTrees", "nearForest", "ground" };
            for (int i = 0; i < forest.layers.Length; i++)
            {
                SerializedProperty layer = forestLayers.GetArrayElementAtIndex(i);
                var tile = (Transform)layer.FindPropertyRelative("tiles").GetArrayElementAtIndex(0).objectReferenceValue;
                SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();
                forest.layers[i] = new RunnerMapCatalog.MapLayer { source = (Texture2D)data.FindProperty(sourceNames[i]).objectReferenceValue,
                    sprite = renderer.sprite, height = tile.lossyScale.y, bottom = i == 0 ? -8.775f : tile.position.y,
                    speed = layer.FindPropertyRelative("speed").floatValue, order = renderer.sortingOrder };
            }
            maps.Add(forest);

            var desert = new RunnerMapCatalog.Map { id = "desert", displayName = "Desert", description = "SUNLIT SAND DUNES",
                skyColor = PixelMenuArt.Hex(0x8cb8e3), soilColor = PixelMenuArt.Hex(0x785039), dustTint = new Color(1.35f, 1.18f, 0.85f), moteColor = PixelMenuArt.Hex(0xe9c58b) };
            desert.layers = new[] {
                Layer("desert bg/desert_sky.png", "desert_sky", 0.015f, -100, true),
                Moon("desert bg/desert_moon.png", "desert"),
                Layer("desert bg/desert_cloud.png", "desert_clouds", 0.035f, -90),
                Layer("desert bg/desert_mountain.png", "desert_mountains", 0.08f, -80),
                Layer("desert bg/desert_dunemid.png", "desert_dunes_mid", 0.18f, -70),
                Layer("desert bg/desert_dunefrontt.png", "desert_dunes_front", 0.4f, -50),
                Ground("desert", 0x574132, 0xffd180) };
            maps.Add(desert);

            var jungle = new RunnerMapCatalog.Map { id = "jungle", displayName = "Jungle", description = "DEEP GREEN CANOPY",
                skyColor = PixelMenuArt.Hex(0xa8d4c6), soilColor = PixelMenuArt.Hex(0x133b36), moteColor = PixelMenuArt.Hex(0xa8e89b) };
            var jungleLayers = new List<RunnerMapCatalog.MapLayer>();
            for (int i = 1; i <= 5; i++) jungleLayers.Add(Layer($"jungle bg/plx-{i}.png", "jungle_layer_" + i,
                new[] { 0.015f, 0.07f, 0.17f, 0.3f, 0.46f }[i - 1], -110 + i * 10, i == 1));
            jungleLayers.Add(Ground("jungle", 0x123c37, 0x99c76c));
            jungle.layers = jungleLayers.ToArray();
            maps.Add(jungle);

            var rural = new RunnerMapCatalog.Map { id = "rural", displayName = "Rural", description = "RIVERSIDE VILLAGE",
                skyColor = PixelMenuArt.Hex(0x7e9caa), soilColor = PixelMenuArt.Hex(0x485538), dustTint = new Color(1.1f, 1.1f, 0.85f), moteColor = PixelMenuArt.Hex(0xe2d698) };
            rural.layers = new[] {
                Layer("rural area bg/ruralparallaxsky.png", "rural_sky", 0.015f, -100, true),
                Moon("rural area bg/ruralparalaxmoon.png", "rural"),
                Layer("rural area bg/ruralparallaxclouds.png", "rural_clouds", 0.03f, -90),
                Layer("rural area bg/ruralparallaxmountainback2.png", "rural_mountains_far", 0.05f, -85),
                Layer("rural area bg/ruralparallaxmountainback.png", "rural_mountains_back", 0.07f, -80),
                Layer("rural area bg/ruralparallaxmountain.png", "rural_mountains", 0.12f, -75),
                Layer("rural area bg/ruralparallaxvillage.png", "rural_village", 0.24f, -60),
                Layer("rural area bg/ruralparallaxriver.png", "rural_river", 0.3f, -50),
                Layer("rural area bg/ruralparallaxriverskyredlex.png", "rural_reflection", 0.3f, -49),
                Layer("rural area bg/ruralparallaxriverfront.png", "rural_river_front", 0.4f, -45),
                Ground("rural", 0x35452f, 0xc4d077) };
            maps.Add(rural);

            catalog = ScriptableObject.CreateInstance<RunnerMapCatalog>();
            var serialized = new SerializedObject(catalog);
            SerializedProperty entries = serialized.FindProperty("maps");
            entries.arraySize = maps.Count;
            for (int i = 0; i < maps.Count; i++)
            {
                RunnerMapCatalog.Map map = maps[i];
                map.preview = RunnerMapArt.Preview(map);
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("id").stringValue = map.id;
                entry.FindPropertyRelative("displayName").stringValue = map.displayName;
                entry.FindPropertyRelative("description").stringValue = map.description;
                entry.FindPropertyRelative("preview").objectReferenceValue = map.preview;
                entry.FindPropertyRelative("skyColor").colorValue = map.skyColor;
                entry.FindPropertyRelative("soilColor").colorValue = map.soilColor;
                entry.FindPropertyRelative("dustTint").colorValue = map.dustTint;
                entry.FindPropertyRelative("moteColor").colorValue = map.moteColor;
                SerializedProperty layers = entry.FindPropertyRelative("layers");
                layers.arraySize = map.layers.Length;
                for (int j = 0; j < map.layers.Length; j++)
                {
                    RunnerMapCatalog.MapLayer layer = map.layers[j];
                    SerializedProperty saved = layers.GetArrayElementAtIndex(j);
                    saved.FindPropertyRelative("source").objectReferenceValue = layer.source;
                    saved.FindPropertyRelative("sprite").objectReferenceValue = layer.sprite;
                    saved.FindPropertyRelative("height").floatValue = layer.height;
                    saved.FindPropertyRelative("bottom").floatValue = layer.bottom;
                    saved.FindPropertyRelative("speed").floatValue = layer.speed;
                    saved.FindPropertyRelative("order").intValue = layer.order;
                    saved.FindPropertyRelative("stationary").boolValue = layer.stationary;
                    saved.FindPropertyRelative("x").floatValue = layer.x;
                }
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            return catalog;
        }

        private static RunnerMapCatalog.MapLayer Layer(string relativePath, string name, float speed, int order, bool sky = false)
        {
            string path = "Assets/2D Sprites/" + relativePath;
            return new RunnerMapCatalog.MapLayer { source = AssetDatabase.LoadAssetAtPath<Texture2D>(path),
                sprite = RunnerMapArt.Loop(path, name, sky,
                    relativePath.StartsWith("jungle bg/", System.StringComparison.Ordinal) ? FilterMode.Point : FilterMode.Bilinear),
                speed = speed, order = order };
        }

        private static RunnerMapCatalog.MapLayer Moon(string relativePath, string id)
        {
            string path = "Assets/2D Sprites/" + relativePath;
            return new RunnerMapCatalog.MapLayer { source = AssetDatabase.LoadAssetAtPath<Texture2D>(path),
                sprite = RunnerMapArt.Moon(path, id), stationary = true, height = 1.2f, bottom = 2.9f, x = 6.4f, order = -95 };
        }

        private static RunnerMapCatalog.MapLayer Ground(string id, uint dark, uint light) => new RunnerMapCatalog.MapLayer {
            source = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/2D Sprites/forest bg/ground.png"),
            sprite = RunnerMapArt.Ground(id, PixelMenuArt.Hex(dark), PixelMenuArt.Hex(light)), height = 1f, bottom = -3.96f, speed = 1f, order = 0 };

        private static void References(SerializedProperty property, Object[] values)
        {
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static Transform Node(string name, Transform parent, float x, float y, float width, float height)
        {
            Transform node = parent.Find(name);
            if (node == null) { node = new GameObject(name, typeof(RectTransform)).transform; node.SetParent(parent, false); }
            var rect = (RectTransform)node;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            return node;
        }

        private static Image Art(Transform node, Sprite sprite, bool raycast = false)
        {
            Image image = node.GetComponent<Image>();
            if (image == null) image = node.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.raycastTarget = raycast;
            image.type = sprite != null && sprite.border.sqrMagnitude > 0 ? Image.Type.Sliced : Image.Type.Simple;
            return image;
        }

        private static Button Button(Transform node, Sprite sprite)
        {
            Image image = Art(node, sprite, true);
            image.pixelsPerUnitMultiplier = 0.5f;
            Button button = node.GetComponent<Button>();
            if (button == null) button = node.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 0.83f);
            colors.pressedColor = new Color(0.74f, 0.84f, 0.7f);
            colors.fadeDuration = 0.07f;
            button.colors = colors;
            return button;
        }

        private static Text Label(Transform node, string value, Font font, int size, Color color)
        {
            Text text = node.GetComponent<Text>();
            if (text == null) text = node.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.text = value;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.alignByGeometry = true;
            text.raycastTarget = false;
            text.lineSpacing = 1.65f;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }
    }
}
