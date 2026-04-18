using UnityEngine;
using UnityEngine.InputSystem;

namespace HybridGame.MasterBlaster.Scripts.Mobile
{
    /// <summary>
    /// Merges New Input System UI actions with on-screen D-pad / bomb for handheld and editor overlay preview.
    /// </summary>
    public static class MobileMenuInputBridge
    {
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
    }
}
