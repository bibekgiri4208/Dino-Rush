using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DinoRush.Tests
{
    public sealed class ForestUiMotionAuthoringTests
    {
        [Test]
        public void SavedSceneHasFeedbackOnEveryButtonAndEditableModalTransitions()
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
                Transform root = game.transform.Find("Forest runner UI/16 by 9 safe content");
                foreach (Button button in root.GetComponentsInChildren<Button>(true))
                {
                    Assert.That(button.GetComponents<RunnerUiButtonFeedback>().Length, Is.EqualTo(1), button.name);
                    Assert.That(button.GetComponent<Outline>(), Is.Not.Null, button.name);
                    Assert.That(button.GetComponent<Outline>().enabled, Is.False, "Resting artwork must stay unchanged.");
                }
                foreach (string path in new[] { "Forest Main Menu/Main menu buttons", "Forest Main Menu/Dino Rush Logo",
                    "Forest Main Menu/Menu dialog/Moss framed dialog", "Menu card", "Forest Pause Menu", "Milestone toast" })
                {
                    Transform target = root.Find(path);
                    Assert.That(target.GetComponent<RunnerUiReveal>(), Is.Not.Null, path);
                    CanvasGroup group = target.GetComponent<CanvasGroup>();
                    Assert.That(group.alpha, Is.EqualTo(1f), path);
                    Assert.That(group.interactable && group.blocksRaycasts, Is.True, path);
                }
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        [Test]
        public void ReinstallingEffectsDoesNotDuplicateComponentsOrChangeTheRestingLayout()
        {
            var root = new GameObject("UI motion test", typeof(RectTransform));
            try
            {
                var control = new GameObject("Jump", typeof(RectTransform), typeof(Image), typeof(Button));
                control.transform.SetParent(root.transform, false);
                var panel = new GameObject("Menu card", typeof(RectTransform), typeof(Image));
                panel.transform.SetParent(root.transform, false);
                var rect = control.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(400, -250);
                rect.localScale = new Vector3(1.2f, 0.9f, 1f);
                RunnerUiEffects.Install((RectTransform)root.transform);
                RunnerUiEffects.Install((RectTransform)root.transform);
                Assert.That(control.GetComponents<RunnerUiButtonFeedback>().Length, Is.EqualTo(1));
                Assert.That(control.GetComponents<Outline>().Length, Is.EqualTo(1));
                Assert.That(control.GetComponents<RunnerUiReveal>().Length, Is.EqualTo(1));
                Assert.That(panel.GetComponents<RunnerUiReveal>().Length, Is.EqualTo(1));
                Assert.That(panel.GetComponents<CanvasGroup>().Length, Is.EqualTo(1));
                Assert.That(rect.anchoredPosition, Is.EqualTo(new Vector2(400, -250)));
                Assert.That(rect.localScale, Is.EqualTo(new Vector3(1.2f, 0.9f, 1f)));
                Assert.That(panel.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
