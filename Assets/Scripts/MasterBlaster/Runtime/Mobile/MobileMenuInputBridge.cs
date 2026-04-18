using UnityEngine;
using UnityEngine.InputSystem;

namespace HybridGame.MasterBlaster.Scripts.Mobile
{
    /// <summary>
    /// Merges New Input System UI actions with on-screen D-pad / bomb for handheld builds.
    /// </summary>
    public static class MobileMenuInputBridge
    {
        public static Vector2 MergeMove(Vector2 fromActions)
        {
            if (!FlowScreenAccessibilityTextScale.IsHandheldMobile())
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
                if (FlowScreenAccessibilityTextScale.IsHandheldMobile())
                    bombHeldLastFrame = MobileOverlayState.BombPressed;
                return true;
            }

            if (!FlowScreenAccessibilityTextScale.IsHandheldMobile())
                return false;

            bool bomb = MobileOverlayState.BombPressed;
            bool edge = bomb && !bombHeldLastFrame;
            bombHeldLastFrame = bomb;
            return edge;
        }
    }
}
