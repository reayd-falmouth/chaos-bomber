using System.Collections;
using HybridGame.MasterBlaster.Scripts;
using HybridGame.MasterBlaster.Scripts.Arena;
using HybridGame.MasterBlaster.Scripts.Debug;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Player
{
    /// <summary>
    /// Rotates and scales this transform each LateUpdate for the active camera mode.
    /// Also reapplies when <see cref="GameModeManager.OnModeChanged"/> fires (after cameras update),
    /// matching <see cref="PlayerDualModeController"/> Billbox sync for spawned bombs/explosions.
    ///
    /// Bomberman — sprite lies flat in the XZ plane, front face pointing UP (+Y) so it is
    ///             correctly visible to the overhead orthographic camera (forward = -Y).
    ///             Use <see cref="bombermanEulerAngles"/> per prefab (aligned with player Billbox for grid ortho).
    ///             Scale uses <see cref="targetWorldSize"/> or one cell when 0.
    ///
    /// ArenaPerspective — same scale as grid; if MainCamera is perspective, pitch follows the camera
    ///                    (see <see cref="BillboardSpriteOrientationMath"/>).
    ///
    /// FPS (or any time <see cref="UnityEngine.Camera.main"/> is perspective) — euler (-90, yaw, 0): yaw toward
    /// gameplay camera on XZ. Grid euler is used only for true top-down (orthographic main) grid modes.
    ///
    /// Billbox local Y for the player is driven by <see cref="PlayerDualModeController"/>; this component only
    /// sets world rotation and uniform scale here.
    /// </summary>
    [DefaultExecutionOrder(500)]
    public class BillboardSprite : MonoBehaviour
    {
        [Tooltip("Desired uniform world scale in Bomberman mode. 0 = use ArenaGrid3D.CellSize.")]
        [SerializeField] private float targetWorldSize;

        [Tooltip("Applied every LateUpdate in Bomberman mode (e.g. bombs: X = 90).")]
        [SerializeField] private Vector3 bombermanEulerAngles = new Vector3(0f, 0f, 0f);

        [Tooltip("When enabled, logs mode/camera/top-down path about once per second (Editor + Development builds).")]
        [SerializeField] private bool debugBillboardOrientation;

        private Vector3 m_BombermanScale = Vector3.one;
        private float m_NextBillboardDebugLogTime;

        private void OnEnable()
        {
            GameModeManager.OnModeChanged += HandleGameModeChanged;
            if (TryGetGameModeManager() != null)
                ApplyBillboardOrientation();
        }

        private void OnDisable()
        {
            GameModeManager.OnModeChanged -= HandleGameModeChanged;
        }

        private void HandleGameModeChanged(GameModeManager.GameMode mode)
        {
            ApplyBillboardOrientation();
        }

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

            var gmm = TryGetGameModeManager();
            if (gmm == null)
            {
                ApplyBillboardWithoutGameModeManager();
                return;
            }

            var mode = gmm.CurrentMode;
            if (!BillboardSpriteCameraHelper.TryResolveBillboardCamera(mode, out var cam))
                return;

            MaybeDebugLogBillboardState(mode, cam);

            if (BillboardSpriteCameraHelper.UseTopDownGridBillboardRotation(mode))
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

            var billboardCam = BillboardSpriteCameraHelper.GetFpsBillboardCameraTransform(cam);
            if (billboardCam == null)
                return;

            transform.rotation = BillboardSpriteOrientationMath.ComputeFpsBillboardRotation(
                transform.position, billboardCam);
        }

        /// <summary>
        /// When <see cref="GameModeManager.Instance"/> is null (Awake order, missing scene object, or duplicate destroyed),
        /// infer grid vs FPS from the tagged <see cref="UnityEngine.Camera.main"/> first. The previous implementation
        /// resolved <see cref="HybridGame.MasterBlaster.Scripts.Camera.BillboardCameraResolver"/> as if mode were FPS while
        /// <c>Camera.main</c> was still orthographic, and applied FPS rotation using the disabled perspective FPS camera.
        /// </summary>
        private void ApplyBillboardWithoutGameModeManager()
        {
            var main = UnityEngine.Camera.main;
            if (main == null)
                return;

            if (main.orthographic)
            {
                transform.rotation = Quaternion.Euler(bombermanEulerAngles);
                return;
            }

            if (!BillboardSpriteCameraHelper.TryResolveBillboardCamera(GameModeManager.GameMode.FPS, out var cam))
                return;

            var billboardCam = BillboardSpriteCameraHelper.GetFpsBillboardCameraTransform(cam);
            if (billboardCam == null)
                return;

            transform.rotation = BillboardSpriteOrientationMath.ComputeFpsBillboardRotation(
                transform.position, billboardCam);
        }

        /// <summary>
        /// Prefer <see cref="GameModeManager.Instance"/>; if not yet registered (script order), find any component in the scene.
        /// </summary>
        private static GameModeManager TryGetGameModeManager()
        {
            if (GameModeManager.Instance != null)
                return GameModeManager.Instance;

            return UnityEngine.Object.FindFirstObjectByType<GameModeManager>(FindObjectsInactive.Include);
        }

        private void ComputeBombermanScale()
        {
            float size = targetWorldSize > 0.001f ? targetWorldSize : ArenaGrid3D.CellSize;
            m_BombermanScale = new Vector3(size, size, size);
        }

        private void MaybeDebugLogBillboardState(GameModeManager.GameMode mode, UnityEngine.Camera resolvedCam)
        {
            if (!debugBillboardOrientation)
                return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Time.unscaledTime < m_NextBillboardDebugLogTime)
                return;
            m_NextBillboardDebugLogTime = Time.unscaledTime + 1f;

            bool topDown = BillboardSpriteCameraHelper.UseTopDownGridBillboardRotation(mode);
            var main = UnityEngine.Camera.main;
            string mainName = main != null ? main.name : "null";
            string mainOrtho = main != null ? main.orthographic.ToString() : "n/a";
            string resolvedName = resolvedCam != null ? resolvedCam.name : "null";
            string resolvedOrtho = resolvedCam != null ? resolvedCam.orthographic.ToString() : "n/a";
            string d = "{\"mode\":\"" + mode + "\",\"topDownGrid\":" + (topDown ? "true" : "false") +
                       ",\"mainName\":\"" + mainName + "\",\"mainOrtho\":" + mainOrtho +
                       ",\"resolvedName\":\"" + resolvedName + "\",\"resolvedOrtho\":" + resolvedOrtho +
                       ",\"go\":\"" + gameObject.name.Replace("\"", "'") + "\"}";
            AgentDebugNdjson.Log("BB", "BillboardSprite.MaybeDebugLogBillboardState", "tick", d);
            UnityEngine.Debug.Log(
                "[BillboardSprite] mode=" + mode + " topDownGrid=" + topDown +
                " main=" + mainName + " ortho=" + mainOrtho +
                " resolved=" + resolvedName + " ortho=" + resolvedOrtho +
                " on " + gameObject.name,
                this);
#endif
        }
    }
}
