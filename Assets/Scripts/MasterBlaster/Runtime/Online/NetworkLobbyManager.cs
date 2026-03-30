using System.Collections;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Online
{
    /// <summary>
    /// Manages Unity Services initialization, Lobby creation/join, and Relay + NGO startup.
    /// Attach to a persistent GameObject (e.g. Bootstrap or NetworkManager).
    /// </summary>
    public class NetworkLobbyManager : MonoBehaviour
    {
        public static NetworkLobbyManager Instance { get; private set; }

        private const string RelayJoinCodeKey = "RelayJoinCode";
        private const float HeartbeatInterval = 15f;

        private Lobby _currentLobby;
        private Coroutine _heartbeatCoroutine;
        private int _nextPlayerId = 2; // host is always player 1

        /// <summary>The join code shown to the host after lobby creation.</summary>
        public string LobbyJoinCode => _currentLobby?.LobbyCode;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Initializes Unity Services and signs in anonymously.
        /// Must be called before CreateLobbyAsync or JoinLobbyAsync.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (UnityServices.State == ServicesInitializationState.Initialized)
                return;

            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            UnityEngine.Debug.Log($"[NetworkLobbyManager] Signed in as: {AuthenticationService.Instance.PlayerId}");
        }

        /// <summary>
        /// Creates a lobby for up to <paramref name="maxPlayers"/>, allocates Relay,
        /// stores the Relay join code in lobby data, then starts NGO as host.
        /// </summary>
        public async Task CreateLobbyAsync(int maxPlayers = 5)
        {
            await InitializeAsync();

            string relayJoinCode = await RelayHandler.AllocateAsync(maxPlayers - 1);

            var options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new System.Collections.Generic.Dictionary<string, DataObject>
                {
                    {
                        RelayJoinCodeKey,
                        new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode)
                    }
                }
            };

            _currentLobby = await LobbyService.Instance.CreateLobbyAsync(
                "MasterBlaster",
                maxPlayers,
                options
            );

            UnityEngine.Debug.Log($"[NetworkLobbyManager] Lobby created. Lobby code: {_currentLobby.LobbyCode}");

            _heartbeatCoroutine = StartCoroutine(HeartbeatRoutine());

            _nextPlayerId = 2;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.StartHost();
        }

        /// <summary>
        /// Joins a lobby by its short code, retrieves the Relay join code from lobby data,
        /// connects to Relay, then starts NGO as client.
        /// </summary>
        public async Task JoinLobbyAsync(string lobbyCode)
        {
            await InitializeAsync();

            _currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);

            if (!_currentLobby.Data.TryGetValue(RelayJoinCodeKey, out var relayCodeEntry))
            {
                UnityEngine.Debug.LogError("[NetworkLobbyManager] Relay join code not found in lobby data.");
                return;
            }

            await RelayHandler.JoinAsync(relayCodeEntry.Value);

            UnityEngine.Debug.Log($"[NetworkLobbyManager] Joined lobby {lobbyCode}. Connecting via Relay...");

            NetworkManager.Singleton.StartClient();
        }

        /// <summary>Leaves the current lobby and shuts down NGO.</summary>
        public async void LeaveLobby()
        {
            if (_heartbeatCoroutine != null)
                StopCoroutine(_heartbeatCoroutine);

            if (_currentLobby != null)
            {
                try
                {
                    await LobbyService.Instance.DeleteLobbyAsync(_currentLobby.Id);
                }
                catch (LobbyServiceException e)
                {
                    UnityEngine.Debug.LogWarning($"[NetworkLobbyManager] Could not delete lobby: {e.Message}");
                }
                _currentLobby = null;
            }

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                if (NetworkManager.Singleton.IsListening)
                    NetworkManager.Singleton.Shutdown();
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            int playerId = _nextPlayerId++;
            var gm = Scenes.Arena.GameManager.Instance;
            if (gm != null)
                gm.AssignNetworkClient(clientId, playerId);
            UnityEngine.Debug.Log($"[NetworkLobbyManager] Client {clientId} assigned to player {playerId}.");
        }

        private IEnumerator HeartbeatRoutine()
        {
            while (_currentLobby != null)
            {
                yield return new WaitForSeconds(HeartbeatInterval);
                if (_currentLobby != null)
                {
                    LobbyService.Instance.SendHeartbeatPingAsync(_currentLobby.Id);
                    UnityEngine.Debug.Log("[NetworkLobbyManager] Lobby heartbeat sent.");
                }
            }
        }

        private void OnDestroy()
        {
            if (_heartbeatCoroutine != null)
                StopCoroutine(_heartbeatCoroutine);
        }
    }
}
