using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HybridGame.MasterBlaster.Scripts.Networking
{
    /// <summary>
    /// Reads local input on a pure client and forwards it to the server via ServerRpc.
    /// Copied from MasterBlaster (Online.NetworkClientInputSender), namespace changed.
    /// Extended with FPS look input (Vector2 lookInput) for Phase 6 FPS movement sync.
    ///
    /// TODO Phase 6: wire SendInputServerRpc to the HybridGame networked player controller.
    /// </summary>
    public class NetworkClientInputSender : MonoBehaviour
    {
        [Header("Input Actions")]
        [SerializeField] private InputActionAsset inputActions;

        private InputAction _moveAction;
        private InputAction _bombAction;
        private InputAction _detonateAction;
        private InputAction _lookAction;   // FPS look (Mouse Delta or right stick)
        private bool _bombHeldLastFrame;

        private void Awake() => BindActions();

        private void BindActions()
        {
            if (inputActions == null) return;
            var map = inputActions.FindActionMap("Player");
            if (map == null) return;
            _moveAction     = map.FindAction("Move");
            _bombAction     = map.FindAction("PlaceBomb");
            _detonateAction = map.FindAction("Detonate");
            _lookAction     = map.FindAction("Look");  // add "Look" action to PlayerControls for FPS mode
        }

        private void OnEnable()
        {
            _moveAction?.Enable();
            _bombAction?.Enable();
            _detonateAction?.Enable();
            _lookAction?.Enable();
        }

        private void OnDisable()
        {
            _moveAction?.Disable();
            _bombAction?.Disable();
            _detonateAction?.Disable();
            _lookAction?.Disable();
        }

        private void FixedUpdate()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || !nm.IsListening || nm.IsServer) return;

            Vector2 move    = _moveAction     != null ? _moveAction.ReadValue<Vector2>()     : Vector2.zero;
            Vector2 look    = _lookAction     != null ? _lookAction.ReadValue<Vector2>()     : Vector2.zero;
            bool bombHeld   = _bombAction     != null && _bombAction.IsPressed();
            bool bombDown   = bombHeld && !_bombHeldLastFrame;
            bool detonate   = _detonateAction != null ? _detonateAction.IsPressed() : bombHeld;

            _bombHeldLastFrame = bombHeld;

            // TODO Phase 6: replace with HybridPlayerNetworkController.SendInputServerRpc(move, look, bombDown, detonate)
            UnityEngine.Debug.Log($"[NetworkClientInputSender] move={move} look={look} bombDown={bombDown}");
        }
    }
}
