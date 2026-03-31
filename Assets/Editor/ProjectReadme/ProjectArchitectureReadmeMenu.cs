using UnityEditor;
using UnityEngine;

namespace HybridGame.MasterBlaster.EditorDocs
{
    public static class ProjectArchitectureReadmeMenu
    {
        private const string ReadmeAssetPath = "Assets/Editor/ProjectReadme/ProjectArchitectureReadme.asset";
        private const string AutoOpenPrefKey = "HybridGame.MasterBlaster.ProjectArchitectureReadme.AutoOpenedOnce";

        [MenuItem("Tools/MasterBlaster/Open Architecture Readme", priority = 10)]
        public static void OpenReadme()
        {
            var readme = AssetDatabase.LoadAssetAtPath<ProjectArchitectureReadme>(ReadmeAssetPath);
            if (readme == null)
            {
                EditorUtility.DisplayDialog(
                    "Architecture Readme not found",
                    "Missing Readme asset at:\n\n" + ReadmeAssetPath + "\n\n" +
                    "If it was deleted, recreate one via:\n" +
                    "Assets > Create > HybridGame > MasterBlaster > Project Architecture Readme",
                    "OK"
                );
                return;
            }

            Selection.activeObject = readme;
            EditorGUIUtility.PingObject(readme);
        }

        [MenuItem("Tools/MasterBlaster/Open Architecture Readme", validate = true)]
        private static bool ValidateOpenReadme() => !EditorApplication.isCompiling;

        [InitializeOnLoadMethod]
        private static void AutoOpenOnceOnProjectLoad()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            if (EditorPrefs.GetBool(AutoOpenPrefKey, false))
                return;

            EditorPrefs.SetBool(AutoOpenPrefKey, true);

            // Defer selection until the editor is fully ready.
            EditorApplication.delayCall += OpenReadme;
        }
    }
}

