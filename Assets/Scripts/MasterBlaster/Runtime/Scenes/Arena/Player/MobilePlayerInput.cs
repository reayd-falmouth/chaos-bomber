using HybridGame.MasterBlaster.Scripts.Mobile;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Player
{
    /// <summary>
    /// Mobile touch input implementation backed by the runtime mobile overlay.
    /// </summary>
    public class MobilePlayerInput : MonoBehaviour, IPlayerInput
    {
        private const string LogPrefix = "[MasterBlaster][MobileHandheld][MobilePlayerInput]";

        [Tooltip(
            "When enabled, logs once from Awake that this component is present and overlay bootstrap was requested. Prefix [MasterBlaster][MobileHandheld][MobilePlayerInput].")]
        [SerializeField]
        private bool logLifecycleToConsole;

        private bool _bombHeldLastFrame;

        private void Awake()
        {
            MobileOverlayBootstrap.EnsurePresent();
            if (logLifecycleToConsole)
            {
                UnityEngine.Debug.Log(
                    LogPrefix + " Awake go=" + gameObject.name
                    + " ensureOverlayBootstrap=ok mergeUiWouldBe="
                    + MobileOverlayBootstrap.ShouldMergeOverlayIntoUiInput()
                    + " (Android/iOS or editor handheld preview).",
                    this);
            }
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
