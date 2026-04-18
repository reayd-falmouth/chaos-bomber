using UnityEngine;
using UnityEngine.InputSystem;

namespace HybridGame.MasterBlaster.Scripts.Mobile
{
    /// <summary>
    /// Merges New Input System UI actions with on-screen D-pad / bomb for handheld and editor overlay preview.
    /// </summary>
    public static class MobileMenuInputBridge
    {
        /// <summary>
        /// Bomberman grid movement: when <see cref="MobileOverlayBootstrap.ShouldMergeOverlayIntoUiInput"/> is true,
        /// on-screen D-pad (<see cref="MobileOverlayState.GetDigitalMove"/>) must win over the Input System Move action,
        /// which can report non-zero values while the cursor is unlocked (blocking the IPlayerInput fallback).
        /// </summary>
        public static Vector2 MergeBombermanGridMove(Vector2 moveActionValue, Vector2 ipMoveDirection)
        {
            bool merge = MobileOverlayBootstrap.ShouldMergeOverlayIntoUiInput();
            Vector2 overlayDigital = merge ? MobileOverlayState.GetDigitalMove() : Vector2.zero;
            return MergeBombermanGridMoveCore(merge, overlayDigital, moveActionValue, ipMoveDirection);
        }

        /// <summary>
        /// Pure merge logic for tests and diagnostics (no static overlay state reads).
        /// </summary>
        public static Vector2 MergeBombermanGridMoveCore(
            bool mergeOverlayUi,
            Vector2 overlayDigital,
            Vector2 moveActionValue,
            Vector2 ipMoveDirection)
        {
            Vector2 rawDir = Vector2.zero;
            if (mergeOverlayUi && overlayDigital.sqrMagnitude >= 0.01f)
                rawDir = overlayDigital;

            if (rawDir.sqrMagnitude < 0.25f)
                rawDir = moveActionValue;

            if (rawDir.sqrMagnitude < 0.25f)
                rawDir = ipMoveDirection;

            return rawDir;
        }

        public static Vector2 MergeMove(Vector2 fromActions)
        {
            if (!MobileOverlayBootstrap.ShouldMergeOverlayIntoUiInput())
                return fromActions;

            Vector2 d = MobileOverlayState.GetDigitalMove();
            if (d.sqrMagnitude < 0.01f)
                return fromActions;

            return d;
        }

        public static bool SubmitPressedThisFrame(InputAction submitAction, ref bool bombHeldLastFrame)
        {
            if (submitAction != null && submitAction.WasPressedThisFrame())
            {
                if (MobileOverlayBootstrap.ShouldMergeOverlayIntoUiInput())
                    bombHeldLastFrame = MobileOverlayState.BombPressed;
                return true;
            }

            if (!MobileOverlayBootstrap.ShouldMergeOverlayIntoUiInput())
                return false;

            bool bomb = MobileOverlayState.BombPressed;
            bool edge = bomb && !bombHeldLastFrame;
            bombHeldLastFrame = bomb;
            return edge;
        }

        /// <summary>
        /// Vertical menu highlight: moved to previous row (stick/D-pad up). Same ±threshold semantics as MainMenuController.
        /// </summary>
        public static bool TryVerticalMenuNavUp(Vector2 previousMerged, Vector2 currentMerged, float threshold = 0.5f) =>
            currentMerged.y > threshold && previousMerged.y <= threshold;

        /// <summary>
        /// Vertical menu highlight: moved to next row (stick/D-pad down).
        /// </summary>
        public static bool TryVerticalMenuNavDown(Vector2 previousMerged, Vector2 currentMerged, float threshold = 0.5f) =>
            currentMerged.y < -threshold && previousMerged.y >= -threshold;
    }
}
