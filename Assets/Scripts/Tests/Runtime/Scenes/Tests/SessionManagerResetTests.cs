using HybridGame.MasterBlaster.Scripts.Core;
using HybridGame.MasterBlaster.Scripts.Scenes.Shop;
using NUnit.Framework;
using UnityEngine;

namespace HybridGame.MasterBlaster.Tests
{
    public class SessionManagerResetTests
    {
        [Test]
        public void ResetSession_ClearsSessionOnlyState()
        {
            var go = new GameObject("SessionManager_ResetSession_Test");
            try
            {
                var session = go.AddComponent<SessionManager>();
                session.Initialize(playerCount: 2);

                session.AddCoins(1, 10);
                session.AddWin(2);
                session.SetUpgradeLevel(1, ShopItemType.PowerUp, 3);
                session.SetMatchWinner(2, "P2");
                session.AssignNetworkClient(clientId: 123ul, playerId: 2);

                session.ResetSession();

                Assert.That(session.PlayerCoins.Count, Is.EqualTo(0));
                Assert.That(session.PlayerWins.Count, Is.EqualTo(0));
                Assert.That(session.PlayerUpgrades.Count, Is.EqualTo(0));
                Assert.That(session.MatchWinnerPlayerId, Is.EqualTo(0));
                Assert.That(session.MatchWinnerName, Is.Null);
                Assert.That(session.GetPlayerIdForClient(123ul), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}

