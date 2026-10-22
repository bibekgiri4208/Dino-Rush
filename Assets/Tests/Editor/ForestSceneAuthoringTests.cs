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
                Assert.That(game.GetComponentsInChildren<Camera>(true).Length, Is.EqualTo(1));
                Assert.That(game.GetComponentsInChildren<Canvas>(true).Length, Is.EqualTo(1));
                Assert.That(game.transform.Find("Forest environment"), Is.Not.Null);
                Assert.That(game.transform.Find("Player/Blue dino player").GetComponent<SpriteRenderer>().sprite, Is.Not.Null);

                var serialized = new SerializedObject(game);
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
                var menuData = new SerializedObject(menu);
                Assert.That(((GameObject)menuData.FindProperty("customizePage").objectReferenceValue).activeSelf, Is.False);
                Assert.That(((GameObject)menuData.FindProperty("optionsPage").objectReferenceValue).activeSelf, Is.True);
                Assert.That(((GameObject)menuData.FindProperty("quitPage").objectReferenceValue).activeSelf, Is.False);
                Assert.That(game.GetComponentInChildren<Canvas>(true).pixelPerfect, Is.True);
                SerializedProperty hud = serialized.FindProperty("hud");
                foreach (string field in new[] { "pausePanel", "resumeControl", "restartControl", "quitRunControl", "resultsMenuControl", "pausedScore", "soundIcon", "statsFrame" })
                {
                    Object reference = hud.FindPropertyRelative(field).objectReferenceValue;
                    Assert.That(reference, Is.Not.Null, "Missing saved gameplay UI binding: " + field);
                    Assert.That(EditorUtility.IsPersistent(reference), Is.False, "UI binding must point into the scene, not a prefab asset.");
                }
                Assert.That(((GameObject)hud.FindPropertyRelative("pausePanel").objectReferenceValue).activeSelf, Is.False);
                SerializedProperty dirtArt = serialized.FindProperty("dirtSprites");
                Assert.That(dirtArt.arraySize, Is.EqualTo(3));
                for (int i = 0; i < dirtArt.arraySize; i++)
                {
                    var sprite = dirtArt.GetArrayElementAtIndex(i).objectReferenceValue as Sprite;
                    Assert.That(sprite, Is.Not.Null);
                    Assert.That(EditorUtility.IsPersistent(sprite), Is.True);
                    Assert.That(sprite.texture.filterMode, Is.EqualTo(FilterMode.Point));
                }
                SerializedProperty dirtPool = serialized.FindProperty("dust");
                Assert.That(dirtPool.arraySize, Is.EqualTo(36));
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
