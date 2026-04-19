using System;
using System.Collections.Generic;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Mobile.Layout
{
    /// <summary>Builds and resolves indexed paths under an overlay root; captures and interpolates descendant <see cref="RectTransform"/> rows.</summary>
    public static class MobileHandheldOverlayRectPathUtility
    {
        public const char PathSeparator = '/';

        /// <summary>Builds a path from <paramref name="root"/> to <paramref name="target"/> (exclusive of root). Returns empty if <paramref name="target"/> is root.</summary>
        public static string BuildIndexedPath(Transform root, Transform target)
        {
            if (root == null || target == null || !target.IsChildOf(root))
                return null;
            if (target == root)
                return string.Empty;

            var segments = new List<string>();
            var t = target;
            while (t != null && t != root)
            {
                int idx = t.GetSiblingIndex();
                segments.Add($"{idx}-{t.name}");
                t = t.parent;
            }

            if (t != root)
                return null;

            segments.Reverse();
            return string.Join(PathSeparator.ToString(), segments);
        }

        /// <summary>Resolves a path built with <see cref="BuildIndexedPath"/> to a <see cref="RectTransform"/> under <paramref name="root"/>.</summary>
        public static RectTransform TryResolvePath(Transform root, string relativePath)
        {
            if (root == null || string.IsNullOrEmpty(relativePath))
                return null;

            var current = root;
            var parts = relativePath.Split(PathSeparator);
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part))
                    continue;
                if (!TryParseSegment(part, out int idx, out string name))
                    return null;
                var next = FindChildBySiblingIndexAndName(current, idx, name);
                if (next == null)
                    return null;
                current = next;
            }

            return current as RectTransform;
        }

        public static bool TryParseSegment(string segment, out int index, out string name)
        {
            index = 0;
            name = segment ?? string.Empty;
            if (string.IsNullOrEmpty(segment))
                return false;
            var i = segment.IndexOf('-');
            if (i <= 0)
                return false;
            if (!int.TryParse(segment.Substring(0, i), out index))
                return false;
            name = segment.Substring(i + 1);
            return true;
        }

        /// <summary>Enumerates descendant <see cref="RectTransform"/>s under <paramref name="root"/> (excluding <paramref name="root"/> and any in <paramref name="exclude"/>).</summary>
        public static MobileHandheldOverlayRectPathEntry[] CaptureDescendants(
            RectTransform root,
            bool includeInactive,
            IReadOnlyCollection<RectTransform> exclude)
        {
            if (root == null)
                return Array.Empty<MobileHandheldOverlayRectPathEntry>();

            var excludeSet = new HashSet<RectTransform>();
            if (exclude != null)
            {
                foreach (var x in exclude)
                {
                    if (x != null)
                        excludeSet.Add(x);
                }
            }

            var rects = root.GetComponentsInChildren<RectTransform>(includeInactive);
            var list = new List<MobileHandheldOverlayRectPathEntry>(rects.Length);
            for (int i = 0; i < rects.Length; i++)
            {
                var rt = rects[i];
                if (rt == root || excludeSet.Contains(rt))
                    continue;
                var path = BuildIndexedPath(root, rt);
                if (string.IsNullOrEmpty(path))
                    continue;
                list.Add(new MobileHandheldOverlayRectPathEntry
                {
                    relativePath = path,
                    rect = MobileHandheldRectSnapshot.Capture(rt),
                });
            }

            list.Sort((a, b) => string.CompareOrdinal(a.relativePath, b.relativePath));
            return list.ToArray();
        }

        /// <summary>Interpolates or copies rows by <see cref="MobileHandheldOverlayRectPathEntry.relativePath"/> (union of keys; matches <see cref="MobileHandheldLayoutPresetSelector"/> vcam behavior).</summary>
        public static MobileHandheldOverlayRectPathEntry[] InterpolateDescendantRows(
            MobileHandheldOverlayRectPathEntry[] lower,
            MobileHandheldOverlayRectPathEntry[] upper,
            float t)
        {
            lower ??= Array.Empty<MobileHandheldOverlayRectPathEntry>();
            upper ??= Array.Empty<MobileHandheldOverlayRectPathEntry>();

            var dictLower = new Dictionary<string, MobileHandheldOverlayRectPathEntry>(StringComparer.Ordinal);
            for (int i = 0; i < lower.Length; i++)
            {
                var row = lower[i];
                if (row != null && !string.IsNullOrEmpty(row.relativePath))
                    dictLower[row.relativePath] = row;
            }

            var dictUpper = new Dictionary<string, MobileHandheldOverlayRectPathEntry>(StringComparer.Ordinal);
            for (int i = 0; i < upper.Length; i++)
            {
                var row = upper[i];
                if (row != null && !string.IsNullOrEmpty(row.relativePath))
                    dictUpper[row.relativePath] = row;
            }

            var allPaths = new HashSet<string>(StringComparer.Ordinal);
            foreach (var kv in dictLower)
                allPaths.Add(kv.Key);
            foreach (var kv in dictUpper)
                allPaths.Add(kv.Key);

            var sorted = new List<string>(allPaths);
            sorted.Sort(StringComparer.Ordinal);

            var result = new List<MobileHandheldOverlayRectPathEntry>(sorted.Count);
            for (int i = 0; i < sorted.Count; i++)
            {
                var path = sorted[i];
                dictLower.TryGetValue(path, out var lo);
                dictUpper.TryGetValue(path, out var up);
                if (lo != null && up != null)
                {
                    result.Add(new MobileHandheldOverlayRectPathEntry
                    {
                        relativePath = path,
                        rect = MobileHandheldRectSnapshot.Lerp(lo.rect, up.rect, t),
                    });
                }
                else if (lo != null)
                {
                    result.Add(new MobileHandheldOverlayRectPathEntry { relativePath = path, rect = lo.rect });
                }
                else if (up != null)
                {
                    result.Add(new MobileHandheldOverlayRectPathEntry { relativePath = path, rect = up.rect });
                }
            }

            return result.ToArray();
        }

        private static Transform FindChildBySiblingIndexAndName(Transform parent, int siblingIndex, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var c = parent.GetChild(i);
                if (c.GetSiblingIndex() == siblingIndex && c.name == name)
                    return c;
            }

            return null;
        }
    }
}
