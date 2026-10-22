using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DinoRush.Tests
{
    public sealed class RunnerMenuThoughtAuthoringTests
    {
        [Test]
        public void SavedMenuHasOneEditableNonInteractiveThoughtPuffWithAllBindings()
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
                RunnerMenuThought[] thoughts = game.GetComponentsInChildren<RunnerMenuThought>(true);
                Assert.That(thoughts.Length, Is.EqualTo(1));
                RunnerMenuThought thought = thoughts[0];
                Assert.That(thought.transform.parent.name, Is.EqualTo("Main menu buttons"));
                var data = new SerializedObject(thought);
                Assert.That(data.FindProperty("game").objectReferenceValue, Is.EqualTo(game));
                Assert.That(data.FindProperty("dino").objectReferenceValue, Is.EqualTo(game.transform.Find("Player/Blue dino player").GetComponent<SpriteRenderer>()));
                Assert.That(data.FindProperty("worldCamera").objectReferenceValue, Is.EqualTo(game.GetComponentInChildren<Camera>(true)));
                Assert.That(data.FindProperty("caption").objectReferenceValue, Is.EqualTo(thought.GetComponentInChildren<Text>(true)));
                Assert.That(data.FindProperty("thoughts").arraySize, Is.GreaterThanOrEqualTo(3));
                Assert.That(data.FindProperty("firstDelay").floatValue, Is.GreaterThan(0));
                Assert.That(thought.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
                Assert.That(thought.GetComponent<Image>().sprite, Is.Not.Null);
                Assert.That(thought.GetComponent<Image>().sprite.texture.filterMode, Is.EqualTo(FilterMode.Point));
                foreach (Graphic graphic in thought.GetComponentsInChildren<Graphic>(true)) Assert.That(graphic.raycastTarget, Is.False);
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }
    }
}
