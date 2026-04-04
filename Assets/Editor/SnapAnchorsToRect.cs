#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace App.Editor
{
    public static class SnapAnchorsToRect
    {
        [MenuItem("Tools/Snap RectTransform Anchors To Rect")]
        static void Snap()
        {
            foreach (var tr in Selection.transforms)
            {
                if (tr is RectTransform rt && tr.parent is RectTransform parent)
                {
                    Undo.RecordObject(rt, "Snap Anchors");
                    rt.anchorMin = PointToNormalizedUnclamped(parent.rect, (Vector2)rt.localPosition + rt.rect.min);
                    rt.anchorMax = PointToNormalizedUnclamped(parent.rect, (Vector2)rt.localPosition + rt.rect.max);
                    rt.sizeDelta = Vector2.zero;
                    rt.anchoredPosition = Vector2.zero;
                }
            }

            // like Rect.PointToNormalzed but can go beyond 0-1
            static Vector2 PointToNormalizedUnclamped(Rect rectangle, Vector2 point)
            {
                return new Vector2(
                    InverseLerpUnclamped(rectangle.x, rectangle.xMax, point.x),
                    InverseLerpUnclamped(rectangle.y, rectangle.yMax, point.y));
            }

            // like Mathf.InverseLerp but can go beyond 0-1
            static float InverseLerpUnclamped(float a, float b, float value)
            {
                if (a != b)
                    return (value - a) / (b - a);
                return 0;
            }
        }
    }
}
#endif