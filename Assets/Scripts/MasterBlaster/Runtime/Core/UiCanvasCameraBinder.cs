using HybridGame.MasterBlaster.Scripts;
using UnityEngine;
using UnityEngine.UI;
using UiCam = global::UnityEngine.Camera;

namespace HybridGame.MasterBlaster.Scripts.Core
{
    /// <summary>
    /// Keeps all <see cref="Canvas"/> instances under this root in <see cref="RenderMode.ScreenSpaceCamera"/>
    /// wired to the sibling camera on <c>UI Canvas</c>, so flow UI still renders after
    /// <see cref="HybridGame.MasterBlaster.Scripts.Camera.HybridCameraManager"/> disables the gameplay main camera.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas), typeof(UiCam))]
    public sealed class UiCanvasCameraBinder : MonoBehaviour
    {
        private void Awake() => Bind();

        private void OnEnable()
        {
            GameModeManager.OnModeChanged += HandleGameModeChanged;
            Bind();
        }

        private void OnDisable()
        {
            GameModeManager.OnModeChanged -= HandleGameModeChanged;
        }

        private void HandleGameModeChanged(GameModeManager.GameMode _) => Bind();

        /// <summary>For tests and editor tooling: assign <paramref name="uiCamera"/> to every Screen Space Camera canvas under <paramref name="root"/>.</summary>
        public static void ApplyUiCameraToSubtree(Canvas root, UiCam uiCamera)
        {
            if (root == null || uiCamera == null)
                return;

            var canvases = root.GetComponentsInChildren<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                var c = canvases[i];
                if (c != null && c.renderMode == RenderMode.ScreenSpaceCamera)
                    c.worldCamera = uiCamera;
            }
        }

        public void Bind()
        {
            var uiCam = GetComponent<UiCam>();
            var root = GetComponent<Canvas>();
            if (uiCam == null || root == null)
                return;
            ApplyUiCameraToSubtree(root, uiCam);
        }
    }
}
