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

        [Test]
        public void ClearShopUpgradesPreserveCoinsAndWins_KeepsCoinsAndWins_ClearsShopTiers()
        {
            var go = new GameObject("SessionManager_ClearShop_Test");
            try
            {
                var session = go.AddComponent<SessionManager>();
                session.Initialize(playerCount: 2);

                session.AddCoins(1, 7);
                session.AddWin(1);
                session.SetUpgradeLevel(1, ShopItemType.ExtraBomb, 2);
                session.SetUpgradeLevel(1, ShopItemType.Superman, 1);
                session.SetUpgradeLevel(2, ShopItemType.Ghost, 1);

                session.ClearShopUpgradesPreserveCoinsAndWins();

                Assert.That(session.GetCoins(1), Is.EqualTo(7));
                Assert.That(session.GetWins(1), Is.EqualTo(1));
                Assert.That(session.GetUpgradeLevel(1, ShopItemType.ExtraBomb), Is.EqualTo(0));
                Assert.That(session.GetUpgradeLevel(1, ShopItemType.Superman), Is.EqualTo(0));
                Assert.That(session.GetUpgradeLevel(2, ShopItemType.Ghost), Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}

