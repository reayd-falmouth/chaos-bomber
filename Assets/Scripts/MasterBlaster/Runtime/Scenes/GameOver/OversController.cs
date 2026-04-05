using HybridGame.MasterBlaster.Runtime.Scenes.Character;
using HybridGame.MasterBlaster.Scripts.Core;
using UnityEngine;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Scripts.Scenes.GameOver
{
    public class WinnerController : MonoBehaviour
    {
        [Header("Winner display")]
        public Text winnerText;

        [Tooltip(
            "Optional. Avatar image for the match winner. Sprites come from playerAvatars using the same rules as standings/wheel (P1 = menu avatar)."
        )]
        public Image avatarImage;

        [Tooltip(
            "Optional. One portrait per character slot, same order as avatar select. Player 1 uses the menu-selected avatar; other winners use slot order (index = playerId - 1). If empty or out of range, avatar is left unchanged."
        )]
        public Sprite[] playerAvatars;

        private void OnEnable()
        {
            string winnerName =
                SessionManager.Instance != null
                    ? SessionManager.Instance.GetMatchWinnerName()
                    : "Unknown";
            if (winnerText != null)
                winnerText.text = $"{winnerName} Wins the Match!".ToUpper();

            int winnerPlayerId =
                SessionManager.Instance != null
                    ? SessionManager.Instance.GetMatchWinnerPlayerId()
                    : 1;
            if (winnerPlayerId <= 0)
                winnerPlayerId = 1;
            if (avatarImage != null && playerAvatars != null && playerAvatars.Length > 0)
            {
                int spriteIdx = AvatarSelectionPrefs.GetPortraitSpriteIndexForPlayer(winnerPlayerId);
                spriteIdx = Mathf.Clamp(spriteIdx, 0, playerAvatars.Length - 1);
                if (playerAvatars[spriteIdx] != null)
                    avatarImage.sprite = playerAvatars[spriteIdx];
            }
        }
    }
}
