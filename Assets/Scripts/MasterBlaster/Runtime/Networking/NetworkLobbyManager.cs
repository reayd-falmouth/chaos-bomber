using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Networking
{
    /// <summary>
    /// Lobby + Relay manager for online multiplayer (Phase 6).
    ///
    /// TODO Phase 6: Implement using either:
    ///   a) com.unity.services.multiplayer 2.x unified API (ISession, ISessionManager) — matches
    ///      the currently installed package (com.unity.services.multiplayer 2.1.3).
    ///   b) Install separate com.unity.services.lobby + com.unity.services.relay packages
    ///      (older API using Unity.Services.Lobbies / Unity.Services.Relay namespaces, same as
    ///      MasterBlaster's NetworkLobbyManager — port that file directly once packages are added).
    ///
    /// Until then, the stub below lets the project compile so Phase 1-5 (single-player) can be tested.
    /// </summary>
    public class NetworkLobbyManager : MonoBehaviour
    {
        public static NetworkLobbyManager Instance { get; private set; }

        public string LobbyJoinCode { get; private set; } = "";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>TODO Phase 6: Initialize Unity Services and sign in anonymously.</summary>
        public Task InitializeAsync()
        {
            UnityEngine.Debug.Log("[NetworkLobbyManager] Stub — Phase 6 not yet implemented.");
            return Task.CompletedTask;
        }

        /// <summary>TODO Phase 6: Create lobby + Relay allocation and start NGO as host.</summary>
        public async Task CreateLobbyAsync(int maxPlayers = 4)
        {
            await InitializeAsync();
            // Must use full type name: Networking.RelayHandler (stub) shadows Online.RelayHandler in this folder.
            HybridGame.MasterBlaster.Scripts.Online.RelayHandler.EnsureUnityTransportForSingleton();
            NetworkManager.Singleton?.StartHost();
            UnityEngine.Debug.Log("[NetworkLobbyManager] Stub — started local host (no Relay).");
        }

        /// <summary>TODO Phase 6: Join lobby by code and start NGO as client.</summary>
        public Task JoinLobbyAsync(string lobbyCode)
        {
            HybridGame.MasterBlaster.Scripts.Online.RelayHandler.EnsureUnityTransportForSingleton();
            NetworkManager.Singleton?.StartClient();
            UnityEngine.Debug.Log($"[NetworkLobbyManager] Stub — started local client (code ignored: {lobbyCode}).");
            return Task.CompletedTask;
        }

        /// <summary>TODO Phase 6: Leave lobby and shut down NGO.</summary>
        public void LeaveLobby()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.Shutdown();
        }
    }
}
