using HybridGame.MasterBlaster.Scripts;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Player
{
    /// <summary>
    /// Pure helpers for <see cref="BillboardSprite"/> grid pitch and FPS billboarding (unit-tested).
    /// </summary>
    public static class BillboardSpriteOrientationMath
    {
        private const float MinDirSqrMag = 0.001f;

        /// <summary>
        /// When true, use fixed <c>bombermanEulerAngles</c> (ortho top-down or Bomberman mode).
        /// When false, apply camera pitch compensation in ArenaPerspective with a perspective MainCamera.
        /// </summary>
        public static bool UseFixedTopDownStyle(GameModeManager.GameMode mode, bool cameraOrthographic)
        {
            return mode == GameModeManager.GameMode.Bomberman || cameraOrthographic;
        }

        /// <summary>
        /// Normalizes Unity euler X to [-180, 180) to avoid wrap jumps when subtracting from sprite pitch.
        /// </summary>
        public static float NormalizeEulerX(float eulerX)
        {
            float x = eulerX % 360f;
            if (x >= 180f) x -= 360f;
            if (x < -180f) x += 360f;
            return x;
        }

        /// <summary>
        /// Perspective arena grid: sprite X = base X minus camera pitch (e.g. cam +60° → extra −60° on X when base is 0).
        /// Y/Z come from the prefab (character facing, roll).
        /// </summary>
        public static Vector3 ComputePerspectiveGridEuler(Vector3 bombermanEulerAngles, float normalizedCameraEulerX)
        {
            return new Vector3(
                bombermanEulerAngles.x - normalizedCameraEulerX,
                bombermanEulerAngles.y,
                bombermanEulerAngles.z);
        }

        /// <summary>
        /// FPS: fixed pitch X = -90°, Z = 0°; Y yaw from horizontal direction to the camera (cylindrical billboard).
        /// Returns false when the horizontal offset is degenerate (see <see cref="ComputeFpsBillboardRotation"/> for fallback).
        /// </summary>
        public static bool TryComputeFpsCylindricalBillboardRotation(
            Vector3 spriteWorldPosition,
            Vector3 cameraWorldPosition,
            out Quaternion rotation)
        {
            Vector3 dir = cameraWorldPosition - spriteWorldPosition;
            dir.y = 0f;
            if (dir.sqrMagnitude <= MinDirSqrMag)
            {
                rotation = Quaternion.identity;
                return false;
            }

            float yaw = Quaternion.LookRotation(dir.normalized, Vector3.up).eulerAngles.y;
            rotation = Quaternion.Euler(-90f, yaw, 0f);
            return true;
        }

        /// <summary>
        /// FPS billboard rotation: yaw toward the camera on XZ when non-degenerate; when the camera is in the same
        /// XZ column as the sprite (typical for a first-person camera and a Billbox on the same player), uses camera yaw.
        /// </summary>
        public static Quaternion ComputeFpsBillboardRotation(Vector3 spriteWorldPosition, Transform camera)
        {
            if (camera == null)
                return Quaternion.identity;

            Vector3 dir = camera.position - spriteWorldPosition;
            dir.y = 0f;
            if (dir.sqrMagnitude > MinDirSqrMag)
            {
                float yaw = Quaternion.LookRotation(dir.normalized, Vector3.up).eulerAngles.y;
                return Quaternion.Euler(-90f, yaw, 0f);
            }

            return Quaternion.Euler(-90f, camera.eulerAngles.y, 0f);
        }

        /// <summary>
        /// FPS: full billboard — transform forward (+Z) points toward the camera. Typical quad faces +Z; euler X is often
        /// near 0 when the camera is level (unlike <see cref="ComputeFpsBillboardRotation"/> cylindrical -90° pitch).
        /// </summary>
        public static Quaternion ComputeFpsBillboardLookAtRotation(Vector3 spriteWorldPosition, Transform camera)
        {
            if (camera == null)
                return Quaternion.identity;

            Vector3 toCam = camera.position - spriteWorldPosition;
            if (toCam.sqrMagnitude <= MinDirSqrMag)
                return Quaternion.LookRotation(camera.forward, Vector3.up);

            return Quaternion.LookRotation(toCam.normalized, Vector3.up);
        }
    }
}
