using UnityEditor;
using UnityEngine;

namespace HybridGame.MasterBlaster.EditorDocs
{
    public static class ProjectArchitectureReadmeMenu
    {
        /// <summary>Course readme asset (Inspector shows custom coursework + architecture UI).</summary>
        public const string ReadmeAssetPath = "Assets/Documentation/ProjectArchitectureReadme.asset";

        private const string AutoOpenPrefKey = "HybridGame.MasterBlaster.ProjectArchitectureReadme.AutoOpenedOnce";

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

        [MenuItem("Tools/MasterBlaster/Reset “show readme on first open” flag", priority = 11)]
        public static void ResetAutoOpenPref()
        {
            EditorPrefs.DeleteKey(AutoOpenPrefKey);
            Debug.Log("[ProjectReadme] Cleared EditorPrefs key — the readme will auto-select on the next domain reload / editor start.");
        }

        [InitializeOnLoadMethod]
        private static void AutoOpenOnceOnProjectLoad()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (EditorPrefs.GetBool(AutoOpenPrefKey, false))
                return;

            // Always defer: on load, isUpdating is often true; the old code returned early and never scheduled work.
            EditorApplication.delayCall += TryAutoOpenWhenEditorReady;
        }

        private static void TryAutoOpenWhenEditorReady()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryAutoOpenWhenEditorReady;
                return;
            }

            var readme = AssetDatabase.LoadAssetAtPath<ProjectArchitectureReadme>(ReadmeAssetPath);
            if (readme == null)
                return;

            SelectReadmeAndFocusInspector(readme);
            EditorPrefs.SetBool(AutoOpenPrefKey, true);
        }

        private static void SelectReadmeAndFocusInspector(ProjectArchitectureReadme readme)
        {
            Selection.activeObject = readme;
            EditorGUIUtility.PingObject(readme);

            // Inspector sometimes does not get focus on the first frame; defer focus.
            EditorApplication.delayCall += FocusInspectorWindow;

            // Second deferred frame helps when Project window steals focus during import.
            EditorApplication.delayCall += () => EditorApplication.delayCall += FocusInspectorWindow;
        }

        private static void FocusInspectorWindow()
        {
            // Unity 6 may split types across modules; resolve from the same assembly as Editor.
            var inspectorType = typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow");
            if (inspectorType != null)
                EditorWindow.GetWindow(inspectorType);
        }
    }
}
