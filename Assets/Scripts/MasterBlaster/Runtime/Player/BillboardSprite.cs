using System.Collections;
using HybridGame.MasterBlaster.Scripts;
using HybridGame.MasterBlaster.Scripts.Arena;
using HybridGame.MasterBlaster.Scripts.Camera;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Player
{
    /// <summary>
    /// Rotates and scales this transform each LateUpdate for the active camera mode.
    ///
    /// Bomberman — sprite lies flat in the XZ plane, front face pointing UP (+Y) so it is
    ///             correctly visible to the overhead orthographic camera (forward = -Y).
    ///             Use <see cref="bombermanEulerAngles"/> per prefab (bombs use X = 90°).
    ///             Scale uses <see cref="targetWorldSize"/> or one cell when 0.
    ///
    /// ArenaPerspective — same scale as grid; if MainCamera is perspective, pitch follows the camera
    ///                    (see <see cref="BillboardSpriteOrientationMath"/>).
    ///
    /// FPS        — euler (-90, yaw, 0): fixed pitch, Y yaw toward resolved gameplay camera on XZ; scale matches grid sizing.
    /// </summary>
    public class BillboardSprite : MonoBehaviour
    {
        [Tooltip("Desired uniform world scale in Bomberman mode. 0 = use ArenaGrid3D.CellSize.")]
        [SerializeField] private float targetWorldSize;

        [Tooltip("Applied every LateUpdate in Bomberman mode (e.g. bombs: X = 90).")]
        [SerializeField] private Vector3 bombermanEulerAngles = new Vector3(0f, 0f, 0f);

        private Vector3 m_BombermanScale = Vector3.one;

        private void Start()
        {
            // If unset, default to lying flat facing up so an overhead camera can see the sprite.
            // (SpriteRenderer faces +Z by default; overhead camera looks down -Y.)
            //if (bombermanEulerAngles == Vector3.zero)
            //    bombermanEulerAngles = new Vector3(-90f, 0f, 0f);
            ComputeBombermanScale();
            StartCoroutine(CoApplyBillboardAfterHybridCameraReady());
        }

        /// <summary>
        /// One frame after Start: <see cref="HybridCameraManager"/> may register after spawned bombs/explosions enable.
        /// </summary>
        private IEnumerator CoApplyBillboardAfterHybridCameraReady()
        {
            yield return null;
            ApplyBillboardOrientation();
        }

        private void LateUpdate()
        {
            ApplyBillboardOrientation();
        }

        private void ApplyBillboardOrientation()
        {
            transform.localScale = m_BombermanScale;

            if (GameModeManager.Instance == null)
            {
                ApplyBillboardWithoutGameModeManager();
                return;
            }

            var mode = GameModeManager.Instance.CurrentMode;
            if (!TryGetBillboardCamera(mode, out var cam) || cam == null)
                return;

            if (GameModeManager.IsGridPresentationMode(mode))
            {
                if (BillboardSpriteOrientationMath.UseFixedTopDownStyle(mode, cam.orthographic))
                    transform.rotation = Quaternion.Euler(bombermanEulerAngles);
                else
                {
                    float camPitchX = BillboardSpriteOrientationMath.NormalizeEulerX(cam.transform.eulerAngles.x);
                    var euler = BillboardSpriteOrientationMath.ComputePerspectiveGridEuler(bombermanEulerAngles, camPitchX);
                    transform.rotation = Quaternion.Euler(euler);
                }

                return;
            }

            transform.rotation = BillboardSpriteOrientationMath.ComputeFpsBillboardRotation(
                transform.position, cam.transform);
        }

        private void ApplyBillboardWithoutGameModeManager()
        {
            UnityEngine.Camera cam = null;
            if (HybridCameraManager.Instance != null &&
                HybridCameraManager.Instance.TryGetCameraForBillboards(GameModeManager.GameMode.FPS, out var resolved))
                cam = resolved;
            if (cam == null)
                cam = UnityEngine.Camera.main;
            if (cam == null)
                return;

            if (cam.orthographic)
                transform.rotation = Quaternion.Euler(bombermanEulerAngles);
            else
                transform.rotation = BillboardSpriteOrientationMath.ComputeFpsBillboardRotation(
                    transform.position, cam.transform);
        }

        private static bool TryGetBillboardCamera(GameModeManager.GameMode mode, out UnityEngine.Camera cam)
        {
            if (HybridCameraManager.Instance != null &&
                HybridCameraManager.Instance.TryGetCameraForBillboards(mode, out cam))
                return true;
            cam = UnityEngine.Camera.main;
            return cam != null;
        }

        private void ComputeBombermanScale()
        {
            float size = targetWorldSize > 0.001f ? targetWorldSize : ArenaGrid3D.CellSize;
            m_BombermanScale = new Vector3(size, size, size);
        }
    }
}
