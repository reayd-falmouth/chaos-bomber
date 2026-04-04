#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace App.Editor
{
    public static class SnapAnchorsToRectRecursively
    {
        [MenuItem("Tools/Snap RectTransform Anchors To Rect (Recursive)")]
        private static void SnapRecursiveMenu()
        {
            // For each selected GameObject, process its entire hierarchy
            foreach (var go in Selection.gameObjects)
            {
                SnapRecursively(go.transform);
            }
        }

        private static void SnapRecursively(Transform t)
        {
            // First, try to snap this transform if it's a RectTransform with a RectTransform parent
            if (t is RectTransform rt && t.parent is RectTransform parentRt)
            {
                Undo.RecordObject(rt, "Snap Anchors Recursively");

                // Calculate the normalized anchor positions (can exceed 0–1)
                var min = (Vector2)rt.localPosition + rt.rect.min;
                var max = (Vector2)rt.localPosition + rt.rect.max;
                rt.anchorMin     = PointToNormalizedUnclamped(parentRt.rect, min);
                rt.anchorMax     = PointToNormalizedUnclamped(parentRt.rect, max);

                // Zero out sizeDelta and anchoredPosition so it 'sticks' to anchors
                rt.sizeDelta        = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
            }

            // Then recurse into children
            for (int i = 0; i < t.childCount; i++)
            {
                SnapRecursively(t.GetChild(i));
            }
        }

        /// <summary>
        /// Like Rect.PointToNormalized but without clamping between 0 and 1.
        /// </summary>
        private static Vector2 PointToNormalizedUnclamped(Rect rectangle, Vector2 point)
        {
            return new Vector2(
                InverseLerpUnclamped(rectangle.xMin, rectangle.xMax, point.x),
                InverseLerpUnclamped(rectangle.yMin, rectangle.yMax, point.y)
            );
        }

        /// <summary>
        /// Like Mathf.InverseLerp but allows values outside 0–1.
        /// </summary>
        private static float InverseLerpUnclamped(float a, float b, float value)
        {
            if (Mathf.Approximately(a, b))
                return 0f;
            return (value - a) / (b - a);
        }
    }
}
#endif
