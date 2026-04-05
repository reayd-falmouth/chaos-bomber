using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Levels
{
    /// <summary>
    /// PlayerPrefs-backed unlock flags for level-select avatar portraits.
    /// Unlocks are per <see cref="LevelSelectionPrefs.SelectedArenaIndexKey"/> (0-based level row in level select):
    /// bit <c>i</c> means avatar id <c>i</c> won a match as player 1 on that arena while selected.
    /// </summary>
    public static class AvatarPortraitUnlockPersistence
    {
        /// <summary>Legacy global mask (pre per-arena); kept for tests and migration helpers.</summary>
        public const string MaskPrefsKey = "AvatarPortraitUnlockMask";

        /// <summary>Valid avatar ids are 0..MaxSupportedAvatarId (inclusive).</summary>
        public const int MaxSupportedAvatarId = 31;

        /// <summary>PlayerPrefs key for bitmask at a given arena index.</summary>
        public static string MaskKeyForArena(int arenaIndex)
        {
            int a = UnityEngine.Mathf.Max(0, arenaIndex);
            return $"AvatarPortraitUnlockArena:{a}";
        }

        public static bool IsUnlockedForArena(int arenaIndex, int avatarId)
        {
            if (avatarId < 0 || avatarId > MaxSupportedAvatarId)
                return false;
            int mask = PlayerPrefs.GetInt(MaskKeyForArena(arenaIndex), 0);
            return (mask & (1 << avatarId)) != 0;
        }

        public static void UnlockForArena(int arenaIndex, int avatarId)
        {
            if (avatarId < 0 || avatarId > MaxSupportedAvatarId)
                return;
            string key = MaskKeyForArena(arenaIndex);
            int mask = PlayerPrefs.GetInt(key, 0);
            int newMask = mask | (1 << avatarId);
            if (newMask == mask)
                return;
            PlayerPrefs.SetInt(key, newMask);
            PlayerPrefs.Save();
        }

        /// <summary>Legacy global bitmask (not used by level-select UI).</summary>
        public static bool IsUnlocked(int avatarId)
        {
            if (avatarId < 0 || avatarId > MaxSupportedAvatarId)
                return false;
            int mask = PlayerPrefs.GetInt(MaskPrefsKey, 0);
            return (mask & (1 << avatarId)) != 0;
        }

        /// <summary>Legacy global unlock (not used by level-select UI).</summary>
        public static void Unlock(int avatarId)
        {
            if (avatarId < 0 || avatarId > MaxSupportedAvatarId)
                return;
            int mask = PlayerPrefs.GetInt(MaskPrefsKey, 0);
            int newMask = mask | (1 << avatarId);
            if (newMask == mask)
                return;
            PlayerPrefs.SetInt(MaskPrefsKey, newMask);
            PlayerPrefs.Save();
        }
    }
}
