using UnityEditor;
using UnityEditor.SceneManagement;

namespace DinoRush.Editor
{
    public static class ForestRunnerMenu
    {
        [MenuItem("Dino Rush/Open Forest Run")]
        public static void OpenForest()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene("Assets/Scenes/ForestRun.unity");
        }
    }
}
