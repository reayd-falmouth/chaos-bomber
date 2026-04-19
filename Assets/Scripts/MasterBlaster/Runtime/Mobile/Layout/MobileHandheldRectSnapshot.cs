using System;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Mobile.Layout
{
    /// <summary>Serialized RectTransform layout for handheld preset capture/apply.</summary>
    [Serializable]
    public struct MobileHandheldRectSnapshot
    {
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public Vector2 pivot;
        public Vector3 localScale;
        public Quaternion localRotation;

        public static MobileHandheldRectSnapshot Capture(RectTransform rt)
        {
            if (rt == null)
                return default;
            return new MobileHandheldRectSnapshot
            {
                anchorMin = rt.anchorMin,
                anchorMax = rt.anchorMax,
                anchoredPosition = rt.anchoredPosition,
                sizeDelta = rt.sizeDelta,
                pivot = rt.pivot,
                localScale = rt.localScale,
                localRotation = rt.localRotation,
            };
        }

        public static void Apply(RectTransform rt, MobileHandheldRectSnapshot s)
        {
            if (rt == null)
                return;
            rt.anchorMin = s.anchorMin;
            rt.anchorMax = s.anchorMax;
            rt.anchoredPosition = s.anchoredPosition;
            rt.sizeDelta = s.sizeDelta;
            rt.pivot = s.pivot;
            rt.localScale = s.localScale;
            rt.localRotation = s.localRotation;
        }

        public static MobileHandheldRectSnapshot Lerp(MobileHandheldRectSnapshot a, MobileHandheldRectSnapshot b, float t)
        {
            t = Mathf.Clamp01(t);
            return new MobileHandheldRectSnapshot
            {
                anchorMin = Vector2.Lerp(a.anchorMin, b.anchorMin, t),
                anchorMax = Vector2.Lerp(a.anchorMax, b.anchorMax, t),
                anchoredPosition = Vector2.Lerp(a.anchoredPosition, b.anchoredPosition, t),
                sizeDelta = Vector2.Lerp(a.sizeDelta, b.sizeDelta, t),
                pivot = Vector2.Lerp(a.pivot, b.pivot, t),
                localScale = Vector3.Lerp(a.localScale, b.localScale, t),
                localRotation = Quaternion.Slerp(a.localRotation, b.localRotation, t),
            };
        }
    }
}
