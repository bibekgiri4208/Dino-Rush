using System.Collections.Generic;
using System.IO;
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
        public void RosterPreservesTheOriginalFourAndAddsAllSixNamedDinos()
        {
            RunnerDinoRoster roster = AssetDatabase.LoadAssetAtPath<RunnerDinoRoster>("Assets/Generated/ForestDinoRoster.asset");
            Assert.That(roster, Is.Not.Null);
            Assert.That(roster.Count, Is.EqualTo(10));
            string[] names = { "Blue", "Red", "Yellow", "Green", "Cole", "Kira", "Kuro", "Loki", "Mono", "Sena" };
            var ids = new HashSet<string>();
            for (int i = 0; i < roster.Count; i++)
            {
                RunnerDinoRoster.Dino dino = roster[i];
                Assert.That(ids.Add(dino.Id), Is.True);
                Assert.That(dino.Id, Is.EqualTo(names[i].ToLowerInvariant()));
                Assert.That(dino.DisplayName, Is.EqualTo(names[i]));
                Assert.That(AssetDatabase.GetAssetPath(dino.Sheet), Is.EqualTo(i < 4
                    ? $"Assets/2D Sprites/dino/DinoSprites - {i + 1}.png" : $"Assets/Generated/Dinos/{dino.Id}_runner.png"));
                Assert.That(dino.Sheet.width, Is.EqualTo(576));
                Assert.That(dino.Sheet.height, Is.EqualTo(24));
                Assert.That(dino.Portrait, Is.Not.Null);
                if (i < 4) Assert.That(dino.Portrait.texture, Is.EqualTo(dino.Sheet));
                else Assert.That(AssetDatabase.GetAssetPath(dino.Portrait), Is.EqualTo($"Assets/2D Sprites/dino/{dino.Id}/base/idle.png"));
                Assert.That(EditorUtility.IsPersistent(dino.Portrait), Is.True);
                Assert.That(roster.Find(dino.Id), Is.EqualTo(i));
            }
            Assert.That(roster.Find("removed-dino"), Is.Zero);
            Assert.That(roster.Find(null), Is.Zero);
        }

        [Test]
        public void ScenePersistsTheRosterAndEveryCollectionCardBinding()
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
                    Assert.That(choices.arraySize, Is.EqualTo(game.DinoRoster.Count));
                    for (int i = 0; i < choices.arraySize; i++) Assert.That(choices.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null);
                }
                foreach (string field in new[] { "selectedDinoLabel", "selectedDinoFrame", "unselectedDinoFrame" })
                    Assert.That(data.FindProperty(field).objectReferenceValue, Is.Not.Null);
                foreach (string field in new[] { "buyMysteryEgg", "shopBalance", "shopResult" })
                    Assert.That(data.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
                SerializedProperty fragments = data.FindProperty("dinoFragments");
                Assert.That(fragments.arraySize, Is.EqualTo(game.DinoRoster.Count));
                for (int i = 0; i < fragments.arraySize; i++) Assert.That(fragments.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null);
                var shopButton = data.FindProperty("customize").objectReferenceValue as UnityEngine.UI.Button;
                Assert.That(shopButton.name, Is.EqualTo("Shop"));
                Assert.That(shopButton.GetComponentInChildren<UnityEngine.UI.Text>(true).text, Is.EqualTo("SHOP"));
            }
            finally
            {
                if (openedForTest) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        [Test]
        public void NewRunnerSheetsUseEveryMatchingLegacyActionCellIncludingKickDashAndSuppliedHurtPoses()
        {
            var roster = AssetDatabase.LoadAssetAtPath<RunnerDinoRoster>("Assets/Generated/ForestDinoRoster.asset");
            // The legacy 11/12 jump pair is visually kick 1/2, NOT the baked-height jump strip.
            // Its six low duck frames are dash 0..5, NOT the upright avoid animation.
            string[] actions = { "idle", "idle", "idle", "idle", "move", "move", "move", "move", "move", "move",
                "kick", "kick", "kick", "hurt", "hurt", "hurt", "hurt", "dash", "dash", "dash", "dash", "dash", "dash", "dash" };
            int[] sourceFrames = { 0, 1, 2, 0, 0, 1, 2, 3, 4, 5, 0, 1, 2, 0, 1, 2, 3, 1, 0, 1, 2, 3, 4, 5 };
            for (int i = 4; i < roster.Count; i++)
            {
                var sheet = new Texture2D(2, 2);
                try
                {
                    sheet.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(roster[i].Sheet)));
                    for (int pose = 0; pose < actions.Length; pose++)
                    {
                        var source = new Texture2D(2, 2);
                        try
                        {
                            source.LoadImage(File.ReadAllBytes("Assets/2D Sprites/dino/" + roster[i].Id + "/base/" + actions[pose] + ".png"));
                            for (int y = 0; y < 24; y++)
                                for (int x = 0; x < 24; x++)
                                {
                                    Color32 expected = source.GetPixel((sourceFrames[pose] % (source.width / 24)) * 24 + x, y);
                                    Assert.That((Color32)sheet.GetPixel(pose * 24 + x, y), Is.EqualTo(expected), roster[i].Id + " " + actions[pose] + " slot " + pose);
                                }
                        }
                        finally { Object.DestroyImmediate(source); }
                    }
                }
                finally { Object.DestroyImmediate(sheet); }
            }
        }

        [Test]
        public void AllTenDinosUseLowDuckSilhouettesAndJumpPosesWithoutBakedVerticalDisplacement()
        {
            var roster = AssetDatabase.LoadAssetAtPath<RunnerDinoRoster>("Assets/Generated/ForestDinoRoster.asset");
            for (int dino = 0; dino < roster.Count; dino++)
            {
                var sheet = new Texture2D(2, 2);
                try
                {
                    sheet.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(roster[dino].Sheet)));
                    int standingTop = OpaqueBounds(sheet, 0).yMax;
                    int groundedBottom = OpaqueBounds(sheet, 0).yMin;
                    Assert.That(OpaqueBounds(sheet, 11).yMin, Is.EqualTo(groundedBottom), roster[dino].Id + " ascending pose must not move the pixels up inside the cell");
                    Assert.That(OpaqueBounds(sheet, 12).yMin, Is.EqualTo(groundedBottom), roster[dino].Id + " falling pose must not move the pixels up inside the cell");
                    for (int frame = 18; frame < 24; frame++)
                        Assert.That(OpaqueBounds(sheet, frame).yMax, Is.LessThan(standingTop), roster[dino].Id + " duck frame " + frame + " must actually lower its head");
                }
                finally { Object.DestroyImmediate(sheet); }
            }
        }

        private static RectInt OpaqueBounds(Texture2D sheet, int frame)
        {
            int left = 24, bottom = 24, right = 0, top = 0;
            for (int y = 0; y < 24; y++)
                for (int x = 0; x < 24; x++)
                    if (sheet.GetPixel(frame * 24 + x, y).a > 0)
                    {
                        left = Mathf.Min(left, x); right = Mathf.Max(right, x + 1);
                        bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y + 1);
                    }
            return new RectInt(left, bottom, right - left, top - bottom);
        }
    }
}
