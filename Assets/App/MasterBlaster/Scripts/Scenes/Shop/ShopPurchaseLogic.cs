namespace HybridGame.MasterBlaster.Scripts.Scenes.Shop
{
    /// <summary>
    /// Pure logic for shop purchases: stackable vs toggle upgrades and affordability.
    /// Extracted for unit testing without Unity/PlayerPrefs.
    /// </summary>
    public static class ShopPurchaseLogic
    {
        /// <summary>
        /// Returns the new upgrade level after one purchase of the given type.
        /// Stackable items increment; toggle items become 1; Exit unchanged.
        /// </summary>
        public static int GetNewLevelAfterPurchase(ShopItemType type, int currentLevel)
        {
            switch (type)
            {
                case ShopItemType.ExtraBomb:
                case ShopItemType.PowerUp:
                case ShopItemType.SpeedUp:
                    return currentLevel + 1;

                case ShopItemType.Superman:
                case ShopItemType.Ghost:
                case ShopItemType.Protection:
                case ShopItemType.Controller:
                case ShopItemType.Timebomb:
                    return 1;

                case ShopItemType.Exit:
                default:
                    return currentLevel;
            }
        }

        /// <summary>
        /// Returns true if the player can afford the cost.
        /// </summary>
        public static bool CanAfford(int coins, int cost)
        {
            return coins >= cost;
        }
    }
}
