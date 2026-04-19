using System;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Mobile.Layout
{
    /// <summary>One screen-size key plus captured layout for gameplay cameras, flow UI, and mobile overlay.</summary>
    [Serializable]
    public sealed class MobileHandheldLayoutPresetEntry
    {
        [Tooltip("Reference resolution width in pixels (key).")]
        public int screenWidth = 1920;

        [Tooltip("Reference resolution height in pixels (key).")]
        public int screenHeight = 1080;

        [Tooltip("Optional label for the Inspector / debugging.")]
        public string label = string.Empty;

        [Header("Gameplay cameras (Cinemachine)")]
        [Tooltip("When true, applies brain output Unity Camera + registered Cinemachine vcams below.")]
        public bool applyGameplayCameras = true;

        [Tooltip("Unity Camera on the same GameObject as CinemachineBrain (viewport rect, ortho size, etc.).")]
        public MobileHandheldUnityCameraSnapshot cinemachineBrainOutputCamera;

        [Tooltip("Per-CinemachineCamera state; matched by GameObject.name to CinemachineModeSwitcher.registeredCameras.")]
        public MobileHandheldCinemachineVcamSnapshotEntry[] cinemachineVcams = Array.Empty<MobileHandheldCinemachineVcamSnapshotEntry>();

        [Header("Flow UI canvas")]
        public bool applyUiCanvas = true;

        public MobileHandheldRectSnapshot uiCanvasRootRect;

        public MobileHandheldCanvasScalerSnapshot uiCanvasScaler;

        [Header("Mobile overlay")]
        public bool applyMobileOverlay = true;

        public MobileHandheldRectSnapshot overlayCanvasRect;

        public MobileHandheldCanvasScalerSnapshot overlayCanvasScaler;

        public MobileHandheldRectSnapshot overlayRootRect;

        public MobileHandheldRectSnapshot overlaySafeAreaRect;

        public float AspectRatio =>
            screenHeight > 0 ? screenWidth / (float)screenHeight : 1f;
    }
}
