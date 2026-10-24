using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DinoRush.Tests
{
    public sealed class RunnerMinimalUiAuthoringTests
    {
        [Test]
        public void SceneSavesFocusedForestThemedCardsAndHiddenLegacyInstructions()
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath("Assets/Scenes/ForestRun.unity");
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestRun.unity", OpenSceneMode.Additive);
            try
            {
                ForestRunnerGame game = null;
                foreach (GameObject owner in scene.GetRootGameObjects())
                    if ((game = owner.GetComponentInChildren<ForestRunnerGame>()) != null) break;
                Assert.That(game, Is.Not.Null);
                var data = new SerializedObject(game);
                SerializedProperty hud = data.FindProperty("hud");
                Assert.That(hud.FindPropertyRelative("minimalPresentation").boolValue, Is.True);
                foreach (string field in new[] { "pausedBest", "resultScore", "resultBest" })
                    Assert.That(hud.FindPropertyRelative(field).objectReferenceValue, Is.Not.Null, field);
                Transform root = game.transform.Find("Forest runner UI/16 by 9 safe content");
                Assert.That(((RectTransform)root.Find("Forest Pause Menu")).sizeDelta, Is.EqualTo(new Vector2(420, 336)));
                Assert.That(((RectTransform)root.Find("Forest Main Menu/Menu dialog/Moss framed dialog")).sizeDelta, Is.EqualTo(new Vector2(480, 352)));
                foreach (string path in new[] { "Keyboard controls", "Controls", "Duck/Drop hint", "Menu card/Description", "Menu card/Result",
                    "Menu card/Shortcut", "Forest Pause Menu/Pause description", "Forest Pause Menu/Pause shortcut" })
                    Assert.That(root.Find(path).gameObject.activeSelf, Is.False, path);
                foreach (string path in new[] { "Forest Pause Menu", "Menu card" })
                {
                    Transform card = root.Find(path);
                    Image image = card.GetComponent<Image>();
                    Assert.That(image.sprite, Is.EqualTo(AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/ForestMenu/DialogFrame.png")));
                    Assert.That(image.type, Is.EqualTo(Image.Type.Sliced));
                    Assert.That(image.sprite.border.sqrMagnitude, Is.GreaterThan(0));
                    Assert.That(image.sprite.texture.filterMode, Is.EqualTo(FilterMode.Point));
                    Assert.That(card.GetComponent<RunnerUiReveal>(), Is.Not.Null);
                    foreach (Text text in card.GetComponentsInChildren<Text>(true))
                        if (text.gameObject.activeSelf) Assert.That(text.font.fontNames, Does.Contain("Press Start 2P"), text.name);
                    foreach (Button button in card.GetComponentsInChildren<Button>(true))
                    {
                        Assert.That(button.GetComponent<Image>().sprite.name, Is.EqualTo("ButtonPlay").Or.EqualTo("ButtonForest"));
                        Assert.That(button.GetComponent<Image>().sprite.texture.filterMode, Is.EqualTo(FilterMode.Point));
                    }
                }
                foreach (string name in new[] { "Jump", "Duck" })
                {
                    Transform control = root.Find(name);
                    Assert.That(control.GetComponent<Image>().sprite.name, Is.EqualTo("HudJump"));
                    Assert.That(control.GetComponentInChildren<Text>(true).font.fontNames, Does.Contain("Press Start 2P"));
                }
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }
    }
}
