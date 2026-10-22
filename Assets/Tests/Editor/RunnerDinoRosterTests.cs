using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DinoRush.Tests
{
    public sealed class RunnerDinoRosterTests
    {
        [Test]
        public void RosterUsesAllFourOriginalSheetsAndExistingPortraitSlices()
        {
            RunnerDinoRoster roster = AssetDatabase.LoadAssetAtPath<RunnerDinoRoster>("Assets/Generated/ForestDinoRoster.asset");
            Assert.That(roster, Is.Not.Null);
            Assert.That(roster.Count, Is.EqualTo(4));
            string[] names = { "Blue", "Red", "Yellow", "Green" };
            var ids = new HashSet<string>();
            for (int i = 0; i < roster.Count; i++)
            {
                RunnerDinoRoster.Dino dino = roster[i];
                Assert.That(ids.Add(dino.Id), Is.True);
                Assert.That(dino.Id, Is.EqualTo(names[i].ToLowerInvariant()));
                Assert.That(dino.DisplayName, Is.EqualTo(names[i]));
                Assert.That(AssetDatabase.GetAssetPath(dino.Sheet), Is.EqualTo($"Assets/2D Sprites/dino/DinoSprites - {i + 1}.png"));
                Assert.That(dino.Sheet.width, Is.EqualTo(576));
                Assert.That(dino.Sheet.height, Is.EqualTo(24));
                Assert.That(dino.Portrait, Is.Not.Null);
                Assert.That(dino.Portrait.texture, Is.EqualTo(dino.Sheet));
                Assert.That(EditorUtility.IsPersistent(dino.Portrait), Is.True);
                Assert.That(roster.Find(dino.Id), Is.EqualTo(i));
            }
            Assert.That(roster.Find("removed-dino"), Is.Zero);
            Assert.That(roster.Find(null), Is.Zero);
        }

        [Test]
        public void ScenePersistsTheRosterAndFourChoiceBindings()
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath("Assets/Scenes/ForestRun.unity");
            bool openedForTest = !scene.IsValid() || !scene.isLoaded;
            if (openedForTest) scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestRun.unity", OpenSceneMode.Additive);
            try
            {
                ForestRunnerGame game = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                    if ((game = root.GetComponentInChildren<ForestRunnerGame>()) != null) break;
                Assert.That(game.DinoRoster, Is.Not.Null);
                var data = new SerializedObject(game.GetComponentInChildren<ForestMainMenu>(true));
                foreach (string field in new[] { "dinoChoices", "dinoPortraits", "dinoCaptions" })
                {
                    SerializedProperty choices = data.FindProperty(field);
                    Assert.That(choices.arraySize, Is.EqualTo(4));
                    for (int i = 0; i < 4; i++) Assert.That(choices.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null);
                }
                foreach (string field in new[] { "selectedDinoLabel", "selectedDinoFrame", "unselectedDinoFrame" })
                    Assert.That(data.FindProperty(field).objectReferenceValue, Is.Not.Null);
            }
            finally
            {
                if (openedForTest) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }
    }
}
