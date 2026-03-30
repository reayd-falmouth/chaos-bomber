using HybridGame.MasterBlaster.Scripts.Scenes.Arena;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HybridGame.MasterBlaster.Scripts.Online
{
    /// <summary>
    /// Attach to the NetworkManager (or any persistent GameObject).
    /// On a pure client (not the host), reads local Input System actions every FixedUpdate
    /// and forwards them to the host via GameManager.SendInputServerRpc.
    /// </summary>
    public class NetworkClientInputSender : MonoBehaviour
    {
        [Header("Input Actions")]
        [Tooltip("Assign the same PlayerControls InputActionAsset used by HumanPlayerInput.")]
        [SerializeField] private InputActionAsset inputActions;

        private InputAction _moveAction;
        private InputAction _bombAction;
        private InputAction _detonateAction;
        private bool _bombHeldLastFrame;

        private void Awake()
        {
            BindActions();
        }

        private void BindActions()
        {
            if (inputActions == null)
                return;
            var map = inputActions.FindActionMap("Player");
            if (map == null)
            {
                UnityEngine.Debug.LogWarning("[NetworkClientInputSender] 'Player' action map not found.");
                return;
            }
            _moveAction     = map.FindAction("Move");
            _detonateAction = map.FindAction("Detonate");
            _bombAction     = map.FindAction("PlaceBomb");
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

        private void FixedUpdate()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || !nm.IsListening || nm.IsServer)
                return; // only pure clients send input

            var gm = GameManager.Instance;
            if (gm == null)
                return;

            Vector2 move    = _moveAction     != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
            bool bombHeld   = _bombAction     != null && _bombAction.IsPressed();
            bool bombDown   = bombHeld && !_bombHeldLastFrame;
            bool detonate   = _detonateAction != null ? _detonateAction.IsPressed() : bombHeld;

            _bombHeldLastFrame = bombHeld;

            gm.SendInputServerRpc(move, bombDown, detonate);
        }
    }
}
