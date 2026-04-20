using System;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Mobile.Layout
{
    /// <summary>Local-space Transform snapshot for handheld layout presets.</summary>
    [Serializable]
    public struct MobileHandheldTransformSnapshot
    {
        public bool captureTransform;

        public Vector3 localPosition;

        public Quaternion localRotation;

        public Vector3 localScale;

        public static MobileHandheldTransformSnapshot Capture(Transform t)
        {
            if (t == null)
                return new MobileHandheldTransformSnapshot { captureTransform = false };
            return new MobileHandheldTransformSnapshot
            {
                captureTransform = true,
                localPosition = t.localPosition,
                localRotation = t.localRotation,
                localScale = t.localScale,
            };
        }

        public static void Apply(Transform t, MobileHandheldTransformSnapshot s)
        {
            if (t == null || !s.captureTransform)
                return;
            t.localPosition = s.localPosition;
            t.localRotation = s.localRotation;
            t.localScale = s.localScale;
        }

        public static MobileHandheldTransformSnapshot Lerp(MobileHandheldTransformSnapshot a, MobileHandheldTransformSnapshot b, float t)
        {
            t = Mathf.Clamp01(t);
            if (!a.captureTransform)
                return b;
            if (!b.captureTransform)
                return a;
            return new MobileHandheldTransformSnapshot
            {
                captureTransform = true,
                localPosition = Vector3.Lerp(a.localPosition, b.localPosition, t),
                localRotation = Quaternion.Slerp(a.localRotation, b.localRotation, t),
                localScale = Vector3.Lerp(a.localScale, b.localScale, t),
            };
        }
    }
}
