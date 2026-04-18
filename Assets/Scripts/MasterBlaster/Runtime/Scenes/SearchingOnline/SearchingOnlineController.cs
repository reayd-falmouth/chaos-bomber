using System;
using System.Threading.Tasks;
using HybridGame.MasterBlaster.Scripts.Core;
using HybridGame.MasterBlaster.Scripts.Mobile;
using HybridGame.MasterBlaster.Scripts.Online;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Scripts.Scenes.SearchingOnline
{
    public class SearchingOnlineController : MonoBehaviour, IFlowScreen
    {
        public enum OnlineBackend
        {
            UnityServicesLobby = 0,
            PlayFabLobby = 1
        }

        [Header("UI")]
        [SerializeField] private Text statusText;
        [SerializeField] private Text connectionCodeText;

        [Header("Matchmaking")]
        [SerializeField] private OnlineBackend backend = OnlineBackend.PlayFabLobby;
        [SerializeField] private bool hostByDefault = true;
        [Range(2, 8)]
        [SerializeField] private int maxPlayers = 5;

        [Header("Input Setup")]
        [Tooltip("Assign UIMenus or PlayerControls Input Action Asset in Inspector.")]
        public InputActionAsset inputActions;

        private InputAction _submitAction;
        private bool _mobileBombHeldLastFrame;
        private bool _started;
        private bool _active;

        private void Awake()
        {
            if (inputActions == null)
            {
                UnityEngine.Debug.LogWarning("[SearchingOnlineController] InputActionAsset not assigned.");
                return;
            }
            var playerMap = inputActions.FindActionMap("Player");
            _submitAction = playerMap.FindAction("PlaceBomb");
        }

        private void OnEnable()
        {
            _submitAction?.Enable();
            _active = true;
            SetStatus("Searching...");
            SetConnectionCode(string.Empty);
            TryStartSearching();
        }

        private void OnDisable()
        {
            _active = false;
            _submitAction?.Disable();
        }

        public void OnFlowPresented()
        {
            _active = true;
            SetStatus("Searching...");
            SetConnectionCode(string.Empty);
            TryStartSearching();
        }

        public void OnFlowDismissed()
        {
            _active = false;
        }

        private void Update()
        {
            if (SceneFlowManager.I != null && SceneFlowManager.I.IsTransitioning)
                return;
            if (_submitAction == null)
                return;

            // Treat submit as "cancel" for now (includes on-screen bomb on handheld).
            if (MobileMenuInputBridge.SubmitPressedThisFrame(_submitAction, ref _mobileBombHeldLastFrame)
                && !GlobalPauseMenuController.IsPaused && !GlobalPauseMenuController.WasClosedThisFrame)
            {
                LeaveLobbyIfPossible();
                SceneFlowManager.I.GoTo(FlowState.LevelSelect);
            }
        }

        private void TryStartSearching()
        {
            if (_started)
                return;
            _started = true;
            _ = StartSearchingAsync();
        }

        private async Task StartSearchingAsync()
        {
            try
            {
                if (hostByDefault)
                {
                    SetStatus("Creating lobby...");
                    await CreateLobbyAsync();
                    if (!_active) return;
                    SetStatus("Waiting for players... (Press confirm to cancel)");
                    SetConnectionCode(GetLobbyCodeOrConnectionString());
                }
                else
                {
                    SetStatus("Joining requires a code (not implemented on this screen).");
                }
            }
            catch (Exception e)
            {
                if (!_active) return;
                SetStatus($"Error: {e.Message}");
            }
        }

        private Task CreateLobbyAsync()
        {
            switch (backend)
            {
                case OnlineBackend.PlayFabLobby:
                    if (PlayFabLobbyManager.Instance == null)
                        throw new InvalidOperationException("PlayFabLobbyManager not present in scene.");
                    return PlayFabLobbyManager.Instance.CreateLobbyAsync(maxPlayers);

                default:
                    if (NetworkLobbyManager.Instance == null)
                        throw new InvalidOperationException("NetworkLobbyManager not present in scene.");
                    return NetworkLobbyManager.Instance.CreateLobbyAsync(maxPlayers);
            }
        }

        private string GetLobbyCodeOrConnectionString()
        {
            switch (backend)
            {
                case OnlineBackend.PlayFabLobby:
                    return PlayFabLobbyManager.Instance != null ? (PlayFabLobbyManager.Instance.LobbyConnectionString ?? string.Empty) : string.Empty;
                default:
                    return NetworkLobbyManager.Instance != null ? (NetworkLobbyManager.Instance.LobbyJoinCode ?? string.Empty) : string.Empty;
            }
        }

        private void LeaveLobbyIfPossible()
        {
            switch (backend)
            {
                case OnlineBackend.PlayFabLobby:
                    PlayFabLobbyManager.Instance?.LeaveLobby();
                    break;
                default:
                    if (NetworkLobbyManager.Instance != null)
                        NetworkLobbyManager.Instance.LeaveLobby();
                    break;
            }
        }

        private void SetStatus(string msg)
        {
            if (statusText != null)
                statusText.text = msg;
            UnityEngine.Debug.Log($"[SearchingOnline] {msg}");
        }

        private void SetConnectionCode(string code)
        {
            if (connectionCodeText != null)
                connectionCodeText.text = code ?? string.Empty;
        }
    }
}

