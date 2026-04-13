using System;
using System.Threading.Tasks;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Online
{
    /// <summary>
    /// Placeholder for PlayFab Matchmaking queues (<c>CreateMatchmakingTicket</c> / <c>GetMatchmakingTicket</c>).
    /// Wire a dashboard queue and call from UI when you want skill-based or pool matchmaking instead of invite lobbies.
    /// </summary>
    public static class PlayFabMatchmaking
    {
        /// <summary>Queue name must match PlayFab Game Manager → Matchmaking → Queues.</summary>
        public const string DefaultQueueName = "DefaultQueue";

        /// <summary>
        /// Future: <c>PlayFabMultiplayerAPI.CreateMatchmakingTicket</c>, poll until Matched, then have elected host
        /// call <see cref="PlayFabPartySession.HostBeginPartyNetwork"/> and publish the Party descriptor to lobby data.
        /// </summary>
        public static Task StartQueueSearchAsync(string queueName = null)
        {
            UnityEngine.Debug.LogWarning("[PlayFabMatchmaking] Not implemented — configure a Matchmaking Queue in PlayFab and implement CreateMatchmakingTicket here.");
            return Task.FromException(new NotImplementedException("PlayFab Matchmaking ticket flow not implemented."));
        }
    }
}
