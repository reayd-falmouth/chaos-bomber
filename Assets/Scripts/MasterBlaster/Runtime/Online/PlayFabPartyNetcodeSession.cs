using System;
using System.Collections.Generic;
using Unity.Netcode;

namespace HybridGame.MasterBlaster.Scripts.Online
{
    /// <summary>
    /// Queues Netcode transport events produced by PlayFab Party message receive and connection discovery.
    /// </summary>
    public static class PlayFabPartyNetcodeSession
    {
        struct Pending
        {
            public NetworkEvent Event;
            public ulong ClientId;
            public byte[] Data;
        }

        static readonly Queue<Pending> Queue = new();

        public static void EnqueueConnect(ulong clientId)
        {
            Queue.Enqueue(new Pending { Event = NetworkEvent.Connect, ClientId = clientId, Data = null });
        }

        public static void EnqueueIncoming(ulong fromClientId, byte[] data)
        {
            Queue.Enqueue(new Pending { Event = NetworkEvent.Data, ClientId = fromClientId, Data = data });
        }

        public static void EnqueueDisconnect(ulong clientId)
        {
            Queue.Enqueue(new Pending { Event = NetworkEvent.Disconnect, ClientId = clientId, Data = null });
        }

        public static bool TryDequeue(out NetworkEvent evt, out ulong clientId, out byte[] data)
        {
            if (Queue.Count == 0)
            {
                evt = default;
                clientId = 0;
                data = null;
                return false;
            }

            Pending p = Queue.Dequeue();
            evt = p.Event;
            clientId = p.ClientId;
            data = p.Data;
            return true;
        }

        public static void Clear()
        {
            Queue.Clear();
        }
    }
}
