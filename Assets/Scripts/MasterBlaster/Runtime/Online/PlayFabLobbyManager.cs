using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using PartyCSharpSDK;
using PlayFab;
using PlayFab.MultiplayerModels;
using Unity.Netcode;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Online
{
    /// <summary>
    /// PlayFab Lobby for discovery + PlayFab Party for NGO packets (no Unity Relay).
    /// Host stores serialized Party network descriptor in lobby data; client joins Party then Netcode.
    /// </summary>
    public class PlayFabLobbyManager : MonoBehaviour
    {
        public static PlayFabLobbyManager Instance { get; private set; }

        const string PartyNetworkDescriptorKey = "PartyNetworkDescriptor";
        const float HeartbeatInterval = 25f;

        string _currentLobbyId;
        Coroutine _heartbeatCoroutine;

        /// <summary>The connection string shown to the host after lobby creation. Share this with other players.</summary>
        public string LobbyConnectionString { get; private set; }

        int _nextPlayerId = 2;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (TryGetComponent<NetworkManager>(out var nm))
                PartyNetworkHelper.EnsurePartyTransportForNetworkManager(nm);
        }

        public Task CreateLobbyAsync(int maxPlayers = 5)
        {
            var tcs = new TaskCompletionSource<bool>();
            StartCoroutine(CoCreateLobby(maxPlayers, tcs));
            return tcs.Task;
        }

        IEnumerator CoCreateLobby(int maxPlayers, TaskCompletionSource<bool> tcs)
        {
            yield return WaitForLogin();

            var auth = PlayFabAuthManager.Instance;
            PartyNetworkHelper.EnsurePartyTransportForNetworkManager(NetworkManager.Singleton);
            PlayFabPartySession.IsHostRole = true;

            uint err = PlayFabPartySession.Initialize(
                PlayFabSettings.staticSettings.TitleId,
                auth.EntityKey.Id,
                auth.EntityTokenString);

            if (PartyError.FAILED(err))
            {
                tcs.SetException(new Exception($"[PlayFabParty] Initialize failed: 0x{err:X}"));
                yield break;
            }

            err = PlayFabPartySession.HostBeginPartyNetwork(maxPlayers);
            if (PartyError.FAILED(err))
            {
                tcs.SetException(new Exception($"[PlayFabParty] HostBeginPartyNetwork failed: 0x{err:X}"));
                yield break;
            }

            while (PlayFabPartySession.LocalEndpoint == null)
                yield return null;

            string descriptor = PlayFabPartySession.SerializedNetworkDescriptor;
            if (string.IsNullOrEmpty(descriptor))
            {
                tcs.SetException(new Exception("[PlayFabParty] Missing serialized network descriptor."));
                yield break;
            }

            var createTcs = new TaskCompletionSource<bool>();
            PlayFabMultiplayerAPI.CreateLobby(
                new CreateLobbyRequest
                {
                    MaxPlayers = (uint)maxPlayers,
                    Owner      = GetEntityKey(),
                    LobbyData  = new Dictionary<string, string> { { PartyNetworkDescriptorKey, descriptor } },
                    AccessPolicy = AccessPolicy.Public
                },
                lobbyResult =>
                {
                    _currentLobbyId = lobbyResult.LobbyId;
                    LobbyConnectionString = lobbyResult.ConnectionString;
                    createTcs.SetResult(true);
                },
                e => createTcs.SetException(new Exception(e.GenerateErrorReport())));

            while (!createTcs.Task.IsCompleted)
                yield return null;
            if (createTcs.Task.IsFaulted)
            {
                tcs.SetException(createTcs.Task.Exception?.InnerException ?? createTcs.Task.Exception ?? new Exception("CreateLobby failed."));
                yield break;
            }

            _heartbeatCoroutine = StartCoroutine(HeartbeatRoutine());
            _nextPlayerId = 2;

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.StartHost();
            tcs.SetResult(true);
        }

        public Task JoinLobbyAsync(string connectionString)
        {
            var tcs = new TaskCompletionSource<bool>();
            StartCoroutine(CoJoinLobby(connectionString, tcs));
            return tcs.Task;
        }

        IEnumerator CoJoinLobby(string connectionString, TaskCompletionSource<bool> tcs)
        {
            yield return WaitForLogin();

            var auth = PlayFabAuthManager.Instance;
            PartyNetworkHelper.EnsurePartyTransportForNetworkManager(NetworkManager.Singleton);
            PlayFabPartySession.IsHostRole = false;

            uint err = PlayFabPartySession.Initialize(
                PlayFabSettings.staticSettings.TitleId,
                auth.EntityKey.Id,
                auth.EntityTokenString);

            if (PartyError.FAILED(err))
            {
                tcs.SetException(new Exception($"[PlayFabParty] Initialize failed: 0x{err:X}"));
                yield break;
            }

            var joinTcs = new TaskCompletionSource<bool>();
            PlayFabMultiplayerAPI.JoinLobby(
                new JoinLobbyRequest
                {
                    ConnectionString = connectionString,
                    MemberEntity     = GetEntityKey()
                },
                r =>
                {
                    _currentLobbyId = r.LobbyId;
                    LobbyConnectionString = connectionString;
                    joinTcs.SetResult(true);
                },
                e => joinTcs.SetException(new Exception(e.GenerateErrorReport())));

            while (!joinTcs.Task.IsCompleted)
                yield return null;
            if (joinTcs.Task.IsFaulted)
            {
                tcs.SetException(joinTcs.Task.Exception?.InnerException ?? joinTcs.Task.Exception ?? new Exception("JoinLobby failed."));
                yield break;
            }

            var getTcs = new TaskCompletionSource<string>();
            PlayFabMultiplayerAPI.GetLobby(
                new GetLobbyRequest { LobbyId = _currentLobbyId },
                r =>
                {
                    if (r.Lobby.LobbyData != null &&
                        r.Lobby.LobbyData.TryGetValue(PartyNetworkDescriptorKey, out string d) &&
                        !string.IsNullOrEmpty(d))
                        getTcs.SetResult(d);
                    else
                        getTcs.SetException(new Exception("[PlayFabLobby] Party network descriptor missing from lobby."));
                },
                e => getTcs.SetException(new Exception(e.GenerateErrorReport())));

            Task<string> getTask = getTcs.Task;
            while (!getTask.IsCompleted)
                yield return null;

            string partyDesc;
            try { partyDesc = getTask.Result; }
            catch (Exception ex) { tcs.SetException(ex); yield break; }

            err = PlayFabPartySession.ClientBeginPartyNetwork(partyDesc);
            if (PartyError.FAILED(err))
            {
                tcs.SetException(new Exception($"[PlayFabParty] ClientBeginPartyNetwork failed: 0x{err:X}"));
                yield break;
            }

            while (PlayFabPartySession.LocalEndpoint == null || PlayFabPartySession.RemoteEndpoint == null)
                yield return null;

            NetworkManager.Singleton.StartClient();
            tcs.SetResult(true);
        }

        public void LeaveLobby()
        {
            if (_heartbeatCoroutine != null)
                StopCoroutine(_heartbeatCoroutine);

            if (!string.IsNullOrEmpty(_currentLobbyId))
            {
                PlayFabMultiplayerAPI.DeleteLobby(
                    new DeleteLobbyRequest { LobbyId = _currentLobbyId },
                    _ => Debug.Log("[PlayFabLobby] Lobby deleted."),
                    e => Debug.LogWarning($"[PlayFabLobby] Delete failed: {e.GenerateErrorReport()}"));
                _currentLobbyId = null;
                LobbyConnectionString = null;
            }

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                if (NetworkManager.Singleton.IsListening)
                    NetworkManager.Singleton.Shutdown();
            }

            PlayFabPartySession.Shutdown();
            PlayFabPartyNetcodeSession.Clear();
        }

        void OnClientConnected(ulong clientId)
        {
            int playerId = _nextPlayerId++;
            var gm = Scenes.Arena.GameManager.Instance;
            if (gm != null)
                gm.AssignNetworkClient(clientId, playerId);
            Debug.Log($"[PlayFabLobby] Client {clientId} assigned to player {playerId}.");
        }

        PlayFab.MultiplayerModels.EntityKey GetEntityKey()
        {
            var auth = PlayFabAuthManager.Instance;
            return new PlayFab.MultiplayerModels.EntityKey { Id = auth.EntityKey.Id, Type = auth.EntityKey.Type };
        }

        IEnumerator WaitForLogin()
        {
            while (!PlayFabAuthManager.Instance || !PlayFabAuthManager.Instance.IsLoggedIn)
                yield return null;
        }

        IEnumerator HeartbeatRoutine()
        {
            while (!string.IsNullOrEmpty(_currentLobbyId))
            {
                yield return new WaitForSeconds(HeartbeatInterval);
                if (string.IsNullOrEmpty(_currentLobbyId)) yield break;

                PlayFabMultiplayerAPI.UpdateLobby(
                    new UpdateLobbyRequest
                    {
                        LobbyId   = _currentLobbyId,
                        LobbyData = new Dictionary<string, string>()
                    },
                    _ => { },
                    e => Debug.LogWarning($"[PlayFabLobby] Heartbeat failed: {e.GenerateErrorReport()}"));
            }
        }

        void OnDestroy()
        {
            if (_heartbeatCoroutine != null)
                StopCoroutine(_heartbeatCoroutine);
        }
    }
}
