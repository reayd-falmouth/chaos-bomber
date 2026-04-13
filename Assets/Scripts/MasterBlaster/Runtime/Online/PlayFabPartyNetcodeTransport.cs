using System;
using PartyCSharpSDK;
using Unity.Netcode;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Online
{
    /// <summary>
    /// Routes Unity Netcode over PlayFab Party endpoints (data messages).
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayFabPartyNetcodeTransport : NetworkTransport
    {
        public const ulong FirstRemoteClientId = 1;
        public const ulong ServerClientIdValue = 0;

        public override ulong ServerClientId => ServerClientIdValue;

        bool _isServer;
        bool _clientConnectQueued;

        public override void Initialize(NetworkManager networkManager = null)
        {
        }

        public override bool StartServer()
        {
            _isServer = true;
            _clientConnectQueued = false;
            PlayFabPartyNetcodeSession.Clear();
            return true;
        }

        public override bool StartClient()
        {
            _isServer = false;
            _clientConnectQueued = false;
            PlayFabPartyNetcodeSession.Clear();
            return true;
        }

        void Update()
        {
            PlayFabPartySession.Pump();

            if (!_isServer && !_clientConnectQueued
                && PlayFabPartySession.LocalEndpoint != null
                && PlayFabPartySession.RemoteEndpoint != null)
            {
                PlayFabPartyNetcodeSession.EnqueueConnect(ServerClientId);
                _clientConnectQueued = true;
            }

            if (_isServer
                && PlayFabPartySession.LocalEndpoint != null
                && PlayFabPartySession.RemoteEndpoint != null
                && !_clientConnectQueued)
            {
                PlayFabPartyNetcodeSession.EnqueueConnect(FirstRemoteClientId);
                _clientConnectQueued = true;
            }
        }

        public override void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery delivery)
        {
            PARTY_ENDPOINT_HANDLE local = PlayFabPartySession.LocalEndpoint;
            PARTY_ENDPOINT_HANDLE remote = PlayFabPartySession.RemoteEndpoint;
            if (local == null || remote == null)
                return;

            var targets = new[] { remote };
            var options = MapDelivery(delivery);
            var queueCfg = new PARTY_SEND_MESSAGE_QUEUING_CONFIGURATION
            {
                Priority = 0,
                IdentityForCancelFilters = 0,
                TimeoutInMilliseconds = 0
            };

            byte[] buf = new byte[payload.Count];
            Array.Copy(payload.Array, payload.Offset, buf, 0, payload.Count);

            uint err = SDK.PartyEndpointSendMessage(local, targets, options, queueCfg, buf);
            if (PartyError.FAILED(err))
                UnityEngine.Debug.LogWarning($"[PlayFabPartyTransport] Send failed: 0x{err:X}");
        }

        static PARTY_SEND_MESSAGE_OPTIONS MapDelivery(NetworkDelivery delivery)
        {
            return delivery switch
            {
                NetworkDelivery.Unreliable => PARTY_SEND_MESSAGE_OPTIONS.PARTY_SEND_MESSAGE_OPTIONS_BEST_EFFORT_DELIVERY,
                _ => PARTY_SEND_MESSAGE_OPTIONS.PARTY_SEND_MESSAGE_OPTIONS_GUARANTEED_DELIVERY |
                     PARTY_SEND_MESSAGE_OPTIONS.PARTY_SEND_MESSAGE_OPTIONS_SEQUENTIAL_DELIVERY
            };
        }

        public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
        {
            receiveTime = Time.realtimeSinceStartup;
            payload = default;

            if (!PlayFabPartyNetcodeSession.TryDequeue(out NetworkEvent evt, out clientId, out byte[] data))
            {
                clientId = 0;
                return NetworkEvent.Nothing;
            }

            if (evt == NetworkEvent.Data && data != null)
                payload = new ArraySegment<byte>(data);

            return evt;
        }

        public override void DisconnectRemoteClient(ulong clientId)
        {
        }

        public override void DisconnectLocalClient()
        {
        }

        public override ulong GetCurrentRtt(ulong clientId) => 0;

        public override void Shutdown()
        {
            _isServer = false;
            _clientConnectQueued = false;
        }
    }
}
