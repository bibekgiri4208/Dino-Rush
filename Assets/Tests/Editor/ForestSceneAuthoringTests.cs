using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DinoRush.Tests
{
    public sealed class ForestSceneAuthoringTests
    {
        [Test]
        public void ForestIsVisibleAndPersistedWithoutEnteringPlayMode()
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene forest = SceneManager.GetSceneByPath("Assets/Scenes/ForestRun.unity");
            bool openedForTest = !forest.IsValid() || !forest.isLoaded;
            if (openedForTest) forest = EditorSceneManager.OpenScene("Assets/Scenes/ForestRun.unity", OpenSceneMode.Additive);
            try
            {
                ForestRunnerGame game = null;
                foreach (GameObject root in forest.GetRootGameObjects())
                {
                    game = root.GetComponentInChildren<ForestRunnerGame>();
                    if (game != null) break;
                }
                Assert.That(game, Is.Not.Null);
                Assert.That(game.HasAuthoredScene, Is.True);
                ForestLeafWind wind = game.GetComponent<ForestLeafWind>();
                Assert.That(wind, Is.Not.Null, "The forest breeze must be wired in the saved scene.");
                var windData = new SerializedObject(wind);
                Assert.That(windData.FindProperty("game").objectReferenceValue, Is.EqualTo(game));
                Assert.That(windData.FindProperty("worldCamera").objectReferenceValue, Is.Not.Null);
                Assert.That(windData.FindProperty("spriteMaterial").objectReferenceValue, Is.Not.Null);
                Assert.That(AssetDatabase.GetAssetPath(windData.FindProperty("leafTexture").objectReferenceValue),
                    Is.EqualTo("Assets/2D Sprites/leaf.png"), "Use the supplied leaf artwork, not a generated replacement.");
                SnowMountainSnowfall snowfall = game.GetComponent<SnowMountainSnowfall>();
                Assert.That(snowfall, Is.Not.Null, "Mountain snowfall must be wired in the saved scene.");
                var snowData = new SerializedObject(snowfall);
                Assert.That(snowData.FindProperty("game").objectReferenceValue, Is.EqualTo(game));
                Assert.That(snowData.FindProperty("worldCamera").objectReferenceValue,
                    Is.EqualTo(windData.FindProperty("worldCamera").objectReferenceValue));
                Assert.That(snowData.FindProperty("spriteMaterial").objectReferenceValue, Is.Not.Null);
                JungleRain rain = game.GetComponent<JungleRain>();
                Assert.That(rain, Is.Not.Null, "Jungle rain must be wired in the saved scene.");
                var rainData = new SerializedObject(rain);
                Assert.That(rainData.FindProperty("game").objectReferenceValue, Is.EqualTo(game));
                Assert.That(rainData.FindProperty("worldCamera").objectReferenceValue,
                    Is.EqualTo(windData.FindProperty("worldCamera").objectReferenceValue));
                Assert.That(rainData.FindProperty("spriteMaterial").objectReferenceValue, Is.Not.Null);
                Assert.That(game.GetComponentsInChildren<Camera>(true).Length, Is.EqualTo(1));
                Assert.That(game.GetComponentsInChildren<Canvas>(true).Length, Is.EqualTo(1));
                Assert.That(game.transform.Find("Forest environment"), Is.Not.Null);
                Transform dino = game.transform.Find("Player/Blue dino player");
                Assert.That(dino.GetComponent<SpriteRenderer>().sprite, Is.Not.Null);
                Assert.That(dino.position.y, Is.EqualTo(RunnerRules.GroundY).Within(0.0001f),
                    "The saved dino preview must use the same grass-contact height as gameplay.");
                Assert.That(game.transform.Find("Player/Dino ground shadow").position.y,
                    Is.EqualTo(RunnerRules.GroundY - 0.025f).Within(0.0001f));
                for (int i = 0; i < 3; i++)
                    Assert.That(game.transform.Find("Obstacle pool/Pooled obstacle " + i).position.y,
                        Is.EqualTo(RunnerRules.GroundY).Within(0.0001f));

                var serialized = new SerializedObject(game);
                SerializedProperty soilArt = serialized.FindProperty("soilSprites");
                Assert.That(soilArt.arraySize, Is.EqualTo(game.MapCatalog.Count));
                for (int i = 0; i < soilArt.arraySize; i++)
                    Assert.That(EditorUtility.IsPersistent(soilArt.GetArrayElementAtIndex(i).objectReferenceValue), Is.True);
                var soil = game.transform.Find("Forest environment/Earth below the trail").GetComponent<SpriteRenderer>();
                Assert.That(soil.sprite, Is.EqualTo(soilArt.GetArrayElementAtIndex(0).objectReferenceValue));
                Assert.That(soil.drawMode, Is.EqualTo(SpriteDrawMode.Tiled));
                Assert.That(soil.transform.localScale, Is.EqualTo(Vector3.one));
                SerializedProperty frames = serialized.FindProperty("dinoFrames");
                Assert.That(frames.arraySize, Is.EqualTo(24));
                for (int i = 0; i < frames.arraySize; i++)
                    Assert.That(EditorUtility.IsPersistent(frames.GetArrayElementAtIndex(i).objectReferenceValue), Is.True);

                int stagedObstacles = 0;
                foreach (SpriteRenderer renderer in game.GetComponentsInChildren<SpriteRenderer>())
                {
                    Assert.That(renderer.sprite, Is.Not.Null);
                    Assert.That(EditorUtility.IsPersistent(renderer.sprite), Is.True, renderer.name + " has a transient sprite");
                    if (renderer.name.StartsWith("Pooled obstacle")) stagedObstacles++;
                }
                Assert.That(stagedObstacles, Is.Zero, "The title-screen preview must not show active gameplay obstacles.");
                ForestMainMenu menu = game.GetComponentInChildren<ForestMainMenu>();
                Assert.That(menu, Is.Not.Null);
                var subtitle = menu.transform.Find("Dino Rush Logo/Forest subtitle plaque/Forest subtitle").GetComponent<UnityEngine.UI.Text>();
                Assert.That(subtitle.text, Is.EqualTo("A JOURNEY HOME"));
                var menuData = new SerializedObject(menu);
                var cornerQuit = (RectTransform)menu.transform.Find("Quit");
                var titleSound = (RectTransform)menu.transform.Find("SFX toggle");
                Assert.That(menuData.FindProperty("quit").objectReferenceValue, Is.EqualTo(cornerQuit.GetComponent<UnityEngine.UI.Button>()));
                Assert.That(menu.transform.Find("Main menu buttons/Quit"), Is.Null);
                Assert.That(cornerQuit.anchoredPosition, Is.EqualTo(new Vector2(-titleSound.anchoredPosition.x, titleSound.anchoredPosition.y)));
                Assert.That(cornerQuit.sizeDelta, Is.EqualTo(titleSound.sizeDelta));
                Assert.That(((GameObject)menuData.FindProperty("customizePage").objectReferenceValue).activeSelf, Is.False);
                Assert.That(((GameObject)menuData.FindProperty("optionsPage").objectReferenceValue).activeSelf, Is.True);
                Assert.That(((GameObject)menuData.FindProperty("quitPage").objectReferenceValue).activeSelf, Is.False);
                var modes = (GameObject)menuData.FindProperty("modePage").objectReferenceValue;
                Assert.That(modes, Is.Not.Null);
                Assert.That(modes.activeSelf, Is.False);
                Assert.That(modes.transform.Find("Story mode").GetComponent<UnityEngine.UI.Button>().interactable, Is.True);
                Assert.That(menuData.FindProperty("endlessMode").objectReferenceValue,
                    Is.EqualTo(modes.transform.Find("Endless mode").GetComponent<UnityEngine.UI.Button>()));
                Assert.That(game.GetComponentInChildren<Canvas>(true).pixelPerfect, Is.True);
                SerializedProperty hud = serialized.FindProperty("hud");
                foreach (string field in new[] { "pausePanel", "resumeControl", "restartControl", "quitRunControl", "resultsMenuControl", "pausedScore", "soundIcon", "statsFrame", "duckButton" })
                {
                    Object reference = hud.FindPropertyRelative(field).objectReferenceValue;
                    Assert.That(reference, Is.Not.Null, "Missing saved gameplay UI binding: " + field);
                    Assert.That(EditorUtility.IsPersistent(reference), Is.False, "UI binding must point into the scene, not a prefab asset.");
                }
                Assert.That(((GameObject)hud.FindPropertyRelative("pausePanel").objectReferenceValue).activeSelf, Is.False);
                var duck = (GameObject)hud.FindPropertyRelative("duckButton").objectReferenceValue;
                Assert.That(duck.activeSelf, Is.False, "The title preview must hide gameplay controls.");
                Assert.That(duck.GetComponent<RunnerDuckControl>(), Is.Not.Null);
                SerializedProperty dirtArt = serialized.FindProperty("dirtSprites");
                Assert.That(dirtArt.arraySize, Is.EqualTo(6));
                for (int i = 0; i < dirtArt.arraySize; i++)
                {
                    var sprite = dirtArt.GetArrayElementAtIndex(i).objectReferenceValue as Sprite;
                    Assert.That(sprite, Is.Not.Null);
                    Assert.That(EditorUtility.IsPersistent(sprite), Is.True);
                    Assert.That(sprite.texture.filterMode, Is.EqualTo(FilterMode.Point));
                }
                SerializedProperty dirtPool = serialized.FindProperty("dust");
                Assert.That(dirtPool.arraySize, Is.EqualTo(ForestRunnerGame.DirtPoolCapacity));
                for (int i = 0; i < dirtPool.arraySize; i++)
                {
                    var renderer = (SpriteRenderer)dirtPool.GetArrayElementAtIndex(i).FindPropertyRelative("renderer").objectReferenceValue;
                    Assert.That(renderer.gameObject.activeSelf, Is.False, "Dirt must not run over the title-screen preview.");
                    Assert.That(renderer.sprite.name, Does.StartWith("Dirt "));
                }
                Font menuFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Fonts/PressStart2P-Regular.ttf");
                Assert.That(menuFont.fontNames, Does.Contain("Press Start 2P"));
            }
            finally
            {
                if (openedForTest) EditorSceneManager.CloseScene(forest, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        [Test]
        public void DetailedDirtArtAddsFeatheredCloudsWithoutReplacingTheOriginalAsset()
        {
            int shapes = 0, feathered = 0;
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath("Assets/Generated/ForestDirtDetailSprites.asset"))
            {
                if (!(asset is Sprite sprite)) continue;
                shapes++;
                Assert.That(sprite.texture.filterMode, Is.EqualTo(FilterMode.Point));
                if (sprite.name != "Dirt puff" && sprite.name != "Dirt curl" && sprite.name != "Dirt ground haze") continue;
                bool edge = false, core = false;
                foreach (Color pixel in sprite.texture.GetPixels())
                {
                    edge |= pixel.a > 0 && pixel.a < 0.5f;
                    core |= pixel.a > 0.9f;
                }
                if (edge && core) feathered++;
            }
            Assert.That(shapes, Is.EqualTo(6)); Assert.That(feathered, Is.EqualTo(3));
            Assert.That(AssetDatabase.LoadMainAssetAtPath("Assets/Generated/ForestDirtSprites.asset"), Is.Not.Null);
        }

        [Test]
        public void MenuFramingDoesNotPaintOverTheForestTrail()
        {
            var texture = new Texture2D(2, 2);
            try
            {
                Assert.That(texture.LoadImage(System.IO.File.ReadAllBytes("Assets/UI/ForestMenu/ForestFrame.png")), Is.True);
                Color32[] pixels = texture.GetPixels32();
                int canopyPixels = 0;
                for (int y = 0; y < texture.height; y++)
                for (int x = 0; x < texture.width; x++)
                {
                    byte alpha = pixels[y * texture.width + x].a;
                    if (y < texture.height / 2)
                        Assert.That(alpha, Is.Zero, "Decorative grass, logs and leaf fragments must not cover the trail.");
                    else if (alpha > 0) canopyPixels++;
                }
                Assert.That(canopyPixels, Is.GreaterThan(0), "Keep the upper forest canopy.");
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void MenuFramesHaveNonStretchingSpriteBorders()
        {
            foreach (string frame in new[] { "ButtonPlay", "ButtonForest", "SoundFrame", "DialogFrame" })
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/UI/ForestMenu/{frame}.png");
                Assert.That(sprite, Is.Not.Null);
                Assert.That(sprite.border.sqrMagnitude, Is.GreaterThan(0), frame + " needs nine-slice borders");
            }
        }
    }
}
