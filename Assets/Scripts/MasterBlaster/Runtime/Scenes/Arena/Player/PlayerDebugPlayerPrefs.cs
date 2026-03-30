using HybridGame.MasterBlaster.Scripts.Scenes.Shop;
using UnityEngine;

// for ShopItemType enum

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Player
{
    public class PlayerPrefsDebugger : MonoBehaviour
    {
        [Tooltip("Which player to inspect (1-based index)")]
        public int playerId = 1;

        private void Start()
        {
            DumpPlayerPrefs(playerId);
        }

        public static void DumpPlayerPrefs(int playerId)
        {
            UnityEngine.Debug.Log($"--- Player {playerId} PlayerPrefs ---");

            // Coins
            int coins = PlayerPrefs.GetInt($"Player{playerId}_Coins", -999);
            UnityEngine.Debug.Log($"Player{playerId}_Coins = {coins}");

            // Loop through all ShopItemTypes
            foreach (ShopItemType type in System.Enum.GetValues(typeof(ShopItemType)))
            {
                string key = $"Player{playerId}_{type}";
                int val = PlayerPrefs.GetInt(key, -999);
                UnityEngine.Debug.Log($"{key} = {val}");
            }

            UnityEngine.Debug.Log($"--- End Player {playerId} PlayerPrefs ---");
        }
    }
}
