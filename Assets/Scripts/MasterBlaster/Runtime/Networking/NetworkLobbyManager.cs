using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Networking
{
    /// <summary>
    /// Compile stub for local NGO testing without PlayFab Party. Uses <see cref="UnityTransport"/> on loopback.
    /// </summary>
    public class NetworkLobbyManager : MonoBehaviour
    {
        public static NetworkLobbyManager Instance { get; private set; }

        public string LobbyJoinCode { get; private set; } = "";

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public Task InitializeAsync()
        {
            Debug.Log("[Networking.NetworkLobbyManager] Stub — local UnityTransport only.");
            return Task.CompletedTask;
        }

        public async Task CreateLobbyAsync(int maxPlayers = 4)
        {
            await InitializeAsync();
            var nm = NetworkManager.Singleton;
            if (nm == null) return;
            var ut = nm.GetComponent<UnityTransport>() ?? nm.gameObject.AddComponent<UnityTransport>();
            if (nm.NetworkConfig != null)
                nm.NetworkConfig.NetworkTransport = ut;
            nm.StartHost();
            Debug.Log("[Networking.NetworkLobbyManager] Stub — started local host (UTP, no Party).");
        }

        public Task JoinLobbyAsync(string lobbyCode)
        {
            var nm = NetworkManager.Singleton;
            if (nm == null) return Task.CompletedTask;
            var ut = nm.GetComponent<UnityTransport>() ?? nm.gameObject.AddComponent<UnityTransport>();
            if (nm.NetworkConfig != null)
                nm.NetworkConfig.NetworkTransport = ut;
            nm.StartClient();
            Debug.Log($"[Networking.NetworkLobbyManager] Stub — started local client (code ignored: {lobbyCode}).");
            return Task.CompletedTask;
        }

        public void LeaveLobby()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.Shutdown();
        }
    }
}
