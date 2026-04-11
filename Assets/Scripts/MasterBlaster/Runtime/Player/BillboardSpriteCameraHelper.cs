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
