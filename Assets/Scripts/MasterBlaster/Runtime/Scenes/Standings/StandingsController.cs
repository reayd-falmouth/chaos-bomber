using System.IO;
using System.Text;
using HybridGame.MasterBlaster.Runtime.Scenes.Character;
using HybridGame.MasterBlaster.Scripts.Core;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Standings
{
    public class StandingsController : MonoBehaviour
    {
        [Header("UI References")]
        public Transform standingsPanel; // Vertical Layout
        public GameObject playerRowPrefab; // Prefab with Avatar + TrophyContainer
        public Sprite trophySprite;

        [Header("Avatars")]
        public Sprite[] avatarSprites; // 5 sprites, one per player

        [Header("Flow Settings")]
        [Tooltip("How long the standings screen stays active before advancing")]
        public float autoAdvanceDelay = 3f;

        [Header("Feedbacks")]
        [SerializeField] private MMF_Player openingFeedbacks;

        private void OnEnable()
        {
            openingFeedbacks?.PlayFeedbacks();

            // 2. Clear out any old rows
            foreach (Transform child in standingsPanel)
            {
                Destroy(child.gameObject);
            }

            // 3. Build rows for players
            int playerCount = PlayerPrefs.GetInt("Players", 2);

            for (int i = 1; i <= playerCount; i++)
            {
                // Spawn row
                GameObject row = Instantiate(playerRowPrefab, standingsPanel);

                // Avatar
                var avatarTr = row.transform.Find("Avatar");
                var avatar = avatarTr != null ? avatarTr.GetComponent<Image>() : null;
                int spriteIdx = AvatarSelectionPrefs.GetPortraitSpriteIndexForPlayer(i);
                // #region agent log
                try
                {
                    var sb = new StringBuilder(200);
                    sb.Append("{\"sessionId\":\"6c4413\",\"runId\":\"avatar-ui\",\"hypothesisId\":\"H1\",\"location\":\"StandingsController.OnEnable\",");
                    sb.Append("\"message\":\"row_avatar_sprite\",\"data\":{\"playerId\":").Append(i).Append(",\"spriteIdx\":").Append(spriteIdx);
                    sb.Append(",\"spritesLen\":").Append(avatarSprites != null ? avatarSprites.Length : -1).Append("},\"timestamp\":");
                    sb.Append(System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()).Append("}\n");
                    File.AppendAllText(Path.Combine(Application.dataPath, "..", "debug-6c4413.log"), sb.ToString());
                }
                catch { }
                // #endregion
                if (avatar != null && avatarSprites != null && spriteIdx >= 0 && spriteIdx < avatarSprites.Length)
                    avatar.sprite = avatarSprites[spriteIdx];

                // Trophies
                var trophyContainer = row.transform.Find("TrophyContainer");
                foreach (Transform child in trophyContainer)
                {
                    Destroy(child.gameObject);
                }

                int wins = SessionManager.Instance != null ? SessionManager.Instance.GetWins(i) : 0;
                UnityEngine.Debug.Log($"Player {i} wins = {wins}");

                for (int t = 0; t < wins; t++)
                {
                    var trophyGO = new GameObject("Trophy", typeof(Image));
                    trophyGO.transform.SetParent(trophyContainer, false);
                    trophyGO.layer = trophyContainer.gameObject.layer;

                    var rt = trophyGO.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(32, 32);

                    trophyGO.GetComponent<Image>().sprite = trophySprite;
                }
            }

            // 4. Auto-advance after delay
            Invoke(nameof(Advance), autoAdvanceDelay);
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(Advance));
        }

        private void Advance()
        {
            SceneFlowManager.I?.SignalScreenDone();
        }
    }
}
