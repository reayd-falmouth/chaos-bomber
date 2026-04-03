#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using HybridGame.MasterBlaster.Runtime.Scenes.LevelSelect;
using HybridGame.MasterBlaster.Scripts.Arena;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HybridGame.Editor.MasterBlaster.Scripts.Editor
{
    /// <summary>
    /// Builds duplicated wall geometry for Level Select top-down previews. Re-run after changing arena layouts.
    /// </summary>
    public static class ArenaPreviewStudioSetup
    {
        private const string StudioName = "ArenaPreviewStudio";
        private const string MenuPath = "HybridGame/Setup/Build Level Select Arena Preview Studio";
        private const string ToolsMenuPath = "Tools/Master Blaster/Build Level Select Arena Preview Studio";

        private static readonly Vector3 StudioWorldPosition = new(5200f, 0f, 5200f);

        [MenuItem(MenuPath, false, 510)]
        [MenuItem(ToolsMenuPath, false, 510)]
        private static void RunBuild()
        {
            // Game flow is often inactive in the editor; the switcher sits on HybridArenaGrid under that hierarchy.
            var switcher = Object.FindFirstObjectByType<HybridArenaLevelRootSwitcher>(FindObjectsInactive.Include);
            if (switcher == null)
            {
                EditorUtility.DisplayDialog(
                    "Arena Preview Studio",
                    "No HybridArenaLevelRootSwitcher found. Open the hybrid scene (e.g. MasterBlaster_FPS), run " +
                    "\"HybridGame → Setup → Master Blaster FPS — Build 10 Arena Slots And Wire Switcher\" if you have not yet, then try again.",
                    "OK");
                return;
            }

            var so = new SerializedObject(switcher);
            var rootsProp = so.FindProperty("levelWallRoots");
            if (rootsProp == null || !rootsProp.isArray || rootsProp.arraySize == 0)
            {
                EditorUtility.DisplayDialog(
                    "Arena Preview Studio",
                    "levelWallRoots is empty. Run \"Master Blaster FPS — Build 10 Arena Slots And Wire Switcher\" first, then save the scene.",
                    "OK");
                return;
            }

            var pairs = new List<(Transform d, Transform i)>();
            for (int n = 0; n < rootsProp.arraySize; n++)
            {
                var el = rootsProp.GetArrayElementAtIndex(n);
                var d = el.FindPropertyRelative("destructibleWallsRoot").objectReferenceValue as Transform;
                var ind = el.FindPropertyRelative("indestructibleWallsRoot").objectReferenceValue as Transform;
                if (d != null && ind != null)
                    pairs.Add((d, ind));
            }

            if (pairs.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Arena Preview Studio",
                    "No complete destructible/indestructible pairs assigned on HybridArenaLevelRootSwitcher.",
                    "OK");
                return;
            }

            Undo.SetCurrentGroupName("Build Level Select Arena Preview Studio");
            int undoGroup = Undo.GetCurrentGroup();

            var studio = FindOrCreateStudioRoot(out bool studioIsNew);
            if (studioIsNew)
                Undo.RegisterCreatedObjectUndo(studio, "Arena Preview Studio Root");
            Undo.RecordObject(studio.transform, "Arena Preview Studio position");
            studio.transform.SetPositionAndRotation(StudioWorldPosition, Quaternion.identity);

            ClearStudioChildren(studio.transform);

            var slots = new GameObject[pairs.Count];
            for (int i = 0; i < pairs.Count; i++)
            {
                var slotGo = new GameObject($"PreviewSlot_{i}");
                Undo.RegisterCreatedObjectUndo(slotGo, "Preview slot");
                slotGo.transform.SetParent(studio.transform, false);
                slotGo.transform.localPosition = Vector3.zero;
                slotGo.transform.localRotation = Quaternion.identity;
                slotGo.transform.localScale = Vector3.one;

                var copyD = (GameObject)Object.Instantiate(pairs[i].d.gameObject, slotGo.transform);
                var copyI = (GameObject)Object.Instantiate(pairs[i].i.gameObject, slotGo.transform);
                copyD.name = pairs[i].d.name + "_Preview";
                copyI.name = pairs[i].i.name + "_Preview";
                Undo.RegisterCreatedObjectUndo(copyD, "Preview walls");
                Undo.RegisterCreatedObjectUndo(copyI, "Preview walls");

                SanitizePreviewHierarchy(slotGo);
                int previewLayer = LayerMask.NameToLayer(LevelArenaPreviewRenderer.PreviewLayerName);
                if (previewLayer < 0)
                    previewLayer = 0;
                SetLayerRecursively(slotGo, previewLayer);

                slotGo.SetActive(false);
                slots[i] = slotGo;
            }

            var camGo = GetOrCreateChild(studio.transform, "ArenaPreviewCamera");
            var cam = camGo.GetComponent<Camera>();
            if (cam == null)
                cam = Undo.AddComponent<Camera>(camGo);
            ConfigurePreviewCamera(cam);

            var lightGo = GetOrCreateChild(studio.transform, "ArenaPreviewLight");
            var light = lightGo.GetComponent<Light>();
            if (light == null)
                light = Undo.AddComponent<Light>(lightGo);
            light.type = LightType.Point;
            light.range = 120f;
            light.intensity = 2.5f;
            lightGo.transform.localPosition = new Vector3(0f, 38f, 0f);

            WireLevelArenaPreviewRenderer(slots, cam, AssetDatabase.LoadAssetAtPath<RenderTexture>(
                "Assets/Settings/MasterBlaster/LevelSelectArenaPreview.renderTexture"));

            Undo.CollapseUndoOperations(undoGroup);
            EditorUtility.DisplayDialog("Arena Preview Studio", $"Built {pairs.Count} preview slot(s) under {StudioName}. Save the scene.", "OK");
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
            int previewLayer = LayerMask.NameToLayer(LevelArenaPreviewRenderer.PreviewLayerName);
            if (previewLayer < 0)
                previewLayer = 0;

            cam.orthographic = true;
            cam.orthographicSize = 14f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 200f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.05f, 0.08f, 1f);
            cam.cullingMask = 1 << previewLayer;
            cam.depth = -10f;
            cam.transform.localPosition = new Vector3(0f, 48f, 0f);
            cam.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
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
                    || typeof(Grid).IsAssignableFrom(t))
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

        private static void WireLevelArenaPreviewRenderer(
            GameObject[] slots,
            Camera cam,
            RenderTexture rt)
        {
            var renderer = Object.FindFirstObjectByType<LevelArenaPreviewRenderer>(FindObjectsInactive.Include);
            if (renderer == null)
            {
                var ls = Object.FindFirstObjectByType<LevelSelectController>(FindObjectsInactive.Include);
                if (ls != null)
                    renderer = Undo.AddComponent<LevelArenaPreviewRenderer>(ls.gameObject);
            }

            if (renderer == null)
            {
                Debug.LogWarning("[ArenaPreviewStudioSetup] No LevelSelectController found; assign LevelArenaPreviewRenderer manually.");
                return;
            }

            var so = new SerializedObject(renderer);
            so.FindProperty("previewCamera").objectReferenceValue = cam;
            so.FindProperty("renderTexture").objectReferenceValue = rt;

            var raw = FindArenaPreviewRawImage();
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

            var levelSelect = Object.FindFirstObjectByType<LevelSelectController>(FindObjectsInactive.Include);
            if (levelSelect != null)
            {
                var lso = new SerializedObject(levelSelect);
                var ap = lso.FindProperty("arenaPreview");
                if (ap != null)
                {
                    ap.objectReferenceValue = renderer;
                    lso.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        private static RawImage FindArenaPreviewRawImage()
        {
            foreach (var raw in Object.FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (raw != null && raw.gameObject.name == "ArenaPreviewRaw")
                    return raw;
            }

            return null;
        }
    }
}
#endif
