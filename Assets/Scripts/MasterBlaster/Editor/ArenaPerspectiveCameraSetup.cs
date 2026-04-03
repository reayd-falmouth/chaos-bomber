using HybridGame.MasterBlaster.Scripts.Camera;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace HybridGame.Editor
{
    /// <summary>
    /// Creates a passive angled <see cref="CinemachineCamera"/> with
    /// <see cref="ArenaPerspectiveCinemachineCamera"/> for <see cref="HybridGame.MasterBlaster.Scripts.GameModeManager.GameMode.ArenaPerspective"/>.
    /// Requires an existing Unity Camera with <see cref="CinemachineBrain"/> in the scene (same as other CM rigs).
    /// Optionally assign a child Camera to <see cref="HybridCameraManager.arenaPerspectiveCamera"/> when not using CM for that path.
    /// </summary>
    public static class ArenaPerspectiveCameraSetup
    {
        private const string MenuPath = "HybridGame/Setup/Add Arena Perspective Camera Rig";

        [MenuItem(MenuPath)]
        public static void CreateRig()
        {
            var parent = Selection.activeGameObject;
            if (parent == null)
            {
                EditorUtility.DisplayDialog(
                    "Arena Perspective Camera",
                    "Select a parent object in the hierarchy (e.g. Game / Cameras), then run this command again.",
                    "OK");
                return;
            }

            var root = new GameObject("ArenaPerspectiveCameraRig");
            Undo.RegisterCreatedObjectUndo(root, "Create Arena Perspective Camera Rig");
            root.transform.SetParent(parent.transform, false);
            root.transform.localPosition = new Vector3(7f, 14f, -10f);
            root.transform.localRotation = Quaternion.Euler(55f, 0f, 0f);

            var cm = root.AddComponent<CinemachineCamera>();
            cm.Priority = 15;

            root.AddComponent<ArenaPerspectiveCinemachineCamera>();

            Selection.activeGameObject = root;
            EditorUtility.DisplayDialog(
                "Arena Perspective Camera",
                "Cinemachine rig created. Tune position/FOV for your arena. " +
                "CinemachineModeSwitcher will prefer this vcam in ArenaPerspective when this marker is present.",
                "OK");
        }
    }
}
