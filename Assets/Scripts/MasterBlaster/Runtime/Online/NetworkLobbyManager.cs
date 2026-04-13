using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Online
{
    /// <summary>
    /// Legacy Unity Gaming Services lobby + Relay path (removed). Use <see cref="PlayFabLobbyManager"/> for online play.
    /// </summary>
    public class NetworkLobbyManager : MonoBehaviour
    {
        public static NetworkLobbyManager Instance { get; private set; }

        public string LobbyJoinCode => "";

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (TryGetComponent<NetworkManager>(out var nm) && nm.NetworkConfig != null && nm.NetworkConfig.NetworkTransport == null)
            {
                var ut = nm.GetComponent<UnityTransport>() ?? nm.gameObject.AddComponent<UnityTransport>();
                nm.NetworkConfig.NetworkTransport = ut;
            }
        }

        public Task InitializeAsync()
        {
            UnityEngine.Debug.LogWarning("[NetworkLobbyManager] Unity Services lobby path is disabled. Use PlayFabLobbyManager.");
            return Task.CompletedTask;
        }

        public Task CreateLobbyAsync(int maxPlayers = 5)
        {
            throw new InvalidOperationException("Unity Relay lobby path removed. Use PlayFabLobbyManager for online multiplayer.");
        }

        public Task JoinLobbyAsync(string lobbyCode)
        {
            throw new InvalidOperationException("Unity Relay lobby path removed. Use PlayFabLobbyManager for online multiplayer.");
        }

        public void LeaveLobby()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.Shutdown();
        }
    }
}
