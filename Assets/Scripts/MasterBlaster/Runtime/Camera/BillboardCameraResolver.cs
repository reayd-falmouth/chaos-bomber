using HybridGame.MasterBlaster.Scripts;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Camera
{
    /// <summary>
    /// Picks which gameplay camera should drive <see cref="HybridGame.MasterBlaster.Scripts.Player.BillboardSprite"/>
    /// for a given mode — same rules as <see cref="HybridCameraManager.SetMode"/> (unit-tested).
    /// </summary>
    public static class BillboardCameraResolver
    {
        /// <summary>
        /// Resolves the camera reference for <paramref name="mode"/>; does not fall back to <see cref="Camera.main"/>.
        /// </summary>
        public static Camera ResolveForMode(
            GameModeManager.GameMode mode,
            Camera fpsCamera,
            Camera bombermanCamera,
            Camera arenaPerspectiveCamera)
        {
            bool isFps = mode == GameModeManager.GameMode.FPS;
            bool isPerspective = mode == GameModeManager.GameMode.ArenaPerspective;
            bool useDedicatedPerspective = isPerspective && arenaPerspectiveCamera != null;

            if (isFps)
                return fpsCamera;
            if (useDedicatedPerspective)
                return arenaPerspectiveCamera;
            return bombermanCamera;
        }

        /// <summary>
        /// Resolves the billboard camera; falls back to <see cref="Camera.main"/> if the mode-specific camera is unassigned.
        /// </summary>
        public static bool TryResolve(
            GameModeManager.GameMode mode,
            Camera fpsCamera,
            Camera bombermanCamera,
            Camera arenaPerspectiveCamera,
            out Camera cam)
        {
            cam = ResolveForMode(mode, fpsCamera, bombermanCamera, arenaPerspectiveCamera);
            if (cam != null)
                return true;
            cam = Camera.main;
            return cam != null;
        }
    }
}
