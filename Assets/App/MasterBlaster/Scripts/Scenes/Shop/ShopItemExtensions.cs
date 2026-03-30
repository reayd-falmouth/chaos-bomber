namespace HybridGame.MasterBlaster.Scripts.Scenes.Shop
{
    public static class ShopItemTypeExtensions
    {
        public static string ToDisplayName(this ShopItemType type)
        {
            switch (type)
            {
                case ShopItemType.ExtraBomb:
                    return "EXTRABOMB";
                case ShopItemType.PowerUp:
                    return "POWERUP";
                case ShopItemType.Superman:
                    return "SUPERMAN";
                case ShopItemType.Ghost:
                    return "GHOST";
                case ShopItemType.Timebomb:
                    return "TIMEBOMB";
                case ShopItemType.Protection:
                    return "PROTECTION";
                case ShopItemType.Controller:
                    return "CONTROLLER";
                case ShopItemType.SpeedUp:
                    return "SPEED-UP"; // prettier
                case ShopItemType.Exit:
                    return "EXIT";
                default:
                    return type.ToString().ToUpper();
            }
        }
    }
}
