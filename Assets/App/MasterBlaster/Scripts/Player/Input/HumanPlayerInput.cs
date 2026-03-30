using UnityEngine;
using UnityEngine.InputSystem;

namespace HybridGame.MasterBlaster.Scripts.Player.Input
{
    /// <summary>
    /// Human input via the new Input System for Bomberman mode.
    /// Copied from MasterBlaster (Scenes.Arena.Player.HumanPlayerInput), namespace changed.
    /// Assigned at runtime by GameModeManager when Bomberman mode is active.
    /// </summary>
    public class HumanPlayerInput : MonoBehaviour, IPlayerInput
    {
        [Tooltip("Assign PlayerControls InputActionAsset.")]
        public InputActionAsset inputActions
        {
            get => _inputActions;
            set { _inputActions = value; BindActions(); }
        }

        [SerializeField]
        private InputActionAsset _inputActions;

        public int deviceIndex;

        private Gamepad _gamepad;
        private InputAction _moveAction;
        private InputAction _bombAction;
        private InputAction _detonateAction;
        private bool _bombHeldLastFrame;

        public void SetGamepad(Gamepad gamepad) => _gamepad = gamepad;

        private void Awake() => BindActions();

        private void BindActions()
        {
            if (_inputActions == null) return;
            var map = _inputActions.FindActionMap("Player");
            if (map == null) return;
            _moveAction     = map.FindAction("Move");
            _bombAction     = map.FindAction("PlaceBomb");
            _detonateAction = map.FindAction("Detonate");
            if (isActiveAndEnabled)
            {
                _moveAction?.Enable();
                _bombAction?.Enable();
                _detonateAction?.Enable();
            }
        }

        private void OnEnable()
        {
            _moveAction?.Enable();
            _bombAction?.Enable();
            _detonateAction?.Enable();
        }

        private void OnDisable()
        {
            _moveAction?.Disable();
            _bombAction?.Disable();
            _detonateAction?.Disable();
        }

        private void LateUpdate() => _bombHeldLastFrame = BombHeld();

        public Vector2 GetMoveDirection()
        {
            if (_gamepad != null)
            {
                var stick = _gamepad.leftStick.ReadValue();
                var dpad  = _gamepad.dpad.ReadValue();
                return stick.sqrMagnitude >= dpad.sqrMagnitude ? stick : dpad;
            }
            return _moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
        }

        public bool GetBombDown()
        {
            bool held = BombHeld();
            return held && !_bombHeldLastFrame;
        }

        public bool GetDetonateHeld()
        {
            if (_gamepad != null)
                return _gamepad.buttonSouth.isPressed || _gamepad.rightShoulder.isPressed;
            if (_detonateAction != null) return _detonateAction.IsPressed();
            return _bombAction != null && _bombAction.IsPressed();
        }

        private bool BombHeld()
        {
            if (_gamepad != null) return _gamepad.buttonSouth.isPressed;
            return _bombAction != null && _bombAction.IsPressed();
        }
    }
}
