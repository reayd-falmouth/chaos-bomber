using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

namespace HybridGame.MasterBlaster.Scripts.Online
{
    /// <summary>
    /// Static helpers for creating and joining Unity Relay allocations,
    /// and wiring the resulting transport data into UnityTransport.
    /// </summary>
    public static class RelayHandler
    {
        /// <summary>
        /// Ensures <paramref name="nm"/> has a <see cref="UnityTransport"/> and that
        /// <see cref="NetworkConfig.NetworkTransport"/> references it.
        /// </summary>
        public static void EnsureUnityTransportForNetworkManager(NetworkManager nm)
        {
            if (nm == null)
                return;

            var ut = nm.GetComponent<UnityTransport>();
            if (ut == null)
                ut = nm.gameObject.AddComponent<UnityTransport>();

            if (nm.NetworkConfig != null && nm.NetworkConfig.NetworkTransport == null)
                nm.NetworkConfig.NetworkTransport = ut;
        }

        /// <summary>
        /// Ensures the singleton <see cref="NetworkManager"/> has a <see cref="UnityTransport"/>
        /// and that <see cref="NetworkConfig.NetworkTransport"/> references it.
        /// Relay allocation and NGO startup require this; scenes often leave transport unset.
        /// </summary>
        public static void EnsureUnityTransportForSingleton()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null)
            {
                UnityEngine.Debug.LogWarning("[RelayHandler] NetworkManager.Singleton is null; cannot wire UnityTransport.");
                return;
            }

            EnsureUnityTransportForNetworkManager(nm);
        }

        /// <summary>
        /// Allocates a Relay server for <paramref name="maxConnections"/> clients (excluding host),
        /// configures UnityTransport as host, and returns the join code.
        /// </summary>
        public static async Task<string> AllocateAsync(int maxConnections)
        {
            EnsureUnityTransportForSingleton();
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            UnityEngine.Debug.Log($"[RelayHandler] Relay allocated. Join code: {joinCode}");
            return joinCode;
        }

        /// <summary>
        /// Joins an existing Relay allocation using <paramref name="joinCode"/>
        /// and configures UnityTransport as client.
        /// </summary>
        public static async Task JoinAsync(string joinCode)
        {
            EnsureUnityTransportForSingleton();
            JoinAllocation join = await RelayService.Instance.JoinAllocationAsync(joinCode);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetClientRelayData(
                join.RelayServer.IpV4,
                (ushort)join.RelayServer.Port,
                join.AllocationIdBytes,
                join.Key,
                join.ConnectionData,
                join.HostConnectionData
            );

            UnityEngine.Debug.Log($"[RelayHandler] Joined Relay with code: {joinCode}");
        }
    }
}
