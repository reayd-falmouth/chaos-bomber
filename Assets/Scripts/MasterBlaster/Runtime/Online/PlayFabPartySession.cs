using System;
using System.Collections.Generic;
using PartyCSharpSDK;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Online
{
    /// <summary>
    /// PlayFab Party session: one Party network and local endpoint. Pump from <see cref="PlayFabPartyNetcodeTransport.Update"/>.
    /// </summary>
    public static class PlayFabPartySession
    {
        public static PARTY_HANDLE PartyHandle { get; private set; }
        public static PARTY_LOCAL_USER_HANDLE LocalUser { get; private set; }
        public static PARTY_NETWORK_HANDLE Network { get; private set; }
        public static PARTY_ENDPOINT_HANDLE LocalEndpoint { get; private set; }
        public static PARTY_ENDPOINT_HANDLE RemoteEndpoint { get; private set; }

        /// <summary>Serialized network descriptor for PlayFab lobby data (host).</summary>
        public static string SerializedNetworkDescriptor { get; private set; }

        /// <summary>Set true before host Party calls, false for client.</summary>
        public static bool IsHostRole { get; set; }

        static PARTY_REGION[] _regions;

        public static uint Initialize(string titleId, string entityId, string entityToken)
        {
            Shutdown();

            uint err = SDK.PartyInitialize(titleId, out PARTY_HANDLE party);
            if (PartyError.FAILED(err))
                return err;

            PartyHandle = party;

            err = SDK.PartyCreateLocalUser(PartyHandle, entityId, entityToken, out PARTY_LOCAL_USER_HANDLE localUser);
            if (PartyError.FAILED(err))
                return err;

            LocalUser = localUser;

            err = SDK.PartyGetRegions(PartyHandle, out _regions);
            if (PartyError.FAILED(err) || _regions == null || _regions.Length == 0)
                _regions = Array.Empty<PARTY_REGION>();

            return PartyError.Success;
        }

        /// <summary>Host: creates Party network, serializes descriptor, begins connect (completed via Pump).</summary>
        public static uint HostBeginPartyNetwork(int maxPlayers)
        {
            SerializedNetworkDescriptor = null;
            RemoteEndpoint = null;
            LocalEndpoint = null;
            Network = null;

            var netCfg = new PARTY_NETWORK_CONFIGURATION
            {
                MaxUserCount               = (uint)Mathf.Max(2, maxPlayers),
                MaxDeviceCount             = (uint)Mathf.Max(2, maxPlayers),
                MaxUsersPerDeviceCount     = 1,
                MaxDevicesPerUserCount     = 4,
                MaxEndpointsPerDeviceCount = 4,
                DirectPeerConnectivityOptions =
                    PARTY_DIRECT_PEER_CONNECTIVITY_OPTIONS.PARTY_DIRECT_PEER_CONNECTIVITY_OPTIONS_ANY_PLATFORM_TYPE |
                    PARTY_DIRECT_PEER_CONNECTIVITY_OPTIONS.PARTY_DIRECT_PEER_CONNECTIVITY_OPTIONS_ANY_ENTITY_LOGIN_PROVIDER
            };

            var invite = new PARTY_INVITATION_CONFIGURATION
            {
                Identifier   = "HybridGame",
                Revocability = PARTY_INVITATION_REVOCABILITY.PARTY_INVITATION_REVOCABILITY_ANYONE,
                EntityIds    = Array.Empty<string>()
            };

            uint err = SDK.PartyCreateNewNetwork(
                PartyHandle,
                LocalUser,
                netCfg,
                _regions,
                invite,
                null,
                out PARTY_NETWORK_DESCRIPTOR descriptor,
                out _);

            if (PartyError.FAILED(err))
                return err;

            err = SDK.PartySerializeNetworkDescriptor(descriptor, out string serialized);
            if (PartyError.FAILED(err))
                return err;

            SerializedNetworkDescriptor = serialized;

            err = SDK.PartyConnectToNetwork(PartyHandle, descriptor, null, out _);
            return err;
        }

        /// <summary>Client: deserialize lobby string and connect (endpoint creation via Pump).</summary>
        public static uint ClientBeginPartyNetwork(string serializedDescriptor)
        {
            RemoteEndpoint = null;
            LocalEndpoint = null;
            Network = null;
            SerializedNetworkDescriptor = null;

            uint err = SDK.PartyDeserializeNetworkDescriptor(serializedDescriptor, out PARTY_NETWORK_DESCRIPTOR descriptor);
            if (PartyError.FAILED(err))
                return err;

            return SDK.PartyConnectToNetwork(PartyHandle, descriptor, null, out _);
        }

        public static void Pump()
        {
            if (PartyHandle == null)
                return;

            uint err = SDK.PartyStartProcessingStateChanges(PartyHandle, out List<PARTY_STATE_CHANGE> changes);
            if (PartyError.FAILED(err) || changes == null || changes.Count == 0)
                return;

            for (int i = 0; i < changes.Count; i++)
                HandleStateChange(changes[i]);

            SDK.PartyFinishProcessingStateChanges(PartyHandle, changes);
        }

        static void HandleStateChange(PARTY_STATE_CHANGE change)
        {
            switch (change.StateChangeType)
            {
                case PARTY_STATE_CHANGE_TYPE.PARTY_STATE_CHANGE_TYPE_CONNECT_TO_NETWORK_COMPLETED:
                    OnConnectToNetworkCompleted((PARTY_CONNECT_TO_NETWORK_COMPLETED_STATE_CHANGE)change);
                    break;
                case PARTY_STATE_CHANGE_TYPE.PARTY_STATE_CHANGE_TYPE_CREATE_ENDPOINT_COMPLETED:
                    OnCreateEndpointCompleted((PARTY_CREATE_ENDPOINT_COMPLETED_STATE_CHANGE)change);
                    break;
                case PARTY_STATE_CHANGE_TYPE.PARTY_STATE_CHANGE_TYPE_ENDPOINT_CREATED:
                    OnEndpointCreated((PARTY_ENDPOINT_CREATED_STATE_CHANGE)change);
                    break;
                case PARTY_STATE_CHANGE_TYPE.PARTY_STATE_CHANGE_TYPE_ENDPOINT_MESSAGE_RECEIVED:
                    OnEndpointMessageReceived((PARTY_ENDPOINT_MESSAGE_RECEIVED_STATE_CHANGE)change);
                    break;
            }
        }

        static void OnConnectToNetworkCompleted(PARTY_CONNECT_TO_NETWORK_COMPLETED_STATE_CHANGE e)
        {
            if (e.result != PARTY_STATE_CHANGE_RESULT.PARTY_STATE_CHANGE_RESULT_SUCCEEDED)
                return;

            Network = e.network;
            SDK.PartyNetworkCreateEndpoint(Network, LocalUser, null, null, out _);
        }

        static void OnCreateEndpointCompleted(PARTY_CREATE_ENDPOINT_COMPLETED_STATE_CHANGE e)
        {
            if (e.result != PARTY_STATE_CHANGE_RESULT.PARTY_STATE_CHANGE_RESULT_SUCCEEDED)
                return;

            LocalEndpoint = e.localEndpoint;
        }

        static void OnEndpointCreated(PARTY_ENDPOINT_CREATED_STATE_CHANGE e)
        {
            if (LocalEndpoint == null)
                return;

            if (!ReferenceEquals(e.endpoint, LocalEndpoint) && RemoteEndpoint == null)
                RemoteEndpoint = e.endpoint;
        }

        static void OnEndpointMessageReceived(PARTY_ENDPOINT_MESSAGE_RECEIVED_STATE_CHANGE e)
        {
            if (e.messageSize == 0 || e.messageBuffer == IntPtr.Zero)
                return;

            byte[] copy = new byte[e.messageSize];
            System.Runtime.InteropServices.Marshal.Copy(e.messageBuffer, copy, 0, (int)e.messageSize);

            bool fromRemote = RemoteEndpoint != null && e.senderEndpoint != null &&
                              !ReferenceEquals(e.senderEndpoint, LocalEndpoint);

            if (!fromRemote)
                return;

            ulong netcodeId = IsHostRole
                ? PlayFabPartyNetcodeTransport.FirstRemoteClientId
                : PlayFabPartyNetcodeTransport.ServerClientIdValue;

            PlayFabPartyNetcodeSession.EnqueueIncoming(netcodeId, copy);
        }

        public static void Shutdown()
        {
            if (LocalEndpoint != null && Network != null)
            {
                SDK.PartyNetworkDestroyEndpoint(Network, LocalEndpoint, null);
                LocalEndpoint = null;
            }

            RemoteEndpoint = null;
            SerializedNetworkDescriptor = null;

            if (Network != null)
            {
                SDK.PartyNetworkLeaveNetwork(Network, null);
                Network = null;
            }

            if (LocalUser != null && PartyHandle != null)
            {
                SDK.PartyDestroyLocalUser(PartyHandle, LocalUser, null);
                LocalUser = null;
            }

            if (PartyHandle != null)
            {
                SDK.PartyCleanup(PartyHandle);
                PartyHandle = null;
            }
        }
    }
}
