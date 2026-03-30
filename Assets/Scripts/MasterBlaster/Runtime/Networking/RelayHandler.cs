using System.Threading.Tasks;

namespace HybridGame.MasterBlaster.Scripts.Networking
{
    /// <summary>
    /// Unity Relay allocation helpers (Phase 6).
    ///
    /// TODO Phase 6: Implement using com.unity.services.multiplayer 2.x or install the separate
    /// com.unity.services.relay package and port MasterBlaster's RelayHandler directly.
    ///
    /// Stub compiles cleanly so Phase 1-5 can proceed.
    /// </summary>
    public static class RelayHandler
    {
        /// <summary>TODO Phase 6: Allocate Relay and return join code.</summary>
        public static Task<string> AllocateAsync(int maxConnections)
        {
            UnityEngine.Debug.Log("[RelayHandler] Stub — Relay not implemented (Phase 6).");
            return Task.FromResult("STUB-CODE");
        }

        /// <summary>TODO Phase 6: Join Relay allocation by join code.</summary>
        public static Task JoinAsync(string joinCode)
        {
            UnityEngine.Debug.Log($"[RelayHandler] Stub — Relay join not implemented (joinCode={joinCode}).");
            return Task.CompletedTask;
        }
    }
}
