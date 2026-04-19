using System;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Mobile.Layout
{
    /// <summary>Viewport and lens values for a Unity <see cref="UnityEngine.Camera"/> (e.g. CinemachineBrain output).</summary>
    [Serializable]
    public struct MobileHandheldUnityCameraSnapshot
    {
        public bool captureEnabled;

        public Rect normalizedViewportRect;

        public bool orthographic;

        public float orthographicSize;

        public float fieldOfView;

        public float nearClipPlane;

        public float farClipPlane;

        public static MobileHandheldUnityCameraSnapshot Capture(UnityEngine.Camera c)
        {
            if (c == null)
                return new MobileHandheldUnityCameraSnapshot { captureEnabled = false };
            return new MobileHandheldUnityCameraSnapshot
            {
                captureEnabled = true,
                normalizedViewportRect = c.rect,
                orthographic = c.orthographic,
                orthographicSize = c.orthographicSize,
                fieldOfView = c.fieldOfView,
                nearClipPlane = c.nearClipPlane,
                farClipPlane = c.farClipPlane,
            };
        }

        public static void Apply(UnityEngine.Camera c, MobileHandheldUnityCameraSnapshot s)
        {
            if (c == null || !s.captureEnabled)
                return;
            c.rect = s.normalizedViewportRect;
            c.orthographic = s.orthographic;
            c.orthographicSize = s.orthographicSize;
            c.fieldOfView = s.fieldOfView;
            c.nearClipPlane = s.nearClipPlane;
            c.farClipPlane = s.farClipPlane;
        }

        public static MobileHandheldUnityCameraSnapshot Lerp(MobileHandheldUnityCameraSnapshot a, MobileHandheldUnityCameraSnapshot b, float t)
        {
            t = Mathf.Clamp01(t);
            if (!a.captureEnabled)
                return b;
            if (!b.captureEnabled)
                return a;
            return new MobileHandheldUnityCameraSnapshot
            {
                captureEnabled = true,
                normalizedViewportRect = new Rect(
                    Mathf.Lerp(a.normalizedViewportRect.xMin, b.normalizedViewportRect.xMin, t),
                    Mathf.Lerp(a.normalizedViewportRect.yMin, b.normalizedViewportRect.yMin, t),
                    Mathf.Lerp(a.normalizedViewportRect.width, b.normalizedViewportRect.width, t),
                    Mathf.Lerp(a.normalizedViewportRect.height, b.normalizedViewportRect.height, t)),
                orthographic = t < 0.5f ? a.orthographic : b.orthographic,
                orthographicSize = Mathf.Lerp(a.orthographicSize, b.orthographicSize, t),
                fieldOfView = Mathf.Lerp(a.fieldOfView, b.fieldOfView, t),
                nearClipPlane = Mathf.Lerp(a.nearClipPlane, b.nearClipPlane, t),
                farClipPlane = Mathf.Lerp(a.farClipPlane, b.farClipPlane, t),
            };
        }
    }
}
