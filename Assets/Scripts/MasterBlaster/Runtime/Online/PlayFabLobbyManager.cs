using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlayFab;
using PlayFab.MultiplayerModels;
using Unity.Netcode;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Online
{
    /// <summary>
    /// Replaces NetworkLobbyManager. Uses PlayFab Lobby for matchmaking and
    /// Unity Relay for the actual network transport (NGO host/client).
    ///
    /// Flow:
    ///   Host → CreateLobbyAsync() → allocates Relay → creates PlayFab Lobby (stores relay code) → StartHost()
    ///   Client → JoinLobbyAsync(connectionString) → fetches relay code from PlayFab Lobby → StartClient()
    /// </summary>
    public class PlayFabLobbyManager : MonoBehaviour
    {
        public static PlayFabLobbyManager Instance { get; private set; }

        const string RelayJoinCodeKey  = "RelayJoinCode";
        const float  HeartbeatInterval = 25f; // lobby expires after 60 s without an owner update

        string   _currentLobbyId;
        Coroutine _heartbeatCoroutine;

        /// <summary>The connection string shown to the host after lobby creation. Share this with other players.</summary>
        public string LobbyConnectionString { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (TryGetComponent<NetworkManager>(out var nm))
                RelayHandler.EnsureUnityTransportForNetworkManager(nm);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a PlayFab Lobby, allocates Unity Relay, stores the relay join code
        /// inside the lobby, then starts NGO as host.
        /// </summary>
        public async Task CreateLobbyAsync(int maxPlayers = 5)
        {
            await WaitForLoginAsync();

            // Allocate Unity Relay — host gets a join code for clients.
            string relayJoinCode = await RelayHandler.AllocateAsync(maxPlayers - 1);

            var tcs = new TaskCompletionSource<bool>();

            PlayFabMultiplayerAPI.CreateLobby(
                new CreateLobbyRequest
                {
                    MaxPlayers = (uint)maxPlayers,
                    Owner      = GetEntityKey(),
                    LobbyData  = new Dictionary<string, string>
                    {
                        { RelayJoinCodeKey, relayJoinCode }
                    },
                    AccessPolicy = AccessPolicy.Public
                },
                result =>
                {
                    _currentLobbyId      = result.LobbyId;
                    LobbyConnectionString = result.ConnectionString;
                    UnityEngine.Debug.Log($"[PlayFabLobby] Created. ID={_currentLobbyId}");
                    tcs.SetResult(true);
                },
                error =>
                {
                    UnityEngine.Debug.LogError($"[PlayFabLobby] CreateLobby failed: {error.GenerateErrorReport()}");
                    tcs.SetException(new Exception(error.GenerateErrorReport()));
                }
            );

            await tcs.Task;

            _heartbeatCoroutine = StartCoroutine(HeartbeatRoutine());
            NetworkManager.Singleton.StartHost();
        }

        /// <summary>
        /// Joins an existing PlayFab Lobby by its connection string, retrieves the
        /// Unity Relay join code stored in lobby data, then starts NGO as client.
        /// </summary>
        public async Task JoinLobbyAsync(string connectionString)
        {
            await WaitForLoginAsync();

            // --- Step 1: join the lobby ---
            var joinTcs = new TaskCompletionSource<bool>();

            PlayFabMultiplayerAPI.JoinLobby(
                new JoinLobbyRequest
                {
                    ConnectionString = connectionString,
                    MemberEntity     = GetEntityKey()
                },
                result =>
                {
                    _currentLobbyId       = result.LobbyId;
                    LobbyConnectionString  = connectionString;
                    UnityEngine.Debug.Log($"[PlayFabLobby] Joined lobby {result.LobbyId}");
                    joinTcs.SetResult(true);
                },
                error =>
                {
                    UnityEngine.Debug.LogError($"[PlayFabLobby] JoinLobby failed: {error.GenerateErrorReport()}");
                    joinTcs.SetException(new Exception(error.GenerateErrorReport()));
                }
            );

            await joinTcs.Task;

            // --- Step 2: read the relay join code from lobby data ---
            var getTcs        = new TaskCompletionSource<string>();

            PlayFabMultiplayerAPI.GetLobby(
                new GetLobbyRequest { LobbyId = _currentLobbyId },
                result =>
                {
                    if (result.Lobby.LobbyData != null &&
                        result.Lobby.LobbyData.TryGetValue(RelayJoinCodeKey, out string code))
                    {
                        getTcs.SetResult(code);
                    }
                    else
                    {
                        getTcs.SetException(new Exception("[PlayFabLobby] Relay code missing from lobby data."));
                    }
                },
                error => getTcs.SetException(new Exception(error.GenerateErrorReport()))
            );

            string relayJoinCode = await getTcs.Task;

            // --- Step 3: join Unity Relay and start NGO client ---
            await RelayHandler.JoinAsync(relayJoinCode);
            NetworkManager.Singleton.StartClient();
        }

        /// <summary>Deletes the lobby and shuts down NGO.</summary>
        public void LeaveLobby()
        {
            if (_heartbeatCoroutine != null)
                StopCoroutine(_heartbeatCoroutine);

            if (!string.IsNullOrEmpty(_currentLobbyId))
            {
                PlayFabMultiplayerAPI.DeleteLobby(
                    new DeleteLobbyRequest { LobbyId = _currentLobbyId },
                    _ => UnityEngine.Debug.Log("[PlayFabLobby] Lobby deleted."),
                    error => UnityEngine.Debug.LogWarning($"[PlayFabLobby] Delete failed: {error.GenerateErrorReport()}")
                );
                _currentLobbyId       = null;
                LobbyConnectionString  = null;
            }

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.Shutdown();
        }

        // ── Internals ─────────────────────────────────────────────────────────────

        PlayFab.MultiplayerModels.EntityKey GetEntityKey()
        {
            var auth = PlayFabAuthManager.Instance;
            return new PlayFab.MultiplayerModels.EntityKey
            {
                Id   = auth.EntityKey.Id,
                Type = auth.EntityKey.Type
            };
        }

        static async Task WaitForLoginAsync()
        {
            while (!PlayFabAuthManager.Instance || !PlayFabAuthManager.Instance.IsLoggedIn)
                await Task.Delay(100);
        }

        IEnumerator HeartbeatRoutine()
        {
            while (!string.IsNullOrEmpty(_currentLobbyId))
            {
                yield return new WaitForSeconds(HeartbeatInterval);
                if (string.IsNullOrEmpty(_currentLobbyId)) yield break;

                // Touching LobbyData resets the owner-inactivity timer.
                PlayFabMultiplayerAPI.UpdateLobby(
                    new UpdateLobbyRequest
                    {
                        LobbyId   = _currentLobbyId,
                        LobbyData = new Dictionary<string, string>()
                    },
                    _ => { },
                    error => UnityEngine.Debug.LogWarning($"[PlayFabLobby] Heartbeat failed: {error.GenerateErrorReport()}")
                );
            }
        }

        void OnDestroy()
        {
            if (_heartbeatCoroutine != null)
                StopCoroutine(_heartbeatCoroutine);
        }
    }
}
