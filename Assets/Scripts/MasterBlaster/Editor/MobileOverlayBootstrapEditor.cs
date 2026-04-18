using HybridGame.MasterBlaster.Scripts.Mobile;
using UnityEditor;
using UnityEngine;

namespace HybridGame.Editor
{
    [CustomEditor(typeof(MobileOverlayBootstrap))]
    public class MobileOverlayBootstrapEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var boot = (MobileOverlayBootstrap)target;
            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                if (GUILayout.Button("Generate Default Mobile Overlay Hierarchy"))
                {
                    Undo.RegisterFullObjectHierarchyUndo(boot.gameObject, "Mobile Overlay Hierarchy");
                    boot.PopulateDefaultAuthoringHierarchy();
                    EditorUtility.SetDirty(boot);
                }
            }

            if (Application.isPlaying)
                EditorGUILayout.HelpBox("Stop Play Mode to generate or edit the authoring hierarchy.", MessageType.Info);
        }

        [MenuItem("GameObject/MasterBlaster/Mobile Overlay Manager", false, 10)]
        private static void CreateMobileOverlayManager()
        {
            var go = new GameObject("MobileOverlayManager");
            Undo.RegisterCreatedObjectUndo(go, "Create Mobile Overlay Manager");
            var boot = go.AddComponent<MobileOverlayBootstrap>();
            boot.PopulateDefaultAuthoringHierarchy();
            EditorUtility.SetDirty(boot);
            Selection.activeGameObject = go;
            UnityEngine.Debug.Log(
                "[MasterBlaster][MobileOverlay] Created MobileOverlayManager. Save your scene to keep it.");
        }
    }
}
