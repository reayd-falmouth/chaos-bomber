using System.Collections.Generic;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Mobile.Layout
{
    /// <summary>How a preset row is chosen for the current screen size.</summary>
    public enum MobileHandheldPresetMatchMode
    {
        ExactPixel = 0,
        NearestAspectRatio = 1,
        InterpolateNearestAspects = 2,
    }

    /// <summary>Pure selection + interpolation for EditMode tests and runtime.</summary>
    public static class MobileHandheldLayoutPresetSelector
    {
        public static bool TrySelectExact(
            IReadOnlyList<MobileHandheldLayoutPresetEntry> entries,
            int screenWidth,
            int screenHeight,
            out MobileHandheldLayoutPresetEntry entry,
            out int index)
        {
            entry = null;
            index = -1;
            if (entries == null || entries.Count == 0)
                return false;
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e == null)
                    continue;
                if (e.screenWidth == screenWidth && e.screenHeight == screenHeight)
                {
                    entry = e;
                    index = i;
                    return true;
                }
            }

            return false;
        }

        public static bool TrySelectNearestAspect(
            IReadOnlyList<MobileHandheldLayoutPresetEntry> entries,
            int screenWidth,
            int screenHeight,
            out MobileHandheldLayoutPresetEntry entry,
            out int index)
        {
            entry = null;
            index = -1;
            if (entries == null || entries.Count == 0 || screenHeight <= 0)
                return false;

            float targetAspect = screenWidth / (float)screenHeight;
            float bestScore = float.MaxValue;
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e == null || e.screenHeight <= 0)
                    continue;
                float d = Mathf.Abs(Mathf.Log(e.AspectRatio) - Mathf.Log(targetAspect));
                if (d < bestScore)
                {
                    bestScore = d;
                    entry = e;
                    index = i;
                }
            }

            return entry != null;
        }

        /// <summary>
        /// Interpolates rect snapshots between the two nearest aspect presets (by aspect ratio).
        /// Gameplay camera snapshots are lerped where both sides capture; vcam rows match by name when possible.
        /// </summary>
        public static bool TryBuildInterpolatedEntry(
            IReadOnlyList<MobileHandheldLayoutPresetEntry> entries,
            int screenWidth,
            int screenHeight,
            out MobileHandheldLayoutPresetEntry interpolated)
        {
            interpolated = null;
            if (entries == null || entries.Count == 0 || screenHeight <= 0)
                return false;

            float targetAspect = screenWidth / (float)screenHeight;

            MobileHandheldLayoutPresetEntry lower = null;
            MobileHandheldLayoutPresetEntry upper = null;
            float lowerAspect = float.NegativeInfinity;
            float upperAspect = float.PositiveInfinity;

            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e == null || e.screenHeight <= 0)
                    continue;
                float a = e.AspectRatio;
                if (a <= targetAspect && a > lowerAspect)
                {
                    lowerAspect = a;
                    lower = e;
                }

                if (a >= targetAspect && a < upperAspect)
                {
                    upperAspect = a;
                    upper = e;
                }
            }

            if (lower == null && upper == null)
                return false;

            if (lower == null || upper == null || lower == upper)
            {
                var single = lower ?? upper;
                interpolated = CloneEntryShallow(single, screenWidth, screenHeight, "interpolated_single");
                return interpolated != null;
            }

            float t = (targetAspect - lowerAspect) / Mathf.Max(1e-6f, upperAspect - lowerAspect);
            t = Mathf.Clamp01(t);

            interpolated = new MobileHandheldLayoutPresetEntry
            {
                screenWidth = screenWidth,
                screenHeight = screenHeight,
                label = $"interpolated({lower.label},{upper.label})",
                simulatedDeviceModel = string.Empty,
                applyGameplayCameras = lower.applyGameplayCameras && upper.applyGameplayCameras,
                cinemachineBrainOutputCamera = MobileHandheldUnityCameraSnapshot.Lerp(
                    lower.cinemachineBrainOutputCamera,
                    upper.cinemachineBrainOutputCamera,
                    t),
                cinemachineVcams = InterpolateVcams(lower.cinemachineVcams, upper.cinemachineVcams, t),
                applyUiCanvas = lower.applyUiCanvas && upper.applyUiCanvas,
                uiCanvasRootRect = MobileHandheldRectSnapshot.Lerp(lower.uiCanvasRootRect, upper.uiCanvasRootRect, t),
                uiCanvasScaler = MobileHandheldCanvasScalerSnapshot.Lerp(lower.uiCanvasScaler, upper.uiCanvasScaler, t),
                applyMobileOverlay = lower.applyMobileOverlay && upper.applyMobileOverlay,
                overlayCanvasRect = MobileHandheldRectSnapshot.Lerp(lower.overlayCanvasRect, upper.overlayCanvasRect, t),
                overlayCanvasScaler = MobileHandheldCanvasScalerSnapshot.Lerp(lower.overlayCanvasScaler, upper.overlayCanvasScaler, t),
                overlayRootRect = MobileHandheldRectSnapshot.Lerp(lower.overlayRootRect, upper.overlayRootRect, t),
                overlaySafeAreaRect = MobileHandheldRectSnapshot.Lerp(lower.overlaySafeAreaRect, upper.overlaySafeAreaRect, t),
            };
            return true;
        }

        private static MobileHandheldCinemachineVcamSnapshotEntry[] InterpolateVcams(
            MobileHandheldCinemachineVcamSnapshotEntry[] a,
            MobileHandheldCinemachineVcamSnapshotEntry[] b,
            float t)
        {
            if (a == null || a.Length == 0)
                return b ?? System.Array.Empty<MobileHandheldCinemachineVcamSnapshotEntry>();
            if (b == null || b.Length == 0)
                return a;

            var result = new List<MobileHandheldCinemachineVcamSnapshotEntry>(a.Length);
            for (int i = 0; i < a.Length; i++)
            {
                var ea = a[i];
                if (ea == null)
                    continue;
                MobileHandheldCinemachineVcamSnapshotEntry eb = null;
                for (int j = 0; j < b.Length; j++)
                {
                    if (b[j] != null && b[j].gameObjectName == ea.gameObjectName)
                    {
                        eb = b[j];
                        break;
                    }
                }

                if (eb == null)
                {
                    result.Add(CloneVcamEntry(ea));
                    continue;
                }

                result.Add(LerpVcamEntry(ea, eb, t));
            }

            return result.ToArray();
        }

        private static MobileHandheldCinemachineVcamSnapshotEntry CloneVcamEntry(MobileHandheldCinemachineVcamSnapshotEntry s)
        {
            return new MobileHandheldCinemachineVcamSnapshotEntry
            {
                gameObjectName = s.gameObjectName,
                priority = s.priority,
                lensModeOverride = s.lensModeOverride,
                fieldOfView = s.fieldOfView,
                orthographicSize = s.orthographicSize,
                nearClipPlane = s.nearClipPlane,
                farClipPlane = s.farClipPlane,
                dutch = s.dutch,
            };
        }

        private static MobileHandheldCinemachineVcamSnapshotEntry LerpVcamEntry(
            MobileHandheldCinemachineVcamSnapshotEntry a,
            MobileHandheldCinemachineVcamSnapshotEntry b,
            float t)
        {
            return new MobileHandheldCinemachineVcamSnapshotEntry
            {
                gameObjectName = a.gameObjectName,
                priority = Mathf.RoundToInt(Mathf.Lerp(a.priority, b.priority, t)),
                lensModeOverride = t < 0.5f ? a.lensModeOverride : b.lensModeOverride,
                fieldOfView = Mathf.Lerp(a.fieldOfView, b.fieldOfView, t),
                orthographicSize = Mathf.Lerp(a.orthographicSize, b.orthographicSize, t),
                nearClipPlane = Mathf.Lerp(a.nearClipPlane, b.nearClipPlane, t),
                farClipPlane = Mathf.Lerp(a.farClipPlane, b.farClipPlane, t),
                dutch = Mathf.Lerp(a.dutch, b.dutch, t),
            };
        }

        private static MobileHandheldLayoutPresetEntry CloneEntryShallow(
            MobileHandheldLayoutPresetEntry source,
            int w,
            int h,
            string labelSuffix)
        {
            if (source == null)
                return null;
            return new MobileHandheldLayoutPresetEntry
            {
                screenWidth = w,
                screenHeight = h,
                label = string.IsNullOrEmpty(source.label) ? labelSuffix : source.label + "_" + labelSuffix,
                simulatedDeviceModel = source.simulatedDeviceModel,
                applyGameplayCameras = source.applyGameplayCameras,
                cinemachineBrainOutputCamera = source.cinemachineBrainOutputCamera,
                cinemachineVcams = source.cinemachineVcams,
                applyUiCanvas = source.applyUiCanvas,
                uiCanvasRootRect = source.uiCanvasRootRect,
                uiCanvasScaler = source.uiCanvasScaler,
                applyMobileOverlay = source.applyMobileOverlay,
                overlayCanvasRect = source.overlayCanvasRect,
                overlayCanvasScaler = source.overlayCanvasScaler,
                overlayRootRect = source.overlayRootRect,
                overlaySafeAreaRect = source.overlaySafeAreaRect,
            };
        }
    }
}
