using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DinoRush.Tests
{
    public sealed class RunnerAccountResetAuthoringTests
    {
        [Test]
        public void SavedOptionsContainAnEditableResetActionAndAnInitiallyHiddenConfirmation()
        {
            Scene scene = SceneManager.GetSceneByPath("Assets/Scenes/ForestRun.unity");
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestRun.unity", OpenSceneMode.Additive);
            try
            {
                var menu = scene.GetRootGameObjects().Select(root => root.GetComponentInChildren<ForestMainMenu>(true)).First(value => value != null);
                Assert.That(menu.AccountReset, Is.Not.Null);
                Assert.That(menu.transform.Find("Menu dialog/Moss framed dialog/Options page/Reset account").GetComponent<Button>(), Is.Not.Null);
                Assert.That(menu.transform.Find("Reset account confirmation").gameObject.activeSelf, Is.False);
                var data = new SerializedObject(menu.AccountReset);
                foreach (string field in new[] { "overlay", "panel", "open", "cancel", "confirm", "heading", "warning", "details", "note" })
                    Assert.That(data.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
                var roster = AssetDatabase.LoadAssetAtPath<RunnerDinoRoster>("Assets/Generated/ForestDinoRoster.asset");
                string[] keys = RunnerAccountProgress.Keys(roster).ToArray();
                Assert.That(keys.Distinct().Count(), Is.EqualTo(keys.Length));
                Assert.That(keys, Does.Contain(RunnerShopProgress.FragmentKey("sena")));
                Assert.That(keys, Does.Contain(RunnerTrailProgress.OwnershipKey(RunnerTrailKind.Smoke)));
                Assert.That(keys, Does.Contain(RunnerSlashProgress.ChargesKey));
                Assert.That(keys, Does.Not.Contain("DinoRush.Muted"));
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
