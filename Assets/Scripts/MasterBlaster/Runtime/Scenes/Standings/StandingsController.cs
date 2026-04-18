using System.Collections;
using System.IO;
using System.Text;
using HybridGame.MasterBlaster.Runtime.Scenes.Character;
using HybridGame.MasterBlaster.Scripts.Core;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Standings
{
    /// <summary>
    /// Post-match standings: waits <see cref="autoAdvanceDelay"/> then calls <see cref="SceneFlowManager.SignalScreenDone"/>.
    /// Does not read mobile D-pad or bomb; flow continues automatically. For player-driven shopping, see
    /// <see cref="HybridGame.MasterBlaster.Scripts.Scenes.Shop.ShopController"/> after the flow reaches the Shop state.
    /// </summary>
    public class StandingsController : MonoBehaviour
    {
        [Header("UI References")]
        public Transform standingsPanel; // Parent uses GridLayoutGroup (see scene prefab)
        public GameObject playerRowPrefab; // Prefab with Avatar + TrophyContainer
        public Sprite trophySprite;

        [Header("Avatars")]
        public Sprite[] avatarSprites; // 5 sprites, one per player

        [Header("Flow Settings")]
        [Tooltip("How long the standings screen stays active before advancing")]
        public float autoAdvanceDelay = 3f;

        [Header("Feedbacks")]
        [SerializeField] private MMF_Player openingFeedbacks;

        private Coroutine _populateRoutine;

        private void OnEnable()
        {
            openingFeedbacks?.PlayFeedbacks();

            if (_populateRoutine != null)
                StopCoroutine(_populateRoutine);
            _populateRoutine = StartCoroutine(PopulateStandingsRoutine());
        }

        private IEnumerator PopulateStandingsRoutine()
        {
            foreach (Transform child in standingsPanel)
                Destroy(child.gameObject);

            // Destroy() is deferred; wait one frame so the grid is empty before we add new rows.
            yield return null;

            int playerCount = PlayerPrefs.GetInt("Players", 2);

            for (int i = 1; i <= playerCount; i++)
            {
                GameObject row = Instantiate(playerRowPrefab, standingsPanel);

                var avatarTr = row.transform.Find("Avatar");
                var avatar = avatarTr != null ? avatarTr.GetComponent<Image>() : null;
                int spriteIdx = AvatarSelectionPrefs.GetPortraitSpriteIndexForPlayer(i);
                // #region agent log
                try
                {
                    var sb = new StringBuilder(200);
                    sb.Append("{\"sessionId\":\"6c4413\",\"runId\":\"avatar-ui\",\"hypothesisId\":\"H1\",\"location\":\"StandingsController.PopulateStandingsRoutine\",");
                    sb.Append("\"message\":\"row_avatar_sprite\",\"data\":{\"playerId\":").Append(i).Append(",\"spriteIdx\":").Append(spriteIdx);
                    sb.Append(",\"spritesLen\":").Append(avatarSprites != null ? avatarSprites.Length : -1).Append("},\"timestamp\":");
                    sb.Append(System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()).Append("}\n");
                    File.AppendAllText(Path.Combine(Application.dataPath, "..", "debug-6c4413.log"), sb.ToString());
                }
                catch { }
                // #endregion
                if (avatar != null && avatarSprites != null && spriteIdx >= 0 && spriteIdx < avatarSprites.Length)
                    avatar.sprite = avatarSprites[spriteIdx];

                var trophyContainer = row.transform.Find("TrophyContainer");
                int wins = SessionManager.Instance != null ? SessionManager.Instance.GetWins(i) : 0;
                UnityEngine.Debug.Log($"Player {i} wins = {wins}");

                if (trophyContainer != null)
                {
                    var hlg = trophyContainer.GetComponent<HorizontalLayoutGroup>();
                    if (hlg != null)
                        hlg.enabled = false;

                    for (int c = trophyContainer.childCount - 1; c >= 0; c--)
                        DestroyImmediate(trophyContainer.GetChild(c).gameObject);

                    for (int t = 0; t < wins; t++)
                    {
                        var trophyGO = new GameObject("Trophy", typeof(Image));
                        trophyGO.transform.SetParent(trophyContainer, false);
                        trophyGO.layer = trophyContainer.gameObject.layer;

                        var rt = trophyGO.GetComponent<RectTransform>();
                        rt.sizeDelta = new Vector2(32, 32);

                        trophyGO.GetComponent<Image>().sprite = trophySprite;
                    }

                    if (hlg != null)
                        hlg.enabled = true;

                    var trophyRt = trophyContainer.GetComponent<RectTransform>();
                    if (trophyRt != null)
                        LayoutRebuilder.ForceRebuildLayoutImmediate(trophyRt);
                }
            }

            _populateRoutine = null;
            Invoke(nameof(Advance), autoAdvanceDelay);
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(Advance));
            if (_populateRoutine != null)
            {
                StopCoroutine(_populateRoutine);
                _populateRoutine = null;
            }
        }

        private void Advance()
        {
            SceneFlowManager.I?.SignalScreenDone();
        }
    }
}
