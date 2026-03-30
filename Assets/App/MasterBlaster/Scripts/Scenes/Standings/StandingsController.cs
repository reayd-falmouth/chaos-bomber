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
                var avatar = row.transform.Find("Avatar").GetComponent<Image>();
                if (avatarSprites != null && avatarSprites.Length >= i)
                {
                    avatar.sprite = avatarSprites[i - 1];
                }

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
            SceneFlowManager.I.SignalScreenDone();
        }
    }
}
