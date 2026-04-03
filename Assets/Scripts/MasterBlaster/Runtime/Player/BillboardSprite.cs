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
    /// FPS        — sprite billboards to face Camera.main; scale reset to (1,1,1).
    /// </summary>
    public class BillboardSprite : MonoBehaviour
    {
        [Tooltip("Desired uniform world scale in Bomberman mode. 0 = use ArenaGrid3D.CellSize.")]
        [SerializeField] private float targetWorldSize;

        [Tooltip("Applied every LateUpdate in Bomberman mode (e.g. bombs: X = 90).")]
        [SerializeField] private Vector3 bombermanEulerAngles = new Vector3(0f, 0f, 0f);

        private SpriteRenderer m_SR;
        private Vector3 m_BombermanScale = Vector3.one;

        private void Awake()
        {
            m_SR = GetComponent<SpriteRenderer>();
        }

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
            if (GameModeManager.Instance == null ||
                GameModeManager.IsGridPresentationMode(GameModeManager.Instance.CurrentMode))
            {
                transform.rotation   = Quaternion.Euler(bombermanEulerAngles);
                transform.localScale = m_BombermanScale;
                return;
            }

            // FPS mode: billboard to face the active camera.
            transform.localScale = Vector3.one;
            var cam = UnityEngine.Camera.main;
            if (cam == null) return;
            Vector3 dirToCamera = cam.transform.position - transform.position;
            dirToCamera.y = 0f;
            if (dirToCamera.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(-dirToCamera, Vector3.up);
        }

        private void ComputeBombermanScale()
        {
            float size = targetWorldSize > 0.001f ? targetWorldSize : ArenaGrid3D.CellSize;
            m_BombermanScale = new Vector3(size, size, size);
        }
    }
}
