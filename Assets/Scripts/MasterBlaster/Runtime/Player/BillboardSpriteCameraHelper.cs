using System;
using HybridGame.MasterBlaster.Scripts;
using HybridGame.MasterBlaster.Scripts.Camera;
using Unity.Cinemachine;
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
        /// Use cylindrical FPS-style billboard math (face gameplay camera on XZ) when the view is first-person or
        /// when <see cref="GameModeManager.CurrentMode"/> is still <see cref="GameModeManager.GameMode.Bomberman"/> but
        /// the active camera is already perspective (Cinemachine / view switched without updating mode, or stale MainCamera tag).
        /// <see cref="GameModeManager.GameMode.ArenaPerspective"/> keeps grid-style billboarding with pitch compensation.
        /// </summary>
        public static bool ShouldUseFpsBillboardRotation(
            GameModeManager.GameMode mode,
            UnityEngine.Camera resolvedForCurrentMode)
        {
            if (mode == GameModeManager.GameMode.FPS)
                return true;
            if (mode == GameModeManager.GameMode.ArenaPerspective)
                return false;
            if (mode != GameModeManager.GameMode.Bomberman)
                return false;

            var main = UnityEngine.Camera.main;
            if (main != null && !main.orthographic)
                return true;

            var facing = GetFpsBillboardFacingTransform(resolvedForCurrentMode);
            if (facing == null)
                return false;
            var fc = facing.GetComponent<UnityEngine.Camera>();
            return fc != null && !fc.orthographic;
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

        static bool s_LoggedOrthoMainWhileFpsWarning;

        /// <summary>
        /// URP stack / UI overlay cameras (e.g. GameObject "UI Canvas") are perspective but are not the gameplay view.
        /// </summary>
        static bool ShouldSkipCameraForFpsBillboardFacing(UnityEngine.Camera c)
        {
            if (c == null)
                return true;
            string n = c.gameObject.name;
            if (n.IndexOf("UI Canvas", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (n.IndexOf("Overlay", StringComparison.OrdinalIgnoreCase) >= 0 &&
                n.IndexOf("UI", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            return false;
        }

        /// <summary>
        /// FPS billboard facing: prefers a <b>perspective</b> gameplay camera. When <see cref="HybridCameraManager"/> is
        /// missing from the scene or <c>MainCamera</c> was not retagged after switching to FPS, <see cref="UnityEngine.Camera.main"/>
        /// can still be the orthographic Bomberman camera — bombs/explosions would keep grid euler-style facing without this fallback.
        /// </summary>
        public static Transform GetFpsBillboardFacingTransform(UnityEngine.Camera resolvedFromModeResolver)
        {
            // When mode is FPS, always prefer the authored FPS rig — Camera.main can still be the ortho camera for
            // one frame or if MainCamera was never retagged (Cinemachine / stack edge cases).
            var gmm = GameModeManager.Instance;
            if (gmm != null && gmm.CurrentMode == GameModeManager.GameMode.FPS &&
                HybridCameraManager.Instance != null &&
                HybridCameraManager.Instance.fpsCamera != null)
            {
                var fps = HybridCameraManager.Instance.fpsCamera;
                if (fps.gameObject.activeInHierarchy && fps.enabled)
                    return fps.transform;
            }

            var main = UnityEngine.Camera.main;
            if (main != null && !main.orthographic)
                return main.transform;

            if (HybridCameraManager.Instance != null &&
                HybridCameraManager.Instance.fpsCamera != null &&
                HybridCameraManager.Instance.fpsCamera.gameObject.activeInHierarchy)
                return HybridCameraManager.Instance.fpsCamera.transform;

            if (resolvedFromModeResolver != null && !resolvedFromModeResolver.orthographic)
                return resolvedFromModeResolver.transform;

            var cameras = UnityEngine.Object.FindObjectsByType<UnityEngine.Camera>(FindObjectsSortMode.None);
            for (int i = 0; i < cameras.Length; i++)
            {
                var c = cameras[i];
                if (c == null || !c.enabled || !c.gameObject.activeInHierarchy || c.orthographic)
                    continue;
                if (ShouldSkipCameraForFpsBillboardFacing(c))
                    continue;
                if (c.GetComponent<CinemachineBrain>() == null)
                    continue;

                if (!s_LoggedOrthoMainWhileFpsWarning && main != null && main.orthographic)
                {
                    s_LoggedOrthoMainWhileFpsWarning = true;
                    UnityEngine.Debug.LogWarning(
                        "[BillboardSpriteCameraHelper] FPS billboard: Camera.main is still orthographic; using CinemachineBrain camera (" +
                        c.gameObject.name +
                        "). Add <HybridCameraManager> to the scene or ensure MainCamera tags the FPS view after mode switch.",
                        c);
                }

                return c.transform;
            }

            for (int i = 0; i < cameras.Length; i++)
            {
                var c = cameras[i];
                if (c == null || !c.enabled || !c.gameObject.activeInHierarchy || c.orthographic)
                    continue;
                if (ShouldSkipCameraForFpsBillboardFacing(c))
                    continue;

                if (!s_LoggedOrthoMainWhileFpsWarning && main != null && main.orthographic)
                {
                    s_LoggedOrthoMainWhileFpsWarning = true;
                    UnityEngine.Debug.LogWarning(
                        "[BillboardSpriteCameraHelper] FPS billboard: Camera.main is still orthographic; using first enabled gameplay perspective camera (" +
                        c.gameObject.name +
                        "). Add <HybridCameraManager> to the scene or ensure MainCamera tags the FPS view after mode switch.",
                        c);
                }

                return c.transform;
            }

            return GetFpsBillboardCameraTransform(resolvedFromModeResolver);
        }
    }
}
