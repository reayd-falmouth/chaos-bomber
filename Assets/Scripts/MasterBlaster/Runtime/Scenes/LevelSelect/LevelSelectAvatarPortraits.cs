using HybridGame.MasterBlaster.Scripts.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Runtime.Scenes.LevelSelect
{
    /// <summary>
    /// Tints level-select avatar portrait <see cref="Image"/>s from per-arena unlock data in
    /// <see cref="AvatarPortraitUnlockPersistence"/> (must match the highlighted level row index).
    /// </summary>
    public class LevelSelectAvatarPortraits : MonoBehaviour
    {
        [SerializeField]
        Image[] portraits;

        [Tooltip("Per-slot avatar ids for PlayerPrefs bitmask; if shorter than portraits, remaining slots use 0,1,2,…")]
        [SerializeField]
        int[] avatarIds;

        [SerializeField]
        Color unlockedColor = Color.white;

        [SerializeField]
        Color lockedColor = Color.black;

        /// <param name="arenaLevelIndex">0-based index of the level row in level select (same as <see cref="LevelSelectionPrefs.SelectedArenaIndexKey"/> when starting a match).</param>
        public void Apply(int arenaLevelIndex)
        {
            if (portraits == null)
                return;

            for (int i = 0; i < portraits.Length; i++)
            {
                var img = portraits[i];
                if (img == null)
                    continue;

                int id = i;
                if (avatarIds != null && i < avatarIds.Length)
                    id = avatarIds[i];

                img.color = AvatarPortraitUnlockPersistence.IsUnlockedForArena(arenaLevelIndex, id)
                    ? unlockedColor
                    : lockedColor;
            }
        }
    }
}
