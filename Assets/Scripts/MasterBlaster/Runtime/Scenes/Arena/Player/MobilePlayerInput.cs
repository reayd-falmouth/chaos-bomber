using HybridGame.MasterBlaster.Scripts.Mobile;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Player
{
    /// <summary>
    /// Mobile touch input implementation backed by the runtime mobile overlay.
    /// </summary>
    public class MobilePlayerInput : MonoBehaviour, IPlayerInput
    {
        private bool _bombHeldLastFrame;

        private void Awake()
        {
            MobileOverlayBootstrap.EnsurePresent();
        }

        private void LateUpdate()
        {
            _bombHeldLastFrame = GetBombHeld();
        }

        public Vector2 GetMoveDirection()
        {
            return MobileOverlayState.GetDigitalMove();
        }

        public bool GetBombDown()
        {
            bool held = GetBombHeld();
            return held && !_bombHeldLastFrame;
        }

        public bool GetDetonateHeld()
        {
            return GetBombHeld();
        }

        private static bool GetBombHeld()
        {
            return MobileOverlayState.BombPressed;
        }
    }
}
