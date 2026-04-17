using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HybridGame.MasterBlaster.Scripts.Mobile
{
    /// <summary>
    /// Press-and-hold button that toggles a shared state flag.
    /// </summary>
    public class MobileHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
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
