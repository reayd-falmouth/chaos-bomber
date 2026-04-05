using HybridGame.MasterBlaster.Scripts;
using HybridGame.MasterBlaster.Scripts.Arena;
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
    /// FPS        — sprite billboards toward Camera.main in 3D; scale reset to (1,1,1).
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
        }

        private void LateUpdate()
        {
            if (GameModeManager.Instance == null)
                return;

            var mode = GameModeManager.Instance.CurrentMode;
            if (GameModeManager.IsGridPresentationMode(mode))
            {
                transform.localScale = m_BombermanScale;
                var cam = UnityEngine.Camera.main;
                if (cam == null ||
                    BillboardSpriteOrientationMath.UseFixedTopDownStyle(mode, cam.orthographic))
                {
                    transform.rotation = Quaternion.Euler(bombermanEulerAngles);
                }
                else
                {
                    float camPitchX = BillboardSpriteOrientationMath.NormalizeEulerX(cam.transform.eulerAngles.x);
                    var euler = BillboardSpriteOrientationMath.ComputePerspectiveGridEuler(bombermanEulerAngles, camPitchX);
                    transform.rotation = Quaternion.Euler(euler);
                }

                return;
            }

            // FPS mode: billboard to face the active camera (full 3D direction).
            transform.localScale = Vector3.one;
            var fpsCam = UnityEngine.Camera.main;
            if (fpsCam == null) return;
            Vector3 dirToCamera = fpsCam.transform.position - transform.position;
            if (dirToCamera.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(-dirToCamera.normalized, Vector3.up);
        }

        private void ComputeBombermanScale()
        {
            float size = targetWorldSize > 0.001f ? targetWorldSize : ArenaGrid3D.CellSize;
            m_BombermanScale = new Vector3(size, size, size);
        }
    }
}
