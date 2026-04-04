#if UNITY_EDITOR
using System.Collections.Generic;
using HybridGame.MasterBlaster.Runtime.Scenes.Character;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HybridGame.Editor.MasterBlaster.Scripts.Editor
{
    /// <summary>
    /// Builds 3D character preview slots for Avatar Select. Assign <see cref="CharacterData.previewPrefab"/> on
    /// <see cref="AvatarController"/> for each entry, then run this menu. Re-run after changing prefabs.
    /// </summary>
    public static class AvatarPreviewStudioSetup
    {
        private const string StudioName = "AvatarPreviewStudio";
        private const string RenderTexturePath = "Assets/Settings/MasterBlaster/AvatarSelectPreview.renderTexture";

        private static readonly Vector3 StudioWorldPosition = new(5300f, 0f, 5300f);

        /// <summary>Invoked from the HybridGame.Menus editor menu bootstrap.</summary>
        public static void RunBuild()
        {
            var avatar = Object.FindFirstObjectByType<AvatarController>(FindObjectsInactive.Include);
            if (avatar == null)
            {
                EditorUtility.DisplayDialog(
                    "Avatar Preview Studio",
                    "No AvatarController found. Open the hybrid scene (e.g. MasterBlaster_FPS).",
                    "OK");
                return;
            }

            var so = new SerializedObject(avatar);
            var charsProp = so.FindProperty("characters");
            if (charsProp == null || !charsProp.isArray || charsProp.arraySize == 0)
            {
                EditorUtility.DisplayDialog("Avatar Preview Studio", "AvatarController.characters is empty.", "OK");
                return;
            }

            int n = charsProp.arraySize;
            int withPrefab = 0;
            for (int i = 0; i < n; i++)
            {
                var pf = charsProp.GetArrayElementAtIndex(i).FindPropertyRelative("previewPrefab");
                if (pf != null && pf.objectReferenceValue != null)
                    withPrefab++;
            }

            if (withPrefab == 0)
            {
                EditorUtility.DisplayDialog(
                    "Avatar Preview Studio",
                    "No preview prefabs assigned. On AvatarController, set Character Data → Preview Prefab for each character (3D model root), then run this again.",
                    "OK");
                return;
            }

            Undo.SetCurrentGroupName("Build Avatar Preview Studio");
            int undoGroup = Undo.GetCurrentGroup();

            var studio = FindOrCreateStudioRoot(out bool studioIsNew);
            if (studioIsNew)
                Undo.RegisterCreatedObjectUndo(studio, "Avatar Preview Studio Root");
            Undo.RecordObject(studio.transform, "Avatar Preview Studio position");
            studio.transform.SetPositionAndRotation(StudioWorldPosition, Quaternion.identity);

            ClearStudioChildren(studio.transform);

            var slots = new GameObject[n];
            for (int i = 0; i < n; i++)
            {
                var slotGo = new GameObject($"AvatarPreviewSlot_{i}");
                Undo.RegisterCreatedObjectUndo(slotGo, "Avatar preview slot");
                slotGo.transform.SetParent(studio.transform, false);
                slotGo.transform.localPosition = Vector3.zero;
                slotGo.transform.localRotation = Quaternion.identity;
                slotGo.transform.localScale = Vector3.one;

                var prefabProp = charsProp.GetArrayElementAtIndex(i).FindPropertyRelative("previewPrefab");
                var prefab = prefabProp != null ? prefabProp.objectReferenceValue as GameObject : null;
                if (prefab != null)
                {
                    var inst = (GameObject)Object.Instantiate(prefab, slotGo.transform);
                    inst.name = prefab.name + "_AvatarPreview";
                    Undo.RegisterCreatedObjectUndo(inst, "Avatar preview instance");
                    SanitizePreviewHierarchy(slotGo);
                }

                int previewLayer = LayerMask.NameToLayer(AvatarPreviewRenderer.PreviewLayerName);
                if (previewLayer < 0)
                    previewLayer = 0;
                SetLayerRecursively(slotGo, previewLayer);

                slotGo.SetActive(false);
                slots[i] = slotGo;
            }

            var camGo = GetOrCreateChild(studio.transform, "AvatarPreviewCamera");
            var cam = camGo.GetComponent<Camera>();
            if (cam == null)
                cam = Undo.AddComponent<Camera>(camGo);
            ConfigurePreviewCamera(cam);

            var lightGo = GetOrCreateChild(studio.transform, "AvatarPreviewKeyLight");
            var light = lightGo.GetComponent<Light>();
            if (light == null)
                light = Undo.AddComponent<Light>(lightGo);
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightGo.transform.localRotation = Quaternion.Euler(50f, -35f, 0f);

            var fillGo = GetOrCreateChild(studio.transform, "AvatarPreviewFillLight");
            var fill = fillGo.GetComponent<Light>();
            if (fill == null)
                fill = Undo.AddComponent<Light>(fillGo);
            fill.type = LightType.Directional;
            fill.intensity = 0.35f;
            fillGo.transform.localRotation = Quaternion.Euler(30f, 120f, 0f);

            WireAvatarPreviewRenderer(slots, cam, AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath));

            Undo.CollapseUndoOperations(undoGroup);
            EditorUtility.DisplayDialog(
                "Avatar Preview Studio",
                $"Built {n} slot(s) ({withPrefab} with prefabs) under {StudioName}. Save the scene.",
                "OK");
        }

        private static GameObject FindOrCreateStudioRoot(out bool createdNew)
        {
            createdNew = false;
            var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var r in roots)
            {
                if (r.name == StudioName)
                    return r;
            }

            createdNew = true;
            return new GameObject(StudioName);
        }

        private static void ClearStudioChildren(Transform studio)
        {
            var toDestroy = new List<GameObject>();
            for (int c = 0; c < studio.childCount; c++)
                toDestroy.Add(studio.GetChild(c).gameObject);
            foreach (var go in toDestroy)
                Undo.DestroyObjectImmediate(go);
        }

        private static GameObject GetOrCreateChild(Transform parent, string childName)
        {
            var t = parent.Find(childName);
            if (t != null)
                return t.gameObject;
            var go = new GameObject(childName);
            Undo.RegisterCreatedObjectUndo(go, childName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return go;
        }

        private static void ConfigurePreviewCamera(Camera cam)
        {
            int previewLayer = LayerMask.NameToLayer(AvatarPreviewRenderer.PreviewLayerName);
            if (previewLayer < 0)
                previewLayer = 0;

            cam.orthographic = false;
            cam.fieldOfView = 32f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 80f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.05f, 0.08f, 1f);
            cam.cullingMask = 1 << previewLayer;
            cam.depth = -9f;
            cam.transform.localPosition = new Vector3(0f, 1.35f, -2.85f);
            cam.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);
            cam.enabled = false;
        }

        private static void SanitizePreviewHierarchy(GameObject root)
        {
            foreach (var col in root.GetComponentsInChildren<Collider>(true))
                Undo.DestroyObjectImmediate(col);

            foreach (var rb in root.GetComponentsInChildren<Rigidbody>(true))
                Undo.DestroyObjectImmediate(rb);

            foreach (var rb2 in root.GetComponentsInChildren<Rigidbody2D>(true))
                Undo.DestroyObjectImmediate(rb2);

            foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null)
                    continue;
                var t = mb.GetType();
                if (typeof(Tilemap).IsAssignableFrom(t)
                    || typeof(TilemapRenderer).IsAssignableFrom(t)
                    || typeof(Grid).IsAssignableFrom(t)
                    || typeof(Animator).IsAssignableFrom(t))
                    continue;
                Undo.DestroyObjectImmediate(mb);
            }
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            var t = go.transform;
            for (int i = 0; i < t.childCount; i++)
                SetLayerRecursively(t.GetChild(i).gameObject, layer);
        }

        private static void WireAvatarPreviewRenderer(GameObject[] slots, Camera cam, RenderTexture rt)
        {
            var renderer = Object.FindFirstObjectByType<AvatarPreviewRenderer>(FindObjectsInactive.Include);
            if (renderer == null)
            {
                var ac = Object.FindFirstObjectByType<AvatarController>(FindObjectsInactive.Include);
                if (ac != null)
                    renderer = Undo.AddComponent<AvatarPreviewRenderer>(ac.gameObject);
            }

            if (renderer == null)
            {
                Debug.LogWarning("[AvatarPreviewStudioSetup] Could not add AvatarPreviewRenderer; assign manually.");
                return;
            }

            var so = new SerializedObject(renderer);
            so.FindProperty("previewCamera").objectReferenceValue = cam;
            so.FindProperty("renderTexture").objectReferenceValue = rt;

            var raw = FindAvatarPreviewRawImage();
            so.FindProperty("targetRawImage").objectReferenceValue = raw;

            var arr = so.FindProperty("previewSlotRoots");
            arr.ClearArray();
            for (int i = 0; i < slots.Length; i++)
            {
                arr.InsertArrayElementAtIndex(i);
                arr.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            if (raw != null && rt != null)
                raw.texture = rt;

            renderer.SetPreviewIndex(0);

            var avatar = Object.FindFirstObjectByType<AvatarController>(FindObjectsInactive.Include);
            if (avatar != null)
            {
                var aso = new SerializedObject(avatar);
                var ap = aso.FindProperty("avatarPreview");
                if (ap != null)
                {
                    ap.objectReferenceValue = renderer;
                    aso.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        private static RawImage FindAvatarPreviewRawImage()
        {
            foreach (var raw in Object.FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (raw != null && raw.gameObject.name == "AvatarPreviewRaw")
                    return raw;
            }

            return null;
        }
    }
}
#endif
