using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Online
{
    /// <summary>
    /// Wires <see cref="PlayFabPartyNetcodeTransport"/> on the same GameObject as <see cref="NetworkManager"/>
    /// and removes <see cref="UnityTransport"/> so NGO uses PlayFab Party instead of Unity Relay.
    /// </summary>
    public static class PartyNetworkHelper
    {
        public static void EnsurePartyTransportForNetworkManager(NetworkManager nm)
        {
            if (nm == null)
                return;

            var ut = nm.GetComponent<UnityTransport>();
            if (ut != null)
                Object.Destroy(ut);

            var partyTransport = nm.GetComponent<PlayFabPartyNetcodeTransport>();
            if (partyTransport == null)
                partyTransport = nm.gameObject.AddComponent<PlayFabPartyNetcodeTransport>();

            if (nm.NetworkConfig != null)
                nm.NetworkConfig.NetworkTransport = partyTransport;
        }
    }
}
