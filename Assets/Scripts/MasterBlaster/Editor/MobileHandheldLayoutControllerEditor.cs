using HybridGame.MasterBlaster.Scripts.Mobile.Layout;
using UnityEditor;
using UnityEngine;

namespace HybridGame.MasterBlaster.Editor
{
    [CustomEditor(typeof(MobileHandheldLayoutController))]
    public sealed class MobileHandheldLayoutControllerEditor : UnityEditor.Editor
    {
        private SerializedProperty _presetLibrary;
        private int _captureScreenW = 1920;
        private int _captureScreenH = 1080;
        private string _captureLabel = "captured";
        private Vector2 _scroll;

        private void OnEnable()
        {
            _presetLibrary = serializedObject.FindProperty("presetLibrary");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();

            var ctrl = (MobileHandheldLayoutController)target;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Capture layout", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MaxHeight(120f));
            _captureScreenW = EditorGUILayout.IntField("Screen width (key)", _captureScreenW);
            _captureScreenH = EditorGUILayout.IntField("Screen height (key)", _captureScreenH);
            _captureLabel = EditorGUILayout.TextField("Label", _captureLabel);
            EditorGUILayout.EndScrollView();

            using (new EditorGUI.DisabledScope(_presetLibrary == null || _presetLibrary.objectReferenceValue == null))
            {
                if (GUILayout.Button("Append capture to preset library asset"))
                {
                    var lib = _presetLibrary.objectReferenceValue as MobileHandheldLayoutPresetLibrary;
                    if (lib != null)
                    {
                        Undo.RecordObject(lib, "Append handheld layout preset");
                        var entry = ctrl.BuildCaptureFromSceneRefs(_captureScreenW, _captureScreenH, _captureLabel);
                        lib.entries.Add(entry);
                        EditorUtility.SetDirty(lib);
                        AssetDatabase.SaveAssets();
                        UnityEngine.Debug.Log(
                            "[MasterBlaster][MobileHandheldLayout][Editor] Appended preset \"" + entry.label + "\" to " + AssetDatabase.GetAssetPath(lib));
                    }
                }
            }

            if (_presetLibrary == null || _presetLibrary.objectReferenceValue == null)
                EditorGUILayout.HelpBox("Assign a Mobile Handheld Layout Preset Library asset to append captures.", MessageType.Info);

            EditorGUILayout.Space(4f);
            if (GUILayout.Button("Apply matching preset now (uses Screen.width/height)"))
            {
                Undo.RecordObject(ctrl, "Apply handheld layout preset");
                ctrl.ApplyNowForCurrentScreen();
                EditorUtility.SetDirty(ctrl);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
