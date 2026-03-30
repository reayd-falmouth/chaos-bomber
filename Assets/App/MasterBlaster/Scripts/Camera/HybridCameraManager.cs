using System.Collections;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Camera
{
    /// <summary>
    /// Switches between the first-person FPS camera and an orthographic overhead Bomberman camera.
    /// Camera.main always refers to the active camera because depth values are swapped.
    /// </summary>
    public class HybridCameraManager : MonoBehaviour
    {
        [Header("Cameras")]
        [Tooltip("The player's first-person child camera (FPS mode)")]
        public UnityEngine.Camera fpsCamera;

        [Tooltip("Overhead orthographic camera for Bomberman mode (scene-level, not player child)")]
        public UnityEngine.Camera bombermanCamera;

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

            // Only one camera should carry MainCamera — avoids Camera.main resolving wrong / blank view when both exist in scene.
            if (fpsCamera != null && bombermanCamera != null)
            {
                if (isFPS)
                {
                    bombermanCamera.gameObject.tag = "Untagged";
                    fpsCamera.gameObject.tag = "MainCamera";
                }
                else
                {
                    fpsCamera.gameObject.tag = "Untagged";
                    bombermanCamera.gameObject.tag = "MainCamera";
                }
            }

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
                    // URP can show a blank/sky-only view if ResetProjectionMatrix runs before PlayerWeaponsManager
                    // applies FOV the same frame. Defer one frame so FOV and camera state are consistent.
                    m_FpsProjectionResetRoutine = StartCoroutine(ResetFpsProjectionNextFrame());
                }
            }

            if (bombermanCamera != null)
            {
                bombermanCamera.gameObject.SetActive(!isFPS);
                bombermanCamera.depth = isFPS ? -1 : 0;
                // if (!isFPS) ApplyLetterbox();
            }
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
