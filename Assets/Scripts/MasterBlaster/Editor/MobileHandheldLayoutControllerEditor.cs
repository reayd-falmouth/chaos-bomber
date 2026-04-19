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
        private bool _useScreenSizeAsKey = true;
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
            EditorGUILayout.HelpBox(
                "Capture calls BuildCaptureFromSceneRefs on the controller (not defined in this Editor). "
                + "Gameplay: Unity Camera on CinemachineBrain (rect, ortho/FoV, clip planes); each CinemachineCamera in CinemachineModeSwitcher (priority, lens override, FoV/ortho size, clip planes, Dutch). "
                + "UI/overlay: RectTransforms and CanvasScaler settings for the assigned roots.",
                MessageType.Info);

            _useScreenSizeAsKey = EditorGUILayout.ToggleLeft(
                "Use current Screen.width / Screen.height as key (recommended in Play Mode)",
                _useScreenSizeAsKey);

            if (!_useScreenSizeAsKey)
            {
                _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MaxHeight(100f));
                _captureScreenW = EditorGUILayout.IntField("Screen width (key)", _captureScreenW);
                _captureScreenH = EditorGUILayout.IntField("Screen height (key)", _captureScreenH);
                EditorGUILayout.EndScrollView();
            }

            _captureLabel = EditorGUILayout.TextField("Label", _captureLabel);

            if (!Application.isPlaying && _useScreenSizeAsKey)
            {
                EditorGUILayout.HelpBox(
                    "Outside Play Mode, Screen size may not match the Game view. Enter Play Mode for an accurate key, or turn off \"Use current Screen…\" and type width/height manually.",
                    MessageType.Warning);
            }

            EditorGUILayout.Space(4f);

            if (GUILayout.Button("Capture current layout to controller (lastCapturedPreset)"))
            {
                int w;
                int h;
                GetCaptureKey(out w, out h);
                Undo.RecordObject(ctrl, "Capture handheld layout to controller");
                ctrl.CaptureCurrentLayoutToScratch(w, h, _captureLabel);
                EditorUtility.SetDirty(ctrl);
                serializedObject.Update();
                UnityEngine.Debug.Log(
                    "[MasterBlaster][MobileHandheldLayout][Editor] Captured to lastCapturedPreset key=" + w + "x" + h + ".");
            }

            if (GUILayout.Button("Append capture to inline presets"))
            {
                int w;
                int h;
                GetCaptureKey(out w, out h);
                Undo.RecordObject(ctrl, "Append handheld layout to inline presets");
                ctrl.AppendCaptureToInlinePresets(w, h, _captureLabel);
                EditorUtility.SetDirty(ctrl);
                serializedObject.Update();
                UnityEngine.Debug.Log(
                    "[MasterBlaster][MobileHandheldLayout][Editor] Appended inline preset key=" + w + "x" + h + ".");
            }

            using (new EditorGUI.DisabledScope(_presetLibrary == null || _presetLibrary.objectReferenceValue == null))
            {
                if (GUILayout.Button("Append capture to preset library asset"))
                {
                    var lib = _presetLibrary.objectReferenceValue as MobileHandheldLayoutPresetLibrary;
                    if (lib != null)
                    {
                        int w;
                        int h;
                        GetCaptureKey(out w, out h);
                        Undo.RecordObject(lib, "Append handheld layout preset");
                        var entry = ctrl.BuildCaptureFromSceneRefs(w, h, _captureLabel);
                        lib.entries.Add(entry);
                        EditorUtility.SetDirty(lib);
                        AssetDatabase.SaveAssets();
                        UnityEngine.Debug.Log(
                            "[MasterBlaster][MobileHandheldLayout][Editor] Appended preset \"" + entry.label + "\" to " + AssetDatabase.GetAssetPath(lib));
                    }
                }
            }

            if (_presetLibrary == null || _presetLibrary.objectReferenceValue == null)
                EditorGUILayout.HelpBox("Optional: assign a Preset Library asset to also append rows into that .asset file.", MessageType.Info);

            EditorGUILayout.Space(4f);
            if (GUILayout.Button("Apply matching preset now (uses Screen.width/height)"))
            {
                Undo.RecordObject(ctrl, "Apply handheld layout preset");
                ctrl.ApplyNowForCurrentScreen();
                EditorUtility.SetDirty(ctrl);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void GetCaptureKey(out int w, out int h)
        {
            if (_useScreenSizeAsKey)
            {
                w = Screen.width;
                h = Screen.height;
            }
            else
            {
                w = _captureScreenW;
                h = _captureScreenH;
            }
        }
    }
}
