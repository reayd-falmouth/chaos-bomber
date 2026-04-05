using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Levels
{
    /// <summary>
    /// PlayerPrefs-backed unlock flags for level-select avatar portraits.
    /// Uses a single int bitmask: bit <c>i</c> means avatar id <c>i</c> has won at least one arena round
    /// as player 1 while that avatar was selected (snapshot at arena load in GameManager).
    /// </summary>
    public static class AvatarPortraitUnlockPersistence
    {
        public const string MaskPrefsKey = "AvatarPortraitUnlockMask";

        /// <summary>Valid avatar ids are 0..MaxSupportedAvatarId (inclusive).</summary>
        public const int MaxSupportedAvatarId = 31;

        public static bool IsUnlocked(int avatarId)
        {
            if (avatarId < 0 || avatarId > MaxSupportedAvatarId)
                return false;
            int mask = PlayerPrefs.GetInt(MaskPrefsKey, 0);
            return (mask & (1 << avatarId)) != 0;
        }

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
