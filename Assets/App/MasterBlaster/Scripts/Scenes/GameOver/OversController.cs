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
            "Optional. Avatar image to show the winner. Set per-player sprites in playerAvatars (index by session match winner ID - 1)."
        )]
        public Image avatarImage;

        [Tooltip(
            "Optional. One sprite per player (1-based player ID). Index 0 = player 1, etc. If empty or out of range, avatar is left unchanged."
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
                int index = Mathf.Clamp(winnerPlayerId - 1, 0, playerAvatars.Length - 1);
                if (playerAvatars[index] != null)
                    avatarImage.sprite = playerAvatars[index];
            }
        }
    }
}
