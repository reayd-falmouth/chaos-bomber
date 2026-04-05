using HybridGame.MasterBlaster.Scripts.Player;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Map;
using UnityEngine;

namespace HybridGame.MasterBlaster.Runtime.Scenes.Character
{
    /// <summary>PlayerPrefs keys and helpers for avatar select → arena (display name, starting perk).</summary>
    public static class AvatarSelectionPrefs
    {
        public const string PlayerDisplayNameKey = "PlayerDisplayName";

        /// <summary>Serialized <see cref="AvatarStartingPerk"/> int; default 0 = <see cref="AvatarStartingPerk.None"/>.</summary>
        public const string AvatarStartingPerkKey = "AvatarStartingPerk";

        /// <summary>0-based 30-sprite block in <see cref="PlayerSpriteSheet"/> for hybrid player 1 (menu → arena).</summary>
        public const string Player1SpriteBlockKey = "Player1SpriteBlock";

        /// <summary>
        /// Resolves the name shown when the match ends (player 1 uses saved display name when set).
        /// </summary>
        public static string ResolveMatchDisplayName(int playerId, string fallbackObjectName)
        {
            if (playerId == 1)
            {
                string s = PlayerPrefs.GetString(PlayerDisplayNameKey, "");
                if (!string.IsNullOrWhiteSpace(s))
                    return s.Trim();
            }

            return fallbackObjectName;
        }

        /// <summary>
        /// Maps a designer-facing perk to an arena item. Returns false for <see cref="AvatarStartingPerk.None"/>.
        /// </summary>
        public static bool TryMapPerkToItemType(AvatarStartingPerk perk, out ItemPickup.ItemType itemType)
        {
            switch (perk)
            {
                case AvatarStartingPerk.None:
                    itemType = default;
                    return false;
                case AvatarStartingPerk.ExtraBomb:
                    itemType = ItemPickup.ItemType.ExtraBomb;
                    return true;
                case AvatarStartingPerk.BlastRadius:
                    itemType = ItemPickup.ItemType.BlastRadius;
                    return true;
                case AvatarStartingPerk.Superman:
                    itemType = ItemPickup.ItemType.Superman;
                    return true;
                case AvatarStartingPerk.Protection:
                    itemType = ItemPickup.ItemType.Protection;
                    return true;
                case AvatarStartingPerk.Ghost:
                    itemType = ItemPickup.ItemType.Ghost;
                    return true;
                case AvatarStartingPerk.SpeedIncrease:
                    itemType = ItemPickup.ItemType.SpeedIncrease;
                    return true;
                case AvatarStartingPerk.TimeBomb:
                    itemType = ItemPickup.ItemType.TimeBomb;
                    return true;
                case AvatarStartingPerk.RemoteBomb:
                    itemType = ItemPickup.ItemType.RemoteBomb;
                    return true;
                default:
                    itemType = default;
                    return false;
            }
        }
    }

    /// <summary>Starting ability when this character is chosen at avatar select (applied once per new session, hybrid 3D).</summary>
    public enum AvatarStartingPerk
    {
        None = 0,
        ExtraBomb,
        BlastRadius,
        Superman,
        Protection,
        Ghost,
        SpeedIncrease,
        TimeBomb,
        RemoteBomb,
    }
}
