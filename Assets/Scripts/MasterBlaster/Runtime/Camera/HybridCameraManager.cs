using System.Collections;
using HybridGame.MasterBlaster.Scripts;
using HybridGame.MasterBlaster.Scripts.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HybridGame.MasterBlaster.Scripts.Camera
{
    /// <summary>
    /// Switches between FPS, orthographic overhead Bomberman, and optional perspective arena cameras.
    /// Camera.main always refers to the active camera because depth values are swapped.
    /// Only one enabled camera should be tagged MainCamera at a time; do not tag disabled helper cameras (e.g. under the player).
    /// </summary>
    public class HybridCameraManager : MonoBehaviour
    {
        public static HybridCameraManager Instance { get; private set; }

        [Header("Cameras")]
        [Tooltip("The player's first-person child camera (FPS mode)")]
        public UnityEngine.Camera fpsCamera;

        [Tooltip("Overhead orthographic camera for Bomberman mode (scene-level, not player child)")]
        public UnityEngine.Camera bombermanCamera;

        [Tooltip("Optional perspective arena camera for ArenaPerspective mode (angled view). If unassigned, ortho is used for that mode too.")]
        public UnityEngine.Camera arenaPerspectiveCamera;

        [Header("Bomberman Camera Settings")]
        [Tooltip("Orthographic size — set to half the arena's largest dimension")]
        public float bombermanOrthoSize = 8f;

        [Tooltip("Height above the arena centre")]
        public float bombermanHeight = 20f;

        [Tooltip("Arena centre in world space (XZ)")]
        public Vector2 arenaCentreXZ = new Vector2(7f, 6f); // default for 15x13 grid

        [Header("Screen Letterboxing (Bomberman)")]
        public Vector2 designResolution = new Vector2(640f, 512f);

        Coroutine m_FpsProjectionResetRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Camera used for <see cref="HybridGame.MasterBlaster.Scripts.Player.BillboardSprite"/> facing; matches
        /// <see cref="SetMode"/> active camera (not necessarily <see cref="UnityEngine.Camera.main"/> the same frame).
        /// </summary>
        public bool TryGetCameraForBillboards(GameModeManager.GameMode mode, out UnityEngine.Camera cam) =>
            BillboardCameraResolver.TryResolve(mode, fpsCamera, bombermanCamera, arenaPerspectiveCamera, out cam);

        private void Start()
        {
            if (fpsCamera != null)
                fpsCamera.rect = new Rect(0, 0, 1, 1);

            if (bombermanCamera != null)
            {
                bombermanCamera.rect = new Rect(0, 0, 1, 1);
                bombermanCamera.orthographic = true;
                bombermanCamera.orthographicSize = bombermanOrthoSize;
                // Position and rotation are set in the scene — do not override here.
            }
        }

        public void SetMode(GameModeManager.GameMode mode)
        {
            bool isFPS = mode == GameModeManager.GameMode.FPS;
            bool isPerspective = mode == GameModeManager.GameMode.ArenaPerspective;
            bool useDedicatedPerspective = isPerspective && arenaPerspectiveCamera != null;
            bool showOrtho = mode == GameModeManager.GameMode.Bomberman
                || (isPerspective && arenaPerspectiveCamera == null);

            void TagMain(UnityEngine.Camera main, UnityEngine.Camera untagA, UnityEngine.Camera untagB)
            {
                if (main != null) main.gameObject.tag = "MainCamera";
                if (untagA != null && untagA != main) untagA.gameObject.tag = "Untagged";
                if (untagB != null && untagB != main) untagB.gameObject.tag = "Untagged";
            }

            if (isFPS)
                TagMain(fpsCamera, bombermanCamera, arenaPerspectiveCamera);
            else if (useDedicatedPerspective)
                TagMain(arenaPerspectiveCamera, fpsCamera, bombermanCamera);
            else if (bombermanCamera != null)
                TagMain(bombermanCamera, fpsCamera, arenaPerspectiveCamera);

            if (fpsCamera != null)
            {
                if (m_FpsProjectionResetRoutine != null)
                {
                    StopCoroutine(m_FpsProjectionResetRoutine);
                    m_FpsProjectionResetRoutine = null;
                }

                fpsCamera.gameObject.SetActive(isFPS);
                fpsCamera.depth = isFPS ? 0 : -1;

                if (isFPS)
                {
                    fpsCamera.enabled = false;
                    fpsCamera.enabled = true;
                    m_FpsProjectionResetRoutine = StartCoroutine(ResetFpsProjectionNextFrame());
                }
            }

            if (bombermanCamera != null)
            {
                bombermanCamera.gameObject.SetActive(showOrtho);
                bombermanCamera.depth = showOrtho ? 0 : -1;
            }

            if (arenaPerspectiveCamera != null)
            {
                arenaPerspectiveCamera.gameObject.SetActive(useDedicatedPerspective);
                arenaPerspectiveCamera.depth = useDedicatedPerspective ? 0 : -1;
            }

            RemoveUiCanvasOverlayFromBaseCameraStack(fpsCamera);
            RemoveUiCanvasOverlayFromBaseCameraStack(bombermanCamera);
            RemoveUiCanvasOverlayFromBaseCameraStack(arenaPerspectiveCamera);

            UiCanvasCameraBinder.RebindAll();
        }

        /// <summary>
        /// UI uses Screen Space - Camera on the gameplay Base camera. The legacy child <see cref="UnityEngine.Camera"/>
        /// on <c>UI Canvas</c> must not stay in URP camera stacks (would duplicate UI / fight with SS-Camera rendering).
        /// </summary>
        static void RemoveUiCanvasOverlayFromBaseCameraStack(UnityEngine.Camera baseCamera)
        {
            if (baseCamera == null)
                return;

            var overlay = ResolveUiCanvasOverlayCamera();
            if (overlay == null)
                return;

            var data = baseCamera.GetUniversalAdditionalCameraData();
            if (data == null)
                return;

            data.cameraStack.Remove(overlay);
        }

        static UnityEngine.Camera ResolveUiCanvasOverlayCamera()
        {
            var go = GameObject.Find("UI Canvas");
            return go != null ? go.GetComponent<UnityEngine.Camera>() : null;
        }

        IEnumerator ResetFpsProjectionNextFrame()
        {
            yield return null;
            m_FpsProjectionResetRoutine = null;
            if (fpsCamera != null && fpsCamera.gameObject.activeInHierarchy && fpsCamera.enabled)
                fpsCamera.ResetProjectionMatrix();
        }

        // private void ApplyLetterbox()
        // {
        //     if (bombermanCamera == null) return;
        //     if (Screen.width <= 0 || Screen.height <= 0 || designResolution.y == 0) return;
        //
        //     float designAspect = designResolution.x / designResolution.y;
        //     float screenAspect = (float)Screen.width / Screen.height;
        //
        //     if (screenAspect > designAspect)
        //     {
        //         // Screen wider than design (e.g. 16:9 vs 5:4) — pillarbox (black bars left/right)
        //         float scaleWidth = designAspect / screenAspect;
        //         float offsetX = (1f - scaleWidth) / 2f;
        //         bombermanCamera.rect = new Rect(offsetX, 0f, scaleWidth, 1f);
        //     }
        //     else
        //     {
        //         // Screen taller than design — letterbox (black bars top/bottom)
        //         float scaleHeight = screenAspect / designAspect;
        //         float offsetY = (1f - scaleHeight) / 2f;
        //         bombermanCamera.rect = new Rect(0f, offsetY, 1f, scaleHeight);
        //     }
        // }
        //
        // private void OnValidate()
        // {
        //     if (bombermanCamera != null)
        //         ApplyLetterbox();
        // }
    }
}
