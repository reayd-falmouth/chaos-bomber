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
    /// </summary>
    public class HybridCameraManager : MonoBehaviour
    {
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

            SyncUiOverlayOnBaseCamera(fpsCamera, isFPS);
            SyncUiOverlayOnBaseCamera(arenaPerspectiveCamera, useDedicatedPerspective);

            UiCanvasCameraBinder.RebindAll();
        }

        /// <summary>
        /// Scene-authored bomberman base already lists the UI Canvas overlay in URP; FPS (and optional arena base) do not.
        /// Add/remove the overlay on those bases so stacked UI renders and <see cref="UiCanvasCameraBinder"/> + <see cref="UnityEngine.Camera.main"/> stay consistent.
        /// </summary>
        static void SyncUiOverlayOnBaseCamera(UnityEngine.Camera baseCamera, bool stackShouldIncludeOverlay)
        {
            if (baseCamera == null)
                return;

            var overlay = ResolveUiCanvasOverlayCamera();
            if (overlay == null)
                return;

            var data = baseCamera.GetUniversalAdditionalCameraData();
            if (data == null)
                return;

            var stack = data.cameraStack;
            if (stackShouldIncludeOverlay)
            {
                if (!stack.Contains(overlay))
                    stack.Add(overlay);
            }
            else
            {
                stack.Remove(overlay);
            }
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
