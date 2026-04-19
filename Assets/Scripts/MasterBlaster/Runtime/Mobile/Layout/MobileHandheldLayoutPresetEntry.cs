using System;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Mobile.Layout
{
    /// <summary>One screen-size key plus captured layout for letterbox, flow UI, and mobile overlay.</summary>
    [Serializable]
    public sealed class MobileHandheldLayoutPresetEntry
    {
        [Tooltip("Reference resolution width in pixels (key).")]
        public int screenWidth = 1920;

        [Tooltip("Reference resolution height in pixels (key).")]
        public int screenHeight = 1080;

        [Tooltip("Optional label for the Inspector / debugging.")]
        public string label = string.Empty;

        [Header("Letterbox (AmigaLetterboxCamera)")]
        public bool applyLetterbox = true;

        [Tooltip("Design aspect numerator; passed to AmigaLetterboxCamera.SetDesignResolution.")]
        public int letterboxDesignWidth = 640;

        [Tooltip("Design aspect denominator.")]
        public int letterboxDesignHeight = 512;

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
