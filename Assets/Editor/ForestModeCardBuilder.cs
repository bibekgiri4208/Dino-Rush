using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DinoRush.Editor
{
    /// <summary>Illustrated mode cards, authored in place without replacing buttons or their bindings.</summary>
    public static class ForestModeCardBuilder
    {
        [MenuItem("Dino Rush/Refresh Endless Map Montage")]
        public static void RefreshEndlessMapMontage()
        {
            if (Application.isPlaying) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestRun.unity");
            ForestMainMenu menu = Object.FindAnyObjectByType<ForestMainMenu>();
            ForestModeCardArt.RefreshEndless(menu.GetComponentInParent<ForestRunnerGame>().MapCatalog);
            Apply(menu);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        [MenuItem("Dino Rush/Illustrate Compact Mode Selector")]
        public static void Build()
        {
            if (Application.isPlaying) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestRun.unity");
            ForestMainMenu menu = Object.FindAnyObjectByType<ForestMainMenu>();
            Apply(menu);
            ForestUiMotionBuilder.Apply(menu.GetComponentInParent<ForestRunnerGame>());
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        internal static void Apply(ForestMainMenu menu)
        {
            Transform panel = menu.transform.Find("Menu dialog/Moss framed dialog");
            Transform page = panel.Find("Mode selection page");
            if (page == null) return;
            ForestModeCardArt.Generate(menu.GetComponentInParent<ForestRunnerGame>().MapCatalog);
            Font font = panel.Find("Dialog heading").GetComponent<Text>().font;
            Sprite dino = menu.GetComponentInParent<ForestRunnerGame>().DinoRoster[0].Portrait;
            Transform legacy = page.Find("Story availability");
            if (legacy != null) legacy.gameObject.SetActive(false);
            foreach (bool story in new[] { true, false })
            {
                Transform card = page.Find(story ? "Story mode" : "Endless mode");
                Button button = card.GetComponent<Button>();
                button.interactable = !story;
                Image frame = card.GetComponent<Image>();
                frame.sprite = PixelMenuArt.Load("DialogFrame");
                frame.type = Image.Type.Sliced;
                frame.pixelsPerUnitMultiplier = 0.5f;
                frame.raycastTarget = true;
                ColorBlock colors = button.colors;
                colors.disabledColor = new Color(0.76f, 0.82f, 0.8f, 1);
                button.colors = colors;
                Image illustration = Picture("Mode illustration", card);
                illustration.sprite = PixelMenuArt.Load(story ? "ModeStory" : "ModeEndless");
                illustration.preserveAspect = !story;
                illustration.color = story ? new Color(0.8f, 0.86f, 0.82f, 1) : Color.white;
                Image runner = Picture("Trail dino", illustration.transform);
                runner.sprite = dino;
                runner.preserveAspect = true;
                runner.color = illustration.color;
                runner.gameObject.SetActive(story); // The montage stays unobstructed; Story keeps its journey dino.
                Text title = card.Find("Label").GetComponent<Text>();
                title.color = story ? PixelMenuArt.Hex(0xb6c8b5) : PixelMenuArt.Hex(0xffd275);
                title.text = story ? "STORY MODE" : "ENDLESS MODE";
                Caption("Mode description", card, font, story ? "A JOURNEY HOME" : "RUN. DODGE. REPEAT.", PixelMenuArt.Hex(0xaecb98));
                Caption("Mode status", card, font, story ? "COMING SOON" : "CHOOSE A MAP  >", story ? PixelMenuArt.Hex(0xd9bd83) : PixelMenuArt.Hex(0xf0f2cf));
            }
            // Author both layouts' resting content while preserving the clean Options editor preview.
            RunnerCompactMenus.ApplyDialog(panel, RunnerCompactMenus.DialogKind.Mode, false, new Vector2(1280, 720));
            menu.ApplyDialogLayout(false, new Vector2(1280, 720));
            EditorUtility.SetDirty(menu);
        }

        private static Transform Node(string name, Transform parent)
        {
            Transform node = parent.Find(name);
            if (node != null) return node;
            node = new GameObject(name, typeof(RectTransform)).transform;
            node.SetParent(parent, false);
            node.gameObject.layer = 5;
            return node;
        }

        private static Image Picture(string name, Transform parent)
        {
            Transform node = Node(name, parent);
            Image image = node.GetComponent<Image>();
            if (image == null) image = node.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private static void Caption(string name, Transform parent, Font font, string value, Color color)
        {
            Transform node = Node(name, parent);
            Text text = node.GetComponent<Text>();
            if (text == null) text = node.gameObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.alignByGeometry = true;
            text.raycastTarget = false;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }
    }
}
