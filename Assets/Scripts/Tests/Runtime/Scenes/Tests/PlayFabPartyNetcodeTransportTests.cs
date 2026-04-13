using HybridGame.MasterBlaster.Scripts.Online;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Runtime
{
    public class PlayFabPartyNetcodeTransportTests
    {
        [Test]
        public void ServerClientId_IsZero()
        {
            var go = new GameObject("TransportTest");
            var tr = go.AddComponent<PlayFabPartyNetcodeTransport>();
            Assert.AreEqual(0UL, tr.ServerClientId);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void FirstRemoteClientId_IsOne()
        {
            Assert.AreEqual(1UL, PlayFabPartyNetcodeTransport.FirstRemoteClientId);
        }
    }
}
