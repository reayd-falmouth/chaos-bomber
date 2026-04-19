using System;
using UnityEngine;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Scripts.Mobile.Layout
{
    /// <summary>Optional <see cref="CanvasScaler"/> settings for handheld presets.</summary>
    [Serializable]
    public struct MobileHandheldCanvasScalerSnapshot
    {
        public bool captureScaler;
        public CanvasScaler.ScaleMode uiScaleMode;
        public float scaleFactor;
        public CanvasScaler.ScreenMatchMode screenMatchMode;
        public Vector2 referenceResolution;
        public float matchWidthOrHeight;
        public float referencePixelsPerUnit;

        public static MobileHandheldCanvasScalerSnapshot Capture(CanvasScaler scaler)
        {
            if (scaler == null)
                return new MobileHandheldCanvasScalerSnapshot { captureScaler = false };
            return new MobileHandheldCanvasScalerSnapshot
            {
                captureScaler = true,
                uiScaleMode = scaler.uiScaleMode,
                scaleFactor = scaler.scaleFactor,
                screenMatchMode = scaler.screenMatchMode,
                referenceResolution = scaler.referenceResolution,
                matchWidthOrHeight = scaler.matchWidthOrHeight,
                referencePixelsPerUnit = scaler.referencePixelsPerUnit,
            };
        }

        public static void Apply(CanvasScaler scaler, MobileHandheldCanvasScalerSnapshot s)
        {
            if (scaler == null || !s.captureScaler)
                return;
            scaler.uiScaleMode = s.uiScaleMode;
            scaler.scaleFactor = s.scaleFactor;
            scaler.screenMatchMode = s.screenMatchMode;
            scaler.referenceResolution = s.referenceResolution;
            scaler.matchWidthOrHeight = s.matchWidthOrHeight;
            scaler.referencePixelsPerUnit = s.referencePixelsPerUnit;
        }

        public static MobileHandheldCanvasScalerSnapshot Lerp(
            MobileHandheldCanvasScalerSnapshot a,
            MobileHandheldCanvasScalerSnapshot b,
            float t)
        {
            t = Mathf.Clamp01(t);
            if (!a.captureScaler)
                return b;
            if (!b.captureScaler)
                return a;
            return new MobileHandheldCanvasScalerSnapshot
            {
                captureScaler = true,
                uiScaleMode = t < 0.5f ? a.uiScaleMode : b.uiScaleMode,
                scaleFactor = Mathf.Lerp(a.scaleFactor, b.scaleFactor, t),
                screenMatchMode = t < 0.5f ? a.screenMatchMode : b.screenMatchMode,
                referenceResolution = Vector2.Lerp(a.referenceResolution, b.referenceResolution, t),
                matchWidthOrHeight = Mathf.Lerp(a.matchWidthOrHeight, b.matchWidthOrHeight, t),
                referencePixelsPerUnit = Mathf.Lerp(a.referencePixelsPerUnit, b.referencePixelsPerUnit, t),
            };
        }
    }
}
