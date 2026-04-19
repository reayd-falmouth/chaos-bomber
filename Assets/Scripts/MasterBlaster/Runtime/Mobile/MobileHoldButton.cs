using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Scripts.Mobile
{
    /// <summary>
    /// Press-and-hold button that toggles a shared state flag.
    /// </summary>
    public class MobileHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private const float MinRaycastAlpha = 1f / 255f;

        [Header("Diagnostics")]
        [Tooltip("When enabled, logs pointer-down with control id (subsystem [MasterBlaster][MobileOverlay]). Off by default.")]
        [SerializeField]
        private bool logPointerDownToConsole;

        [Tooltip(
            "When enabled, logs when this instance corrects transparent UI so touches hit this graphic ([MasterBlaster][MobileOverlay]). Off by default.")]
        [SerializeField]
        private bool logRaycastFixToConsole;

        [SerializeField] private MobileControl control;
        public MobileControl Control
        {
            get => control;
            set => control = value;
        }

        public enum MobileControl
        {
            Up,
            Down,
            Left,
            Right,
            Bomb
        }

        public event Action<bool> OnPressedChanged;

        /// <summary>
        /// Re-applies raycast fixes for fully transparent graphics and decorative parents. Idempotent; used at runtime
        /// from Awake/OnEnable and callable from tests (Edit Mode tests may not invoke Unity event methods on ad-hoc GameObjects).
        /// </summary>
        public void RefreshRaycastTarget()
        {
            ApplyMobileOverlayRaycastFixes();
        }

        private void Awake()
        {
            ApplyMobileOverlayRaycastFixes();
        }

        private void OnEnable()
        {
            // Run after Awake so we still correct CanvasRenderer after Graphic/Image setup (and any graphic rebuild).
            ApplyMobileOverlayRaycastFixes();
        }

        private void ApplyMobileOverlayRaycastFixes()
        {
            EnsureRaycastableGraphic();
            TryDisableDecorativeParentImageRaycast();
        }

        /// <summary>
        /// Fully transparent <see cref="Image"/>s are often culled from raycasts; parent decorations then receive the pointer
        /// without a <see cref="MobileHoldButton"/>. Ensures this graphic participates in UI raycasts.
        /// </summary>
        private void EnsureRaycastableGraphic()
        {
            var image = GetComponent<Image>();
            bool bumpedAlpha = false;
            if (image != null && image.color.a <= 0f)
            {
                var c = image.color;
                c.a = MinRaycastAlpha;
                image.color = c;
                bumpedAlpha = true;
            }

            var canvasRenderer = GetComponent<CanvasRenderer>();
            bool hadCull = canvasRenderer != null && canvasRenderer.cullTransparentMesh;
            if (canvasRenderer != null)
                canvasRenderer.cullTransparentMesh = false;

            if (logRaycastFixToConsole && (hadCull || bumpedAlpha))
            {
                UnityEngine.Debug.Log(
                    "[MasterBlaster][MobileOverlay] MobileHoldButton raycast fix go=" + gameObject.name
                    + " control=" + control
                    + " hadCullTransparentMesh=" + hadCull
                    + " minAlphaApplied=" + bumpedAlpha);
            }
        }

        /// <summary>
        /// Scene-authored layouts often use a large decorative <see cref="Image"/> on <c>DPadRoot</c> / <c>BombRoot</c>
        /// with raycasting enabled. That graphic can win the raycast over child tiles (which carry this component),
        /// but it has no <see cref="MobileHoldButton"/> — so pointers never update <see cref="MobileOverlayState"/>.
        /// </summary>
        private void TryDisableDecorativeParentImageRaycast()
        {
            var parent = transform.parent;
            if (parent == null)
                return;
            if (parent.name != "DPadRoot" && parent.name != "BombRoot")
                return;
            if (parent.GetComponent<MobileHoldButton>() != null)
                return;

            var parentImage = parent.GetComponent<Image>();
            if (parentImage == null || !parentImage.raycastTarget)
                return;

            parentImage.raycastTarget = false;

            if (logRaycastFixToConsole)
            {
                UnityEngine.Debug.Log(
                    "[MasterBlaster][MobileOverlay] Disabled decorative Image.raycastTarget on parent="
                    + parent.name + " so child=" + gameObject.name + " control=" + control + " receives pointers.");
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            SetPressed(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            SetPressed(false);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (eventData != null && eventData.dragging)
                SetPressed(false);
        }

        private void OnDisable()
        {
            SetPressed(false);
        }

        private void SetPressed(bool pressed)
        {
            if (logPointerDownToConsole && pressed)
            {
                UnityEngine.Debug.Log(
                    "[MasterBlaster][MobileOverlay] MobileHoldButton pointer down control=" + control
                    + " go=" + gameObject.name);
            }

            switch (control)
            {
                case MobileControl.Up:
                    MobileOverlayState.UpPressed = pressed;
                    break;
                case MobileControl.Down:
                    MobileOverlayState.DownPressed = pressed;
                    break;
                case MobileControl.Left:
                    MobileOverlayState.LeftPressed = pressed;
                    break;
                case MobileControl.Right:
                    MobileOverlayState.RightPressed = pressed;
                    break;
                case MobileControl.Bomb:
                    MobileOverlayState.BombPressed = pressed;
                    break;
            }

            OnPressedChanged?.Invoke(pressed);
        }
    }
}
