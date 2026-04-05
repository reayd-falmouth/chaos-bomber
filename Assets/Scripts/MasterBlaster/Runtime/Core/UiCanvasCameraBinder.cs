using HybridGame.MasterBlaster.Scripts;
using UnityEngine;
using UnityEngine.UI;
using UiCam = global::UnityEngine.Camera;

namespace HybridGame.MasterBlaster.Scripts.Core
{
    /// <summary>
    /// Keeps Screen Space - Camera canvases under <c>UI Canvas</c> using the active <see cref="UiCam.main"/> gameplay
    /// camera (Base), not the URP Overlay camera on the same GameObject. Updates when <see cref="GameModeManager"/> mode changes.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
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

        /// <summary>Rebinds every <see cref="UiCanvasCameraBinder"/> in loaded scenes (e.g. after <see cref="HybridGame.MasterBlaster.Scripts.Camera.HybridCameraManager"/> updates <c>MainCamera</c>).</summary>
        public static void RebindAll()
        {
            var binders = FindObjectsByType<UiCanvasCameraBinder>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < binders.Length; i++)
            {
                var b = binders[i];
                if (b != null)
                    b.Bind();
            }
        }

        /// <summary>For tests: assign <paramref name="worldCamera"/> to every Screen Space Camera canvas under <paramref name="root"/>.</summary>
        public static void ApplyUiCameraToSubtree(Canvas root, UiCam worldCamera)
        {
            if (root == null || worldCamera == null)
                return;

            var canvases = root.GetComponentsInChildren<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                var c = canvases[i];
                if (c != null && c.renderMode == RenderMode.ScreenSpaceCamera)
                    c.worldCamera = worldCamera;
            }
        }

        public void Bind()
        {
            var root = GetComponent<Canvas>();
            var main = UiCam.main;
            if (root == null || main == null)
                return;
            ApplyUiCameraToSubtree(root, main);
        }
    }
}
