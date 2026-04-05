using UnityEditor;
using UnityEngine;

namespace HybridGame.MasterBlaster.EditorDocs
{
    /// <summary>
    /// Opens the custom ProjectArchitectureReadme asset (Mermaid, coursework fields). The primary welcome
    /// screen is RMC ReadMe — see Assets/Documentation/ReadMe.asset and ReadMeHelper (com.rmc.rmc-readme package).
    /// </summary>
    public static class ProjectArchitectureReadmeMenu
    {
        public const string ReadmeAssetPath = "Assets/Documentation/ProjectArchitectureReadme.asset";

        [MenuItem("Tools/MasterBlaster/Open Project Readme (Documentation)", priority = 10)]
        public static void OpenReadme()
        {
            var readme = AssetDatabase.LoadAssetAtPath<ProjectArchitectureReadme>(ReadmeAssetPath);
            if (readme == null)
            {
                EditorUtility.DisplayDialog(
                    "Project Readme not found",
                    "Missing Readme asset at:\n\n" + ReadmeAssetPath + "\n\n" +
                    "Recreate via Assets > Create > HybridGame > MasterBlaster > Project Architecture Readme",
                    "OK"
                );
                return;
            }

            SelectReadmeAndFocusInspector(readme);
        }

        [MenuItem("Tools/MasterBlaster/Open Project Readme (Documentation)", validate = true)]
        private static bool ValidateOpenReadme() => !EditorApplication.isCompiling;

        private static void SelectReadmeAndFocusInspector(ProjectArchitectureReadme readme)
        {
            Selection.activeObject = readme;
            EditorGUIUtility.PingObject(readme);

            EditorApplication.delayCall += FocusInspectorWindow;
            EditorApplication.delayCall += () => EditorApplication.delayCall += FocusInspectorWindow;
        }

        private static void FocusInspectorWindow()
        {
            // Qualify UnityEditor.Editor — unqualified `Editor` can resolve to the HybridGame.MasterBlaster.Editor namespace.
            var inspectorType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
            if (inspectorType != null)
                EditorWindow.GetWindow(inspectorType);
        }
    }
}
