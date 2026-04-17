using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Mobile
{
    /// <summary>
    /// Shared state consumed by mobile input providers.
    /// </summary>
    public static class MobileOverlayState
    {
        public static bool UpPressed { get; set; }
        public static bool DownPressed { get; set; }
        public static bool LeftPressed { get; set; }
        public static bool RightPressed { get; set; }
        public static bool BombPressed { get; set; }

        public static Vector2 GetDigitalMove()
        {
            int x = 0;
            int y = 0;

            if (LeftPressed) x -= 1;
            if (RightPressed) x += 1;
            if (DownPressed) y -= 1;
            if (UpPressed) y += 1;

            // Strict digital D-pad: prefer vertical axis if both are down.
            if (y != 0)
                x = 0;

            return new Vector2(x, y);
        }

        public static void ResetAll()
        {
            UpPressed = false;
            DownPressed = false;
            LeftPressed = false;
            RightPressed = false;
            BombPressed = false;
        }
    }
}
