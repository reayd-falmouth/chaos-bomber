using UnityEngine;
using UnityEngine.InputSystem;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Player
{
    /// <summary>
    /// Human input via the new Input System. This component is added at runtime by GameManager
    /// when a player has a controller assigned (see GameManager.AttachInputProvider).
    /// The Input Action Asset is set from GameManager's "Player Input Actions" field (assign
    /// PlayerControls in the Game scene). Move and PlaceBomb are required; Detonate is optional.
    /// </summary>
    public class HumanPlayerInput : MonoBehaviour, IPlayerInput
    {
        [Header("Input System")]
        [Tooltip("Assign PlayerControls or UIMenus Input Action Asset in Inspector (or set from GameManager).")]
        public InputActionAsset inputActions
        {
            get => _inputActions;
            set { _inputActions = value; BindActions(); }
        }

        [SerializeField]
        private InputActionAsset _inputActions;

        [Header("Device")]
        [Tooltip("Kept for compatibility with GameManager; input comes from the action asset.")]
        public int deviceIndex;

        // When set, all input is read directly from this gamepad, ensuring per-player isolation.
        private Gamepad _gamepad;
        private float _debugUntil;

        private InputAction _moveAction;
        private InputAction _bombAction;
        private InputAction _detonateAction;
        private bool _bombHeldLastFrame;

        /// <summary>Lock this component to a specific gamepad so it ignores all other controllers.</summary>
        public void SetGamepad(Gamepad gamepad)
        {
            _gamepad = gamepad;
            _debugUntil = Time.unscaledTime + 5f; // log input for 5 s after assignment
            UnityEngine.Debug.Log($"[HumanPlayerInput] {gameObject.name} → locked to: {(gamepad != null ? $"{gamepad.displayName} ({gamepad.GetType().Name})" : "NULL")}");
        }

        public void Init(int deviceIndex, KeyCode up, KeyCode down, KeyCode left, KeyCode right, KeyCode bomb, KeyCode detonate)
        {
            this.deviceIndex = deviceIndex;
        }

        private void Awake()
        {
            BindActions();
        }

        private void BindActions()
        {
            if (_inputActions == null)
                return;
            var map = _inputActions.FindActionMap("Player");
            if (map == null)
                return;
            _moveAction = map.FindAction("Move");
            _bombAction = map.FindAction("PlaceBomb");
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

        private void LateUpdate()
        {
            _bombHeldLastFrame = GetBombHeldInternal();

            if (Time.unscaledTime < _debugUntil && _gamepad != null)
            {
                var stick = _gamepad.leftStick.ReadValue();
                var dpad  = _gamepad.dpad.ReadValue();
                bool bomb = _gamepad.buttonSouth.isPressed;
                if (stick != Vector2.zero || dpad != Vector2.zero || bomb)
                    UnityEngine.Debug.Log($"[HumanPlayerInput] {gameObject.name} | stick={stick:F2} dpad={dpad:F2} bomb={bomb}");
            }
        }

        public Vector2 GetMoveDirection()
        {
            if (_gamepad != null)
            {
                var stick = _gamepad.leftStick.ReadValue();
                var dpad  = _gamepad.dpad.ReadValue();
                return stick.sqrMagnitude >= dpad.sqrMagnitude ? stick : dpad;
            }
            if (_moveAction == null)
                return Vector2.zero;
            return _moveAction.ReadValue<Vector2>();
        }

        public bool GetBombDown()
        {
            bool held = GetBombHeldInternal();
            return held && !_bombHeldLastFrame;
        }

        private bool GetBombHeldInternal()
        {
            if (_gamepad != null)
            {
                if (_gamepad.buttonSouth.isPressed)
                    return true;
                // Keyboard PlaceBomb still works when a gamepad is assigned (South is primary).
                if (_bombAction != null && _bombAction.IsPressed())
                    return true;
                return false;
            }

            if (_bombAction == null)
                return false;
            return _bombAction.IsPressed();
        }

        /// <summary>True while the detonate/bomb button is held (do not detonate). When false (button released), time/remote bombs may detonate.</summary>
        public bool GetDetonateHeld()
        {
            if (_gamepad != null)
                return _gamepad.buttonSouth.isPressed || _gamepad.rightShoulder.isPressed;
            if (_detonateAction != null)
                return _detonateAction.IsPressed();
            return _bombAction != null && _bombAction.IsPressed();
        }
    }
}
