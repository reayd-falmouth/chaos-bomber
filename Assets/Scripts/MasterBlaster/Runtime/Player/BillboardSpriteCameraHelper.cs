using HybridGame.MasterBlaster.Scripts;
using HybridGame.MasterBlaster.Scripts.Camera;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Player
{
    /// <summary>
    /// Shared camera resolution for <see cref="BillboardSprite"/> (and unit tests).
    /// Prefer <see cref="HybridCameraManager"/> when present; always fall back to <see cref="Camera.main"/>.
    /// </summary>
    public static class BillboardSpriteCameraHelper
    {
        /// <summary>
        /// When true, <see cref="BillboardSprite"/> uses grid <c>bombermanEulerAngles</c> (flat / face-up for ortho).
        /// Uses the <see cref="HybridCameraManager"/>-resolved camera for this mode when available so a stale
        /// <see cref="UnityEngine.Camera.main"/> tag does not force the wrong path (bombs vs player prefab euler).
        /// </summary>
        public static bool UseTopDownGridBillboardRotation(GameModeManager.GameMode mode)
        {
            if (!GameModeManager.IsGridPresentationMode(mode))
                return false;
            if (HybridCameraManager.Instance != null &&
                HybridCameraManager.Instance.TryGetCameraForBillboards(mode, out var resolved) &&
                resolved != null)
                return resolved.orthographic;
            var main = UnityEngine.Camera.main;
            if (main == null)
                return true;
            return main.orthographic;
        }

        /// <summary>
        /// Sets <paramref name="t"/>.localPosition.y from grid vs FPS height (preserves local X/Z).
        /// Matches <see cref="PlayerDualModeController"/> Billbox Y semantics; unit-tested.
        /// </summary>
        public static void ApplyBillboxLocalY(Transform t, bool gridPresentation, float yGrid, float yFps)
        {
            if (t == null)
                return;
            var lp = t.localPosition;
            lp.y = gridPresentation ? yGrid : yFps;
            t.localPosition = lp;
        }

        /// <summary>
        /// Resolves which camera should drive billboard facing for <paramref name="mode"/>.
        /// </summary>
        public static bool TryResolveBillboardCamera(GameModeManager.GameMode mode, out UnityEngine.Camera cam)
        {
            cam = null;
            if (HybridCameraManager.Instance != null)
                HybridCameraManager.Instance.TryGetCameraForBillboards(mode, out cam);
            if (cam == null)
                cam = UnityEngine.Camera.main;
            return cam != null;
        }

        /// <summary>
        /// FPS: after <see cref="HybridCameraManager.SetMode"/>, <see cref="Camera.main"/> is the authoritative
        /// gameplay view; use it when available so billboards match the tagged main camera even if references diverge.
        /// </summary>
        public static Transform GetFpsBillboardCameraTransform(UnityEngine.Camera resolvedCam)
        {
            if (UnityEngine.Camera.main != null)
                return UnityEngine.Camera.main.transform;
            return resolvedCam != null ? resolvedCam.transform : null;
        }
    }
}
